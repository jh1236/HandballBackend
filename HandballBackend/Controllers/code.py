@user.post("/login/")
def login():
    """
    SCHEMA:
    {
        userId: <int> = id of the user attempting to log in
        password: <str> = password of the user attempting to log in
    }
    """
    user_id = request.json.get("userId")
    password = request.json.get("password")
    long_session = request.json.get("longSession", False)
    if permissions.check_password(user_id, password):
        token = permissions.get_token(user_id, password, long_session)
        user = People.query.filter_by(id=user_id).first()
        # set cookie to token
        response = jsonify({"token": token, 'username': user.name, "permissionLevel": user.permission_level})
        response.set_cookie('token', token)
        response.set_cookie('userID', user_id)
        return response
    return "Incorrect Details", 403


@logged_in_only
@user.get("/logout/")
def logout():
    """
    SCHEMA:
    {
        userId: <int> = id of the user attempting to log in
        password: <str> = password of the user attempting to log in
    }
    """
    permissions.logout()
    return "", 204