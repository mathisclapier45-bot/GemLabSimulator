"""Site settings. Everything can be overridden with GEMLAB_* environment variables."""
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def env(name, default=None):
    return os.environ.get("GEMLAB_" + name, default)


# "dev" = local testing (test mailbox page, relaxed cookies). "prod" = real server.
ENV = env("ENV", "dev")
DEV = ENV != "prod"

# The name of the site, shown everywhere (header, titles, mails, legal pages, share image). Change it here or with GEMLAB_SITE_NAME.
SITE_NAME = env("SITE_NAME", "GemXP")
DEFAULT_LANG = env("LANG", "fr")
BASE_URL = env("BASE_URL", "http://127.0.0.1:8000").rstrip("/")

# Search engines: off by default (the site is private until it is ready). INDEXABLE=1 opens the public pages to Google (see README).
INDEXABLE = env("INDEXABLE", "0") == "1"

# Who runs the site (legal notice, privacy policy, contact). Filled in through the environment; empty = shown as "to complete".
LEGAL_FIELDS = {
    "name": "PUBLISHER_NAME",             # person or company that publishes the site
    "status": "PUBLISHER_STATUS",         # e.g. micro-entreprise, association, particulier
    "address": "PUBLISHER_ADDRESS",
    "director": "PUBLISHER_DIRECTOR",     # director of the publication
    "email": "CONTACT_EMAIL",
    "host_name": "HOST_NAME",
    "host_address": "HOST_ADDRESS",
    "host_phone": "HOST_PHONE",
}


def legal(field):
    return (env(LEGAL_FIELDS[field], "") or "").strip()


DATA_DIR = ROOT / "data"
I18N_DIR = ROOT / "i18n"
STATIC_DIR = ROOT / "static"
SIM_DIR = Path(env("SIM_DIR", str(ROOT / "private" / "simulateur")))
VAR_DIR = Path(env("VAR_DIR", str(ROOT / "var")))
DB_PATH = Path(env("DB_PATH", str(VAR_DIR / "gemlab.db")))

# Cookies: "Secure" means the browser only sends them over https. Must be on in production.
COOKIE_SECURE = env("COOKIE_SECURE", "0" if DEV else "1") == "1"
# Only trust the X-Forwarded-For header when a reverse proxy we control sits in front.
TRUST_PROXY = env("TRUST_PROXY", "0") == "1"

# Mail: "file" writes mails to var/outbox (and the /dev/courriels page); "smtp" really sends them.
MAIL_MODE = env("MAIL_MODE", "file" if DEV else "smtp")
MAIL_FROM = env("MAIL_FROM", SITE_NAME + " <no-reply@example.invalid>")
SMTP_HOST = env("SMTP_HOST", "")
SMTP_PORT = int(env("SMTP_PORT", "587"))
SMTP_USER = env("SMTP_USER", "")
SMTP_PASSWORD = env("SMTP_PASSWORD", "")

PBKDF2_ITERATIONS = int(env("PBKDF2_ITERATIONS", "600000"))
SESSION_DAYS = 30
VERIFY_TOKEN_HOURS = 48
RESET_TOKEN_HOURS = 1

MAX_BODY_BYTES = 64 * 1024
USERNAME_RE = r"[A-Za-z0-9_-]{3,24}"
PASSWORD_MIN = 10
PASSWORD_MAX = 200

# Forum
FORUM_CATEGORIES = ["bienvenue", "identification", "instruments", "simulateur", "libre"]
TITLE_MIN, TITLE_MAX = 3, 120
POST_MIN, POST_MAX = 2, 8000
PAGE_SIZE = 20

# Anti-spam limits: (max events, window in seconds)
LIMITS = {
    "register_ip": (5, 3600),
    "login_ip": (20, 900),
    "login_email": (8, 900),
    "mail_ip": (6, 3600),
    "mail_email": (3, 3600),
    "post_user_burst": (1, 15),
    "post_user_hour": (10, 3600),
    "topic_user_hour": (3, 3600),
    "report_user_day": (15, 86400),
    "price_user_day": (10, 86400),
}
NEW_ACCOUNT_SECONDS = 3 * 86400
NEW_ACCOUNT_MAX_LINKS = 1
MAX_LINKS = 4

SIM_FILES = {
    "WebCours.loader.js": "application/javascript",
    "WebCours.framework.js.gz": "application/javascript",
    "WebCours.data.gz": "application/octet-stream",
    "WebCours.wasm.gz": "application/wasm",
}
