"""Automatic checks:  python3 -m unittest discover -s site/tests   (from the repository root)
or, from the site folder:  python3 -m unittest discover tests"""
import glob
import io
import os
import re
import sys
import tempfile
import unittest
from http.cookies import SimpleCookie
from pathlib import Path
from urllib.parse import urlencode

SITE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(SITE))

from app import config, db, i18n, security  # noqa: E402
from app import main as site_main  # noqa: E402

config.PBKDF2_ITERATIONS = 1000   # fast hashing for tests only


class Client:
    def __init__(self):
        self.cookies = {}

    def request(self, method, path, data=None, headers=None, csrf=True):
        path_info, _, query = path.partition("?")
        body = b""
        env = {
            "REQUEST_METHOD": method, "PATH_INFO": path_info, "QUERY_STRING": query,
            "REMOTE_ADDR": "10.0.0.1", "wsgi.input": io.BytesIO(b""), "CONTENT_LENGTH": "0",
            "SERVER_NAME": "test", "SERVER_PORT": "80", "wsgi.url_scheme": "http",
        }
        if method == "POST":
            data = dict(data or {})
            if csrf:
                if "gl_csrf" not in self.cookies:
                    self.request("GET", "/")
                data.setdefault("csrf", self.cookies.get("gl_csrf", ""))
            body = urlencode(data).encode()
            env["wsgi.input"] = io.BytesIO(body)
            env["CONTENT_LENGTH"] = str(len(body))
            env["CONTENT_TYPE"] = "application/x-www-form-urlencoded"
        if self.cookies:
            env["HTTP_COOKIE"] = "; ".join("{}={}".format(k, v) for k, v in self.cookies.items())
        for k, v in (headers or {}).items():
            env["HTTP_" + k.upper().replace("-", "_")] = v
        out = {}

        def start_response(status, hdrs):
            out["status"] = int(status.split()[0])
            out["headers"] = hdrs

        chunks = site_main.application(env, start_response)
        payload = b"".join(chunks)
        hdrs = {}
        for k, v in out["headers"]:
            if k.lower() == "set-cookie":
                jar = SimpleCookie()
                jar.load(v)
                for name, m in jar.items():
                    if m.value == "" or m["max-age"] == "0":
                        self.cookies.pop(name, None)
                    else:
                        self.cookies[name] = m.value
            else:
                hdrs[k.lower()] = v
        return Response(out["status"], hdrs, payload)

    def get(self, path, **kw):
        return self.request("GET", path, **kw)

    def post(self, path, data=None, **kw):
        return self.request("POST", path, data, **kw)


class Response:
    def __init__(self, status, headers, body):
        self.status, self.headers, self.body = status, headers, body

    @property
    def text(self):
        return self.body.decode("utf-8", "replace")


class SiteTestCase(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        config.DB_PATH = Path(cls.tmp.name) / "test.db"
        config.SIM_DIR = Path(cls.tmp.name) / "sim"
        config.SIM_DIR.mkdir()
        for name in config.SIM_FILES:
            (config.SIM_DIR / name).write_bytes(b"fake-" + name.encode())
        db.init()

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def setUp(self):
        conn = db.connect()
        for table in ("rate_events", "reports", "posts", "topics", "tokens", "sessions", "outbox", "users"):
            conn.execute("DELETE FROM " + table)
        conn.commit()
        conn.close()

    # helpers
    def mails(self):
        conn = db.connect()
        rows = conn.execute("SELECT * FROM outbox ORDER BY id").fetchall()
        conn.close()
        return rows

    def last_link(self, kind):
        for row in reversed(self.mails()):
            m = re.search(r"/{}\?token=([\w-]+)".format(kind), row["body"])
            if m:
                return "/{}?token={}".format(kind, m.group(1))
        self.fail("no mail link for " + kind)

    def register(self, client, name="alice", email=None, pw="correct-horse-battery"):
        email = email or name + "@example.com"
        return client.post("/inscription", {"username": name, "email": email, "password": pw, "password2": pw})

    def member(self, name="alice", verified=True, admin=False):
        c = Client()
        r = self.register(c, name)
        self.assertEqual(r.status, 200, r.text[:300])
        if verified:
            c.get(self.last_link("verification"))
        if admin:
            conn = db.connect()
            conn.execute("UPDATE users SET role = 'admin' WHERE username = ?", (name,))
            conn.commit()
            conn.close()
        r = c.post("/connexion", {"email": name + "@example.com", "password": "correct-horse-battery"})
        self.assertEqual(r.status, 303)
        return c


class TestNoIndex(SiteTestCase):
    def test_every_page_is_noindex(self):
        c = Client()
        paths = ["/", "/encyclopedie", "/encyclopedie/diamant", "/encyclopedie/formes", "/lecons", "/lecons/loupe",
                 "/connexion", "/inscription", "/mot-de-passe-oublie", "/forum", "/simulateur", "/page-qui-n-existe-pas",
                 "/robots.txt", "/static/css/site.css"]
        for p in paths:
            r = c.get(p)
            self.assertIn("noindex", r.headers.get("x-robots-tag", ""), p)
            self.assertIn("nofollow", r.headers.get("x-robots-tag", ""), p)
            if "text/html" in r.headers.get("content-type", "") and r.body:
                self.assertIn('<meta name="robots" content="noindex, nofollow">', r.text, p)

    def test_robots_and_no_sitemap(self):
        c = Client()
        r = c.get("/robots.txt")
        self.assertEqual(r.status, 200)
        self.assertIn("Disallow: /", r.text)
        self.assertEqual(c.get("/sitemap.xml").status, 404)


class TestEncyclopedia(SiteTestCase):
    def test_all_species_pages(self):
        from app import catalog
        c = Client()
        for s in catalog.data()["species"]:
            r = c.get("/encyclopedie/" + s["slug"])
            self.assertEqual(r.status, 200, s["id"])

    def test_search_and_filters(self):
        c = Client()
        r = c.get("/encyclopedie?q=rubis")
        self.assertIn("Rubis", r.text)
        self.assertNotIn("Emeraude", r.text)
        r = c.get("/encyclopedie?q=beryl")     # without accent finds "Béryl"
        self.assertEqual(r.status, 200)
        self.assertIn("résultat", r.text)
        r = c.get("/encyclopedie?hardness=hard&optic=Uniaxe")
        self.assertEqual(r.status, 200)
        r = c.get("/encyclopedie?q=zzzzzz")
        self.assertIn("Aucune pierre", r.text)

    def test_species_page_has_instrument_data(self):
        c = Client()
        r = c.get("/encyclopedie/corindon-rubis")
        self.assertIn("Spectre d&#x27;absorption", r.text)
        self.assertIn("<svg", r.text)
        self.assertIn("Fluorescence", r.text)

    def test_species_page_has_drawings_and_composition(self):
        c = Client()
        page = c.get("/encyclopedie/emeraude").text
        self.assertIn("Be₃Al₂Si₆O₁₈", page)
        self.assertIn("Hexagonal", page)
        self.assertIn("Couleurs possibles", page)
        self.assertIn("data-shape=", page)
        self.assertIn("pal-data", page)
        self.assertIn('class="conoscope"', page)             # interference figure
        self.assertIn('class="figure dichro"', page)            # dichroscope windows
        self.assertIn("uvbox", c.get("/encyclopedie/corindon-rubis").text)
        self.assertIn("Cubique", c.get("/encyclopedie/diamant").text)
        self.assertIn("Matière organique", c.get("/encyclopedie/perle").text)

    def test_conoscope_and_dichroscope_not_applicable(self):
        c = Client()
        page = c.get("/encyclopedie/diamant").text         # isotropic
        self.assertEqual(page.count("Test non applicable"), 2)
        self.assertNotIn('class="conoscope"', page)
        self.assertIn("Test non applicable", c.get("/encyclopedie/agate").text)       # aggregate
        ruby = c.get("/encyclopedie/corindon-rubis").text
        self.assertNotIn("Test non applicable", ruby)
        self.assertIn("Pléochroïsme : ", ruby)
        self.assertIn("Dichroscope", ruby)

    def test_spectrum_always_shown(self):
        c = Client()
        from app import catalog
        for s in catalog.data()["species"]:
            self.assertIn('class="spectrum"', c.get("/encyclopedie/" + s["slug"]).text, s["id"])

    def test_pleochroism_strength_words(self):
        from app import diagrams
        self.assertEqual(diagrams.pleo_strength("Vert / Rouge selon les trois axes (extrêmement marqué)."), "very_strong")
        self.assertEqual(diagrams.pleo_strength("Jaune pâle / Jaune soutenu selon l'axe (faible)."), "light")
        self.assertEqual(diagrams.pleo_strength("Bleu / Vert selon l'axe (faible à modéré)."), "light_moderate")
        self.assertEqual(diagrams.pleo_strength("Rose / Orangé selon l'axe (modéré)."), "moderate")
        self.assertEqual(diagrams.pleo_strength("Bleu / Bleu-vert selon l'axe d'observation (marqué)."), "strong")

    def test_color_charts(self):
        from app import catalog
        c = Client()
        for s in catalog.data()["species"]:
            self.assertIsNotNone(s.get("palette"), s["id"])
        self.assertIn("Naturelle", c.get("/encyclopedie/saphir-bleu").text)       # synthetic toggle
        self.assertIn("Couleur D", c.get("/encyclopedie/diamant").text)            # diamond D-Z scale
        self.assertEqual(c.get("/static/js/palette.js").status, 200)

    def test_3d_viewer_files(self):
        import json
        c = Client()
        for path in ("/static/js/gem3d.js", "/static/data/meshes/index.json", "/static/data/meshes/rond__brillant.json",
                     "/static/data/meshes/ovale__cabochon.json"):
            self.assertEqual(c.get(path).status, 200, path)
        index = json.loads(c.get("/static/data/meshes/index.json").body)
        for style, shapes in index["allowed"].items():
            for shape in shapes:
                self.assertEqual(c.get("/static/data/meshes/{}__{}.json".format(shape, style)).status, 200)
        self.assertIn("gem3d.js", c.get("/encyclopedie/saphir-bleu").text)

    def test_every_species_has_composition(self):
        from app import catalog
        missing = [s["id"] for s in catalog.data()["species"] if not s.get("extra")]
        self.assertEqual(missing, [])

    def test_animated_lesson(self):
        c = Client()
        page = c.get("/lecons/refractometre").text
        self.assertEqual(page.count('class="anim"'), 7)
        self.assertIn("/static/js/refraction.js", page)
        self.assertEqual(c.get("/static/js/refraction.js").status, 200)
        self.assertNotIn('class="anim"', c.get("/lecons/loupe").text)

    def test_identification_by_data(self):
        c = Client()
        self.assertEqual(c.get("/identification").status, 200)
        # a ruby: RI 1.766, density 4.0, uniaxial, pleochroic
        page = c.get("/identification?ri=1,766&ri_tol=0.005&density=4&density_tol=0.1&optic=Uniaxe&pleo=yes").text
        self.assertIn("Corindon (Rubis)", page)
        self.assertNotIn("Émeraude", page)
        self.assertIn("compatible", page)
        self.assertIn("Aucune pierre ne correspond", c.get("/identification?ri=3.5&ri_tol=0.005").text)
        self.assertIn("Émeraude", c.get("/identification?color=green&optic=Uniaxe&hardness_from=7").text)
        self.assertEqual(c.get("/identification?ri=abc&density=%3Cscript%3E").status, 200)
        self.assertIn('href="/identification"', c.get("/").text)

    def test_unknown_species(self):
        self.assertEqual(Client().get("/encyclopedie/n-importe-quoi").status, 404)

    def test_lessons(self):
        from app import catalog
        c = Client()
        self.assertEqual(c.get("/lecons").status, 200)
        for item in catalog.lessons():
            self.assertEqual(c.get("/lecons/" + item["id"]).status, 200, item["id"])
        self.assertEqual(c.get("/lecons/..%2Fx").status, 404)

    def test_lesson_html_is_safe(self):
        from app import markup
        out = markup.lesson_html("Un <b>mot</b> et <script>alert(1)</script>")
        self.assertIn("<b>mot</b>", out)
        self.assertNotIn("<script>", out)


class TestAccounts(SiteTestCase):
    def test_register_verify_login_logout(self):
        c = Client()
        r = self.register(c)
        self.assertEqual(r.status, 200)
        mails = self.mails()
        self.assertEqual(len(mails), 1)
        self.assertEqual(mails[0]["to_addr"], "alice@example.com")
        link = self.last_link("verification")
        r = c.post("/connexion", {"email": "alice@example.com", "password": "correct-horse-battery"})
        self.assertEqual(r.status, 303)
        self.assertIn("gl_session", c.cookies)
        self.assertIn("pas encore vérifiée", c.get("/compte").text)
        self.assertEqual(c.get(link).status, 303)
        self.assertIn("vérifiée", c.get("/compte").text)
        self.assertNotIn("pas encore vérifiée", c.get("/compte").text)
        # the link works only once
        self.assertEqual(Client().get(link).status, 400)
        c.post("/deconnexion")
        self.assertNotIn("gl_session", c.cookies)
        self.assertEqual(c.get("/compte").status, 303)

    def test_password_is_hashed(self):
        c = Client()
        self.register(c)
        conn = db.connect()
        h = conn.execute("SELECT pw_hash FROM users").fetchone()["pw_hash"]
        conn.close()
        self.assertTrue(h.startswith("pbkdf2_sha256$"))
        self.assertNotIn("correct-horse", h)

    def test_validation(self):
        c = Client()
        self.assertEqual(self.register(c, pw="court").status, 400)
        self.assertEqual(c.post("/inscription", {"username": "a", "email": "x@example.com", "password": "correct-horse-battery",
                                                   "password2": "correct-horse-battery"}).status, 400)
        self.assertEqual(c.post("/inscription", {"username": "bob", "email": "pas-un-mail", "password": "correct-horse-battery",
                                                   "password2": "correct-horse-battery"}).status, 400)
        self.assertEqual(c.post("/inscription", {"username": "bob", "email": "b@example.com", "password": "correct-horse-battery",
                                                   "password2": "autre-chose-vraiment"}).status, 400)

    def test_duplicate_email_does_not_leak(self):
        c1, c2 = Client(), Client()
        r1 = self.register(c1, "alice", "same@example.com")
        r2 = self.register(c2, "carol", "same@example.com")
        self.assertEqual(r1.status, r2.status)
        self.assertEqual(r1.text.replace("alice", ""), r2.text.replace("carol", ""))
        conn = db.connect()
        self.assertEqual(conn.execute("SELECT COUNT(*) AS n FROM users").fetchone()["n"], 1)
        conn.close()

    def test_duplicate_username(self):
        self.register(Client(), "alice", "a@example.com")
        r = self.register(Client(), "ALICE", "b@example.com")
        self.assertEqual(r.status, 400)
        self.assertIn("déjà pris", r.text)

    def test_login_errors_and_rate_limit(self):
        self.register(Client())
        c = Client()
        for _ in range(8):
            r = c.post("/connexion", {"email": "alice@example.com", "password": "mauvais-mot-de-passe"})
            self.assertEqual(r.status, 400)
            self.assertIn("incorrect", r.text)
        r = c.post("/connexion", {"email": "alice@example.com", "password": "correct-horse-battery"})
        self.assertEqual(r.status, 429)

    def test_unknown_email_gives_same_error(self):
        r = Client().post("/connexion", {"email": "nobody@example.com", "password": "whatever-password"})
        self.assertEqual(r.status, 400)
        self.assertIn("Adresse e-mail ou mot de passe incorrect", r.text)

    def test_register_is_rate_limited(self):
        c = Client()
        statuses = [self.register(c, "user%d" % i).status for i in range(7)]
        self.assertEqual(statuses[:5], [200] * 5)
        self.assertEqual(statuses[5], 429)

    def test_password_reset(self):
        self.register(Client())
        c = Client()
        r = c.post("/mot-de-passe-oublie", {"email": "alice@example.com"})
        self.assertEqual(r.status, 200)
        link = self.last_link("reinitialisation")
        self.assertEqual(c.get(link).status, 200)
        r = c.post("/reinitialisation", {"token": link.split("=")[1], "password": "brand-new-password", "password2": "brand-new-password"})
        self.assertEqual(r.status, 303)
        self.assertEqual(c.post("/connexion", {"email": "alice@example.com", "password": "correct-horse-battery"}).status, 400)
        self.assertEqual(c.post("/connexion", {"email": "alice@example.com", "password": "brand-new-password"}).status, 303)
        self.assertEqual(Client().get(link).status, 400)     # single use

    def test_reset_for_unknown_email_looks_the_same(self):
        r = Client().post("/mot-de-passe-oublie", {"email": "ghost@example.com"})
        self.assertEqual(r.status, 200)
        self.assertEqual(len(self.mails()), 0)

    def test_reset_closes_other_sessions(self):
        c = self.member("alice")
        self.assertEqual(c.get("/compte").status, 200)
        Client().post("/mot-de-passe-oublie", {"email": "alice@example.com"})
        link = self.last_link("reinitialisation")
        Client().post("/reinitialisation", {"token": link.split("=")[1], "password": "brand-new-password", "password2": "brand-new-password"})
        self.assertEqual(c.get("/compte").status, 303)

    def test_csrf_required(self):
        c = Client()
        r = c.post("/connexion", {"email": "a@example.com", "password": "x" * 12}, csrf=False)
        self.assertEqual(r.status, 403)
        c.get("/")
        r = c.post("/connexion", {"email": "a@example.com", "password": "x" * 12, "csrf": "wrong"}, csrf=False)
        self.assertEqual(r.status, 403)

    def test_honeypot(self):
        c = Client()
        r = c.post("/inscription", {"username": "bot", "email": "bot@example.com", "password": "correct-horse-battery",
                                    "password2": "correct-horse-battery", "website": "http://spam"})
        self.assertEqual(r.status, 400)

    def test_open_redirect_blocked(self):
        self.register(Client())
        c = Client()
        r = c.post("/connexion", {"email": "alice@example.com", "password": "correct-horse-battery", "suite": "//evil.example.com"})
        self.assertEqual(r.headers["location"], "/")
        r = Client().post("/connexion", {"email": "alice@example.com", "password": "correct-horse-battery", "suite": "https://evil.example.com"})
        self.assertEqual(r.headers["location"], "/")

    def test_banned_user_cannot_log_in(self):
        c = self.member("alice")
        conn = db.connect()
        conn.execute("UPDATE users SET banned_at = 1")
        conn.commit()
        conn.close()
        self.assertEqual(c.get("/compte").status, 303)
        self.assertEqual(Client().post("/connexion", {"email": "alice@example.com", "password": "correct-horse-battery"}).status, 400)


class TestForum(SiteTestCase):
    def new_topic(self, c, cat="libre", title="Un sujet", body="Un message de test"):
        return c.post("/forum/{}/nouveau".format(cat), {"title": title, "body": body})

    def test_visitors_are_sent_to_login(self):
        c = Client()
        for p in ("/forum", "/forum/libre", "/forum/libre/nouveau", "/forum/sujet/1", "/admin"):
            r = c.get(p)
            self.assertEqual(r.status, 303, p)
            self.assertTrue(r.headers["location"].startswith("/connexion"), p)
        self.assertEqual(c.post("/forum/libre/nouveau", {"title": "x" * 5, "body": "yyyy"}).status, 303)

    def test_unverified_cannot_write(self):
        c = self.member("alice", verified=False)
        self.assertEqual(c.get("/forum").status, 200)
        r = self.new_topic(c)
        self.assertEqual(r.status, 403)
        self.assertIn("Vérifiez d&#x27;abord", r.text)

    def test_create_topic_and_reply(self):
        c = self.member("alice")
        r = self.new_topic(c, title="Mon rubis", body="Voici **ma** pierre\n\n- un\n- deux")
        self.assertEqual(r.status, 303)
        url = r.headers["location"]
        page = c.get(url).text
        self.assertIn("Mon rubis", page)
        self.assertIn("<strong>ma</strong>", page)
        self.assertIn("<li>un</li>", page)
        self.assertEqual(c.post(url + "/repondre", {"body": "Une réponse"}).status, 429)   # too fast after first post
        conn = db.connect()
        conn.execute("DELETE FROM rate_events")
        conn.commit()
        conn.close()
        r = c.post(url + "/repondre", {"body": "Une réponse"})
        self.assertEqual(r.status, 303)
        self.assertIn("Une réponse", c.get(url).text)

    def test_html_is_escaped(self):
        c = self.member("alice")
        r = self.new_topic(c, title="<b>titre</b>", body='<script>alert(1)</script> <img src=x onerror=alert(2)>')
        page = c.get(r.headers["location"]).text
        self.assertNotIn("<script>alert", page)
        self.assertNotIn("<img src=x", page)
        self.assertIn("&lt;script&gt;", page)
        self.assertIn("&lt;b&gt;titre", page)

    def test_links_get_nofollow_and_limits(self):
        c = self.member("alice")
        r = self.new_topic(c, body="Voir https://example.com/page.")
        self.assertIn('rel="nofollow ugc noopener noreferrer"', c.get(r.headers["location"]).text)
        conn = db.connect()
        conn.execute("DELETE FROM rate_events")
        conn.commit()
        conn.close()
        r = self.new_topic(c, body="a https://a.example b https://b.example")   # 2 links on a new account
        self.assertEqual(r.status, 400)
        self.assertIn("Trop de liens", r.text)

    def test_rate_limits(self):
        c = self.member("alice")
        self.assertEqual(self.new_topic(c).status, 303)
        r = self.new_topic(c, title="Autre sujet")
        self.assertEqual(r.status, 429)

    def test_topic_limit_per_hour(self):
        c = self.member("alice")
        for i in range(3):
            conn = db.connect()
            conn.execute("DELETE FROM rate_events WHERE key LIKE 'post_user%'")
            conn.commit()
            conn.close()
            self.assertEqual(self.new_topic(c, title="Sujet numéro %d" % i, body="Corps %d" % i).status, 303)
        conn = db.connect()
        conn.execute("DELETE FROM rate_events WHERE key LIKE 'post_user%'")
        conn.commit()
        conn.close()
        self.assertEqual(self.new_topic(c, title="Sujet de trop", body="Corps de trop").status, 429)

    def test_validation_keeps_text(self):
        c = self.member("alice")
        r = self.new_topic(c, title="ab", body="Du texte à conserver")
        self.assertEqual(r.status, 400)
        self.assertIn("Du texte à conserver", r.text)
        self.assertEqual(self.new_topic(c, cat="inexistante").status, 404)

    def test_only_admin_can_moderate(self):
        author = self.member("alice")
        r = self.new_topic(author)
        url = r.headers["location"]
        topic_id = url.rsplit("/", 1)[1]
        bob = self.member("bob")
        for action in ("epingler", "verrouiller", "supprimer"):
            self.assertEqual(bob.post("/forum/sujet/{}/{}".format(topic_id, action)).status, 403, action)
        self.assertEqual(author.post("/forum/message/1/supprimer").status, 403)
        self.assertEqual(bob.get("/admin").status, 403)
        # state unchanged
        conn = db.connect()
        row = conn.execute("SELECT pinned, locked, deleted FROM topics").fetchone()
        conn.close()
        self.assertEqual((row["pinned"], row["locked"], row["deleted"]), (0, 0, 0))

    def test_admin_moderation(self):
        author = self.member("alice")
        url = self.new_topic(author).headers["location"]
        topic_id = url.rsplit("/", 1)[1]
        admin = self.member("root", admin=True)
        self.assertEqual(admin.post("/forum/sujet/{}/epingler".format(topic_id)).status, 303)
        self.assertEqual(admin.post("/forum/sujet/{}/verrouiller".format(topic_id)).status, 303)
        self.assertIn("Épinglé", admin.get("/forum/libre").text)
        # locked: members cannot reply, admins can
        conn = db.connect()
        conn.execute("DELETE FROM rate_events")
        conn.commit()
        conn.close()
        self.assertEqual(author.post(url + "/repondre", {"body": "Réponse bloquée"}).status, 403)
        self.assertEqual(admin.post(url + "/repondre", {"body": "Réponse de l'admin"}).status, 303)
        # delete the topic
        self.assertEqual(admin.post("/forum/sujet/{}/supprimer".format(topic_id)).status, 303)
        self.assertEqual(author.get(url).status, 404)

    def test_report_and_delete_post(self):
        author = self.member("alice")
        url = self.new_topic(author).headers["location"]
        conn = db.connect()
        conn.execute("DELETE FROM rate_events")
        conn.commit()
        conn.close()
        author.post(url + "/repondre", {"body": "Un message gênant"})
        reader = self.member("bob")
        self.assertEqual(reader.get("/forum/message/2/signaler").status, 200)
        self.assertEqual(reader.post("/forum/message/2/signaler", {"reason": "Publicité"}).status, 200)
        # reporting twice does not duplicate
        reader.post("/forum/message/2/signaler", {"reason": "Publicité"})
        admin = self.member("root", admin=True)
        page = admin.get("/admin").text
        self.assertIn("Un message gênant", page)
        self.assertEqual(page.count("Publicité"), 1)
        self.assertEqual(admin.post("/forum/message/2/supprimer").status, 303)
        self.assertIn("Message supprimé par un administrateur", reader.get(url).text)
        self.assertNotIn("Un message gênant", reader.get(url).text)
        self.assertIn("Aucun signalement", admin.get("/admin").text)

    def test_unverified_cannot_report(self):
        author = self.member("alice")
        self.new_topic(author)
        c = self.member("bob", verified=False)
        self.assertEqual(c.post("/forum/message/1/signaler", {"reason": "Spam"}).status, 403)

    def test_admin_ban(self):
        author = self.member("alice")
        self.new_topic(author)
        admin = self.member("root", admin=True)
        self.assertEqual(admin.post("/admin/membre/1/bannir").status, 303)
        self.assertEqual(author.get("/forum").status, 303)   # session closed
        self.assertEqual(self.member("bob").post("/admin/membre/1/bannir").status, 403)


class TestSimulator(SiteTestCase):
    def test_visitor_is_blocked(self):
        c = Client()
        r = c.get("/simulateur")
        self.assertEqual(r.status, 303)
        for name in config.SIM_FILES:
            r = c.get("/simulateur/fichiers/" + name)
            self.assertEqual(r.status, 403, name)
            self.assertNotIn(b"fake-", r.body)
        # the private folder is not reachable through the static route either
        self.assertEqual(c.get("/static/../private/simulateur/WebCours.loader.js").status, 404)
        self.assertEqual(c.get("/private/simulateur/WebCours.wasm.gz").status, 404)

    def test_member_gets_files_with_gzip_header(self):
        c = self.member("alice", verified=False)
        page = c.get("/simulateur")
        self.assertEqual(page.status, 200)
        self.assertIn("unity-canvas", page.text)
        self.assertIn("unsafe-eval", page.headers["content-security-policy"])
        r = c.get("/simulateur/fichiers/WebCours.wasm.gz")
        self.assertEqual(r.status, 200)
        self.assertEqual(r.headers["content-encoding"], "gzip")
        self.assertEqual(r.headers["content-type"], "application/wasm")
        self.assertEqual(r.body, b"fake-WebCours.wasm.gz")
        r = c.get("/simulateur/fichiers/WebCours.framework.js.gz")
        self.assertEqual(r.headers["content-type"], "application/javascript")
        self.assertEqual(r.headers["content-encoding"], "gzip")
        r = c.get("/simulateur/fichiers/WebCours.loader.js")
        self.assertNotIn("content-encoding", r.headers)
        self.assertEqual(c.get("/simulateur/fichiers/WebCours.data.gz").headers["content-type"], "application/octet-stream")
        self.assertIn("private", r.headers["cache-control"])

    def test_etag_and_unknown_files(self):
        c = self.member("alice")
        r = c.get("/simulateur/fichiers/WebCours.data.gz")
        again = c.get("/simulateur/fichiers/WebCours.data.gz", headers={"If-None-Match": r.headers["etag"]})
        self.assertEqual(again.status, 304)
        self.assertEqual(c.get("/simulateur/fichiers/autre.txt").status, 404)
        self.assertEqual(c.get("/simulateur/fichiers/..%2F..%2Fapp%2Fconfig.py").status, 404)

    def test_files_stop_after_logout(self):
        c = self.member("alice")
        self.assertEqual(c.get("/simulateur/fichiers/WebCours.loader.js").status, 200)
        c.post("/deconnexion")
        self.assertEqual(c.get("/simulateur/fichiers/WebCours.loader.js").status, 403)


class TestCode(unittest.TestCase):
    def test_all_translation_keys_exist(self):
        table = i18n.load("fr")
        keys = set()
        for f in glob.glob(str(SITE / "app" / "*.py")):
            with open(f, encoding="utf-8") as fh:
                src = fh.read()
            keys |= set(re.findall(r'\bt\(\s*"([^"{]+)"\s*[,)]', src))
            keys |= set(re.findall(r'"((?:err|flash|mail|error|forum\.cat)\.[a-z_.0-9]+)"', src))
        keys |= {"forum.cat." + c + suffix for c in config.FORUM_CATEGORIES for suffix in ("", ".desc")}
        keys |= {"error.title.%d" % s for s in (400, 403, 404, 405, 413, 429, 500)}
        keys |= {"error.%d" % s for s in (400, 403, 404, 405, 413, 429, 500)}
        missing = sorted(k for k in keys if k not in table and not k.endswith("."))
        self.assertEqual(missing, [])

    def test_unused_translation_keys_are_listed(self):
        # Not a failure: just keeps the file tidy. Keys must at least be valid strings.
        for k, v in i18n.load("fr").items():
            self.assertIsInstance(v, str, k)

    def test_no_french_text_in_python_code(self):
        """Visible text must live in i18n/*.json. Comments and docstrings are ignored."""
        import ast
        pattern = re.compile(r"[àâçéèêëîïôûùœÀÉ]")
        offenders = []
        for f in glob.glob(str(SITE / "app" / "*.py")):
            with open(f, encoding="utf-8") as fh:
                tree = ast.parse(fh.read())
            docstrings = set()
            for node in ast.walk(tree):
                if isinstance(node, (ast.Module, ast.FunctionDef, ast.ClassDef)) and node.body \
                        and isinstance(node.body[0], ast.Expr) and isinstance(getattr(node.body[0], "value", None), ast.Constant):
                    docstrings.add(id(node.body[0].value))
            for node in ast.walk(tree):
                if isinstance(node, ast.Constant) and isinstance(node.value, str) and id(node) not in docstrings:
                    if pattern.search(node.value):
                        offenders.append((os.path.basename(f), node.value[:40]))
        self.assertEqual(offenders, [])


if __name__ == "__main__":
    unittest.main()
