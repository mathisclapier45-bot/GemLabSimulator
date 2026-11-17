"""Entry point for a real server (gunicorn, waitress, uWSGI...):  gunicorn wsgi:application"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from app import main  # noqa: E402

main.init()
application = main.application
