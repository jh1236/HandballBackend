"""Defines the comments object and provides functions to get and manipulate one"""
import time

from database import db


# create table main.eloChange
# (
#     id           INTEGER
# primary key autoincrement,
# gameId       INTEGER references main.games,
# playerId     INTEGER references main.people,
# tournamentId INTEGER references main.tournaments,
# eloChange    INTEGER
# );


class QOTD(db.Model):
    __tablename__ = "quoteOfTheDay"

    # Auto-initialised fields
    id = db.Column(db.Integer(), primary_key=True)
    author = db.Column(db.Text(), nullable=False)
    quote = db.Column(db.Text(), nullable=False)

    def as_dict(self):
        return {
            "id": self.id,
            "author": self.author,
            "quote": self.quote
        }
