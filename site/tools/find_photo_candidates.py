#!/usr/bin/env python3
"""Looks on Wikimedia Commons for freely licensed photos of each species. Downloads NOTHING: it only reads
the list of candidates (title, author, licence, address) and writes data/photo_candidates.json.
A person must look at them and approve; then tools/fetch_photos.py downloads the approved ones.

  python3 site/tools/find_photo_candidates.py
"""
import json
import re
import sys
import time
import urllib.parse
import urllib.request
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
API = "https://commons.wikimedia.org/w/api.php"
OK_LICENSES = re.compile(r"^(cc0|cc[- ]by(?!-nc)(?!.*nd)|cc[- ]by[- ]sa|public domain|pd)", re.I)
BAD = re.compile(r"nc|nd|no known|fair use", re.I)
UA = "GemLabSiteBot/0.1 (local research tool)"


def query(term):
    params = {"action": "query", "format": "json", "generator": "search", "gsrnamespace": "6", "gsrsearch": term,
              "gsrlimit": "8", "prop": "imageinfo", "iiprop": "url|extmetadata|size|mime", "iiurlwidth": "480"}
    req = urllib.request.Request(API + "?" + urllib.parse.urlencode(params), headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.load(r)


def clean(html):
    return re.sub(r"\s+", " ", re.sub(r"<[^>]+>", "", html or "")).strip()


def main():
    data = json.load(open(SITE / "data" / "gemlab_data.json", encoding="utf-8"))
    out = {}
    for s in data["species"]:
        name = re.sub(r"\s*\(.*?\)", "", s["displayName"]).strip()
        term = "{} gemme OR gemstone OR gem OR cristal".format(name)
        pages = None
        for attempt in range(5):
            try:
                pages = (query(term).get("query") or {}).get("pages", {})
                break
            except Exception as exc:   # too many requests: wait and try again
                print("!", s["id"], exc, "(attempt {})".format(attempt + 1), file=sys.stderr)
                time.sleep(20 * (attempt + 1))
        if pages is None:
            continue
        cands = []
        for p in sorted(pages.values(), key=lambda x: x.get("index", 99)):
            info = (p.get("imageinfo") or [{}])[0]
            meta = info.get("extmetadata", {})
            lic = clean(meta.get("LicenseShortName", {}).get("value"))
            if not lic or BAD.search(lic) or not OK_LICENSES.match(lic) or not info.get("mime", "").startswith("image/jpeg"):
                continue
            if info.get("width", 0) < 600:
                continue
            cands.append({"title": p["title"], "author": clean(meta.get("Artist", {}).get("value"))[:120] or "?",
                          "license": lic, "page": info.get("descriptionurl"), "thumb": info.get("thumburl"),
                          "file_url": info.get("url"), "description": clean(meta.get("ImageDescription", {}).get("value"))[:160]})
        out[s["id"]] = {"name": s["displayName"], "slug": s["id"].lower().replace("_", "-"), "candidates": cands[:3]}
        print("{:32s} {} candidat(s)".format(s["id"], len(cands[:3])))
        time.sleep(2.0)
    (SITE / "data" / "photo_candidates.json").write_text(json.dumps(out, ensure_ascii=False, indent=1), encoding="utf-8")
    print("Écrit : data/photo_candidates.json")


if __name__ == "__main__":
    main()
