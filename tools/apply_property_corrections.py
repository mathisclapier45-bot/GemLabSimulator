#!/usr/bin/env python3
"""Applique les corrections de propriétés (Docs/corrections_proprietes.json) aux données du jeu.

  python3 tools/apply_property_corrections.py                 montre ce qui changerait (rien n'est écrit)
  python3 tools/apply_property_corrections.py --write         écrit dans le jeu
  python3 tools/apply_property_corrections.py --also D1       applique aussi ces corrections facultatives (apply=false)
  python3 tools/apply_property_corrections.py --out DOSSIER   écrit les fichiers modifiés dans ce dossier (pour vérifier), sans toucher au jeu

Trois types de correction :
  asset_value     une valeur numérique d'un fichier GemLabSimulator/Assets/Data/Gems/<espèce>.asset (la valeur actuelle doit être exactement « old »)
  asset_text      un morceau de texte d'un .asset (écrit comme dans le YAML : \\xE9 pour é)
  spectrum_entry  une entrée de Assets/Resources/Spectra/spectra.json (créée ou remplacée)
Les notes (type « note ») ne changent rien : ce sont des décisions ou des points à confirmer.
Ensuite : python3 tools/export_game_data.py ; pour le site, python3 site/tools/sync_data.py ; les nouveaux textes français sont à traduire
(python3 site/tools/species_strings.py les liste)."""
import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "GemLabSimulator" / "Assets"
FIXES = ROOT / "Docs" / "corrections_proprietes.json"
SPECTRA = ASSETS / "Resources" / "Spectra" / "spectra.json"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--also", nargs="*", default=[])
    ap.add_argument("--out")
    args = ap.parse_args()
    fixes = json.loads(FIXES.read_text(encoding="utf-8"))["corrections"]
    files = {}           # chemin relatif -> contenu modifié

    def read(rel):
        if rel not in files:
            files[rel] = (ASSETS / rel).read_text(encoding="utf-8")
        return files[rel]

    changed = 0
    for fix in fixes:
        if not fix["apply"] and fix["id"] not in args.also:
            print("[ignorée] {} {} ({}) : {}".format(fix["id"], fix["espece"], fix["niveau"], fix["pourquoi"][:80]))
            continue
        kind = fix["type"]
        if kind == "note":
            print("[note]    {} {} : {}".format(fix["id"], fix["espece"], fix["pourquoi"][:100]))
        elif kind == "asset_value":
            text = read(fix["file"])
            pattern = re.compile(r"^(  {}: ){}$".format(re.escape(fix["field"]), re.escape(fix["old"])), re.M)
            if len(pattern.findall(text)) != 1:
                print("[ERREUR]  {} : {} n'a pas la valeur {} dans {} (déjà corrigé ?)".format(fix["id"], fix["field"], fix["old"], fix["file"]))
                continue
            files[fix["file"]] = pattern.sub(lambda m: m.group(1) + fix["new"], text)
            print("[modifiée] {} {} : {} {} -> {}".format(fix["id"], fix["espece"], fix["field"], fix["old"], fix["new"]))
            changed += 1
        elif kind == "asset_text":
            text = read(fix["file"])
            if text.count(fix["old"]) != 1:
                print("[ERREUR]  {} : le texte à remplacer n'est pas trouvé une fois dans {}".format(fix["id"], fix["file"]))
                continue
            files[fix["file"]] = text.replace(fix["old"], fix["new"])
            print("[modifiée] {} {} : texte remplacé".format(fix["id"], fix["espece"]))
            changed += 1
        elif kind == "spectrum_entry":
            rel = "Resources/Spectra/spectra.json"
            table = json.loads(read(rel))
            entry = fix["entry"]
            keys = [e["key"] for e in table["entries"]]
            if entry["key"] in keys:
                table["entries"][keys.index(entry["key"])] = entry
                print("[remplacée] {} spectre {}".format(fix["id"], entry["key"]))
            else:
                table["entries"].append(entry)
                print("[créée]   {} spectre {}".format(fix["id"], entry["key"]))
            files[rel] = json.dumps(table, indent=1, ensure_ascii=False)       # même mise en forme que le fichier d'origine
            changed += 1
        else:
            sys.exit("Type inconnu : " + kind)
    print("\n{} correction(s) à appliquer.".format(changed))
    if args.out:
        for rel, text in files.items():
            dest = Path(args.out) / rel
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_text(text, encoding="utf-8")
        print("Fichiers écrits dans", args.out)
    elif args.write:
        for rel, text in files.items():
            (ASSETS / rel).write_text(text, encoding="utf-8")
            print("Écrit :", ASSETS / rel)
    else:
        print("(simulation : rien n'est écrit, ajoute --write pour appliquer)")


if __name__ == "__main__":
    main()
