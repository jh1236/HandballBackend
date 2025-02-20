from FixtureGenerators.FixturesGenerator import FixturesGenerator
from database.models import Tournaments, Games
from structure import manage_game


# [sf1, sf2], [3v3, 4v4, 5v5]
# [sf1, 3v3, sf2, 4v4, 5v5]

class PooledFinals(FixturesGenerator):
    def __init__(self, tournament_id):
        super().__init__(tournament_id, fill_officials=True, editable=False, fill_courts=True)
    
    def _end_of_round(self, tournament_id):
        pool_one, pool_two = [[j[0] for j in i] for i in Tournaments.query.filter(Tournaments.id == tournament_id).first().ladder()]
        finals_games = Games.query.filter(Games.tournament_id == tournament_id, Games.is_final == True).all()
        last_game = Games.query.filter(Games.tournament_id == tournament_id).order_by(Games.round.desc()).first()

        if len(finals_games) > 2:
            self.end_tournament()
        elif finals_games:
            manage_game.create_game(tournament_id, finals_games[0].losing_team_id, finals_games[1].losing_team_id,
                                    is_final=True,
                                    round_number=finals_games[-1].round + 1)
            manage_game.create_game(tournament_id, finals_games[0].winning_team_id, finals_games[1].winning_team_id,
                                    is_final=True,
                                    round_number=finals_games[-1].round + 1)
        else:
            manage_game.create_game(tournament_id, pool_one[0].id, pool_two[1].id, is_final=True,
                                    round_number=last_game.round + 1)
            manage_game.create_game(tournament_id, pool_two[0].id, pool_one[1].id, is_final=True,
                                    round_number=last_game.round + 1)
