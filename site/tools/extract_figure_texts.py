#!/usr/bin/env python3
"""Lists the texts written in the animated drawings of the lessons (static/js/lessonfig*.js), to translate them.

  python3 site/tools/extract_figure_texts.py            writes static/i18n/figures.template.json  { "texte français": "" }
  python3 site/tools/extract_figure_texts.py --check    only checks that the template is up to date (exit code 1 if not)

To make an English version of the drawings: copy figures.template.json to figures.en.json and fill in the values. The page loads
/static/i18n/figures.<lang>.json when <html lang> is not "fr"; a text without translation stays in French."""
import json
import re
import sys
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
FILES = ["lessonfig.js", "lessonfig_figs.js", "lessonfig_figs2.js"]
TEMPLATE = SITE / "static" / "i18n" / "figures.template.json"

# Strings of the code that are keys or names, not words shown to the reader.
NOT_TEXT = {"Emeraude", "Peridot", "Grenat_Almandin", "Corindon_Rubis", "ct", "nm", "DOMContentLoaded", "IntersectionObserver", "use strict"}
LITERAL = re.compile(r'"((?:[^"\\\n]|\\.)*)"')
LABEL_ARG = re.compile(r'label\(\s*"((?:[^"\\\n]|\\.)*)"')   # the first text given to a label is always a text, even a lowercase word



def strip_comment(line):
    """The line without its // comment (a // inside a string is kept)."""
    quote = False
    i = 0
    while i < len(line):
        c = line[i]
        if c == "\\" and quote:
            i += 2
            continue
        if c == '"':
            quote = not quote
        elif c == "/" and not quote and line[i:i + 2] == "//":
            return line[:i]
        i += 1
    return line


def looks_like_text(s):
    if len(s.strip()) < 2 or s.strip() in NOT_TEXT or s in NOT_TEXT:
        return False
    if re.fullmatch(r"[a-z0-9_\-:.,/ ]+", s) and " " not in s:      # identifiers, ids, css words
        return False
    if re.fullmatch(r"[A-Za-z]+_[A-Za-z_]+", s):                     # names such as Corindon_Rubis
        return False
    if any(k in s for k in ("rgba(", "sans-serif", "px ", "#", "system-ui", "uni_", "bi_", "prefers-")):
        return False
    return bool(re.search(r"[A-Za-zÀ-ÿ]", s))


def figure_texts():
    found = []
    for name in FILES:
        for line in (SITE / "static" / "js" / name).read_text(encoding="utf-8").splitlines():
            code = strip_comment(line)
            direct = {m.group(1) for m in LABEL_ARG.finditer(code)}
            for m in LITERAL.finditer(code):
                try:
                    s = json.loads('"' + m.group(1) + '"')
                except ValueError:
                    continue
                if (looks_like_text(s) or (m.group(1) in direct and s.strip() not in NOT_TEXT and len(s.strip()) >= 3)) and s not in found:
                    found.append(s)
    return sorted(found)


def main(argv):
    texts = figure_texts()
    data = {s: "" for s in texts}
    if "--check" in argv:
        have = json.loads(TEMPLATE.read_text(encoding="utf-8")) if TEMPLATE.exists() else {}
        if list(have) != texts:
            print("figures.template.json n'est plus à jour : relancer sans --check.")
            return 1
        print("ok : {} textes".format(len(texts)))
        return 0
    TEMPLATE.parent.mkdir(parents=True, exist_ok=True)
    TEMPLATE.write_text(json.dumps(data, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    print("{} textes écrits dans {}".format(len(texts), TEMPLATE))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
