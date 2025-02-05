from datetime import datetime
import random

from flask import send_file

from database.models.QOTD import QOTD


def init_api(app):
    from website.endpoints.endpoints import add_endpoints

    add_endpoints(app)

    @app.get("/api/qotd")
    def qotd():
        quotes = QOTD.query.all()
        day = ((datetime.today() - datetime.fromtimestamp(0)).days)
        return quotes[day % len(quotes)].as_dict()

    @app.get("/robots.txt")
    def robots():
        return send_file("./resources/robots.txt")

    @app.get("/rules/current")
    def rules():
        return send_file("./resources/documents/pdf/rules.pdf"), 200

    @app.get("/rules/simple")
    def simple_rules():
        return send_file("./resources/documents/pdf/rules_simple.pdf"), 200

    @app.get("/rules/proposed")
    def new_rules():
        return send_file("./resources/documents/pdf/proposed_rules.pdf"), 200

    @app.get("/code_of_conduct/")
    def code_of_conduct():
        rand = random.Random()
        if rand.randrange(1, 10):
            return send_file("./resources/documents/pdf/code_of_conduct_2.pdf"), 200
        return send_file("./resources/documents/pdf/code_of_conduct.pdf"), 200

    @app.get("/favicon.ico/")
    def icon():
        return send_file("./static/favicon.ico")


def sign(elo_delta):
    if elo_delta >= 0:
        return "+"
    else:
        return "-"
