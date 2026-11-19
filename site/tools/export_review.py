#!/usr/bin/env python3
"""Writes a reading file for an expert (a gemmologist): every quiz question with its right answer and explanation, the glossary,
and the pleochroism colours as the site draws them (read from the text of the data, so approximate).

  python3 site/tools/export_review.py            writes Docs/RELECTURE_CONTENU.md (and prints where)

The expert only has to read and note what is wrong: no need to run the site."""
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(SITE))

from app import catalog, diagrams  # noqa: E402

OUT = SITE.parent / "Docs" / "RELECTURE_CONTENU.md"
LETTERS = "ABCD"


def quizzes():
    out = ["## 1. Les quiz ({} questions)\n".format(sum(len(catalog.quiz(it["id"]) or []) for it in catalog.lessons())),
           "Pour chaque question : la bonne réponse est marquée **✔**. Dire si la question est claire, si la réponse est exacte, si les autres choix sont bien faux.\n"]
    for item in catalog.lessons():
        bank = catalog.quiz(item["id"])
        if not bank:
            continue
        out.append("### {} (`{}`)\n".format(item["title"], item["id"]))
        for n, q in enumerate(bank, 1):
            out.append("**{}. {}**\n".format(n, q["q"]))
            for k, choice in enumerate(q["choices"]):
                out.append("- {} {}{}".format(LETTERS[k], choice, "  **✔**" if k == q["answer"] else ""))
            out.append("\n*Explication :* {}\n".format(q["why"]))
            out.append("> Avis de l'expert : ☐ correct   ☐ à corriger : ........................................\n")
    return out


def glossary():
    terms = catalog.glossary()[0]
    out = ["## 2. Le glossaire ({} mots)\n".format(len(terms)),
           "Dire si chaque définition est exacte et assez simple pour un débutant.\n"]
    for term in terms:
        also = " — aussi : {}".format(", ".join(term["aliases"])) if term.get("aliases") else ""
        out.append("**{}**{}\n\n{}\n".format(term["term"], also, term["def"]))
        out.append("> Avis de l'expert : ☐ correct   ☐ à corriger : ........................................\n")
    return out


def pleochroism():
    species = [s for s in catalog.data()["species"] if s.get("pleochroic")]
    out = ["## 3. Le pléochroïsme ({} espèces)\n".format(len(species)),
           "Le site dessine, dans les deux fenêtres du dichroscope, les couleurs lues dans le texte de la colonne « Texte des données ». "
           "Ce sont des **approximations** : la colonne « Couleurs dessinées » donne ce que le programme a compris (code couleur) et la force indiquée.\n",
           "| Espèce | Texte des données | Couleurs dessinées | Force |", "|---|---|---|---|"]
    for s in species:
        text = s.get("pleochroicDescription") or ""
        colors = ", ".join("{} `{}`".format(name, hexa) for name, hexa in diagrams.pleochroism_colors(text)) or "(aucune couleur reconnue)"
        out.append("| {} | {} | {} | {} |".format(s["displayName"], text.replace("|", "/"), colors.replace("|", "/"), diagrams.pleo_strength(text) or "—"))
    out.append("\nÀ vérifier : les couleurs dessinées correspondent-elles à ce qu'on voit réellement au dichroscope ? La force (faible, modérée, forte) est-elle juste ?\n")
    return out


def main():
    lines = ["# Relecture du contenu (pour un gemmologue)\n",
             "Ce document rassemble ce que le site affiche et qui doit être vérifié par quelqu'un qui connaît la gemmologie. "
             "Les textes des leçons viennent du jeu ; **les quiz, le glossaire et les couleurs de pléochroïsme sont plus récents et n'ont pas été relus par un spécialiste.**\n",
             "Merci de noter, pour chaque point : correct, ou ce qu'il faut changer. Un retour par mail avec le numéro de la question suffit.\n"]
    lines += quizzes() + glossary() + pleochroism()
    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("Écrit :", OUT)


if __name__ == "__main__":
    main()
