import csv
import random
from dataclasses import dataclass
from math import floor
from typing import NamedTuple

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
                                  teams=[97, 112, 113, 114, 95, 116, 73, 118, 19, 123],
                                  officials=[1, 2, 4, 11, 12, 14, 10])


def delete_eighth_tournament():
    ts = Tournaments.query.filter(Tournaments.searchable_name == 'eighth_suss_championship').all()
    for t in ts:
        Games.query.filter(Games.tournament_id == t.id).delete()
        PlayerGameStats.query.filter(PlayerGameStats.tournament_id == t.id).delete()
        EloChange.query.filter(EloChange.tournament_id == t.id).delete()
        TournamentTeams.query.filter(TournamentTeams.tournament_id == t.id).delete()
        TournamentOfficials.query.filter(TournamentOfficials.tournament_id == t.id).delete()
        GameEvents.query.filter(GameEvents.tournament_id == t.id).delete()
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


def trim_tournament_ids():
    id_map = {t.id: i for i, t in enumerate(Tournaments.query.all(), start=1)}
    elo = EloChange.query.all()
    game_events = GameEvents.query.all()
    games = Games.query.all()
    pgs = PlayerGameStats.query.all()
    tournament_teams = TournamentTeams.query.all()
    tournament_officials = TournamentOfficials.query.all()
    for j in [game_events, games, elo, pgs, tournament_teams, tournament_officials]:
        print("Fixing ")
        for i in j:
            if i.tournament_id not in id_map:
                input(i)
            i.tournament_id = id_map[i.tournament_id]
    for i in Tournaments.query.all():
        i.id = id_map[i.id] + 100_000
    db.session.commit()
    i = None
    for i in Tournaments.query.all():
        i.id = i.id - 100_000
    db.session.commit()
    return i.id


def trim_tournament_teams_ids():
    id_map = {t.id: i for i, t in enumerate(TournamentTeams.query.all(), start=1)}
    for i in TournamentTeams.query.all():
        i.id = id_map[i.id] + 100_000
    db.session.commit()
    i = None
    for i in TournamentTeams.query.all():
        i.id = i.id - 100_000
    db.session.commit()
    return i.id


def trim_tournament_officials_ids():
    id_map = {t.id: i for i, t in enumerate(TournamentTeams.query.all(), start=1)}
    for i in TournamentTeams.query.all():
        i.id = id_map[i.id] + 100_000
    db.session.commit()
    i = None
    for i in TournamentTeams.query.all():
        i.id = i.id - 100_000
    db.session.commit()
    return i.id


def trim_elo_ids():
    id_map = {t.id: i for i, t in enumerate(EloChange.query.all(), start=1)}
    for i in EloChange.query.all():
        i.id = id_map[i.id] + 100_000
    db.session.commit()
    i = None
    for i in EloChange.query.all():
        i.id = i.id - 100_000
    db.session.commit()
    return i.id


def trim_pgs_ids():
    id_map = {t.id: i for i, t in enumerate(PlayerGameStats.query.all(), start=1)}
    for i in PlayerGameStats.query.all():
        i.id = id_map[i.id] + 100_000
    db.session.commit()
    i = None
    for i in PlayerGameStats.query.all():
        i.id = i.id - 100_000
    db.session.commit()
    return i.id


def trim_game_ids():
    games = Games.query.order_by(Games.tournament_id != 2, Games.tournament_id != 3, Games.id).all()
    id_map = {g.id: i for i, g in enumerate(games, start=1)}
    elo = EloChange.query.all()
    game_events = GameEvents.query.all()
    pgs = PlayerGameStats.query.all()
    for j, title in [(game_events, "Game Events"), (elo, "Elo"), (pgs, "Player Game Stats")]:
        print(f'Fixing {title}...')
        for i in j:
            if i.game_id not in id_map:
                input(i)
            i.game_id = id_map[i.game_id]
    db.session.commit()
    print('First Pass over game IDs...')
    for i in Games.query.all():
        i.id = id_map[i.id] + 100_000
    db.session.commit()
    print('Correcting game IDs...')
    i = None
    for i in Games.query.all():
        i.id = i.id - 100_000
    db.session.commit()
    print('Done!')
    return i.id


def trim_all_db_id():
    out = {
        'PlayerGameStats': trim_pgs_ids(),
        'EloChange': trim_elo_ids(),
        'TournamentTeams': trim_tournament_teams_ids(),
        'Tournaments': trim_tournament_ids(),
        'Games': trim_game_ids(),
        'TournamentOfficials': trim_tournament_officials_ids(),
    }
    print(out)


def rerun_game(game_num):
    """ONLY TO BE USED IN EXTREME CIRCUMSTANCES"""
    GameEventDetails = NamedTuple('GameEventDetails', event_type=str, first_team=bool, left_side=bool, notes=str,
                                  details=int, time=float)
    game = Games.query.filter(Games.game_number == game_num).one()
    marked_for_review = game.marked_for_review
    best_player = game.best_player.searchable_name
    game_id = game.id
    protests: list[str | None] = [None, None]
    ratings = [3, 3]
    notes: list[str | None] = [None, None]
    end: list[GameEventDetails | None] = [None]

    def event_to_func(e: GameEventDetails):
        fix_time = True
        prev = GameEvents.query.order_by(GameEvents.id.desc()).first()
        match e.event_type:
            case 'Score':
                if e.notes == 'Ace':
                    manage_game.ace(game_id)
                else:
                    manage_game.score_point(game_id, e.first_team, e.left_side, e.notes)
            case 'Start':
                pass
            case 'Fault':
                manage_game.fault(game_id)
            case 'Warning' | 'Green Card' | 'Yellow Card' | 'Red Card':
                left = prev.team_one_left if e.first_team else prev.team_two_left
                right = prev.team_one_right if e.first_team else prev.team_two_right
                flip = False
                correct = None
                while correct not in ['y', 'n']:
                    correct = input(
                        f'{e.event_type} for {left.name if e.left_side else right.name} for {e.notes}?\t').lower()
                    flip = correct == 'n'
                if flip:
                    print('Swapping Cards...')
                manage_game.card(game_id, e.first_team, e.left_side != flip, e.event_type.replace(' Card', ''),
                                 e.details,
                                 e.notes)
            case 'Resolve':
                manage_game.resolve_game(game_id)
            case 'Substitute':
                manage_game.substitute(game_id, e.first_team, e.left_side)
            case 'Forfeit':
                manage_game.forfeit(game_id, e.first_team)
            case 'End Timeout':
                manage_game.end_timeout(game_id)
            case 'Protest':
                fix_time = False
                protests[not e.first_team] = e.notes
            case 'Notes':
                fix_time = False
                ratings[not e.first_team] = e.details
                notes[not e.first_team] = e.notes
            case 'End Game':
                fix_time = False
                end[0] = e
        if fix_time:
            prev.created_at = e.time

    out: list[GameEventDetails] = []
    first_team = game.team_one_id
    events = GameEvents.query.filter(GameEvents.game_id == game_id).all()
    print('Saving Events...')
    for i in events:
        out.append(GameEventDetails(i.event_type, i.team_id == first_team if i.team_id else None,
                                    i.player_id in [i.team_one_left_id, i.team_two_left_id] if i.player_id else None,
                                    i.notes, i.details, i.created_at))
    GameEvents.query.filter(GameEvents.game_id == game_id, GameEvents.event_type != 'Start').delete()
    EloChange.query.filter(EloChange.game_id == game_id).delete()
    manage_game.sync(game_id)
    db.session.commit()
    print('Rerunning Events...')
    for i in out:
        event_to_func(i)
    manage_game.end_game(game_id, best_player, ratings[0], ratings[1], end[0].notes or '',
                         protests[0], protests[1], notes[0], notes[1], marked_for_review, redone=True)
    db.session.commit()
    print('Fixing End Times...')
    fix_times = GameEvents.query.filter(GameEvents.game_id == game_id, GameEvents.created_at > end[0].time).all()
    for i in fix_times:
        i.created_at = end[0].time
    db.session.commit()
    print('Done!')


def delete_orphaned_events():
    games = [i.id for i in Games.query.all()]
    for i in [EloChange, GameEvents, PlayerGameStats]:
        for j in i.query.all():
            if j.game_id not in games:
                print(f'Deleting {j} (game {j.game_id} doesn\'t exist)')
                i.query.filter(i.game_id == j.game_id).delete()
    db.session.commit()


if __name__ == '__main__':
    with app.app_context():
        reassign_game_numbers()
