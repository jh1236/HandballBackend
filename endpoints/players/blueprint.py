from flask import Blueprint

players = Blueprint('players', __name__, url_prefix='/players')
user = Blueprint('users', __name__)
