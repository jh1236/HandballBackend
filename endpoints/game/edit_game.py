from flask import request, jsonify

from database.models import Games
from endpoints.game import edit_games
from structure import manage_game
from utils.logging_handler import logger
from utils.permissions import officials_only, admin_only, fetch_user, umpire_manager_only


@edit_games.post("/start")
@officials_only
def start():
    """
    SCHEMA:
        {
            id: <int> = id of the current game
            swapService: <bool> = if the team listed first is serving
            teamOneIGA: <bool> = if the team listed first is on the IGA side of the court
            teamOne: <list[str]> = the searchable names of the team listed first in order [left, right, substitute]
            teamTwo: <list[str]> = the searchable names of the team listed second in order [left, right, substitute]
            official: <str> (OPTIONAL) = the official who is actually umpiring the game
            scorer: <str> (OPTIONAL) = the scorer who is actually scoring the game
        }
    """
    logger.info(f"Request for start: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    swap_serve = request.json["swapService"]
    team_one = request.json.get("teamOne", None)
    team_two = request.json.get("teamTwo", None)
    first_is_iga = request.json["teamOneIGA"]
    umpire = request.json.get("official", None)
    scorer = request.json.get("scorer", None)
    manage_game.start_game(game_id, swap_serve, team_one, team_two,
                           first_is_iga,
                           umpire, scorer)
    return "", 204


@edit_games.post("/score")
@officials_only
def score():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        firstTeam: <bool> = if the team listed first scored
        leftPlayer: <bool> = if the player listed as left scored
    }
    """
    logger.info(f"Request for score: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    first_team = request.json["firstTeam"]
    left_player = request.json["leftPlayer"]
    score_method = request.json.get("method", None)
    manage_game.score_point(game_id, first_team, left_player, score_method)
    return "", 204


@edit_games.post("/ace")
@officials_only
def ace():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    logger.info(f"Request for ace: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.ace(game_id)
    return "", 204


@edit_games.post("/substitute")
@officials_only
def substitute():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        firstTeam: <bool> = if the team listed first is substituting
        leftPlayer: <bool> = if the player listed as left is leaving the court
    }
    """
    logger.info(f"Request for substitute: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    first_team = request.json["firstTeam"]
    first_player = request.json["leftPlayer"]
    manage_game.substitute(game_id, first_team, first_player)
    return "", 204


@edit_games.post("/pardon")
@umpire_manager_only
def pardon():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        firstTeam: <bool> = if the team listed first is being pardoned
        leftPlayer: <bool> = if the player listed as left is being pardoned
    }
    """
    logger.info(f"Request for pardon: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    first_team = request.json["firstTeam"]
    first_player = request.json["leftPlayer"]
    manage_game.pardon(game_id, first_team, first_player)
    return "", 204


@edit_games.post("/end")
@officials_only
def end():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        bestPlayer: <str> = the name of the best on ground
        notes: <str> (OPTIONAL) = any notes the umpire wishes to leave
        protestTeamOne: str = null if no protest, the reason if a protest is present
        protestTeamTwo: str = null if no protest, the reason if a protest is present
    }
    """
    logger.info(f"Request for end: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    best = request.json.get("bestPlayer", None)
    overall_notes = request.json.get("notes", '')
    team_one_rating = request.json["teamOneRating"]
    team_two_rating = request.json["teamTwoRating"]
    team_one_notes = request.json.get("teamOneNotes", '')
    team_two_notes = request.json.get("teamTwoNotes", '')
    protest_team_one = request.json.get("protestTeamOne", None)
    protest_team_two = request.json.get("protestTeamTwo", None)
    marked_for_review = request.json.get("markedForReview", False) == 'true'
    manage_game.end_game(game_id, best, team_one_rating, team_two_rating, overall_notes, protest_team_one,
                         protest_team_two, team_one_notes, team_two_notes, marked_for_review)
    return "", 204


@edit_games.post("/timeout")
@officials_only
def timeout():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        firstTeam: <bool> = if the team listed first called the timeout
    }
    """
    logger.info(f"Request for timeout: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    first_team = request.json["firstTeam"]
    manage_game.time_out(game_id, first_team)
    return "", 204


@edit_games.post("/forfeit")
@officials_only
def forfeit():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        firstTeam: <bool> = if the team listed first forfeited
    }
    """
    logger.info(f"Request for forfeit: {request.json}")
    first_team = request.json["firstTeam"]
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.forfeit(game_id, first_team)
    return "", 204


@edit_games.post("/endTimeout")
@officials_only
def end_timeout():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    logger.info(f"Request for end_timeout: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.end_timeout(game_id)
    return "", 204


@edit_games.post("/serveClock")
@officials_only
def serve_timer():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        start: <bool> = if the serve timer is starting or ending
    }
    """
    logger.info(f"Request for serve_clock: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.serve_timer(game_id, request.json['start'])
    return "", 204


@edit_games.post("/fault")
@officials_only
def fault():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    logger.info(f"Request for fault: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.fault(game_id)
    return "", 204


@edit_games.post("/officialTimeout")
@officials_only
def official_timeout():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    logger.info(f"Request for Official Timeout: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.official_timeout(game_id)
    return "", 204


@edit_games.post("/undo")
@officials_only
def undo():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    logger.info(f"Request for undo: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    override = fetch_user().is_umpire_manager
    manage_game.undo(game_id, override)
    return "", 204


@edit_games.post("/delete")
@officials_only
def delete():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
    }
    """
    logger.info(f"Request for delete: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    override = fetch_user().is_umpire_manager
    manage_game.delete(game_id, override)
    return "", 204


@edit_games.post("/card")
@officials_only
def card():
    """
    SCHEMA:
    {
        id: <int> = id of the current game
        firstTeam: <bool> = if the team listed first received the card
        leftPlayer: <bool> = if the player listed as left received the card
        color: <str> = the type of card ("Red", "Yellow", "Green", "Warning")
        duration: <int> = the amount of rounds the card carries
        reason: <str> = the reason for the card
    }
    """
    logger.info(f"Request for card: {request.json}")
    color = request.json["color"]
    first_team = request.json["firstTeam"]
    left_player = request.json["leftPlayer"]
    game_id = Games.game_number_to_id(int(request.json["id"]))
    duration = request.json["duration"]
    reason = request.json["reason"]
    manage_game.card(game_id, first_team, left_player, color, duration, reason)
    return "", 204


@edit_games.post("/resolve")
@umpire_manager_only
def resolve():
    """
    SCHEMA:
    {
        id: <int> = id of the game to resolve
    }
    """
    logger.info(f"Request for resolve: {request.json}")
    game_id = Games.game_number_to_id(int(request.json["id"]))
    manage_game.resolve_game(game_id)
    return "", 204


@edit_games.post("/create")
@officials_only
def create():
    """
    SCHEMA:
    {
        tournament: str = the searchable name of the tournament
        teamOne: str = the searchable name of the first team, or the name of the team to be created if players is populated
        teamTwo: str = the searchable name of the second team, or the name of the team to be created if players is populated
        official: str = the searchable name of the official (used to change officials)
        scorer: str (OPTIONAL) = the searchable name of the scorer (used to change scorer)
        playersOne: list[str] (OPTIONAL) = the list of players' true name on team one if the game is created by players
        playersTwo: list[str] (OPTIONAL) = the list of players' true name on team two if the game is created by players
    }
    """
    logger.info(request.json)
    gid = manage_game.create_game(request.json["tournament"], request.json.get("teamOne", ''),
                                  request.json.get("teamTwo", ''),
                                  request.json["official"], request.json.get("playersOne", None),
                                  request.json.get("playersTwo", None))
    return jsonify({"id": gid})
