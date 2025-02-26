from FixtureGenerators.FixturesGenerator import FixturesGenerator
from database import db
from database.models import TournamentTeams, Tournaments, Games, Teams
from structure import manage_game


class Pooled(FixturesGenerator):

    def __init__(self, tournament_id):
        super().__init__(tournament_id, fill_officials=True, editable=False, fill_courts=True)

    def _begin_tournament(self, tournament_id):
        tournament = Tournaments.query.filter(Tournaments.id == tournament_id).first()
        teams = TournamentTeams.query.filter(TournamentTeams.tournament_id == tournament_id).all()
        teams = sorted(teams, key=lambda x: (x.team.elo() != 1500, -x.team.elo()))
        pool = 0
        for i in teams:
            i.pool = 1 + pool
            pool = 1 - pool
        tournament.is_pooled = True
        db.session.commit()

    def _end_of_round(self, tournament_id):
        tournament = Tournaments.query.filter(Tournaments.id == tournament_id).first()
        teams = TournamentTeams.query.filter(TournamentTeams.tournament_id == tournament_id).all()
        game = Games.query.filter(Games.tournament_id == tournament_id).order_by(Games.round.desc()).first()
        rounds = game.round if game else 0

        pools = [[j.team for j in teams if j.pool == i] for i in range(1, 3)]

        for pool in pools:
            if len(pool) % 2 != 0:
                pool += [Teams.BYE]

        if max(len(i) for i in pools) <= rounds + 1:
            tournament.in_finals = True
            return
        for _ in range(rounds):
            for pool in pools:
                pool[1:] = [pool[-1]] + pool[1:-1]
            # Rotate the teams except the first one
        for pool in pools:
            mid = len(pool) // 2
            for j in range(mid):
                team_one = pool[j]
                team_two = pool[len(pool) - 1 - j]
                manage_game.create_game(tournament_id, team_one.searchable_name, team_two.searchable_name,
                                        round_number=rounds + 1)
