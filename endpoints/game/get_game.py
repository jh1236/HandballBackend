from collections import defaultdict

from flask import request, jsonify

from database.models import Games, Tournaments, Teams, PlayerGameStats, People, Officials
from endpoints.game import games
from structure import manage_game
from utils.permissions import fetch_user, umpire_manager_only
from utils.util import fixture_sorter


@games.get("/change_code")
def change_code():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    game_id = int(request.args["id"])
    return jsonify({"code": manage_game.change_code(Games.game_number_to_id(game_id))})


@games.get("/next")
def next_game():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    game_id = int(request.args["id"])
    old_game = Games.query.filter(Games.game_number == game_id).first()
    new_game = Games.query.filter(Games.game_number > game_id, Games.court == old_game.court,
                                  Games.tournament_id == old_game.tournament_id).first()
    return jsonify({"id": new_game.game_number if new_game else -1})


@games.route('/<int:id>', methods=['GET'])
def get_game(id):
    """
    SCHEMA :
    {
        id: <int> = the id of the game
        includeGameEvents: <bool> (OPTIONAL) = whether gameEvents should be included
        includePlayerStats: <bool> (OPTIONAL) = whether Player Stats should be included
    }
    """
    user = fetch_user()
    is_admin = user and user.is_umpire_manager
    is_official = user and user.is_official
    game = Games.query.filter(Games.game_number == id).first()
    include_game_events = request.args.get('includeGameEvents', None, type=bool)
    include_prev_cards = request.args.get('includePreviousCards', False, type=bool)
    format_data = request.args.get('formatData', False, type=bool)
    if include_prev_cards and not is_official:
        return 'Previous cards are only accesible by officials', 403
    include_stats = request.args.get('includeStats', False, type=bool)
    out = {
        "game": game.as_dict(include_game_events=include_game_events, include_stats=include_stats,
                             admin_view=is_admin, official_view=is_official, make_nice=format_data),
    }
    return out


@games.get('/noteable')
@umpire_manager_only
def get_noteable_games():
    tournament_searchable = request.args.get('tournament', None, type=str)
    tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
    include_game_events = request.args.get('includeGameEvents', False, type=bool)
    limit = request.args.get('limit', -1, type=int)
    include_player_stats = request.args.get('includePlayerStats', False, type=bool)
    return_tournament = request.args.get('returnTournament', False, type=bool)
    user = fetch_user()
    is_admin = user and user.is_umpire_manager
    query = Games.query
    if tournament:
        query = query.filter(Games.tournament_id == tournament.id)
    query = query.filter(Games.is_noteable)
    if limit > 0:
        query.limit(limit)
    query.order_by(Games.game_number)

    out = {"games": [i.as_dict(include_game_events=include_game_events, include_stats=include_player_stats,
                               admin_view=is_admin) for i in query.all()]}
    if return_tournament and tournament_searchable:
        out["tournament"] = tournament.as_dict()
    return out


@games.route('/', methods=['GET'])
def get_games():
    """
    SCHEMA :
    {
        tournament: <str> (OPTIONAL) = the searchable name of the tournament the games are from
        team: List<str> (OPTIONAL) = the searchable name of the team who played in the game
        player: List<str> (OPTIONAL) = the searchable name of the player who played in the game
        official: List<str> (OPTIONAL) = the searchable name of the officials who officiated in the game
        court: <str> (OPTIONAL) = the court the game was on
        includeGameEvents: <bool> (OPTIONAL) = whether Game Events should be included
        includePlayerStats: <bool> (OPTIONAL) = whether Player Stats should be included
        limit: int (OPTIONAL) = the limit of the games which should be returned
    }
    """
    user = fetch_user()
    is_admin = user and user.is_umpire_manager
    tournament_searchable = request.args.get('tournament', None, type=str)
    include_byes = request.args.get('includeByes', False, type=bool)
    limit = request.args.get('limit', -1, type=int)
    team_searchable = request.args.getlist('team', type=str)
    player_searchable = request.args.getlist('player', type=str)
    official_searchable = request.args.getlist('official', type=str)
    court = request.args.get('court', None, type=int)
    include_game_events = request.args.get('includeGameEvents', False, type=bool)
    include_player_stats = request.args.get('includePlayerStats', False, type=bool)
    return_tournament = request.args.get('returnTournament', False, type=bool)
    games = Games.query
    tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
    if not include_byes:
        games = games.filter(Games.is_bye == False)  # '== False' as sqlalchemy overrides the __eq__ operator
    if tournament_searchable:
        tid = tournament.id
        games = games.filter(Games.tournament_id == tid)
    for i in official_searchable:
        off_id = Officials.query.join(People, Officials.person_id == People.id).filter(
            People.searchable_name == i).first().id
        games = games.filter((Games.official_id == off_id) | (Games.scorer_id == off_id))
    for i in team_searchable:
        team_id = Teams.query.filter(Teams.searchable_name == i).first().id
        games = games.filter((Games.team_one_id == team_id) | (Games.team_two_id == team_id))
    if court:
        games = games.filter(Games.court == court - 1)
    for i in player_searchable:
        pid = People.query.filter(People.searchable_name == i).first().id
        # this repeated join is a bit cursed, but oh well!
        games = games.join(PlayerGameStats, PlayerGameStats.game_id == Games.id).filter(
            PlayerGameStats.player_id == pid)
    games = games.order_by((Games.start_time.desc()), Games.id.desc())
    if limit > 0:
        games = games.limit(limit)
    out = {"games": [i.as_dict(include_game_events=include_game_events, include_stats=include_player_stats,
                               admin_view=is_admin) for i in games.all()]}
    if return_tournament and tournament_searchable:
        out["tournament"] = tournament.as_dict()
    return out


@games.route('/fixtures')
def get_fixtures():
    """
    SCHEMA :
    {
        tournament: <str> = the searchable name of the tournament
    }
    """
    user = fetch_user()
    is_admin = user and user.is_umpire_manager
    tournament_searchable = request.args.get('tournament', type=str)
    separate_finals = request.args.get('separateFinals', type=bool)
    tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
    max_rounds = request.args.get('maxRounds', -1, type=int)

    tid = tournament.id
    return_tournament = request.args.get('returnTournament', False, type=bool)
    if max_rounds > 0:
        last_round = Games.query.filter(Games.tournament_id == tid).order_by(
            Games.round.desc()).first().round - max_rounds
    else:
        last_round = 0
    fixtures = defaultdict(list)
    games = Games.query.filter(Games.tournament_id == tid, Games.round > last_round).all()
    for game in games:
        fixtures[game.round].append(game)
    new_fixtures = []
    for k, v in fixtures.items():
        new_fixtures.append(
            {"games": [j.as_dict(admin_view=is_admin) for j in fixture_sorter(v)], "final": v[0].is_final})
    fixtures = new_fixtures
    if separate_finals:
        finals = [i for i in fixtures if i["final"]]
        fixtures = [i for i in fixtures if not i["final"]]
        out = {"fixtures": fixtures, "finals": finals}
    else:
        out = {"fixtures": fixtures}
    if return_tournament and tournament_searchable:
        out["tournament"] = tournament.as_dict()
    return out
