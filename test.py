import csv
import random
from itertools import count
from math import floor

from database import db
from database.models import *
from database.models.QOTD import QOTD
from start import app
from structure import manage_game
from utils.statistics import calc_elo


def regen_elo():
    EloChange.query.delete()
    games = Games.query.all()
    for game in games:
        if not game.ranked or game.is_final or game.is_bye: continue

        team_one = PlayerGameStats.query.filter(PlayerGameStats.team_id == game.team_one_id,
                                                PlayerGameStats.game_id == game.id)

        team_two = PlayerGameStats.query.filter(PlayerGameStats.team_id == game.team_two_id,
                                                PlayerGameStats.game_id == game.id)
        if not GameEvents.query.filter(GameEvents.game_id == game.id, GameEvents.event_type == 'Forfeit').all():
            team_one.filter((PlayerGameStats.rounds_on_court + PlayerGameStats.rounds_carded) > 0)
            team_two.filter((PlayerGameStats.rounds_on_court + PlayerGameStats.rounds_carded) > 0)
        team_one = team_one.all()
        team_two = team_two.all()
        print(f"{game.team_two.name} vs {game.team_one.name}")
        teams = [team_one, team_two]
        elos = [0, 0]
        for i, v in enumerate(teams):
            elos[i] = sum(j.player.elo() for j in v)
            elos[i] /= len(v)

        print(elos)
        for i in teams:
            my_team = i != teams[0]
            win = game.winning_team_id == i[0].team_id
            for j in i:
                player_id = j.player_id
                elo_delta = calc_elo(elos[my_team], elos[not my_team], win)
                add = EloChange(game_id=game.id, player_id=player_id, tournament_id=game.tournament_id,
                                elo_delta=elo_delta)
                db.session.add(add)
    db.session.commit()


def sync_all_games():
    games = Games.query.all()
    for i in games:
        if i.id % 20 == 0:
            print(f"Syncing Game {i.id}")
        if i.is_bye:
            continue
        try:
            manage_game.sync(i.id)
        except Exception as e:
            print(e.args)
            print(f"Game {i.id} failed to sync")
    db.session.commit()


def interpolate_start_times():
    games = Games.query.filter(Games.tournament_id == 1, Games.id <= 342).all()
    first_ever_game = 1690887600
    last_time_stamp = 1709901482.6885524
    time_per_event = 29.718165623703534
    current_start_time = 0
    prev_round = 0
    for i in games:
        if i.round != prev_round:
            prev_round = i.round
            # linearly interpolate the two start times
            current_start_time = first_ever_game + ((i.round - 1) / 32) * (last_time_stamp - first_ever_game)
        length = len(GameEvents.query.filter(GameEvents.game_id == i.id).all()) * time_per_event
        i.start_time = current_start_time
        i.length = length
        current_start_time += length + 300
    db.session.commit()


def modify_team_colors():
    FACTOR = 0.7
    teams = Teams.query.filter(Teams.team_color != None).all()
    for i in teams:
        r = floor(int(i.team_color[1:3], 16) * FACTOR)
        g = floor(int(i.team_color[3:5], 16) * FACTOR)
        b = floor(int(i.team_color[5:7], 16) * FACTOR)
        i.team_color = f'#{r:02X}{g:02X}{b:02X}'
    db.session.commit()


def load_quotes():
    with open("C:/Users/Jared Healy/Downloads/Handball Availability for 21_02 - Handball QOTD.csv",
              'r', errors="ignore") as read_obj:
        csv_reader = csv.reader(read_obj)

        # convert string to list
        list_of_csv = list(csv_reader)
    random.shuffle(list_of_csv)
    for n, i in enumerate(list_of_csv):
        q = QOTD()
        q.quote = i[0]
        q.author = i[1]
        db.session.add(q)
    db.session.commit()


def eight_suss_championship():
    manage_game.create_tournament("The Eighth SUSS Championship",
                                  "Pooled",
                                  "PooledFinals",
                                  True,
                                  True,
                                  True,
                                  teams=[97, 112, 113, 114, 95, 7, 116, 73, 118, 119, 19, 123],
                                  officials=[1, 2, 4, 6, 12, 14, 10])


def delete_eighth_tournament():
    ts = Tournaments.query.filter(Tournaments.searchable_name == 'eighth_suss_championship').all()
    for t in ts:
        Games.query.filter(Games.tournament_id == t.id).delete()
        PlayerGameStats.query.filter(PlayerGameStats.tournament_id == t.id).delete()
        EloChange.query.filter(EloChange.tournament_id == t.id).delete()
        TournamentTeams.query.filter(TournamentTeams.tournament_id == t.id).delete()
        TournamentOfficials.query.filter(TournamentOfficials.tournament_id == t.id).delete()
        Tournaments.query.filter(Tournaments.id == t.id).delete()
    db.session.commit()


def reassign_game_numbers():
    count = 1
    for game in Games.query.all():
        if game.is_bye:
            game.game_number = -1
        else:
            game.game_number = count
            count += 1
    db.session.commit()


if __name__ == '__main__':
    with app.app_context():
        delete_eighth_tournament()
        eight_suss_championship()
