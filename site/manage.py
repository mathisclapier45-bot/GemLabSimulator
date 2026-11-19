#!/usr/bin/env python3
"""Administration commands.

  python3 site/manage.py make-admin someone@example.com   gives the administrator role to an existing member
  python3 site/manage.py verify-email someone@example.com marks an address as verified (handy in local tests)
  python3 site/manage.py list-users                       lists members
  python3 site/manage.py import-prices fichier.csv        adds prices from a CSV file (see data/prices_template.csv), published at once
  python3 site/manage.py export-prices [fichier.csv]      writes the published prices as CSV (no user data)
  python3 site/manage.py check-publication                says what is still missing before the site can go public
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from app import db  # noqa: E402
from app.db import now  # noqa: E402


def check_publication():
    from app import config
    items = [
        ("GEMLAB_ENV=prod", config.ENV == "prod"),
        ("GEMLAB_BASE_URL (adresse publique du site)", "127.0.0.1" not in config.BASE_URL and config.BASE_URL.startswith("https://")),
        ("GEMLAB_COOKIE_SECURE", config.COOKIE_SECURE),
        ("GEMLAB_MAIL_MODE=smtp et GEMLAB_SMTP_HOST", config.MAIL_MODE == "smtp" and bool(config.SMTP_HOST)),
        ("GEMLAB_MAIL_FROM (adresse d'expédition réelle)", "example.invalid" not in config.MAIL_FROM),
    ]
    items += [("GEMLAB_{} (mentions légales)".format(config.LEGAL_FIELDS[f]), bool(config.legal(f))) for f in config.LEGAL_FIELDS]
    items.append(("GEMLAB_INDEXABLE=1 (ouvre le site aux moteurs de recherche, à faire en dernier)", config.INDEXABLE))
    missing = 0
    for label, ok in items:
        print("[{}] {}".format("ok" if ok else "à faire", label))
        missing += 0 if ok else 1
    print("\n{}".format("Tout est prêt." if not missing else "{} point(s) à compléter avant la mise en ligne.".format(missing)))
    return 0 if not missing else 1


def main(argv):
    if not argv:
        print(__doc__)
        return 1
    if argv[0] == "check-publication":
        return check_publication()
    db.init()
    conn = db.connect()
    cmd = argv[0]
    try:
        if cmd in ("make-admin", "verify-email") and len(argv) == 2:
            user = conn.execute("SELECT * FROM users WHERE email = ?", (argv[1].strip().lower(),)).fetchone()
            if not user:
                print("Aucun membre avec cette adresse.")
                return 1
            if cmd == "make-admin":
                conn.execute("UPDATE users SET role = 'admin', email_verified_at = COALESCE(email_verified_at, ?) WHERE id = ?",
                             (now(), user["id"]))
                print("{} est maintenant administrateur.".format(user["username"]))
            else:
                conn.execute("UPDATE users SET email_verified_at = ? WHERE id = ?", (now(), user["id"]))
                print("Adresse vérifiée pour {}.".format(user["username"]))
            conn.commit()
            return 0
        if cmd == "import-prices" and len(argv) == 2:
            from app import prices
            added, problems = prices.import_csv(conn, Path(argv[1]).read_text(encoding="utf-8-sig"))
            conn.commit()
            print("{} prix ajoutés.".format(added))
            for line, key in problems[:30]:
                print("  ligne {} ignorée : {}".format(line, key))
            return 0
        if cmd == "export-prices" and len(argv) in (1, 2):
            from app import prices
            text = prices.export_csv(conn)
            if len(argv) == 2:
                Path(argv[1]).write_text(text, encoding="utf-8")
            else:
                sys.stdout.write(text)
            return 0
        if cmd == "list-users":
            for u in conn.execute("SELECT * FROM users ORDER BY id"):
                print("{:4d}  {:20s}  {:30s}  {}  {}".format(u["id"], u["username"], u["email"], u["role"],
                                                              "vérifié" if u["email_verified_at"] else "non vérifié"))
            return 0
    finally:
        conn.close()
    print(__doc__)
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
