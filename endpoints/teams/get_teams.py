import os

from flask import request, send_file

from database.models import Teams, Tournaments, People, TournamentTeams
from endpoints.teams.blueprint import teams
from utils.permissions import fetch_user


@teams.route('', methods=['GET'])
def get_teams():
    """
    SCHEMA:
    {
        tournament: <str> (OPTIONAL) = the searchable name of the tournament the games are from
        player: List<str> (OPTIONAL) = the searchable name of the player who played in the game
        includeStats: <bool> (OPTIONAL) = whether stats should be included
        includePlayerStats: <bool> (OPTIONAL) = whether stats should be included
    }
    """
    make_nice = request.args.get('formatData', False, type=bool)

    tournament_searchable = request.args.get('tournament', None, type=str)
    player_searchable = request.args.getlist('player', type=str)
    include_stats = request.args.get('includeStats', False, type=bool)
    include_player_stats = request.args.get('includePlayerStats', None, type=bool)
    return_tournament = request.args.get('returnTournament', False, type=bool)
    q = Teams.query.filter(Teams.id != 1)
    tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
    tournament_id = None if not tournament else tournament.id
    user = fetch_user()
    admin = user and user.is_umpire_manager
    if tournament_searchable:
        q = q.join(TournamentTeams, TournamentTeams.team_id == Teams.id).filter(
            TournamentTeams.tournament_id == tournament_id)
    for i in player_searchable:
        pid = People.query.filter(People.searchable_name == i).first().id
        q = q.filter((Teams.captain_id == pid) | (Teams.non_captain_id == pid) | (Teams.substitute_id == pid))
    q = q.order_by(Teams.image_url.like('https://api.squarers.club/teams/image?name=%').desc(),
                   Teams.name.like('(Solo) %'), Teams.searchable_name)
    out = {"teams":
               [i.as_dict(include_stats=include_stats, include_player_stats=include_player_stats,
                          make_nice=make_nice, tournament=tournament_id, admin_view=admin) for
                i in q.all()]}
    if return_tournament and tournament_searchable:
        out["tournament"] = tournament.as_dict()
    return out


@teams.route('/<searchable>', methods=['GET'])
def get_team(searchable):
    """
    SCHEMA:
    {
        tournament: <str> (OPTIONAL) = the searchable name of the tournament to pull statistics from
    }
    """
    user = fetch_user()
    admin = user and user.is_umpire_manager
    tournament_searchable = request.args.get("tournament", None)
    make_nice = request.args.get('formatData', False, type=bool)
    return_tournament = request.args.get('returnTournament', False, type=bool)
    tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
    tournament_id = tournament.id if tournament else None

    out = {"team": Teams.query.filter(Teams.searchable_name == searchable).first().as_dict(include_stats=True,
                                                                                           tournament=tournament_id,
                                                                                           make_nice=make_nice,
                                                                                           admin_view=admin,
                                                                                           single=True)}
    if return_tournament and tournament_searchable:
        out["tournament"] = tournament.as_dict()
    return out


@teams.get('/ladder/')
def get_ladder():
    """
    SCHEMA:
    {
        tournament: <str> (OPTIONAL) = the searchable name of the tournament the games are from
        includeStats: <bool> (OPTIONAL) = whether stats should be included
    }
    """
    user = fetch_user()
    admin = user and user.is_umpire_manager
    tournament_searchable = request.args.get('tournament', None, type=str)
    include_stats = request.args.get('includeStats', False, type=bool)
    make_nice = request.args.get('formatData', False, type=bool)
    tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
    return_tournament = request.args.get('returnTournament', False, type=bool)
    tournament_id = tournament.id if tournament else None
    if tournament:
        ladder: list[tuple["Teams", dict[str, float]]] | list[
            list[tuple["Teams", dict[str, float]]]] = tournament.ladder()
        pooled = tournament.is_pooled
    else:
        ladder: list[tuple["Teams", dict[str, float]]] = Tournaments.all_time_ladder()
        pooled = False

    out = {}
    if pooled:
        out["poolOne"] = [
            i[0].as_dict(include_stats=include_stats, include_player_stats=False, make_nice=make_nice,
                         tournament=tournament_id, admin_view=admin) for i in
            ladder[0]]
        out["poolTwo"] = [
            i[0].as_dict(include_stats=include_stats, include_player_stats=False, make_nice=make_nice,
                         tournament=tournament_id, admin_view=admin) for i in
            ladder[1]]
    else:
        out["ladder"] = [i[0].as_dict(include_stats=include_stats, include_player_stats=False, make_nice=make_nice,
                                      tournament=tournament_id, admin_view=admin)
                         for i in ladder]
    out["pooled"] = pooled
    if return_tournament and tournament_searchable:
        out["tournament"] = tournament.as_dict()
    return out


@teams.get("/image")
def team_image():
    team = request.args.get("name", type=str)
    big = request.args.get("big", type=bool)
    path = f"./resources/images/{'big/' if big else ''}teams/{team}.png"
    if os.path.isfile(path):
        return send_file(
            f"{path}", mimetype="image/png"
        )
    else:
        return send_file(
            f"./resources/images/{'big/' if big else ''}teams/blank.png", mimetype="image/png"
        )
