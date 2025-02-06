import time

from database import db

# create table main.teams
# (
#     id             INTEGER
# primary key autoincrement,
# name           TEXT,
# searchableName TEXT,
# imageURL       TEXT,
# primaryColor   TEXT,
# secondaryColor TEXT,
# captain        INTEGER
# references main.people,
# nonCaptain     INTEGER
# references main.people,
# substitute     INTEGER
# references main.people
# );

PERCENTAGES = [
    "Percentage"
]

MULTI_GAME_KEYS = [
    "Games Played",
    "Games Won",
    "Games Lost",
    "Percentage",
]


def hex_to_rgba(hexa):
    if not hexa:
        return None
    hexa = hexa.lstrip('#')
    return [*(int(hexa[i:i + 2], 16) for i in (0, 2, 4)), 255]


class Teams(db.Model):
    __tablename__ = "teams"

    # Auto-initialised fields
    id = db.Column(db.Integer(), primary_key=True)
    created_at = db.Column(db.Integer(), default=time.time, nullable=False)

    # Set fields
    name = db.Column(db.Text(), nullable=False)
    searchable_name = db.Column(db.Text(), nullable=False)
    image_url = db.Column(db.Text())
    team_color = db.Column(db.Text())
    captain_id = db.Column(db.Integer(), db.ForeignKey("people.id"), nullable=False)
    non_captain_id = db.Column(db.Integer(), db.ForeignKey("people.id"))
    substitute_id = db.Column(db.Integer(), db.ForeignKey("people.id"))

    captain = db.relationship("People", foreign_keys=[captain_id])
    non_captain = db.relationship("People", foreign_keys=[non_captain_id])
    substitute = db.relationship("People", foreign_keys=[substitute_id])

    def elo(self, last_game=None):
        from database.models import People
        players = People.query.filter(
            (People.id == self.captain_id) | (People.id == self.non_captain_id) | (
                    People.id == self.substitute_id)).all()
        if not players:
            return 1500.0
        elos = []
        for i in players:
            elos.append(i.elo(last_game))
        return sum(elos) / len(elos)

    @property
    def short_name(self):
        return self.name if len(self.name) < 30 else self.name[:27] + "..."

    def players(self):
        return [i for i in [self.captain, self.non_captain, self.substitute] if i]

    def stats(self, games_filter=None, make_nice=True, ranked=True, admin_view=False):

        from database.models import PlayerGameStats, Games
        games = Games.query.filter((Games.team_one_id == self.id) | (Games.team_two_id == self.id),
                                   Games.is_bye == False, Games.is_final == False)
        pgs = PlayerGameStats.query.join(Games, PlayerGameStats.game_id == Games.id).filter(
            Games.is_bye == False, Games.is_final == False,
            PlayerGameStats.team_id == self.id)
        # '== False' is not an error here, as Model overrides __eq__, so using a not operator provides a different result
        if self.non_captain_id is not None and ranked:
            games = games.filter(Games.ranked)
            pgs = pgs.filter(Games.ranked)

        if games_filter:
            games = games_filter(games)
            pgs = games_filter(pgs)

        games = games.all()
        pgs = pgs.all()

        ret = {
            "Elo": self.elo(games[-1].id if games else 9999999),
            "Games Played": len([i for i in games if i.started]),
            "Games Won": sum(i.winning_team_id == self.id for i in games if i.ended),
            "Games Lost": sum(i.winning_team_id != self.id for i in games if i.ended),
            "Percentage": sum(i.winning_team_id == self.id for i in games if i.ended) / (
                    len([i for i in games if i.ended]) or 1),
            "Green Cards": sum(i.green_cards for i in pgs),
            "Yellow Cards": sum(i.yellow_cards for i in pgs),
            "Red Cards": sum(i.red_cards for i in pgs),
            "Faults": sum(i.faults for i in pgs),
            "Double Faults": sum(i.double_faults for i in pgs),
            "Timeouts Called": sum(
                (i.team_one_timeouts if i.team_one_id == self.id else i.team_two_timeouts) for i in games),
            # Points for and against are different because points for shouldn't include opponents double faults, but points against should
            "Points Scored": sum(i.points_scored for i in pgs),
            "Points Against": sum((i.team_two_score if i.team_one_id == self.id else i.team_one_score) for i in games),
            "Point Difference": sum(i.points_scored for i in pgs) - sum(
                (i.team_two_score if i.team_one_id == self.id else i.team_one_score) for i in games),
        }
        if admin_view:
            ret |= {
                "Warnings": sum(i.warnings for i in pgs),
                "Penalty Points": ret["Green Cards"] * 2 + ret["Yellow Cards"] * 5 + ret["Red Cards"] * 10
            }
        if make_nice:
            for k, v in ret.items():
                if k in PERCENTAGES:
                    ret[k] = f"{100.0 * v: .2f}%".strip()
                elif isinstance(v, float):
                    ret[k] = round(v, 2)
        return ret

    @classmethod
    @property
    def BYE(cls):
        return cls.query.filter(cls.id == 1).first()

    def as_dict(self, include_stats=False, tournament=None, include_player_stats=None, make_nice=False, game_id=None,
                admin_view=False):
        include_player_stats = include_stats if include_player_stats is None else include_player_stats
        d = {
            "name": self.name,
            "searchableName": self.searchable_name,
            "imageUrl": self.image_url if not self.image_url or not self.image_url.startswith(
                "/") else "https://api.squarers.club" + self.image_url,
            "teamColor": self.team_color,
            "teamColorAsRGBABecauseDigbyIsLazy": hex_to_rgba(self.team_color),
            "captain": self.captain.as_dict(include_stats=include_player_stats,
                                            tournament=tournament, game_id=game_id,
                                            make_nice=make_nice) if self.captain else None,
            "nonCaptain": self.non_captain.as_dict(include_stats=include_player_stats,
                                                   tournament=tournament,
                                                   game_id=game_id, make_nice=make_nice) if self.non_captain_id else None,
            "substitute": self.substitute.as_dict(include_stats=include_player_stats,
                                                  tournament=tournament, game_id=game_id,
                                                  make_nice=make_nice) if self.substitute_id else None,
        }
        if tournament:
            from database.models.TournamentTeams import TournamentTeams
            tt = TournamentTeams.query.filter(TournamentTeams.id == tournament,
                                              TournamentTeams.team_id == self.id).first()
            if tt:
                d["imageUrl"] = tt.image_url if tt.image_url else d["imageUrl"]
                d["teamColor"] = tt.team_color if tt.image_url else d["teamColor"]
                d["teamColorAsRGBABecauseDigbyIsLazy"] = tt.team_color if tt.image_url else d[
                    "teamColorAsRGBABecauseDigbyIsLazy"]
                d["name"] = tt.name if tt.name else d["name"]
        if game_id:
            from database.models.TournamentTeams import TournamentTeams
            from database.models.GameEvents import GameEvents
            from database.models.Games import Games
            game = Games.query.filter(Games.id == game_id).first()
            tt = TournamentTeams.query.filter(TournamentTeams.id == game.tournament_id,
                                              TournamentTeams.team_id == self.id).first()
            if tt:
                d["imageUrl"] = tt.image_url if tt.image_url else d["imageUrl"]
                d["teamColor"] = tt.team_color if tt.image_url else d["teamColor"]
                d["teamColorAsRGBABecauseDigbyIsLazy"] = tt.team_color if tt.image_url else d[
                    "teamColorAsRGBABecauseDigbyIsLazy"]
                d["name"] = tt.name if tt.name else d["name"]
            last_time_served = GameEvents.query.filter(
                GameEvents.game_id == game_id, GameEvents.team_who_served_id == self.id,
                GameEvents.event_type == 'Score').order_by(
                GameEvents.id.desc()).first()
            if not last_time_served:
                d["servedFromLeft"] = False
            else:
                d["servedFromLeft"] = last_time_served.side_served == "Left"
        if include_stats:
            from database.models.Games import Games
            ranked = True
            if game_id:
                game_filter = (lambda a: a.filter(Games.id == game_id))
            elif tournament:
                from database.models.Tournaments import Tournaments
                ranked = Tournaments.query.filter(Tournaments.id == tournament).first().ranked
                game_filter = (lambda a: a.filter(Games.tournament_id == tournament))
            else:
                game_filter = None
            d["stats"] = self.stats(game_filter, make_nice=make_nice, admin_view=admin_view, ranked=ranked)
            if game_id:
                from database.models.EloChange import EloChange
                from database.models.PlayerGameStats import PlayerGameStats
                elo_delta_q = EloChange.query.join(PlayerGameStats,
                                                   (EloChange.player_id == PlayerGameStats.player_id) & (
                                                           EloChange.game_id == game_id)).filter(
                    PlayerGameStats.team_id == self.id).all()
                elo_delta = sum(i.elo_delta for i in elo_delta_q) / (len(elo_delta_q) or 1)

                if make_nice:
                    d["stats"]["Elo Delta"] = round(elo_delta, 2) if elo_delta else 0
                else:
                    d["stats"]["Elo Delta"] = elo_delta if elo_delta else 0
                for i in MULTI_GAME_KEYS:
                    del d["stats"][i]

        return d
