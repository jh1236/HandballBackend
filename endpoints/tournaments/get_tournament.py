import json
import os

from flask import request
from flask import send_file

from database import db
from database.models import Tournaments, Games, Teams
from endpoints.tournaments.blueprint import tournaments
from utils.logging_handler import logger
from utils.permissions import umpire_manager_only


@tournaments.post("/note")
@umpire_manager_only
def note():
    """
    SCHEMA:
    {
        tournament: str = the searchable name of the tournament
        note: str = the note for the tournament
    }
    """
    logger.info(f"Request for notes: {request.json}")
    tournament = request.json["tournament"]
    note = request.json["note"]
    t = Tournaments.query.filter(Tournaments.searchable_name == tournament).first()
    t.notes = note
    db.session.commit()
    return "", 204


@tournaments.get("/<searchable>")
def get_tournament(searchable):
    """
    SCHEMA:
    {
        name: str = the searchable name of the tournament
    }
    """
    return {"tournament": Tournaments.query.filter(Tournaments.searchable_name == searchable).first().as_dict()}


@tournaments.get("/")
def get_tournaments():
    """
    SCHEMA:
    {
        name: str = the searchable name of the tournament
    }
    """
    return {"tournaments": [i.as_dict() for i in Tournaments.query.all()]}


@tournaments.get("/<searchable>/winners")
def get_tournament_winners(searchable):
    """
    SCHEMA:
    {
        name: str = the searchable name of the tournament
    }
    """
    tournament = Tournaments.query.filter(Tournaments.searchable_name == searchable).first()
    games = Games.query.filter(Games.tournament_id == tournament.id, Games.is_final == True).order_by(
        Games.round.desc(), Games.court, Games.id.desc()).all()
    grand_final = games[0]
    first = grand_final.winning_team.as_dict()
    second = Teams.query.filter(Teams.id == grand_final.losing_team_id).first().as_dict()
    if games[1].winning_team_id in [grand_final.team_one_id,
                                    grand_final.team_two_id]:
        third = Teams.query.filter(Teams.id == games[1].losing_team_id).first().as_dict()
    else:
        third = games[1].winning_team.as_dict()
    return {"first": first, "second": second, "third": third,
            "podium": [first, second, third]}, 200


@tournaments.get("/image")
def tourney_image():
    """
    SCHEMA:
    {
        name: str = the searchable name of the tournament
    }
    """
    tournament = request.args.get("name", type=str)
    big = request.args.get("big", type=bool)
    if os.path.isfile(f"./resources/images/tournaments/{tournament}.png"):
        return send_file(
            f"./resources/images{'/big' if big else ''}/tournaments/{tournament}.png", mimetype="image/png"
        )
    else:
        return send_file(
            f"./resources/images{'/big' if big else ''}/teams/blank.png", mimetype="image/png"
        )


@tournaments.post("/serveStyle")
@umpire_manager_only
def serve_style():
    """
    WARNING: DO NOT CHANGE WHILE A GAME IS IN PROGRESS
    SCHEMA:
    {
        tournament: str = the searchable name of the tournament
        badminton_serves: bool = if the tournament should use badminton serving
    }
    """
    logger.info(f"Request for serve_style: {request.json}")
    tournament = request.json["tournament"]
    t = Tournaments.query.filter(Tournaments.searchable_name == tournament).first()
    t.badminton_serves = request.json.get("badmintonServes", not t.badminton_serves)
    db.session.commit()
    return "", 204


@tournaments.post("/umpire")
def umpire():
    """
    SCHEMA:
    {
        umpire: str = the name of the umpire
    }
    """
    with open("config/signups/officials.json") as fp:
        umpires = json.load(fp)
    umpires.append(request.json["umpire"])
    with open("config/signups/officials.json", "w+") as fp:
        json.dump(umpires, fp)
    return "", 204
