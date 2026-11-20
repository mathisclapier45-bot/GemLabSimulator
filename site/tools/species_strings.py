#!/usr/bin/env python3
"""Lists every French text of the species data that the English pages must translate (data/en/species.json maps each one to its English).

  python3 site/tools/species_strings.py            prints the texts still missing from data/en/species.json
  python3 site/tools/species_strings.py --all      prints every text
"""
import json
import sys
from pathlib import Path

DATA = Path(__file__).resolve().parent.parent / "data"
SPECIES_FIELDS = ("displayName", "description", "luster", "color", "transparency", "pleochroicDescription", "fluorescenceLongWave",
                  "fluorescenceShortWave", "opticalCharacter", "absorptionSpectrum", "inclusions", "interferenceFigure",
                  "chelseaFilterReaction", "magnetism")


def _load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def collect():
    """{French text: group}, in a stable order."""
    raw = _load(DATA / "gemlab_data.json")
    out = {}

    def add(text, group):
        if isinstance(text, str) and text.strip():
            out.setdefault(text, group)

    extra_path = DATA / "extra_species.json"
    if extra_path.exists():                       # species of the site that the game data does not have (yet)
        known = {s["id"] for s in raw["species"]}
        added = [e for e in _load(extra_path)["species"] if e["id"] not in known]
        raw["species"] = list(raw["species"]) + added
        for table, field in (("fluorescence", "fluoEntry"), ("spectra", "spectraEntry"), ("chelsea", "chelseaEntry")):     # their own tables
            raw[table]["entries"] = list(raw[table]["entries"]) + [dict(e[field], key=e["id"]) for e in added if e.get(field)]
    for s in raw["species"]:
        for f in SPECIES_FIELDS:
            add(s.get(f), f)
        for o in s.get("origins", []):
            add(o.get("name"), "origin")
    pv_path = DATA / "pleochroism_variants.json"
    if pv_path.exists():
        for rows in _load(pv_path).values():
            for row in rows:
                add(row["label"], "pleochroism")
                add(row["text"], "pleochroism")
    for e in raw["spectra"]["entries"]:
        add(e.get("description"), "spectrum")
        add(e.get("label"), "spectrum")
    for e in added if extra_path.exists() else []:
        add((e.get("spectraEntry") or {}).get("label"), "spectrum")
        for v in e.get("spectraVariants", []):
            add(v.get("description"), "spectrum")
            add(v.get("label"), "spectrum")
    for e in raw["fluorescence"]["entries"]:
        add(e.get("longText"), "fluorescence")
        add(e.get("shortText"), "fluorescence")
    for e in raw["chelsea"]["entries"]:
        add(e.get("reaction"), "chelsea")
    for h in raw["crystal_habits"]:
        for k in ("name", "short", "description"):
            add(h.get(k), "habit")

    def walk(x):
        if isinstance(x, list):
            if len(x) > 6 and isinstance(x[6], str) and isinstance(x[0], (int, float)):
                add(x[6], "colour")
                if len(x) > 10:
                    add(x[10], "colour")
            else:
                for y in x:
                    walk(y)
        elif isinstance(x, dict):
            for y in x.values():
                walk(y)
    walk(_load(DATA / "palettes.json"))
    for e in _load(DATA / "species_extra.json").values():
        add(e.get("note"), "note")
    return out


def main():
    texts = collect()
    done = {}
    path = DATA / "en" / "species.json"
    if path.exists():
        done = _load(path)
    show_all = "--all" in sys.argv
    missing = [t for t in texts if t not in done]
    todo = list(texts) if show_all else missing
    for t in todo:
        print("[{}] {}".format(texts[t], t))
    print("\n{} texts, {} translated, {} missing{}".format(len(texts), len(texts) - len(missing), len(missing),
          "; {} translations no longer match a French text".format(len([k for k in done if k not in texts])) if done else ""), file=sys.stderr)
    return 1 if missing and not show_all else 0


if __name__ == "__main__":
    sys.exit(main())
