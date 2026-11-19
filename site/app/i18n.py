"""Translations: every visible text lives in i18n/<lang>.json, never in the pages."""
import json
import threading

from . import config

_lock = threading.Lock()
_cache = {}


def load(lang):
    with _lock:
        if lang not in _cache:
            path = config.I18N_DIR / (lang + ".json")
            with open(path, encoding="utf-8") as f:
                _cache[lang] = json.load(f)
        return _cache[lang]


def reload():
    with _lock:
        _cache.clear()


def translate(lang, key, **kw):
    table = load(lang)
    text = table.get(key)
    if text is None and lang != "fr":
        text = load("fr").get(key)
    if text is None:
        return key  # visible hint of a missing translation; tests catch these
    text = text.replace("{site_name}", config.SITE_NAME)      # the name of the site comes from the settings, not from the texts
    if kw:
        try:
            return text.format(**kw)
        except (KeyError, IndexError):
            return text
    return text


def has(lang, key):
    return key in load(lang)
