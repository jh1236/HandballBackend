from datetime import datetime
import random

import pytz
from flask import send_file

from database.models.QOTD import QOTD


def init_api(app):
    from website.endpoints.endpoints import add_endpoints

    add_endpoints(app)

    @app.get("/api/qotd")
    def qotd():
        quotes = QOTD.query.all()
        day = ((datetime.now(pytz.timezone('Australia/Perth')) - datetime.fromtimestamp(0, pytz.utc)).days)
        return quotes[day % len(quotes)].as_dict()

    @app.get("/robots.txt")
    def robots():
        return send_file("./resources/robots.txt")

    @app.get("/favicon.ico/")
    def icon():
        return send_file("./static/favicon.ico")


def sign(elo_delta):
    if elo_delta >= 0:
        return "+"
    else:
        return "-"
