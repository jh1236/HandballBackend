"""Defines the comments object and provides functions to get and manipulate one"""
import time
import typing
from functools import reduce

from sqlalchemy.ext.hybrid import hybrid_property
from sqlalchemy.sql.operators import exists

from database import db

if typing.TYPE_CHECKING:
    pass

# create table main.games
# (
#     id                INTEGER primary key autoincrement,
# tournamentId      INTEGER references main.tournaments,
# teamOne           INTEGER references main.teams,
# teamTwo           INTEGER references main.teams,
# teamOneScore      INTEGER,
# teamTwoScore      INTEGER,
# teamOneTimeouts   INTEGER,
# teamTwoTimeouts   INTEGER,
# winningTeam       INTEGER,
# started           INTEGER,
# ended             INTEGER,
# someoneHasWon     INTEGER,
# protested         INTEGER,
# resolved          INTEGER,
# isRanked          INTEGER,
# bestPlayer        INTEGER references main.people,
# official          INTEGER references main.officials,
# scorer            INTEGER references main.officials,
# IGASide           INTEGER references main.teams,
# gameStringVersion INTEGER,
# gameString        TEXT,
# playerToServe     INTEGER references main.people,
# teamToServe       INTEGER references main.teams,
# sideToServe       TEXT,
# startTime         INTEGER,
# length            INTEGER,
# court             INTEGER,
# isFinal           INTEGER,
# round             INTEGER,
# notes             TEXT,
# isBye             INTEGER,
# pool              INTEGER,
# status            TEXT,
# adminStatus       TEXT,
# noteableStatus    TEXT
# );


NO_ACTION_REQUIRED = [
    'Resolved',
    'In Progress',
    'Official',
    'Ended',
    'Waiting For Start',
    'Forfeit',
    'Bye'
]


class Games(db.Model):
    __tablename__ = "games"

    # Auto-initialised fields
    id = db.Column(db.Integer(), primary_key=True)
    created_at = db.Column(db.Integer(), default=time.time, nullable=False)

    # Set fields
    tournament_id = db.Column(db.Integer(), db.ForeignKey("tournaments.id"), nullable=False)
    team_one_id = db.Column(db.Integer(), db.ForeignKey("teams.id"), nullable=False)
    team_two_id = db.Column(db.Integer(), db.ForeignKey("teams.id"), nullable=False)
    team_one_score = db.Column(db.Integer(), default=0, nullable=False)
    team_two_score = db.Column(db.Integer(), default=0, nullable=False)
    team_one_timeouts = db.Column(db.Integer(), default=0, nullable=False)
    team_two_timeouts = db.Column(db.Integer(), default=0, nullable=False)
    winning_team_id = db.Column(db.Integer(), db.ForeignKey("teams.id"))
    started = db.Column(db.Boolean(), default=False, nullable=False)
    someone_has_won = db.Column(db.Boolean(), default=False, nullable=False)
    ended = db.Column(db.Boolean(), default=False, nullable=False)
    protested = db.Column(db.Boolean(), default=False, nullable=False)
    resolved = db.Column(db.Boolean(), default=False, nullable=False)
    ranked = db.Column(db.Boolean(), nullable=False)
    best_player_id = db.Column(db.Integer(), db.ForeignKey("people.id"))
    official_id = db.Column(db.Integer(), db.ForeignKey("officials.id"), nullable=False)
    scorer_id = db.Column(db.Integer(), db.ForeignKey("officials.id"))
    iga_side_id = db.Column(db.Integer(), db.ForeignKey("teams.id"))
    player_to_serve_id = db.Column(db.Integer(), db.ForeignKey("people.id"))
    team_to_serve_id = db.Column(db.Integer(), db.ForeignKey("teams.id"))
    side_to_serve = db.Column(db.Text(), default='Left', nullable=False)
    start_time = db.Column(db.Integer())
    serve_timer = db.Column(db.Integer())
    length = db.Column(db.Integer())
    court = db.Column(db.Integer(), default=0, nullable=False)
    is_final = db.Column(db.Boolean(), default=False, nullable=False)
    round = db.Column(db.Integer(), nullable=False)
    notes = db.Column(db.Text())
    game_number = db.Column(db.Integer())
    is_bye = db.Column(db.Boolean(), default=False, nullable=False)
    status = db.Column(db.Text(), default='Waiting For Start', nullable=False)
    admin_status = db.Column(db.Text(), default='Waiting For Start', nullable=False)
    noteable_status = db.Column(db.Text(), default='Waiting For Start', nullable=False)
    marked_for_review = db.Column(db.Boolean(), default=False, nullable=False)

    tournament = db.relationship("Tournaments", foreign_keys=[tournament_id])
    winning_team = db.relationship("Teams", foreign_keys=[winning_team_id])
    team_one = db.relationship("Teams", foreign_keys=[team_one_id])
    team_two = db.relationship("Teams", foreign_keys=[team_two_id])
    best_player = db.relationship("People", foreign_keys=[best_player_id])
    official = db.relationship("Officials", foreign_keys=[official_id])
    scorer = db.relationship("Officials", foreign_keys=[scorer_id])
    iga_side = db.relationship("Teams", foreign_keys=[iga_side_id])
    player_to_serve = db.relationship("People", foreign_keys=[player_to_serve_id])
    team_to_serve = db.relationship("Teams", foreign_keys=[team_to_serve_id])
    elo_delta = db.relationship("EloChange")

    row_titles = ["Rounds",
                  "Score Difference",
                  "Elo Gap",
                  "Length",
                  "Cards",
                  "Warnings",
                  "Green Cards",
                  "Yellow Cards",
                  "Red Cards",
                  "Timeouts Used",
                  "Aces Scored",
                  "Ace Percentage",
                  "Faults",
                  "Fault Percentage",
                  "Start Time",
                  "Court",
                  "Timeline",
                  "Umpire",
                  "Format",
                  "Tournament"
                  ]

    @classmethod
    def game_number_to_id(cls, num):
        return Games.query.filter(Games.game_number == num).first().id

    @classmethod
    def get_latest_game_number(cls):
        return Games.query.order_by(Games.game_number.desc()).limit(1).first().game_number

    @property
    def formatted_start_time(self):
        if self.start_time < 0: return "??"
        return time.strftime("%d/%m/%y (%H:%M)", time.localtime(self.start_time))

    @property
    def formatted_length(self):
        if self.start_time < 0: return "??"
        return time.strftime("(%H:%M:%S)", time.localtime(self.length))

    @hybrid_property
    def losing_team_id(self):
        # cheeky maths hack
        return self.team_one_id + self.team_two_id - self.winning_team_id

    @hybrid_property
    def requires_action(self):
        return self.admin_status not in NO_ACTION_REQUIRED

    @requires_action.expression
    def requires_action(cls):
        return cls.admin_status.in_(NO_ACTION_REQUIRED)

    @hybrid_property
    def is_noteable(self):
        return self.noteable_status not in NO_ACTION_REQUIRED

    @is_noteable.expression
    def is_noteable(cls):
        return cls.noteable_status.in_(NO_ACTION_REQUIRED) == False

    def reset(self):
        self.started = False
        self.someone_has_won = False
        self.ended = False
        self.protested = False
        self.resolved = False
        self.best_player_id = None
        self.team_one_score = 0
        self.team_two_score = 0
        self.team_one_timeouts = 0
        self.team_two_timeouts = 0
        self.notes = None
        self.winning_team_id = None

    @hybrid_property
    def rounds(self):
        return self.team_one_score + self.team_two_score

    @property
    def on_fault(self):
        from database.models import GameEvents
        prev_event = GameEvents.query.filter(GameEvents.game_id == self.id, (GameEvents.event_type == 'Fault') | (
                GameEvents.event_type == 'Score')).order_by(GameEvents.id.desc()).first()
        if not prev_event:
            return False
        return prev_event.event_type == 'Fault'

    def stats(self):
        from database.models import PlayerGameStats
        pgs = PlayerGameStats.query.filter(PlayerGameStats.game_id == self.id).all()
        return {
            "Rounds": self.rounds,
            "Score Difference": abs(self.team_one_score - self.team_two_score),
            "Elo Gap": abs(self.team_one.elo(self.id) - self.team_two.elo(self.id)),
            "Length": self.length,
            "Cards": sum(i.green_cards + i.yellow_cards + i.red_cards for i in pgs),
            "Warnings": sum(i.warnings for i in pgs),
            "Green Cards": sum(i.green_cards for i in pgs),
            "Yellow Cards": sum(i.yellow_cards for i in pgs),
            "Red Cards": sum(i.red_cards for i in pgs),
            "Aces Scored": sum(i.aces_scored for i in pgs),
            "Ace Percentage": sum(i.aces_scored for i in pgs) / ((self.team_one_score + self.team_two_score) or 1),
            "Faults": sum(i.faults for i in pgs),
            "Fault Percentage": sum(i.faults for i in pgs) / ((self.team_one_score + self.team_two_score) or 1),
            "Start Time": 0 if not self.start_time or self.start_time <= 0 else (self.start_time - Games.query.filter(
                Games.start_time > 0).order_by(Games.start_time).first().start_time) / (24.0 * 60 * 60 * 60),
            "Court": self.court,
            "Ranked": self.ranked,
            "Timeline": self.id,
            "Umpire": self.official.person.name if self.official else "None",
            "Format": "Practice" if self.tournament_id == 1 else "Championship",
            "Tournament": self.tournament.name,
        }

    def as_dict(self, admin_view=False, include_game_events=False, include_stats=False, official_view=False,
                make_nice=False):
        from structure.manage_game import change_code, get_timeout_time, is_official_timeout
        d = {
            "id": self.game_number,
            "tournament": self.tournament.as_dict(),
            "teamOne": self.team_one.as_dict(game_id=self.id, include_stats=include_stats,
                                             make_nice=make_nice, admin_view=admin_view, official_view=official_view),
            "teamTwo": self.team_two.as_dict(game_id=self.id, include_stats=include_stats,
                                             make_nice=make_nice, admin_view=admin_view, official_view=official_view),
            "teamOneScore": self.team_one_score,
            "teamTwoScore": self.team_two_score,
            "teamOneTimeouts": self.team_one_timeouts,
            "teamTwoTimeouts": self.team_two_timeouts,
            "firstTeamWinning": self.winning_team_id == self.team_one_id,
            "started": self.started,
            "someoneHasWon": self.someone_has_won,
            "ended": self.ended,
            "ranked": self.ranked,
            "bestPlayer": self.best_player.as_dict() if self.best_player else None,
            "official": self.official.as_dict(tournament=self.tournament) if self.official else None,
            "scorer": self.scorer.as_dict(tournament=self.tournament) if self.scorer else None,
            "firstTeamIga": self.iga_side_id == self.team_one_id,
            "firstTeamToServe": self.team_to_serve_id == self.team_one_id,
            "sideToServe": self.side_to_serve,
            "startTime": self.start_time,
            "serveTimer": self.serve_timer,
            "length": self.length,
            "court": self.court,
            "isFinal": self.is_final,
            "round": self.round,
            "isBye": self.is_bye,
            "status": self.admin_status if admin_view else self.status,
            "faulted": self.on_fault,
            "changeCode": change_code(self.id),
            "timeoutExpirationTime": 1000 * get_timeout_time(self.id),
            "isOfficialTimeout": is_official_timeout(self.id),
        }
        if admin_view:
            from database.models import GameEvents
            rating_events = None if not self.ended else GameEvents.query.filter(
                GameEvents.game_id == self.id, GameEvents.event_type == 'Notes').all()
            team_one_protest = GameEvents.query.filter(GameEvents.game_id == self.id,
                                                       GameEvents.event_type == 'Protest',
                                                       GameEvents.team_id == self.team_one_id).first()
            team_two_protest = GameEvents.query.filter(GameEvents.game_id == self.id,
                                                       GameEvents.event_type == 'Protest',
                                                       GameEvents.team_id == self.team_two_id).first()
            team_one_notes = [i.notes for i in rating_events if i.team_id == self.team_one_id][
                0] if rating_events else None
            team_two_notes = [i.notes for i in rating_events if i.team_id == self.team_two_id][
                0] if rating_events else None
            d["admin"] = {
                "markedForReview": self.marked_for_review,
                "requiresAction": self.requires_action,
                "noteableStatus": self.noteable_status,
                "notes": self.notes if self.notes and self.notes.strip() else None,
                "teamOneRating": [i.details for i in rating_events if i.team_id == self.team_one_id][
                    0] if rating_events else 3,
                "teamTwoRating": [i.details for i in rating_events if i.team_id == self.team_two_id][
                    0] if rating_events else 3,
                "teamOneNotes": team_one_notes if team_one_notes and team_one_notes.strip() else None,
                "teamTwoNotes": team_two_notes if team_two_notes and team_two_notes.strip() else None,
                "teamOneProtest": team_one_protest.notes if team_one_protest else None,
                "teamTwoProtest": team_two_protest.notes if team_two_protest else None,
                "cards": [i.as_dict(include_game=False, card_details=True) for i in
                          GameEvents.query.filter(GameEvents.game_id == self.id,
                                                  (GameEvents.event_type == 'Warning') | (
                                                      GameEvents.event_type.like('% Card'))).all()],
                "resolved": self.resolved,
            }
        if include_game_events:
            from database.models import GameEvents
            d["events"] = [i.as_dict(include_game=False) for i in
                           GameEvents.query.filter(GameEvents.game_id == self.id).all()]
        if include_stats:
            from database.models import PlayerGameStats
            d["players"] = [i.as_dict(include_game=False, make_nice=make_nice) for i in
                            PlayerGameStats.query.filter(PlayerGameStats.game_id == self.id).all()]

        return d
