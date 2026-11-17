#!/usr/bin/env python3
"""Copies the compiled simulator into the site (private folder, never public).

  python3 site/tools/sync_simulator.py                 copies GemLabSimulator/Builds/WebCours/Build/*
  python3 site/tools/sync_simulator.py /autre/dossier  copies from another Build folder

The source is only read. The 4 files are replaced all at once, so a visitor never gets a half-updated mix.
"""
import shutil
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
REPO = SITE.parent
DEFAULT_SRC = REPO / "GemLabSimulator" / "Builds" / "WebCours" / "Build"
DST = SITE / "private" / "simulateur"
FILES = ["WebCours.loader.js", "WebCours.framework.js.gz", "WebCours.data.gz", "WebCours.wasm.gz"]


def main():
    src = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_SRC
    missing = [f for f in FILES if not (src / f).is_file()]
    if missing:
        sys.exit("Fichiers manquants dans {} : {}\nLance d'abord BuildCourseWeb dans Unity.".format(src, ", ".join(missing)))
    DST.mkdir(parents=True, exist_ok=True)
    tmp = DST.parent / ".simulateur_new"
    if tmp.exists():
        shutil.rmtree(tmp)
    tmp.mkdir()
    total = 0
    for f in FILES:
        shutil.copyfile(src / f, tmp / f)
        total += (tmp / f).stat().st_size
    old = DST.parent / ".simulateur_old"
    if old.exists():
        shutil.rmtree(old)
    if DST.exists():
        DST.rename(old)
    tmp.rename(DST)
    if old.exists():
        shutil.rmtree(old)
    print("Simulateur copié dans {} ({:.1f} Mo).".format(DST, total / 1e6))
    print("Rien à redémarrer : le site sert directement les nouveaux fichiers.")


if __name__ == "__main__":
    main()
