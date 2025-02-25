from flask import send_file

from endpoints.documents.blueprint import documents


@documents.get("/rules")
def rules():
    return send_file("./resources/documents/pdf/rules.pdf"), 200


@documents.get("/simplified_rules")
def simple_rules():
    return send_file("./resources/documents/pdf/rules_simple.pdf"), 200


@documents.get("/code_of_conduct/")
def code_of_conduct():
    return send_file("./resources/documents/pdf/code_of_conduct.pdf"), 200


@documents.get("/tournament_regulations/")
def regulations():
    return send_file("./resources/documents/pdf/tournament_regulations.pdf"), 200
