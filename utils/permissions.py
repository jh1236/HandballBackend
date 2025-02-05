import secrets
import time

import bcrypt
from flask import redirect, request, render_template

from database import db
from database.models.People import People



def encrypt(password):
    salt = bcrypt.gensalt()
    pw = bytes(password, 'utf-8')
    hashed_password = bcrypt.hashpw(pw, salt)
    return hashed_password


def set_password(person_id, password):
    """ args: person_id:int, password
    Sets the password for the person with the given id"""
    password = encrypt(password)
    People.query.filter(People.id == person_id).first().password = password
    db.session.commit()


def check_password(person_id, password):
    """ args: person_id:int, password
    Returns True if the password is correct, otherwise returns False"""
    hashed_password = People.query.filter(People.id == person_id).first().password
    return bcrypt.checkpw(bytes(password, "utf-8"), hashed_password)


def get_time():
    return int(time.time())


def reset_token(person_id):
    person = People.query.filter(People.id == person_id).first()
    person.session_token = None
    db.session.commit()


def get_token(person_id, password):
    """ args: person_id:int, password
    Returns the token if the password is correct, otherwise returns False
    If the token already exists and is not expired, it will return that token"""
    if check_password(person_id, password):
        person = People.query.filter(People.id == person_id).first()
        # if they have no token, or their token has expired, give them a new one
        if not person.session_token or person.token_timeout < get_time():
            # session_token = f"WhereDidYouComeFrom.{get_time()}.WhyAreYouLookingAtMe.{secrets.token_urlsafe(16)}.PleaseImNotWearingAnyClothes"
            session_token = f"{get_time()}{secrets.token_urlsafe(16)}"
            person.session_token = session_token
        person.token_timeout = get_time() + 60 * 60 * 24 * 7  # 1 week
        db.session.commit()
        return person.session_token
    return False


def check_valid_token(token):
    if not token: return None
    person = People.query.filter(People.session_token == token).first()
    return person and person.token_timeout > get_time()


def logout():
    resp = redirect("/")
    resp.delete_cookie("token")
    resp.delete_cookie("userID")
    resp.delete_cookie("userKey")
    resp.delete_cookie("userName")
    return resp


def fetch_token():
    auth = request.headers.get("Authorization")
    if auth:
        return auth.split("Bearer ")[1]
    return request.cookies.get("token", None)


def fetch_user():
    if check_valid_token((token := fetch_token())):
        return People.query.filter(People.session_token == token).first()
    return None


def fetch_user_name():
    user = fetch_user()
    return user.name if user else None


def admin_only(func):
    def inner(*args, **kwargs):

        user = fetch_user()
        if not user:
            return "This page requires authentication.", 401
        if user.is_admin:
            return func(*args, **kwargs)

        return "Insufficient Permissions", 401

    inner.__name__ = func.__name__  # changing name of inner function so flask acts nicely <3
    return inner


def officials_only(func):
    def inner(*args, **kwargs):
        user = fetch_user()
        if not user:
            return "This page requires authentication.", 401

        return func(*args, **kwargs)

    inner.__name__ = func.__name__  # changing name of inner function so flask acts nicely <3
    return inner


def user_on_mobile() -> bool:
    user_agent = request.headers.get("User-Agent")
    user_agent = user_agent.lower()
    phones = ["android", "iphone"]

    if any(x in user_agent for x in phones):
        return True
    return False
