"""Defines the comments object and provides functions to get and manipulate one"""
import time
from collections import defaultdict

from sqlalchemy.ext.hybrid import hybrid_property

from database import db

# create table main.people
# (
#     id             INTEGER
# primary key autoincrement,
# name           TEXT,
# searchableName TEXT,
# password       TEXT,
# imageURL       TEXT,
# sessionToken   TEXT,
# tokenTimeout   INTEGER
# );
#

PERCENTAGES = [
    "Percentage",
    "Serve Ace Rate",
    "Serve Fault Rate",
    "Percentage of Points Scored",
    "Percentage of Points Scored For Team",
    "Percentage of Games Starting Left",
    "Percentage of Points Served Won",
    "Serve Return Rate"
]


def beatify_player_stats(d):
    out = {}
    for k, v in d.items():
        if v is None:
            out[k] = "-"
        elif k in PERCENTAGES:
            out[k] = f"{100.0 * v: .2f}%".strip()
        elif isinstance(v, float):
            out[k] = round(v, 2)
        else:
            out[k] = v
    return out


class People(db.Model):
    __tablename__ = "people"

    # Auto-initialised fields
    id = db.Column(db.Integer(), primary_key=True)
    created_at = db.Column(db.Integer(), default=time.time, nullable=False)

    # Set fields
    name = db.Column(db.Text(), nullable=False)
    searchable_name = db.Column(db.Text(), nullable=False)
    password = db.Column(db.Text())
    image_url = db.Column(db.Text())
    session_token = db.Column(db.Text())
    token_timeout = db.Column(db.Integer())
    permission_level = db.Column(db.Integer(), nullable=False, default=False)

    def image(self, tournament=None, big=False, default="/api/image?name=umpire"):
        from start import MY_ADDRESS
        from database.models import Teams
        from database.models import TournamentTeams
        out = None
        if self.image_url:
            out = self.image_url
        if tournament:
            if not isinstance(tournament, int):
                tournament = tournament.id
            t = Teams.query.join(TournamentTeams, TournamentTeams.team_id == Teams.id).filter(
                (Teams.captain_id == self.id) | (Teams.non_captain_id == self.id) | (
                        Teams.substitute_id == self.id), TournamentTeams.tournament_id == tournament).order_by(
                Teams.image_url.like('/api/teams/image?%').desc(),
                Teams.id).first()
        else:
            t = Teams.query.filter((Teams.captain_id == self.id) | (Teams.non_captain_id == self.id) | (
                    Teams.substitute_id == self.id)).order_by(Teams.image_url.like('/api/teams/image?%').desc(),
                                                              Teams.id).first()
        if out is None:
            out = t.image(tournament, big) if t else default
        return MY_ADDRESS + out if out.startswith('/') else out

    def elo(self, last_game=None):
        from database.models import EloChange
        elo_deltas = EloChange.query.filter(self.id == EloChange.player_id)
        if last_game:
            elo_deltas = elo_deltas.filter(EloChange.game_id <= last_game)
        return 1500.0 + sum(i.elo_delta for i in elo_deltas)

    def simple_stats(self, games_filter=None, make_nice=True, include_unranked=False, include_solo=False) -> dict[
        str, str | float]:
        from database.models import PlayerGameStats, Games
        from database.models import Teams
        q = db.session.query(Games, PlayerGameStats, Teams).filter(
            PlayerGameStats.game_id == Games.id,
            PlayerGameStats.player_id == self.id,
            Teams.id == PlayerGameStats.team_id)
        q = q.filter(Games.is_bye == False, Games.is_final == False)
        # '== False' is not an error here, as Model overrides __eq__, so using a not operator provides a different result
        if games_filter:
            q = games_filter(q)
        if not include_unranked:
            if not include_solo:
                q = q.filter(Games.ranked)
            else:
                q = q.filter(Games.ranked | (Teams.non_captain_id == None))

        q = q.all()
        games = [i[0] for i in q]
        players = [i[1] for i in q]
        games_played = len(
            [i for i in games if i.started]) or 1  # used as a divisor to save me thinking about div by zero
        ret = {
            "B&F Votes": len([i for i in games if i.best_player_id == self.id]),
            "Elo": self.elo(max([i.id for i in games] + [0])),
            "Games Won": len([g for g, p in zip(games, players) if g.winning_team_id == p.team_id and g.ended]),
            "Games Lost": len([g for g, p in zip(games, players) if g.winning_team_id != p.team_id and g.ended]),
            "Games Played": len([i for i in games if i.started]),
            "Percentage": len([g for g, p in zip(games, players) if g.winning_team_id == p.team_id]) / games_played,
            "Points Scored": sum(i.points_scored for i in players),
            "Points Served": sum(i.served_points for i in players),
            "Aces Scored": sum(i.aces_scored for i in players),
            "Faults": sum(i.faults for i in players),
            "Double Faults": sum(i.double_faults for i in players),
            "Green Cards": sum(i.green_cards for i in players),
            "Yellow Cards": sum(i.yellow_cards for i in players),
            "Red Cards": sum(i.red_cards for i in players)
        }
        for k, v in ret.items():
            if k in PERCENTAGES:
                ret[k] = f"{100.0 * v: .2f}%"
            elif isinstance(v, float):
                ret[k] = round(v, 2)
        return ret

    def stats(self, games_filter=None, make_nice=True, include_unranked=False, include_solo=False, admin=False,
              include_court_stats=False) -> dict[
        str, str | float]:
        from database.models import PlayerGameStats, Games
        from database.models import EloChange
        from database.models import Teams
        q = db.session.query(Games, PlayerGameStats, EloChange, Teams).outerjoin(
            EloChange, (EloChange.game_id == Games.id) & (EloChange.player_id == self.id)
        ).filter(
            PlayerGameStats.game_id == Games.id,
            PlayerGameStats.player_id == self.id,
            Teams.id == PlayerGameStats.team_id)
        q = q.filter(Games.is_bye == False, Games.is_final == False)
        # '== False' is not an error here, as Model overrides __eq__, so using a not operator provides a different result
        if games_filter:
            q = games_filter(q)
        if not include_unranked:
            if not include_solo:
                q = q.filter(Games.ranked)
            else:
                q = q.filter(Games.ranked | (Teams.non_captain_id == None))

        q = q.all()
        games = [i[0] for i in q]
        players = [i[1] for i in q]
        elo_delta = [i[2] for i in q]
        games_played = len(games) or 1  # used as a divisor to save me thinking about div by zero
        games_lost = len([g for g, p in zip(games, players) if g.winning_team_id != p.team_id and g.ended])
        ret = {
            "B&F Votes": len([i for i in games if i.best_player_id == self.id]),
            "Elo": self.elo(max([i.id for i in games] + [0])),
            "Games Won": len([g for g, p in zip(games, players) if g.winning_team_id == p.team_id]),
            "Games Lost": games_lost,
            "Games Played": len([i for i in games if i.started]),
            "Percentage": len([g for g, p in zip(games, players) if g.winning_team_id == p.team_id]) / games_played,
            "Points Scored": sum(i.points_scored for i in players),
            "Points Served": sum(i.served_points for i in players),
            "Aces Scored": sum(i.aces_scored for i in players),
            "Faults": sum(i.faults for i in players),
            "Double Faults": sum(i.double_faults for i in players),
            "Green Cards": sum(i.green_cards for i in players),
            "Yellow Cards": sum(i.yellow_cards for i in players),
            "Red Cards": sum(i.red_cards for i in players),
            "Rounds on Court": sum(i.rounds_on_court for i in players if i),
            "Rounds Carded": sum(i.rounds_carded for i in players),
            "Net Elo Delta": sum(i.elo_delta for i in elo_delta if i),
            "Average Elo Delta": sum(i.elo_delta for i in elo_delta if i) / games_played,
            "Points per Game": sum(i.points_scored for i in players) / games_played,
            "Points per Loss": sum(i.points_scored for i in players) / (games_lost or 1),
            # make it fuckin huge if they've never lost
            "Aces per Game": sum(i.aces_scored for i in players) / games_played,
            "Faults per Game": sum(i.faults for i in players) / games_played,
            "Cards": sum(i.green_cards + i.yellow_cards + i.red_cards for i in players),
            "Cards per Game": sum(i.green_cards + i.yellow_cards + i.red_cards for i in players) / games_played,
            "Points per Card": sum(i.points_scored for i in players) / (
                    sum(i.green_cards + i.yellow_cards + i.red_cards for i in players) or 1),
            "Serves per Game": sum(i.served_points for i in players) / games_played,
            "Serves per Ace": sum(i.served_points for i in players) / (sum(i.aces_scored for i in players) or 1),
            "Serves per Fault": sum(i.served_points for i in players) / (sum(i.faults for i in players) or 1),
            "Serve Ace Rate": sum(i.aces_scored for i in players) / (
                    sum(i.served_points for i in players) or 1),
            "Serve Fault Rate": sum(i.faults for i in players) / (
                    sum(i.served_points for i in players) or 1),
            "Percentage of Points Scored": sum(i.points_scored for i in players) / (
                    sum(i.rounds_on_court + i.rounds_carded for i in players) or 1),
            "Percentage of Points Scored For Team": sum(i.points_scored for i in players) / (sum(
                g.team_one_score if g.team_one_id == p.team_id else g.team_two_score for g, p in
                zip(games, players)) or 1),
            "Percentage of Games Starting Left": len([i for i in players if i.start_side == 'Left']) / games_played,
            "Percentage of Points Served Won": sum(i.served_points_won for i in players) / (
                    sum(i.served_points for i in players) or 1),
            "Serves Received": sum(i.serves_received for i in players),
            "Serves Returned": sum(i.serves_returned for i in players),
            "Max Serve Streak": max([i.serve_streak for i in players] + [0]),
            "Average Serve Streak": sum(i.serve_streak for i in players) / games_played,
            "Max Ace Streak": max([i.ace_streak for i in players] + [0]),
            "Average Ace Streak": sum(i.ace_streak for i in players) / games_played,
            "Serve Return Rate": sum(i.serves_returned for i in players) / (
                    sum(i.serves_received for i in players) or 1),
            "Votes per 100 games": 100 * len([i for i in games if i.best_player_id == self.id]) / games_played,
        }
        if admin:
            from database.models import GameEvents
            ret["Penalty Points"] = ret["Green Cards"] * 2 + ret["Yellow Cards"] * 5 + ret["Red Cards"] * 10
            ret["Warnings"] = sum(i.warnings for i in players)
            rated_games = [i.rating for i in players if i.rating]
            ret["Average Rating"] = sum(rated_games) / len(rated_games) if rated_games else 3

        if make_nice:
            ret = beatify_player_stats(ret)
        if include_court_stats:
            if not games_filter:
                games_filter = lambda a: a
            courts = [self.stats(games_filter=lambda a: games_filter(a).filter(Games.court == i)) for i in range(2)]
            for i, c in enumerate(courts):
                ret[f"Court {i + 1}"] = c
        return ret

    def get_admin_games(self, tournament=None, include_stats=False):
        from database.models import GameEvents
        from database.models import PlayerGameStats
        notes_events = GameEvents.query.join(
            PlayerGameStats,
            (PlayerGameStats.game_id == GameEvents.game_id) &
            (PlayerGameStats.team_id == GameEvents.team_id)
        ).filter(
            (GameEvents.tournament_id == tournament) | (tournament is None),
            PlayerGameStats.player_id == self.id,
            GameEvents.event_type == 'Notes'
        )
        card_event_types = GameEvents.query.filter(
            (GameEvents.tournament_id == tournament) | (tournament is None),
            GameEvents.player_id == self.id,
            (GameEvents.event_type == 'Warning') | (GameEvents.event_type.like('% Card'))
        )
        cards = defaultdict(list)
        for i in card_event_types:
            cards[i.game_id].append(i.as_dict(include_game=False, card_details=True))
        notes = {i.game_id: i for i in notes_events if i.details <= 2 or (i.notes and i.notes.strip())}
        relevant_ids = list(cards.keys())
        relevant_ids += list(notes.keys())
        if include_stats:
            from database.models import Games
            return {i: {"notes": notes[i].notes if i in notes else '', "cards": cards[i],
                        "rating": notes[i].details if i in notes else 3,
                        "game": Games.query.filter(Games.id == i).first().as_dict(admin_view=True, official_view=True)}
                    for
                    i in
                    relevant_ids}
        else:
            return {i: {"notes": notes[i].notes if i in notes else '', "cards": cards[i],
                        "rating": notes[i].details if i in notes else 3} for i in
                    relevant_ids}

    def played_in_tournament(self, tournament_searchable_name):
        if not tournament_searchable_name:
            return True
        from database.models import PlayerGameStats
        from database.models import Tournaments
        tournament_id = Tournaments.query.filter(Tournaments.searchable_name == tournament_searchable_name).first().id
        return bool(PlayerGameStats.query.filter(PlayerGameStats.player_id == self.id,
                                                 PlayerGameStats.tournament_id == tournament_id).first())

    @hybrid_property
    def is_admin(self):
        return self.permission_level == 5

    @hybrid_property
    def is_official(self):
        return self.permission_level >= 2

    @hybrid_property
    def is_umpire_manager(self):
        return self.permission_level >= 2

    def as_dict(self, include_stats=False, tournament=None, admin_view=False, make_nice=False, game_id=None,
                include_court_stats=False, single=False, official_view=False, include_prev_cards=False):
        from database.models import PlayerGameStats
        if game_id:
            pgs = PlayerGameStats.query.filter(PlayerGameStats.game_id == game_id,
                                               PlayerGameStats.player_id == self.id).first()
            if pgs:
                return pgs.as_dict(include_game=False, include_stats=include_stats, official_view=official_view,
                                   include_prev_cards=include_prev_cards)
        img = self.image(tournament=tournament)
        big_img = self.image(tournament=tournament, big=True)
        d = {
            "name": self.name,
            "searchableName": self.searchable_name,
            "imageUrl": img if not img or not img.startswith("/") else "https://api.squarers.club" + img,
            "bigImageUrl": big_img if not big_img or not big_img.startswith(
                "/") else "https://api.squarers.club" + big_img,
        }
        if include_stats:
            include_unranked = False
            if tournament:
                from database.models.Tournaments import Tournaments
                t = Tournaments.query.filter(Tournaments.id == tournament).first()
                include_unranked = not t.ranked
            game_filter = (lambda a: a.filter(PlayerGameStats.tournament_id == tournament)) if tournament else None
            d["stats"] = self.stats(game_filter, make_nice=make_nice, admin=admin_view,
                                    include_court_stats=include_court_stats, include_unranked=include_unranked)
        if (admin_view or official_view) and include_prev_cards and tournament:
            from database.models import GameEvents
            cards: list[GameEvents] = GameEvents.query.filter(GameEvents.tournament_id == self.tournament_id,
                                                              GameEvents.player_id == self.id,
                                                              GameEvents.is_card == True).all()
            d["prevCards"] = [i.as_dict(include_game=False, include_player=False, card_details=True) for i in cards]
        if admin_view:
            d |= {
                "isAdmin": self.is_admin,
                "gameDetails": self.get_admin_games(tournament, include_stats=include_stats and single)
            }
        return d
