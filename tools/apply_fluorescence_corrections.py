#!/usr/bin/env python3
"""Applique les corrections de fluorescence (Docs/corrections_fluorescence.json) à la table du jeu
(GemLabSimulator/Assets/Resources/Fluorescence/fluorescence.json).

  python3 tools/apply_fluorescence_corrections.py                 montre ce qui changerait (rien n'est écrit)
  python3 tools/apply_fluorescence_corrections.py --write         écrit la table du jeu
  python3 tools/apply_fluorescence_corrections.py --also C1 C3    applique aussi ces corrections facultatives (apply=false)
  python3 tools/apply_fluorescence_corrections.py --out x.json    écrit le résultat ailleurs (pour vérifier)

Une entrée qui existe est mise à jour champ par champ ; une entrée absente est créée. Ensuite : python3 tools/export_game_data.py
puis, pour le site, python3 site/tools/sync_data.py ; les textes anglais des fiches sont à compléter avec site/tools/species_strings.py."""
import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TABLE = ROOT / "GemLabSimulator" / "Assets" / "Resources" / "Fluorescence" / "fluorescence.json"
FIXES = ROOT / "Docs" / "corrections_fluorescence.json"
FIELDS = ("longText", "longColor", "longIntensity", "shortText", "shortColor", "shortIntensity", "phosphorescent")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--also", nargs="*", default=[], help="numéros des corrections facultatives à appliquer aussi")
    ap.add_argument("--out")
    args = ap.parse_args()
    table = json.loads(TABLE.read_text(encoding="utf-8"))
    entries = table["entries"]
    by_key = {e["key"]: e for e in entries}
    fixes = json.loads(FIXES.read_text(encoding="utf-8"))["corrections"]
    changed = 0
    for fix in fixes:
        if not fix["apply"] and fix["id"] not in args.also:
            print("[ignorée] {} {} ({}) : {}".format(fix["id"], fix["espece"], fix["niveau"], fix["pourquoi"][:70]))
            continue
        new = fix["entry"]
        old = by_key.get(new["key"])
        if old is None:
            missing = [f for f in FIELDS if f not in new]
            if missing:
                sys.exit("Entrée nouvelle {} incomplète : {}".format(new["key"], missing))
            entries.append(dict(new))
            by_key[new["key"]] = entries[-1]
            print("[créée]   {} {}".format(fix["id"], new["key"]))
            changed += 1
            continue
        diffs = [(k, old.get(k), v) for k, v in new.items() if k != "key" and old.get(k) != v]
        for k, a, b in diffs:
            old[k] = b
        if diffs:
            changed += 1
            print("[modifiée] {} {}".format(fix["id"], new["key"]))
            for k, a, b in diffs:
                print("     {:15s} {!r}  ->  {!r}".format(k, a, b))
    print("\n{} entrée(s) changée(s).".format(changed))
    text = json.dumps(table, indent=1, ensure_ascii=False)          # même mise en forme que le fichier d'origine (pas de retour à la ligne final)
    if args.out:
        Path(args.out).write_text(text, encoding="utf-8")
        print("Écrit dans", args.out)
    elif args.write:
        TABLE.write_text(text, encoding="utf-8")
        print("Table du jeu mise à jour :", TABLE)
    else:
        print("(simulation : rien n'est écrit, ajoute --write pour appliquer)")


if __name__ == "__main__":
    main()
