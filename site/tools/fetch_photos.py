#!/usr/bin/env python3
"""Downloads the photos you approved (and only those) from Wikimedia Commons into static/photos/,
and records the author and licence in data/photo_credits.json (shown under each photo on the site).

  python3 site/tools/fetch_photos.py corindon-rubis=1 emeraude=2 diamant=1
        (slug=number of the candidate, as shown on the page /dev/photos)
"""
import json
import sys
import time
import urllib.request
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
UA = "GemLabSiteBot/0.1 (local research tool)"


def main(args):
    cands = json.load(open(SITE / "data" / "photo_candidates.json", encoding="utf-8"))
    by_slug = {v["slug"]: v for v in cands.values()}
    cred_path = SITE / "data" / "photo_credits.json"
    credits = json.load(open(cred_path, encoding="utf-8")) if cred_path.exists() else {}
    (SITE / "static" / "photos").mkdir(parents=True, exist_ok=True)
    for arg in args:
        slug, _, num = arg.partition("=")
        item = by_slug.get(slug)
        if not item or not num.isdigit() or not 1 <= int(num) <= len(item["candidates"]):
            print("Ignoré :", arg)
            continue
        c = item["candidates"][int(num) - 1]
        url = (c["thumb"] or c["file_url"]).replace("/480px-", "/900px-")
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        data = None
        for attempt in range(5):          # Wikimedia asks for a slow pace: wait and retry when refused
            try:
                with urllib.request.urlopen(req, timeout=60) as r:
                    data = r.read()
                break
            except Exception as exc:
                print("  ({}) attente...".format(exc))
                time.sleep(15 * (attempt + 1))
        if data is None:
            print("Échec :", slug)
            continue
        time.sleep(1.5)
        (SITE / "static" / "photos" / (slug + ".jpg")).write_bytes(data)
        credits[slug] = {"file": slug + ".jpg", "author": c["author"], "license": c["license"], "url": c["page"]}
        print("OK {} ({} Ko, {})".format(slug, len(data) // 1024, c["license"]))
    cred_path.write_text(json.dumps(credits, ensure_ascii=False, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main(sys.argv[1:])
