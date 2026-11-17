"""Forum rules, in one place. Every write goes through these functions, which decide
whether `user` is allowed to do it (this plays the role of row-level security: the
browser can never reach the database any other way).

Each function returns None when it worked, or an error key (translated by the page)."""
from . import config, markup, security
from .db import now


class Denied(Exception):
    def __init__(self, key, status=403):
        Exception.__init__(self, key)
        self.key = key
        self.status = status


def is_admin(user):
    return bool(user) and user["role"] == "admin"


def require_member(user):
    if not user:
        raise Denied("err.login_required", 403)
    return user


def require_verified(user):
    require_member(user)
    if not user["email_verified_at"]:
        raise Denied("err.unverified", 403)
    return user


def require_admin(user):
    require_member(user)
    if not is_admin(user):
        raise Denied("err.admin_only", 403)
    return user


# ------------------------------------------------------------------ reading

def categories_overview(conn):
    rows = conn.execute(
        "SELECT category, COUNT(*) AS topics, MAX(last_post_at) AS last FROM topics "
        "WHERE deleted = 0 GROUP BY category").fetchall()
    stats = {r["category"]: r for r in rows}
    return [(slug, stats.get(slug)) for slug in config.FORUM_CATEGORIES]


def list_topics(conn, category, page):
    total = conn.execute("SELECT COUNT(*) AS n FROM topics WHERE category = ? AND deleted = 0", (category,)).fetchone()["n"]
    rows = conn.execute(
        "SELECT t.*, u.username FROM topics t JOIN users u ON u.id = t.author_id "
        "WHERE t.category = ? AND t.deleted = 0 ORDER BY t.pinned DESC, t.last_post_at DESC LIMIT ? OFFSET ?",
        (category, config.PAGE_SIZE, (page - 1) * config.PAGE_SIZE)).fetchall()
    return rows, total


def get_topic(conn, topic_id):
    return conn.execute("SELECT t.*, u.username FROM topics t JOIN users u ON u.id = t.author_id "
                        "WHERE t.id = ? AND t.deleted = 0", (topic_id,)).fetchone()


def list_posts(conn, topic_id, page):
    total = conn.execute("SELECT COUNT(*) AS n FROM posts WHERE topic_id = ?", (topic_id,)).fetchone()["n"]
    rows = conn.execute(
        "SELECT p.*, u.username, u.role FROM posts p JOIN users u ON u.id = p.author_id "
        "WHERE p.topic_id = ? ORDER BY p.id LIMIT ? OFFSET ?",
        (topic_id, config.PAGE_SIZE, (page - 1) * config.PAGE_SIZE)).fetchall()
    return rows, total


# ------------------------------------------------------------------ anti-spam checks

def _check_text(user, text, minimum, maximum):
    text = text.strip()
    if len(text) < minimum:
        raise Denied("err.too_short", 400)
    if len(text) > maximum:
        raise Denied("err.too_long", 400)
    links = markup.count_links(text)
    age = now() - user["created_at"]
    if links > config.MAX_LINKS or (age < config.NEW_ACCOUNT_SECONDS and links > config.NEW_ACCOUNT_MAX_LINKS):
        raise Denied("err.too_many_links", 400)
    return text


def _check_post_limits(conn, user):
    if security.over_limit(conn, "post_user_burst", user["id"]):
        raise Denied("err.rate_burst", 429)
    if security.over_limit(conn, "post_user_hour", user["id"]):
        raise Denied("err.rate_hour", 429)


def _record_post(conn, user):
    security.record(conn, "post_user_burst", user["id"])
    security.record(conn, "post_user_hour", user["id"])


# ------------------------------------------------------------------ writing

def create_topic(conn, user, category, title, body):
    require_verified(user)
    if category not in config.FORUM_CATEGORIES:
        raise Denied("err.not_found", 404)
    title = _check_text(user, title, config.TITLE_MIN, config.TITLE_MAX).replace("\n", " ")
    body = _check_text(user, body, config.POST_MIN, config.POST_MAX)
    _check_post_limits(conn, user)
    if security.over_limit(conn, "topic_user_hour", user["id"]):
        raise Denied("err.rate_topics", 429)
    t = now()
    cur = conn.execute(
        "INSERT INTO topics (category, author_id, title, created_at, last_post_at, post_count) VALUES (?,?,?,?,?,1)",
        (category, user["id"], title, t, t))
    topic_id = cur.lastrowid
    conn.execute("INSERT INTO posts (topic_id, author_id, body, created_at) VALUES (?,?,?,?)",
                 (topic_id, user["id"], body, t))
    _record_post(conn, user)
    security.record(conn, "topic_user_hour", user["id"])
    return topic_id


def reply(conn, user, topic_id, body):
    require_verified(user)
    topic = get_topic(conn, topic_id)
    if not topic:
        raise Denied("err.not_found", 404)
    if topic["locked"] and not is_admin(user):
        raise Denied("err.locked", 403)
    body = _check_text(user, body, config.POST_MIN, config.POST_MAX)
    last = conn.execute("SELECT body FROM posts WHERE author_id = ? ORDER BY id DESC LIMIT 1", (user["id"],)).fetchone()
    if last and last["body"] == body:
        raise Denied("err.duplicate", 400)
    _check_post_limits(conn, user)
    t = now()
    cur = conn.execute("INSERT INTO posts (topic_id, author_id, body, created_at) VALUES (?,?,?,?)",
                       (topic_id, user["id"], body, t))
    conn.execute("UPDATE topics SET last_post_at = ?, post_count = post_count + 1 WHERE id = ?", (t, topic_id))
    _record_post(conn, user)
    return cur.lastrowid


def report_post(conn, user, post_id, reason):
    require_verified(user)
    post = conn.execute("SELECT p.id FROM posts p JOIN topics t ON t.id = p.topic_id "
                        "WHERE p.id = ? AND p.deleted = 0 AND t.deleted = 0", (post_id,)).fetchone()
    if not post:
        raise Denied("err.not_found", 404)
    reason = reason.strip()[:300]
    if len(reason) < 3:
        raise Denied("err.too_short", 400)
    if security.over_limit(conn, "report_user_day", user["id"]):
        raise Denied("err.rate_hour", 429)
    security.record(conn, "report_user_day", user["id"])
    conn.execute("INSERT OR IGNORE INTO reports (post_id, reporter_id, reason, created_at) VALUES (?,?,?,?)",
                 (post_id, user["id"], reason, now()))


# ------------------------------------------------------------------ moderation (admins only)

def set_flag(conn, user, topic_id, flag, value):
    require_admin(user)
    if flag not in ("pinned", "locked"):
        raise Denied("err.not_found", 404)
    conn.execute("UPDATE topics SET {} = ? WHERE id = ?".format(flag), (1 if value else 0, topic_id))


def delete_topic(conn, user, topic_id):
    require_admin(user)
    conn.execute("UPDATE topics SET deleted = 1 WHERE id = ?", (topic_id,))


def delete_post(conn, user, post_id):
    require_admin(user)
    row = conn.execute("SELECT topic_id, (SELECT MIN(id) FROM posts WHERE topic_id = p.topic_id) AS first_id "
                       "FROM posts p WHERE id = ?", (post_id,)).fetchone()
    if not row:
        raise Denied("err.not_found", 404)
    conn.execute("UPDATE posts SET deleted = 1 WHERE id = ?", (post_id,))
    conn.execute("UPDATE reports SET handled_at = ?, handled_by = ? WHERE post_id = ? AND handled_at IS NULL",
                 (now(), user["id"], post_id))
    if row["first_id"] == post_id:   # deleting the first message deletes the whole topic
        conn.execute("UPDATE topics SET deleted = 1 WHERE id = ?", (row["topic_id"],))
    return row["topic_id"]


def open_reports(conn):
    return conn.execute(
        "SELECT r.id, r.reason, r.created_at, p.id AS post_id, p.body, p.topic_id, p.author_id, "
        "a.username AS author, a.banned_at, rp.username AS reporter, t.title "
        "FROM reports r JOIN posts p ON p.id = r.post_id JOIN topics t ON t.id = p.topic_id "
        "JOIN users a ON a.id = p.author_id JOIN users rp ON rp.id = r.reporter_id "
        "WHERE r.handled_at IS NULL AND p.deleted = 0 ORDER BY r.created_at").fetchall()


def dismiss_report(conn, user, report_id):
    require_admin(user)
    conn.execute("UPDATE reports SET handled_at = ?, handled_by = ? WHERE id = ?", (now(), user["id"], report_id))


def ban_user(conn, user, target_id, banned=True):
    require_admin(user)
    target = conn.execute("SELECT * FROM users WHERE id = ?", (target_id,)).fetchone()
    if not target:
        raise Denied("err.not_found", 404)
    if target["role"] == "admin":
        raise Denied("err.admin_only", 403)
    conn.execute("UPDATE users SET banned_at = ? WHERE id = ?", (now() if banned else None, target_id))
    if banned:
        security.delete_user_sessions(conn, target_id)
