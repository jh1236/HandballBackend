import flask

from database import db
import endpoints
from utils.args_handler import args
from utils.logging_handler import logger

if not args.debug:
    from flask_minify import Minify
    from waitress import create_server
    from utils.permissions import admin_only

logger.setLevel(args.log)

MY_ADDRESS = 'https://api.squarers.club'

app = flask.Flask(__name__)
app.config["DEBUG"] = args.debug and False
app.config["SQLALCHEMY_DATABASE_URI"] = args.database
app.config['SECRET_KEY'] = 'secret!'
app.config['EXIT_CODE'] = 1  # 0 = stop server, 1 = fatal error or restart, 2 = update and restart server

db.init_app(app)
app.register_blueprint(endpoints.api_blueprint)

if __name__ == "__main__":
    with app.app_context():
        db.create_all()

    port = (80 if args.debug else 5001) if args.port == -1 else args.port


    @app.after_request
    def cors_fixer(response):
        response.headers.add("Access-Control-Allow-Origin", "*")
        response.headers.add("Access-Control-Allow-Headers", "Authorization,content-type")

        return response


    if args.debug:
        app.run(host="0.0.0.0", port=port)

    else:
        Minify(app=app, html=True, js=True, cssless=True)


        @app.get("/api/stop")
        @admin_only
        def stop_server():
            # by default restart the server because we don't want it to have downtime
            app.config['EXIT_CODE'] = int(flask.request.args.get("exit_code", 1))
            exit_reasons = {0: "stop", 1: "restart", 2: "update and restart server", 3: "running test.py",
                            4: "commit and push server"}
            logger.important(
                f"User requested server to stop, exit code: {app.config['EXIT_CODE']}: {exit_reasons[app.config['EXIT_CODE']]}\nThe error below is expected and can be ignored")
            server.close()
            return "Stopping server", 200  # there is like a 50% chance this will not be returned, and the server will just close without sending a message to the client. whoopsie


        logger.info("Starting server...")
        server = create_server(app, host="0.0.0.0", port=port)
        server.run()

    logger.info(f"The server has closed, exit code: {app.config['EXIT_CODE']}")
    exit(app.config['EXIT_CODE'])  # return the exit code to the shell
