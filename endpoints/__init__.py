from flask import Blueprint

from endpoints.documents import documents
from endpoints.game import games
from endpoints.miscellaneous import miscellaneous
from endpoints.officials import officials
from endpoints.players import players, user
from endpoints.teams import teams
from endpoints.tournaments import tournaments

api_blueprint = Blueprint('api', __name__, url_prefix='/api')

api_blueprint.register_blueprint(tournaments)
api_blueprint.register_blueprint(teams)
api_blueprint.register_blueprint(miscellaneous)
api_blueprint.register_blueprint(games)
api_blueprint.register_blueprint(documents)
api_blueprint.register_blueprint(players)
api_blueprint.register_blueprint(user)
api_blueprint.register_blueprint(officials)
