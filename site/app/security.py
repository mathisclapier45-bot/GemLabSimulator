"""Passwords, sessions, one-time tokens, anti-spam counters."""
import base64
import hashlib
import hmac
import secrets

from . import config
from .db import now


# ------------------------------------------------------------------ passwords

def hash_password(password):
    iterations = config.PBKDF2_ITERATIONS
    salt = secrets.token_bytes(16)
    digest = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, iterations)
    return "pbkdf2_sha256${}${}${}".format(
        iterations, base64.b64encode(salt).decode(), base64.b64encode(digest).decode())


def verify_password(password, stored):
    try:
        algo, iterations, salt, digest = stored.split("$")
        if algo != "pbkdf2_sha256":
            return False
        calc = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"),
                                   base64.b64decode(salt), int(iterations))
        return hmac.compare_digest(calc, base64.b64decode(digest))
    except (ValueError, TypeError):
        return False


# Used when the e-mail is unknown, so that the answer takes as long as for a known one.
_DUMMY = None


def dummy_verify(password):
    global _DUMMY
    if _DUMMY is None:
        _DUMMY = hash_password("dummy-password-for-timing")
    verify_password(password, _DUMMY)


# ------------------------------------------------------------------ random tokens

def _digest(raw):
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()


def new_secret():
    return secrets.token_urlsafe(32)


# ------------------------------------------------------------------ sessions

def create_session(conn, user_id):
    raw = new_secret()
    t = now()
    conn.execute("INSERT INTO sessions (token_hash, user_id, created_at, expires_at) VALUES (?,?,?,?)",
                 (_digest(raw), user_id, t, t + config.SESSION_DAYS * 86400))
    return raw


def user_for_session(conn, raw):
    if not raw:
        return None
    return conn.execute(
        "SELECT u.* FROM sessions s JOIN users u ON u.id = s.user_id "
        "WHERE s.token_hash = ? AND s.expires_at > ? AND u.banned_at IS NULL",
        (_digest(raw), now())).fetchone()


def delete_session(conn, raw):
    if raw:
        conn.execute("DELETE FROM sessions WHERE token_hash = ?", (_digest(raw),))


def delete_user_sessions(conn, user_id):
    conn.execute("DELETE FROM sessions WHERE user_id = ?", (user_id,))


# ------------------------------------------------------------------ one-time tokens (e-mail check, password reset)

def create_token(conn, user_id, kind, hours):
    raw = new_secret()
    conn.execute("DELETE FROM tokens WHERE user_id = ? AND kind = ?", (user_id, kind))
    conn.execute("INSERT INTO tokens (token_hash, user_id, kind, expires_at) VALUES (?,?,?,?)",
                 (_digest(raw), user_id, kind, now() + hours * 3600))
    return raw


def peek_token(conn, raw, kind):
    """Returns the user id if the token is valid, without using it."""
    if not raw:
        return None
    row = conn.execute(
        "SELECT user_id FROM tokens WHERE token_hash = ? AND kind = ? AND used_at IS NULL AND expires_at > ?",
        (_digest(raw), kind, now())).fetchone()
    return row["user_id"] if row else None


def consume_token(conn, raw, kind):
    """Returns the user id and marks the token as used (it works only once)."""
    user_id = peek_token(conn, raw, kind)
    if user_id is None:
        return None
    cur = conn.execute("UPDATE tokens SET used_at = ? WHERE token_hash = ? AND used_at IS NULL",
                       (now(), _digest(raw)))
    return user_id if cur.rowcount == 1 else None


# ------------------------------------------------------------------ rate limits

def over_limit(conn, name, who):
    """True when `who` already did too many `name` actions in the time window."""
    limit, window = config.LIMITS[name]
    row = conn.execute("SELECT COUNT(*) AS n FROM rate_events WHERE key = ? AND ts > ?",
                       ("{}:{}".format(name, who), now() - window)).fetchone()
    return row["n"] >= limit


def record(conn, name, who):
    conn.execute("INSERT INTO rate_events (key, ts) VALUES (?, ?)", ("{}:{}".format(name, who), now()))
    # Light cleanup of old counters.
    conn.execute("DELETE FROM rate_events WHERE ts < ?", (now() - 2 * 86400,))


def hit(conn, name, who):
    """Records an action. Returns False (and records nothing) when the limit is already reached."""
    if over_limit(conn, name, who):
        return False
    record(conn, name, who)
    return True
