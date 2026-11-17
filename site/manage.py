#!/usr/bin/env python3
"""Administration commands.

  python3 site/manage.py make-admin someone@example.com   gives the administrator role to an existing member
  python3 site/manage.py verify-email someone@example.com marks an address as verified (handy in local tests)
  python3 site/manage.py list-users                       lists members
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from app import db  # noqa: E402
from app.db import now  # noqa: E402


def main(argv):
    if not argv:
        print(__doc__)
        return 1
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
