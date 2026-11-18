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

_lessons = {"mtime": None, "data": None}


def lessons():
    files = sorted((config.DATA_DIR / "lessons").glob("*.json")) + sorted((config.DATA_DIR / "lessons_extra").glob("*.json"))
    mtime = max([f.stat().st_mtime for f in files] or [0])
    with _lock:
        if _lessons["mtime"] != mtime:
            found = {}
            for f in files:
                with open(f, encoding="utf-8") as fh:
                    item = json.load(fh)
                found[item["id"]] = item
            ordered = [found[i] for i in LESSON_ORDER if i in found]
            ordered += [found[i] for i in sorted(found) if i not in LESSON_ORDER]
            _lessons["data"] = ordered
            _lessons["mtime"] = mtime
        return _lessons["data"]


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
