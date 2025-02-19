from flask import send_file


def add_document_endpoints(app):
    @app.get("/api/documents/rules")
    def rules():
        return send_file("./resources/documents/pdf/rules.pdf"), 200

    @app.get("/api/documents/simplified_rules")
    def simple_rules():
        return send_file("./resources/documents/pdf/rules_simple.pdf"), 200

    @app.get("/api/documents/code_of_conduct/")
    def code_of_conduct():
        return send_file("./resources/documents/pdf/code_of_conduct.pdf"), 200
