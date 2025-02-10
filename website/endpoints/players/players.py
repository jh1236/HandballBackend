from collections import defaultdict

from flask import request

from database.models import PlayerGameStats, Teams, Tournaments, People
from database.models.People import beatify_player_stats
from utils.permissions import fetch_user


def add_get_player_endpoints(app):
    @app.get("/api/players")
    def get_players():
        """
        SCHEMA:
        {
            tournament: <str> (OPTIONAL) = the searchable name of the tournament the games are from
            team: <str> (OPTIONAL) = the searchable name of the team the player played with
            includeStats: <bool> (OPTIONAL) = whether stats should be included
        }
        """
        tournament_searchable = request.args.get("tournament", None)
        team = request.args.get("team", None)
        make_nice = request.args.get('formatData', False, type=bool)
        return_tournament = request.args.get('returnTournament', False, type=bool)
        include_stats = request.args.get("includeStats", False, type=bool)
        user = fetch_user()
        admin = user and user.is_admin
        q = PlayerGameStats.query
        tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
        if tournament_searchable:
            tid = tournament.id
            q = q.filter(PlayerGameStats.tournament_id == tid)
        if team:
            tid = Teams.query.filter(Teams.searchable_name == team).first().id
            q = q.filter(PlayerGameStats.team_id == tid)
        players = q.group_by(PlayerGameStats.player_id).all()
        out = {"players": [i.player.as_dict(include_stats=include_stats, make_nice=make_nice,
                                            tournament=tournament.id if tournament else None, admin_view=admin) for
                           i in
                           players]}
        if return_tournament and tournament_searchable:
            out["tournament"] = tournament.as_dict()
        return out

    @app.get("/api/players/<searchable>")
    def get_player(searchable):
        """
        SCHEMA:
        {
            tournament: <str> (OPTIONAL) = the searchable name of the tournament to pull statistics from
            game: <int> (OPTIONAL) = the game to get the stats for this player
        }
        """
        make_nice = request.args.get('formatData', False, type=bool)
        return_tournament = request.args.get('returnTournament', False, type=bool)
        include_court_stats = request.args.get('includeCourtStats', False, type=bool)
        game = request.args.get("game", None, type=int)
        user = fetch_user()
        admin = user and user.is_admin
        if game:
            return PlayerGameStats.query.join(People, PlayerGameStats.player_id == People.id).filter(
                PlayerGameStats.game_id == game, People.searchable_name == searchable).first().as_dict()
        tournament_searchable = request.args.get("tournament", None)
        tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
        tid = tournament.id if tournament else None
        out = {"player": People.query.filter(People.searchable_name == searchable).first().as_dict(include_stats=True,
                                                                                                   tournament=tid,
                                                                                                   make_nice=make_nice,
                                                                                                   admin_view=admin,
                                                                                                   include_court_stats=include_court_stats)}
        if return_tournament and tournament_searchable:
            out["tournament"] = tournament.as_dict()
        return out

    @app.get("/api/players/stats")
    def get_average_stats():
        """
        SCHEMA:
        {
            tournament: <str> (OPTIONAL) = the searchable name of the tournament to pull statistics from
            game: <int> (OPTIONAL) = the game to get the stats for this player
        }
        """
        make_nice = request.args.get('formatData', False, type=bool)
        return_tournament = request.args.get('returnTournament', False, type=bool)
        game = request.args.get("game", None, type=int)
        user = fetch_user()
        admin = user and user.is_admin
        tournament_searchable = request.args.get("tournament", None)
        tournament = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable).first()
        include_unranked = False
        if tournament:
            include_unranked = not tournament.ranked
        if game:
            all_stats = [i.as_dict()["stats"] for i in
                         PlayerGameStats.query.join(People, PlayerGameStats.player_id == People.id).filter(
                             PlayerGameStats.game_id == game).all()]
        else:
            game_filter = (lambda a: a.filter(PlayerGameStats.tournament_id == tournament.id)) if tournament else None
            q = People.query
            if tournament:
                q = q.join(PlayerGameStats, PlayerGameStats.player_id == People.id).filter(
                    PlayerGameStats.tournament_id == tournament.id).group_by(People.id)
            all_stats = [
                i.stats(admin=admin, games_filter=game_filter, include_unranked=include_unranked, make_nice=False) for i
                in
                q.all()]
        out = defaultdict(lambda: 0)
        for d in all_stats:
            for stat, value in d.items():
                if isinstance(value, str):
                    out[stat] = None
                else:
                    out[stat] += value
        for stat in out:
            if not out[stat]: continue
            out[stat] /= len(all_stats)
        if make_nice:
            out = beatify_player_stats(out)
        out = {"stats": out}
        if return_tournament and tournament_searchable:
            out["tournament"] = tournament.as_dict()
        return out
