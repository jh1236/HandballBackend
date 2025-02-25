from flask import Blueprint

edit_games = Blueprint('edit games', __name__, url_prefix='/update')
games = Blueprint('games', __name__, url_prefix='/games')
games.register_blueprint(edit_games)
