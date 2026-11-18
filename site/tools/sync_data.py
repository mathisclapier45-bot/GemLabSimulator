#!/usr/bin/env python3
"""Copies the game data into the site (encyclopedia + lessons). Reads the game folder, never changes it.

  python3 site/tools/sync_data.py            copy what the game currently has
  python3 site/tools/sync_data.py --export   first re-run tools/export_game_data.py (needs the game files)
"""
import json
import shutil
import subprocess
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
REPO = SITE.parent
SRC_JSON = REPO / "Docs" / "gemlab_data.json"
SRC_LESSONS = REPO / "GemLabSimulator" / "Assets" / "Resources" / "Lessons"
DST_JSON = SITE / "data" / "gemlab_data.json"
DST_LESSONS = SITE / "data" / "lessons"


def main():
    if "--export" in sys.argv:
        print("Export des données du jeu...")
        subprocess.check_call([sys.executable, str(REPO / "tools" / "export_game_data.py")])
    if not SRC_JSON.exists():
        sys.exit("Introuvable : {}\nLance d'abord : python3 tools/export_game_data.py".format(SRC_JSON))
    with open(SRC_JSON, encoding="utf-8") as f:
        data = json.load(f)
    DST_JSON.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(SRC_JSON, DST_JSON)
    print("Espèces : {} (données du {})".format(len(data["species"]), data["meta"].get("generated")))

    # The spectra used by the animated lesson drawings (static/js/lessonfig_figs.js): rubis, émeraude, péridot, almandin.
    keep = ("Corindon_Rubis", "Emeraude", "Peridot", "Grenat_Almandin")
    spectra = {e["key"]: e["lines"] for e in data["spectra"]["entries"] if e["key"] in keep}
    with open(SITE / "static" / "data" / "lesson_spectra.json", "w", encoding="utf-8") as f:
        json.dump(spectra, f, ensure_ascii=False, separators=(",", ":"))

    DST_LESSONS.mkdir(parents=True, exist_ok=True)
    for old in DST_LESSONS.glob("*.json"):
        old.unlink()
    count = 0
    for src in sorted(SRC_LESSONS.glob("*.json")):
        shutil.copyfile(src, DST_LESSONS / src.name)
        count += 1
    print("Leçons : {}".format(count))
    print("Terminé. Le site relit les fichiers tout seul, pas besoin de le redémarrer.")


if __name__ == "__main__":
    main()
