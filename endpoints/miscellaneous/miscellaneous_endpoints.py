from datetime import datetime

import pytz
from flask import send_file, request

from database.models.QOTD import QOTD
from endpoints.miscellaneous.blueprint import miscellaneous
from utils.logging_handler import logger


@miscellaneous.get("/qotd")
def qotd():
    quotes = QOTD.query.all()
    day = ((datetime.now(pytz.timezone('Australia/Perth')) - datetime.fromtimestamp(0, pytz.utc)).days)
    return quotes[day % len(quotes)].as_dict()


@miscellaneous.get("/image")
def image():
    team = request.args.get("name", type=str)
    big = request.args.get("big", type=bool)
    return send_file(f"./resources/images{'/big' if big else ''}/{team}.png", mimetype="image/png")


# testing related endpoints
@miscellaneous.get("/mirror")
def mirror():
    logger.info(f"Request for score: {request.args}")
    d = dict(request.args)
    if not d:
        d = {
            "All these webs on me": " You think I'm Spiderman",
            "Shout out": "martin luther King",
            "this is the sound of a robot": "ELELALAELE-BING-ALELILLELALE",
        }
    return str(d), 200
