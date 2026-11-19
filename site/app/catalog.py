"""Encyclopedia and lessons, read from data/gemlab_data.json and data/lessons/*.json
(copied there by tools/sync_data.py)."""
import json
import re
import threading
import unicodedata

from . import config

_lock = threading.Lock()
_state = {"mtime": None, "data": None}

LESSON_ORDER = ["gemmes", "durete", "tenacite", "stabilite", "systemes_cristallins", "loupe",
                "balance_carats", "balance_hydrostatique", "refractometre", "polariscope",
                "dichroscope", "spectroscope", "filtre_chelsea", "cabine_uv",
                # Lessons written for the site only (instruments and topics the game has no lesson for), in data/lessons_extra
                "microscope", "micrometre", "aimant", "traitements", "testeurs_diamant", "infrarouge_raman",
                "origine_geographique", "diamant_laboratoire"]


# ------------------------------------------------------------------ language of the page

_ctx = threading.local()


def set_lang(lang):
    """Set by the server at the start of each request: lessons, quizzes and glossary are then read in that language."""
    _ctx.lang = lang if lang in ("fr", "en") else "fr"


def current_lang():
    return getattr(_ctx, "lang", "fr")


# Made by people, though not flagged "synthetic" in the data: these are not natural stones.
ARTIFICIAL = {"CZ", "GGG", "YAG", "Titanate_Strontium", "Verre", "Moissanite_Synthetique"}


def natural_species(species):
    return [s for s in species if not s["synthetic"] and s["id"] not in ARTIFICIAL]


def fold(text):
    """Lower case, no accents: 'Béryl' and 'beryl' match each other."""
    nfd = unicodedata.normalize("NFD", text or "")
    return "".join(c for c in nfd if unicodedata.category(c) != "Mn").lower()


def slug_of(species_id):
    return species_id.lower().replace("_", "-")


def _first_word(text):
    return fold(text).split(" ")[0] if text else ""


def _build():
    path = config.DATA_DIR / "gemlab_data.json"
    with open(path, encoding="utf-8") as f:
        raw = json.load(f)
    species = []
    for s in raw["species"]:
        s = dict(s)
        s["slug"] = slug_of(s["id"])
        s["transparency_group"] = _first_word(s.get("transparency", ""))
        h = s.get("hardness") or 0
        s["hardness_group"] = "soft" if h < 5 else ("medium" if h <= 7 else "hard")
        s["synthetic"] = bool(s.get("forcedSynthetic")) or "synth" in fold(s["id"])
        s["search_text"] = fold(" ".join([
            s["displayName"], s["id"].replace("_", " "), s.get("description", ""), s.get("color", ""),
            s.get("inclusions", "")]))
        s["name_key"] = fold(s["displayName"])
        species.append(s)
    species.sort(key=lambda s: s["name_key"])

    def by_key(block):
        return {e["key"]: e for e in raw[block]["entries"] if not e.get("variant")}

    extra_path = config.DATA_DIR / "species_extra.json"
    extra = {}
    if extra_path.exists():
        with open(extra_path, encoding="utf-8") as f:
            extra = json.load(f)
    credits_path = config.DATA_DIR / "photo_credits.json"
    credits = {}
    if credits_path.exists():
        with open(credits_path, encoding="utf-8") as f:
            credits = json.load(f)
    pal_path = config.DATA_DIR / "palettes.json"
    palettes = {}
    if pal_path.exists():
        with open(pal_path, encoding="utf-8") as f:
            palettes = json.load(f)
    for s in species:
        s["palette"] = palettes.get(s["id"])
        s["extra"] = extra.get(s["id"], {})
        photo = credits.get(s["slug"])
        s["photo"] = photo if photo and (config.STATIC_DIR / "photos" / photo["file"]).is_file() else None

    habits = {h["id"]: h for h in raw["crystal_habits"]}
    for s in species:
        s["habit_list"] = [habits[h["habit"]] for h in s.get("habits", []) if h["habit"] in habits]
        s["has_spectrum"] = False
    spectra = by_key("spectra")
    for s in species:
        s["has_spectrum"] = s["id"] in spectra and bool(spectra[s["id"]].get("lines"))
    habit_species = {}
    for s in species:
        for h in s["habit_list"]:
            habit_species.setdefault(h["id"], []).append(s)

    return {
        "meta": raw["meta"],
        "species": species,
        "by_slug": {s["slug"]: s for s in species},
        "habits": raw["crystal_habits"],
        "habit_species": habit_species,
        "spectra": spectra,
        "scale_bands": raw["spectra"].get("scale_bands", []),
        "chelsea": by_key("chelsea"),
        "fluorescence": by_key("fluorescence"),
    }


def data():
    """Loaded once, and again whenever the JSON file changes (so a data update needs no restart)."""
    path = config.DATA_DIR / "gemlab_data.json"
    mtime = tuple(p.stat().st_mtime for p in (path, config.DATA_DIR / "species_extra.json", config.DATA_DIR / "photo_credits.json", config.DATA_DIR / "palettes.json") if p.exists())
    with _lock:
        if _state["mtime"] != mtime:
            _state["data"] = _build()
            _state["mtime"] = mtime
        return _state["data"]


def search(q="", transparency="", optic="", hardness="", extra="", synthetic=""):
    d = data()
    words = fold(q).split()
    out = []
    for s in d["species"]:
        if words and not all(w in s["search_text"] for w in words):
            continue
        if transparency and s["transparency_group"] != transparency:
            continue
        if optic and s.get("opticalCharacterClassName") != optic:
            continue
        if hardness and s["hardness_group"] != hardness:
            continue
        if extra == "pleochroic" and not s.get("pleochroic"):
            continue
        if extra == "spectrum" and not s["has_spectrum"]:
            continue
        if extra == "fluorescent" and s["id"] not in d["fluorescence"]:
            continue
        if extra == "habit" and not s["habit_list"]:
            continue
        if synthetic == "yes" and not s["synthetic"]:
            continue
        if synthetic == "no" and s["synthetic"]:
            continue
        out.append(s)
    return out


# ------------------------------------------------------------------ lessons

_lessons = {}        # language -> {"mtime": ..., "data": [...]}


def _read_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def _overlay_lesson(item, over):
    """The lesson with the texts of its translation (same structure: titles, texts of the steps, card of the instrument)."""
    new = dict(item)
    new["title"] = over.get("title", item["title"])
    if over.get("tool"):
        tool = dict(item.get("tool") or {})
        tool.update(over["tool"])
        new["tool"] = tool
    steps = []
    translated = over.get("steps") or []
    for i, step in enumerate(item["steps"]):
        s = dict(step)
        o = translated[i] if i < len(translated) else {}
        s["title"] = o.get("title", step["title"])
        s["text"] = o.get("text", step["text"])
        steps.append(s)
    new["steps"] = steps
    return new


def lessons():
    lang = current_lang()
    files = sorted((config.DATA_DIR / "lessons").glob("*.json")) + sorted((config.DATA_DIR / "lessons_extra").glob("*.json"))
    over_files = sorted((config.DATA_DIR / lang / "lessons").glob("*.json")) if lang != "fr" else []
    mtime = max([f.stat().st_mtime for f in files + over_files] or [0])
    with _lock:
        cache = _lessons.get(lang)
        if not cache or cache["mtime"] != mtime:
            found = {}
            for f in files:
                item = _read_json(f)
                found[item["id"]] = item
            for f in over_files:
                over = _read_json(f)
                if over["id"] in found:
                    found[over["id"]] = _overlay_lesson(found[over["id"]], over)
            ordered = [found[i] for i in LESSON_ORDER if i in found]
            ordered += [found[i] for i in sorted(found) if i not in LESSON_ORDER]
            cache = _lessons[lang] = {"mtime": mtime, "data": ordered}
        return cache["data"]


def lesson(lesson_id):
    for item in lessons():
        if item["id"] == lesson_id:
            return item
    return None


# Dispersion of the game (GemLook.DispersionOf): half the spread of the index between red and blue light, known for the fiery stones,
# otherwise estimated from the refractive index. The site shows the whole spread (twice that), as the tables of gemmology do.
FIERY_HALF = {"Diamant": .022, "Diamant_Synthetique_CVD": .022, "Diamant_Synthetique_HPHT": .022, "CZ": .030, "Moissanite_Synthetique": .052,
              "YAG": .014, "GGG": .022, "Zircon": .020, "Grenat_Demantoide": .036, "Sphene": .026, "Sphalerite": .078, "Titanate_Strontium": .095}


def dispersion(species):
    """(dispersion, is_known): the spread of the index between blue and red light; is_known is False when estimated."""
    sid = species["id"]
    if sid in FIERY_HALF:
        return FIERY_HALF[sid] * 2, True
    ri = species.get("refractiveIndex") or 0
    ior = ri if ri > 1.2 else 1.6
    return (0.004 + max(0.0, ior - 1.4) * 0.012) * 2, False


SAFE_ID = re.compile(r"^[a-z0-9_-]+$")


# ------------------------------------------------------------------ glossary and quizzes

_glossary = {}       # language -> {"mtime": ..., "data": (...)}
_quizzes = {}        # (language, lesson id) -> {"mtime": ..., "data": [...]}


def glossary():
    """(terms sorted for the page, dict id -> term, compiled regex of every written form, dict lower form -> id)."""
    lang = current_lang()
    path = config.DATA_DIR / "glossary.json"
    over_path = config.DATA_DIR / lang / "glossary.json" if lang != "fr" else None
    mtime = (path.stat().st_mtime if path.exists() else 0) + (over_path.stat().st_mtime if over_path and over_path.exists() else 0)
    with _lock:
        cache = _glossary.get(lang)
        if not cache or cache["mtime"] != mtime:
            terms = _read_json(path) if path.exists() else []
            if over_path and over_path.exists():
                over = {o["id"]: o for o in _read_json(over_path)}
                terms = [dict(t, **{k: over[t["id"]][k] for k in ("term", "aliases", "def") if k in over[t["id"]]}) if t["id"] in over else t for t in terms]
            by_id = {t["id"]: t for t in terms}
            forms = {}
            for t in terms:
                for form in [t["term"]] + t.get("aliases", []):
                    forms[form.lower()] = t["id"]
            pattern = None
            if forms:
                alternatives = "|".join(re.escape(f) for f in sorted(forms, key=len, reverse=True))
                pattern = re.compile(r"(?<![\w-])(" + alternatives + r")(?![\w-])", re.IGNORECASE)
            cache = _glossary[lang] = {"mtime": mtime, "data": (sorted(terms, key=lambda t: fold(t["term"])), by_id, pattern, forms)}
        return cache["data"]


def quiz(lesson_id):
    """The questions of a lesson (data/quizzes/<id>.json, translated by data/<lang>/quizzes/<id>.json), or None.
    A translation changes the texts only: the right answer stays the one of the French file."""
    if not SAFE_ID.match(lesson_id):
        return None
    lang = current_lang()
    path = config.DATA_DIR / "quizzes" / (lesson_id + ".json")
    if not path.exists():
        return None
    over_path = config.DATA_DIR / lang / "quizzes" / (lesson_id + ".json") if lang != "fr" else None
    mtime = path.stat().st_mtime + (over_path.stat().st_mtime if over_path and over_path.exists() else 0)
    cache = _quizzes.get((lang, lesson_id))
    if cache and cache["mtime"] == mtime:
        return cache["data"]
    questions = _read_json(path)["questions"]
    if over_path and over_path.exists():
        over = _read_json(over_path)["questions"]
        merged = []
        for i, q in enumerate(questions):
            o = over[i] if i < len(over) else {}
            ok = len(o.get("choices", [])) == len(q["choices"])
            merged.append({"q": o.get("q", q["q"]), "choices": o["choices"] if ok else q["choices"], "answer": q["answer"], "why": o.get("why", q["why"])})
        questions = merged
    _quizzes[(lang, lesson_id)] = {"mtime": mtime, "data": questions}
    return questions


# ------------------------------------------------------------------ search by data (identification)

COLOR_FAMILIES = {
    "red": r"rouge|rubis", "pink": r"rose", "orange": r"orang", "yellow": r"jaun|dor", "green": r"vert",
    "blue": r"bleu", "violet": r"violet|pourpr|mauve|lavande", "brown": r"brun|marron|cafe|beige", "black": r"noir",
    "gray": r"gris", "colorless": r"incolore|blanc|ivoire", "multi": r"multicolore|bande|bicolore",
}
LUSTER_FAMILIES = {"vitreous": "vitreux", "adamantine": "adamantin", "resinous": "resineux", "waxy": "cireux",
                   "metallic": "metallique", "pearly": "nacre", "greasy": "gras", "silky": "soyeux", "dull": "mat"}
NUMERIC = (("ri", "refractiveIndex", 0.01), ("density", "density", 0.1), ("birefringence", "birefringence", 0.005))


def _num(text):
    try:
        return float(str(text).replace(",", ".").strip())
    except ValueError:
        return None


def identify(q):
    """Species compatible with every value filled in, closest first. `q` is a dict of strings.
    Returns a list of (species, score, notes)."""
    d = data()
    wanted = {}
    for key, field, default_tol in NUMERIC:
        v = _num(q.get(key, ""))
        if v is not None:
            tol = _num(q.get(key + "_tol", "")) or default_tol
            wanted[key] = (v, tol, field)
    h_from, h_to = _num(q.get("hardness_from", "")), _num(q.get("hardness_to", ""))
    results = []
    for s in d["species"]:
        score, notes, ok = 0.0, [], True
        for key, (v, tol, field) in wanted.items():
            have = s.get(field)
            if have is None or (key == "birefringence" and not have and v > tol):
                ok = False
                break
            gap = abs(have - v)
            if gap > tol + 1e-9:
                ok = False
                break
            score += gap / tol
            notes.append(key)
        if not ok:
            continue
        h = s.get("hardness") or 0
        if (h_from is not None and h < h_from) or (h_to is not None and h > h_to):
            continue
        if q.get("optic") and s.get("opticalCharacterClassName") != q["optic"]:
            continue
        pleo = q.get("pleo")
        if pleo == "yes" and not s.get("pleochroic"):
            continue
        if pleo == "no" and s.get("pleochroic"):
            continue
        if q.get("transparency") and s["transparency_group"] != q["transparency"]:
            continue
        fam = q.get("color")
        if fam in COLOR_FAMILIES and not re.search(COLOR_FAMILIES[fam], fold(s.get("color", ""))):
            continue
        lus = q.get("luster")
        if lus in LUSTER_FAMILIES and LUSTER_FAMILIES[lus] not in fold(s.get("luster", "")):
            continue
        fl = d["fluorescence"].get(s["id"]) or {}
        glows = max(fl.get("longIntensity", 0), fl.get("shortIntensity", 0)) >= 0.15
        if q.get("uv") == "yes" and not glows:
            continue
        if q.get("uv") == "no" and glows:
            continue
        ch = d["chelsea"].get(s["id"]) or {}
        if q.get("chelsea") == "red" and ch.get("intensity", 0) < 0.5:
            continue
        if q.get("chelsea") == "none" and ch.get("intensity", 0) >= 0.5:
            continue
        if q.get("spectrum") == "yes" and not s["has_spectrum"]:
            continue
        if q.get("spectrum") == "no" and s["has_spectrum"]:
            continue
        if q.get("thermal") == "yes" and not s.get("isThermallyConductiveLikeDiamond"):
            continue
        if q.get("magnet") == "yes" and "aucune reaction" in fold(s.get("magnetism", "")):
            continue
        if q.get("magnet") == "no" and "aucune reaction" not in fold(s.get("magnetism", "")):
            continue
        if q.get("synthetic") == "yes" and not s["synthetic"]:
            continue
        if q.get("synthetic") == "no" and s["synthetic"]:
            continue
        results.append((s, score, notes))
    results.sort(key=lambda r: (r[1], r[0]["name_key"]))
    return results
