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
        for table in ("rate_events", "price_reports", "reports", "posts", "topics", "tokens", "sessions", "outbox", "users"):
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

    def test_lesson_figures(self):
        c = Client()
        body = c.get("/lecons/gemmes").body.decode("utf-8")
        self.assertIn('class="lessonfig"', body)
        self.assertIn("/static/js/lessonfig.js", body)
        self.assertNotIn("lessonfig.js", c.get("/lecons/aimant").body.decode("utf-8"))
        for name in ("lessonfig.js", "lessonfig_figs.js", "lessonfig_figs2.js", "../data/lesson_spectra.json"):
            self.assertEqual(c.get("/static/js/" + name).status, 200, name)

    def test_lesson_html_is_safe(self):
        from app import markup
        out = markup.lesson_html("Un <b>mot</b> et <script>alert(1)</script>")
        self.assertIn("<b>mot</b>", out)
        self.assertNotIn("<script>", out)


class TestLearning(SiteTestCase):
    def test_quiz_data_is_valid(self):
        from app import catalog
        ids = [it["id"] for it in catalog.lessons()]
        for lid in ids:
            qs = catalog.quiz(lid)
            self.assertTrue(qs, "no quiz for " + lid)
            for q in qs:
                self.assertEqual(len(q["choices"]), 4, q["q"])
                self.assertTrue(0 <= q["answer"] < 4, q["q"])
                self.assertTrue(q["why"], q["q"])
        terms = catalog.glossary()[0]
        for term in terms:
            for lid in term.get("lessons", []):
                self.assertIn(lid, ids, term["id"])

    def test_glossary_page_and_links(self):
        c = Client()
        r = c.get("/glossaire")
        self.assertEqual(r.status, 200)
        self.assertIn('id="pleochroisme"', r.text)
        lesson = c.get("/lecons/dichroscope").text
        self.assertIn('class="gloss"', lesson)
        self.assertIn('/glossaire#pleochroisme', lesson)
        species = c.get("/encyclopedie/corindon-rubis").text
        self.assertIn('/glossaire#indice-de-refraction', species)
        self.assertIn('/comparer?a=corindon-rubis', species)

    def test_each_term_is_linked_once_per_page(self):
        from app import markup
        seen = set()
        out = markup.link_terms("<p>La dureté, la dureté, et un <a href=\"/x\">dureté</a>.</p>", seen)
        self.assertEqual(out.count('class="gloss"'), 1)

    def take(self, c, lesson_id, right=True):
        """Fills in the quiz of a lesson exactly as the page asked it (the questions are drawn at random) and sends it."""
        from app import catalog
        page = c.get("/lecons/" + lesson_id).text
        asked = re.search(r'name="asked" value="([0-9,]+)"', page).group(1)
        bank = catalog.quiz(lesson_id)
        data = {"asked": asked}
        for i in map(int, asked.split(",")):
            data["q%d" % i] = str(bank[i]["answer"] if right else (bank[i]["answer"] + 1) % len(bank[i]["choices"]))
        return c.post("/lecons/%s/quiz" % lesson_id, data)

    def test_quiz_as_visitor(self):
        c = Client()
        page = c.get("/lecons/aimant").text
        self.assertIn('id="quiz"', page)
        self.assertNotIn("data-answer", page)
        r = self.take(c, "aimant")
        self.assertEqual(r.status, 200)
        self.assertIn("5</strong> / 5", r.text)
        self.assertIn("pas enregistré", r.text)
        self.assertEqual(c.get("/progression").status, 303)
        self.assertEqual(c.post("/lecons/nimporte/quiz", {}).status, 404)

    def test_quiz_is_drawn_and_shuffled(self):
        from app import catalog
        c = Client()
        draws, first_choice = set(), set()
        for _ in range(12):
            page = c.get("/lecons/diamant_laboratoire").text
            asked = re.search(r'name="asked" value="([0-9,]+)"', page).group(1)
            ids = asked.split(",")
            self.assertEqual(len(ids), 5)
            self.assertEqual(len(set(ids)), 5)
            draws.add(asked)
            for i in ids:
                self.assertEqual(len(re.findall(r'name="q%s"' % i, page)), 4)
            first_choice.add(re.search(r'name="q%s" value="(\d)"' % ids[0], page).group(1))
        self.assertGreater(len(draws), 1, "the questions never change")
        self.assertGreater(len(first_choice), 1, "the choices are never shuffled")
        self.assertGreaterEqual(len(catalog.quiz("diamant_laboratoire")), 6)

    def test_quiz_ignores_bad_questions_ids(self):
        c = Client()
        r = c.post("/lecons/aimant/quiz", {"asked": "999,abc,-1,0,0", "q0": "0"})
        self.assertEqual(r.status, 200)
        self.assertIn("/ 1", r.text)

    def test_quiz_progress_of_a_member(self):
        c = self.member("quizzer")
        r = self.take(c, "aimant", right=False)
        self.assertEqual(r.status, 200)
        self.assertIn("Pas encore réussi", r.text)
        self.take(c, "aimant", right=True)
        self.take(c, "aimant", right=False)
        prog = c.get("/progression")
        self.assertEqual(prog.status, 200)
        self.assertIn("1</strong> / 22", prog.text)
        conn = db.connect()
        row = conn.execute("SELECT best, last, attempts FROM quiz_results").fetchone()
        conn.close()
        self.assertEqual((row["best"], row["attempts"]), (5, 3))
        self.assertIn("5/5", c.get("/lecons").text)
        self.assertIn("Mon compte", c.get("/compte").text)

    def play_mixed(self, c, theme="all", right=True):
        from app import catalog
        page = c.get("/quiz/jouer?theme=" + theme).text
        asked = re.search(r'name="asked" value="([^"]+)"', page).group(1)
        data = {"theme": theme, "asked": asked}
        for token in asked.split(","):
            lid, num = token.split(":")
            q = catalog.quiz(lid)[int(num)]
            data["m_%s_%s" % (lid, num)] = str(q["answer"] if right else (q["answer"] + 1) % len(q["choices"]))
        return page, asked, c.post("/quiz/jouer", data)

    def test_mixed_quiz_draw(self):
        c = Client()
        self.assertEqual(c.get("/quiz").status, 200)
        self.assertIn('href="/quiz"', c.get("/").text)
        seen = set()
        for theme in ("all", "base", "instruments"):
            page = c.get("/quiz/jouer?theme=" + theme).text
            self.assertEqual(page.count('<fieldset class="question">'), 20, theme)
            asked = re.search(r'name="asked" value="([^"]+)"', page).group(1).split(",")
            self.assertEqual(len(set(asked)), 20, theme)
            seen.add(tuple(asked))
            if theme == "all":
                per_lesson = {}
                for token in asked:
                    per_lesson[token.split(":")[0]] = per_lesson.get(token.split(":")[0], 0) + 1
                self.assertLessEqual(max(per_lesson.values()), 2)
                self.assertGreaterEqual(len(per_lesson), 10)
        self.assertEqual(len(seen), 3)
        a = re.search(r'name="asked" value="([^"]+)"', c.get("/quiz/jouer").text).group(1)
        b = re.search(r'name="asked" value="([^"]+)"', c.get("/quiz/jouer").text).group(1)
        self.assertNotEqual(a, b)
        self.assertNotEqual(c.get("/quiz/jouer?theme=n-importe-quoi").status, 500)

    def test_mixed_quiz_correction(self):
        c = Client()
        page, asked, r = self.play_mixed(c, right=True)
        self.assertEqual(r.status, 200)
        self.assertIn("20</strong> / 20", r.text)
        self.assertIn("Excellent", r.text)
        self.assertEqual(r.text.count('class="answer ok"'), 20)
        page, asked, r = self.play_mixed(c, theme="instruments", right=False)
        self.assertIn("0</strong> / 20", r.text)
        self.assertEqual(r.text.count('class="answer ko"'), 20)
        self.assertIn("La bonne réponse", r.text)
        self.assertIn("À revoir", r.text)
        self.assertIn("Revoir la leçon", r.text)
        self.assertIn("pas enregistré", r.text)
        self.assertEqual(c.post("/quiz/jouer", {"asked": "x:1,zzz,aimant:999", "theme": "all"}).status, 303)

    def test_mixed_quiz_is_kept_for_members(self):
        c = self.member("mixer")
        self.play_mixed(c, right=True)
        self.play_mixed(c, right=False)
        conn = db.connect()
        rows = conn.execute("SELECT score, total FROM mixed_quiz ORDER BY id").fetchall()
        conn.close()
        self.assertEqual([(r["score"], r["total"]) for r in rows], [(20, 20), (0, 20)])
        prog = c.get("/progression").text
        self.assertIn("2 quiz faits", prog)
        self.assertIn("meilleur score 100", prog)
        self.assertIn("meilleur score est 100", c.get("/quiz").text)
        c.post("/compte/supprimer", {"current": "correct-horse-battery"})
        conn = db.connect()
        self.assertEqual(conn.execute("SELECT COUNT(*) AS n FROM mixed_quiz").fetchone()["n"], 0)
        conn.close()

    def test_mixed_quiz_is_not_indexed_but_the_menu_page_is(self):
        config.INDEXABLE = True
        try:
            c = Client()
            self.assertEqual(c.get("/quiz").headers.get("x-robots-tag"), "index, follow")
            self.assertIn("noindex", c.get("/quiz/jouer").headers.get("x-robots-tag", ""))
            self.assertIn("<loc>http://127.0.0.1:8000/quiz</loc>", c.get("/sitemap.xml").text)
        finally:
            config.INDEXABLE = False

    def test_site_search(self):
        c = Client()
        r = c.get("/recherche?q=pl%C3%A9ochro%C3%AFsme")
        self.assertEqual(r.status, 200)
        self.assertIn("Leçons", r.text)
        self.assertIn("Glossaire", r.text)
        self.assertIn('href="/glossaire#pleochroisme"', r.text)
        self.assertIn('href="/lecons/dichroscope', r.text)
        self.assertIn("<mark>", r.text)
        self.assertIn("pleochroisme", c.get("/recherche?q=pleochroisme").text)
        rubis = c.get("/recherche?q=rubis").text
        self.assertIn('href="/encyclopedie/corindon-rubis"', rubis)
        self.assertIn("Espèces", rubis)
        self.assertIn("Rien trouvé", c.get("/recherche?q=zzzzqqq").text)
        self.assertIn("Écris un nom", c.get("/recherche").text)
        injected = c.get("/recherche?q=%3Cscript%3Ealert(1)%3C%2Fscript%3E").text
        self.assertNotIn("<script>alert(1)", injected)

    def test_search_is_reachable_and_not_indexed(self):
        c = Client()
        self.assertIn('href="/recherche"', c.get("/").text)
        self.assertIn('action="/recherche"', c.get("/").text)
        config.INDEXABLE = True
        try:
            self.assertIn("noindex", c.get("/recherche?q=rubis").headers.get("x-robots-tag", ""))
            self.assertIn("Disallow: /recherche", c.get("/robots.txt").text)
        finally:
            config.INDEXABLE = False

    def test_home_page_sections(self):
        c = Client()
        r = c.get("/")
        self.assertEqual(r.status, 200)
        for marker in ('class="ribbon"', 'class="star"', 'class="facts"', 'class="path"', 'class="instruments"', 'class="quickform"', 'class="lessons-home"', 'herosearch'):
            self.assertIn(marker, r.text, marker)
        self.assertIn('action="/identification"', r.text)
        self.assertEqual(c.get("/identification?ri=1.76&density=4.0").status, 200)

    def test_home_never_shows_an_artificial_stone_of_the_day(self):
        import datetime
        from app import catalog, home
        natural = {s["id"] for s in home.natural_species(catalog.data()["species"])}
        for forbidden in ("CZ", "Verre", "YAG", "GGG", "Titanate_Strontium"):
            self.assertNotIn(forbidden, natural)
        self.assertGreater(len(natural), 90)

    def test_compare(self):
        c = Client()
        self.assertEqual(c.get("/comparer").status, 200)
        r = c.get("/comparer?a=corindon-rubis&b=spinelle")
        self.assertEqual(r.status, 200)
        self.assertIn("Tableau comparatif", r.text)
        self.assertIn("polariscope", r.text)
        self.assertIn("réfractomètre", r.text.lower())
        self.assertIn("Choisis deux espèces différentes", c.get("/comparer?a=spinelle&b=spinelle").text)
        self.assertIn("Choisis la deuxième", c.get("/comparer?a=spinelle").text)
        self.assertEqual(c.get("/comparer?a=n-importe-quoi&b=x").status, 200)


class TestPublication(SiteTestCase):
    FIELDS = {"GEMLAB_PUBLISHER_NAME": "Jeanne Dupont", "GEMLAB_PUBLISHER_STATUS": "micro-entreprise", "GEMLAB_PUBLISHER_ADDRESS": "1 rue des Gemmes, 75001 Paris",
              "GEMLAB_PUBLISHER_DIRECTOR": "Jeanne Dupont", "GEMLAB_CONTACT_EMAIL": "contact@example.org", "GEMLAB_HOST_NAME": "Hebergeur SA",
              "GEMLAB_HOST_ADDRESS": "2 rue du Serveur, 59000 Lille", "GEMLAB_HOST_PHONE": "03 00 00 00 00"}

    def test_legal_pages_exist_and_ask_to_complete(self):
        c = Client()
        for path, marker in (("/mentions-legales", "Éditeur du site"), ("/confidentialite", "Cookies"), ("/contact", "Contact"), ("/a-propos", "Le simulateur")):
            r = c.get(path)
            self.assertEqual(r.status, 200, path)
            self.assertIn(marker, r.text, path)
        self.assertIn('class="todo"', c.get("/mentions-legales").text)
        home = c.get("/").text
        for link in ("/mentions-legales", "/confidentialite", "/contact", "/a-propos"):
            self.assertIn('href="{}"'.format(link), home)

    def test_legal_pages_show_the_publisher(self):
        os.environ.update(self.FIELDS)
        try:
            c = Client()
            notice = c.get("/mentions-legales").text
            for value in ("Jeanne Dupont", "micro-entreprise", "Hebergeur SA", "03 00 00 00 00"):
                self.assertIn(value, notice)
            self.assertNotIn('class="todo"', notice)
            self.assertIn("mailto:contact@example.org", c.get("/contact").text)
            self.assertIn("1 rue des Gemmes", c.get("/confidentialite").text)
        finally:
            for k in self.FIELDS:
                os.environ.pop(k, None)

    def test_private_by_default(self):
        c = Client()
        self.assertEqual(c.get("/sitemap.xml").status, 404)
        self.assertIn("Disallow: /\n", c.get("/robots.txt").text)
        r = c.get("/encyclopedie/diamant")
        self.assertNotIn("canonical", r.text)
        self.assertIn("noindex", r.headers.get("x-robots-tag", ""))

    def test_open_to_search_engines(self):
        config.INDEXABLE = True
        try:
            c = Client()
            r = c.get("/encyclopedie/diamant")
            self.assertEqual(r.headers.get("x-robots-tag"), "index, follow")
            self.assertIn('<meta name="robots" content="index, follow">', r.text)
            self.assertIn('rel="canonical"', r.text)
            for private in ("/connexion", "/inscription", "/forum", "/simulateur", "/compte"):
                self.assertIn("noindex", c.get(private).headers.get("x-robots-tag", ""), private)
            robots = c.get("/robots.txt").text
            self.assertIn("Sitemap:", robots)
            self.assertIn("Disallow: /forum", robots)
            self.assertNotIn("Disallow: /\n", robots)
            sitemap = c.get("/sitemap.xml")
            self.assertEqual(sitemap.status, 200)
            self.assertIn("/encyclopedie/diamant", sitemap.text)
            self.assertIn("/lecons/aimant", sitemap.text)
            self.assertIn("/glossaire", sitemap.text)
            self.assertNotIn("/forum", sitemap.text)
            self.assertNotIn("/simulateur", sitemap.text)
        finally:
            config.INDEXABLE = False

    def test_share_tags_and_icon(self):
        c = Client()
        r = c.get("/lecons/aimant")
        for tag in ('property="og:title"', 'property="og:image"', 'name="twitter:card"', 'name="description"', 'rel="icon"'):
            self.assertIn(tag, r.text, tag)
        self.assertEqual(c.get("/favicon.ico").status, 303)
        for path in ("/static/img/favicon.svg", "/static/img/og-image.png", "/static/img/apple-touch-icon.png"):
            self.assertEqual(c.get(path).status, 200, path)

    def test_not_found_page_helps(self):
        r = Client().get("/page-qui-nexiste-pas")
        self.assertEqual(r.status, 404)
        self.assertIn('name="q"', r.text)
        self.assertIn('href="/glossaire"', r.text)

    def test_delete_account(self):
        c = self.member("bye")
        c.post("/lecons/aimant/quiz", {"q0": "0", "q1": "1", "q2": "2", "q3": "3"})
        r = c.post("/forum/libre/nouveau", {"title": "Mon sujet", "body": "Un message qui doit rester"})
        self.assertEqual(r.status, 303)
        topic_url = r.headers["location"]
        self.assertEqual(c.post("/compte/supprimer", {"current": "mauvais mot de passe"}).status, 400)
        r = c.post("/compte/supprimer", {"current": "correct-horse-battery"})
        self.assertEqual(r.status, 303)
        conn = db.connect()
        user = conn.execute("SELECT * FROM users WHERE username LIKE 'membre-supprime-%'").fetchone()
        self.assertIsNotNone(user)
        self.assertNotIn("bye", user["email"])
        self.assertEqual(conn.execute("SELECT COUNT(*) AS n FROM quiz_results WHERE user_id = ?", (user["id"],)).fetchone()["n"], 0)
        conn.close()
        self.assertEqual(c.get("/compte").status, 303)
        reader = self.member("reader")
        page = reader.get(topic_url).text
        self.assertIn("Un message qui doit rester", page)
        self.assertIn("membre-supprime-", page)
        login = Client().post("/connexion", {"email": "bye@example.com", "password": "correct-horse-battery"})
        self.assertNotEqual(login.status, 303)

    def test_admin_cannot_delete_account_here(self):
        c = self.member("boss", admin=True)
        self.assertEqual(c.post("/compte/supprimer", {"current": "correct-horse-battery"}).status, 400)


class TestAccessibility(SiteTestCase):
    PAGES = ["/", "/encyclopedie", "/encyclopedie/diamant", "/encyclopedie/formes", "/identification", "/comparer?a=corindon-rubis&b=spinelle",
             "/glossaire", "/lecons", "/lecons/dichroscope", "/quiz", "/quiz/jouer", "/recherche?q=rubis", "/connexion", "/inscription",
             "/mentions-legales", "/confidentialite", "/contact", "/a-propos", "/page-introuvable"]

    @staticmethod
    def audit(page):
        """Basic structure problems of a page: language, one h1, alt texts, labels of the fields, names of the buttons."""
        from html.parser import HTMLParser
        problems = []

        class P(HTMLParser):
            def __init__(self):
                super().__init__()
                self.h1 = 0
                self.stack = []
                self.label_for = set()
                self.fields = []
                self.lang = False
                self.button_text = None
                self.buttons = []

            def handle_starttag(self, tag, attrs):
                a = dict(attrs)
                if tag == "html" and a.get("lang"):
                    self.lang = True
                if tag == "h1":
                    self.h1 += 1
                if tag == "img" and "alt" not in a:
                    problems.append("image sans alt : " + str(a.get("src")))
                if tag == "label":
                    if a.get("for"):
                        self.label_for.add(a["for"])
                    self.stack.append("label")
                if tag in ("input", "select", "textarea") and a.get("type") not in ("hidden", "submit", "button"):
                    self.fields.append((a, "label" in self.stack))
                if tag == "button":
                    self.button_text = ""
                    self.buttons.append([a, ""])
                if tag == "a" and a.get("href", "").startswith("/") is False and a.get("target") == "_blank" and "noopener" not in a.get("rel", ""):
                    problems.append("lien externe sans noopener : " + a.get("href", ""))

            def handle_endtag(self, tag):
                if tag == "label" and "label" in self.stack:
                    self.stack.remove("label")
                if tag == "button":
                    self.button_text = None

            def handle_data(self, data):
                if self.button_text is not None:
                    self.buttons[-1][1] += data.strip()

        p = P()
        p.feed(page)
        if not p.lang:
            problems.append("pas d'attribut lang")
        if p.h1 != 1:
            problems.append("{} titres h1".format(p.h1))
        for a, inside in p.fields:
            if not (inside or a.get("aria-label") or a.get("id") in p.label_for):
                problems.append("champ sans libellé : " + str(a.get("name")))
        for a, text in p.buttons:
            if not (text or a.get("aria-label")):
                problems.append("bouton sans nom")
        if 'id="main"' not in page or 'class="skip"' not in page:
            problems.append("pas de lien d'évitement ni de zone principale")
        return problems

    def test_pages_are_well_structured(self):
        c = Client()
        for path in self.PAGES:
            r = c.get(path)
            self.assertIn(r.status, (200, 404), path)
            self.assertEqual(self.audit(r.text), [], path)

    def test_contrast_of_the_theme(self):
        sys.path.insert(0, str(SITE / "tools"))
        import check_contrast
        self.assertEqual(check_contrast.main(), 0)

    def test_every_drawing_has_a_text_description(self):
        from app import catalog
        c = Client()
        for item in catalog.lessons():
            page = c.get("/lecons/" + item["id"]).text
            figures = [s["figure"] for s in item["steps"] if s.get("figure")]
            self.assertEqual(page.count('class="figdesc"'), len(figures), item["id"])
            for fig in figures:
                self.assertTrue(i18n.has("fr", "fig.desc." + fig), fig)
        self.assertIn("Décrire ce schéma", c.get("/lecons/dichroscope").text)

    def test_animations_can_be_paused(self):
        c = Client()
        home = c.get("/").text
        self.assertIn('class="motion-toggle', home)
        self.assertIn('/static/js/motion.js', home)
        self.assertEqual(c.get("/static/js/motion.js").status, 200)
        self.assertIn("[data-motion=\"off\"]", c.get("/static/css/theme.css").text)

    def test_drawing_texts_are_ready_to_translate(self):
        sys.path.insert(0, str(SITE / "tools"))
        import extract_figure_texts
        import json as _json
        template = _json.loads((SITE / "static" / "i18n" / "figures.template.json").read_text(encoding="utf-8"))
        self.assertEqual(list(template), extract_figure_texts.figure_texts(), "relancer tools/extract_figure_texts.py")
        js = (SITE / "static" / "js" / "lessonfig.js").read_text(encoding="utf-8")
        self.assertIn("figures.", js)
        self.assertIn("kit.t(String(text))", js)


class TestEnglish(SiteTestCase):
    def test_every_text_is_translated_with_the_same_holes(self):
        import json as _json
        fr = _json.loads((SITE / "i18n" / "fr.json").read_text(encoding="utf-8"))
        en = _json.loads((SITE / "i18n" / "en.json").read_text(encoding="utf-8"))
        self.assertEqual(set(fr), set(en), "i18n/en.json n'a pas les mêmes clés que fr.json")
        hole = re.compile(r"\{[a-z_0-9]+\}")
        for key in fr:
            self.assertEqual(sorted(hole.findall(fr[key])), sorted(hole.findall(en[key])), key)
        self.assertEqual(sum(1 for v in en.values() if not v.strip()), 0)

    def test_content_files_are_complete(self):
        import json as _json
        from app import catalog
        for item in catalog.lessons():
            over = SITE / "data" / "en" / "lessons" / (item["id"] + ".json")
            self.assertTrue(over.exists(), item["id"])
            data = _json.loads(over.read_text(encoding="utf-8"))
            self.assertEqual(len(data["steps"]), len(item["steps"]), item["id"])
            quiz = _json.loads((SITE / "data" / "en" / "quizzes" / (item["id"] + ".json")).read_text(encoding="utf-8"))["questions"]
            french = catalog.quiz(item["id"])
            self.assertEqual(len(quiz), len(french), item["id"])
            for q, f in zip(quiz, french):
                self.assertEqual(len(q["choices"]), len(f["choices"]), q["q"])
        glossary = _json.loads((SITE / "data" / "en" / "glossary.json").read_text(encoding="utf-8"))
        self.assertEqual([g["id"] for g in glossary], [g["id"] for g in catalog.glossary()[0]] if False else [t["id"] for t in _json.loads((SITE / "data" / "glossary.json").read_text(encoding="utf-8"))])
        figures = _json.loads((SITE / "static" / "i18n" / "figures.en.json").read_text(encoding="utf-8"))
        template = _json.loads((SITE / "static" / "i18n" / "figures.template.json").read_text(encoding="utf-8"))
        self.assertEqual(list(figures), list(template), "figures.en.json n'est plus à jour")
        self.assertEqual(sum(1 for v in figures.values() if not v), 0)

    def test_english_pages_and_links(self):
        c = Client()
        r = c.get("/en")
        self.assertEqual(r.status, 200)
        self.assertIn('<html lang="en">', r.text)
        self.assertIn("Learn to recognize gemstones", r.text)
        self.assertIn('href="/en/encyclopedie"', r.text)
        self.assertIn('href="/en/lecons"', r.text)
        self.assertIn('href="/static/css/theme.css"', r.text)
        self.assertNotIn('href="/encyclopedie"', r.text)
        self.assertIn('action="/en/recherche"', r.text)
        for path in ("/en/encyclopedie", "/en/encyclopedie/diamant", "/en/lecons", "/en/lecons/dichroscope", "/en/glossaire", "/en/quiz", "/en/quiz/jouer",
                     "/en/comparer", "/en/identification", "/en/recherche?q=hardness", "/en/connexion", "/en/inscription", "/en/mentions-legales",
                     "/en/confidentialite", "/en/contact", "/en/a-propos", "/en/page-qui-nexiste-pas"):
            page = c.get(path)
            self.assertIn(page.status, (200, 404), path)
            self.assertIn('<html lang="en">', page.text, path)
            body = re.sub(r'<a class="langswitch"[^>]*>', "", page.text)
            self.assertNotIn('href="/encyclopedie', body, path)
        self.assertEqual(c.get("/en/page-qui-nexiste-pas").status, 404)
        self.assertIn("Hardness 10 · RI 2.417", c.get("/en/encyclopedie?q=diamant").text)
        self.assertIn("Dureté 10 · IR 2,417", c.get("/encyclopedie?q=diamant").text)

    def test_language_switch_and_alternates(self):
        c = Client()
        fr = c.get("/lecons/dichroscope?x=1").text
        self.assertIn('hreflang="en" lang="en"', fr)
        self.assertIn('href="/en/lecons/dichroscope?x=1" hreflang="en"', fr)
        self.assertIn('rel="alternate" hreflang="en"', fr)
        en = c.get("/en/lecons/dichroscope").text
        self.assertIn('href="/lecons/dichroscope" hreflang="fr"', en)
        self.assertIn('rel="alternate" hreflang="fr"', en)
        self.assertIn('href="/en"', c.get("/en/encyclopedie").text)
        self.assertIn('href="/en" hreflang="fr"' if False else 'hreflang="fr" lang="fr"', c.get("/en").text)

    def test_lessons_quiz_and_glossary_in_english(self):
        c = Client()
        lesson = c.get("/en/lecons/dichroscope").text
        self.assertIn("The dichroscope", lesson)
        self.assertIn("pleochroism", lesson)
        self.assertIn('href="/en/glossaire#pleochroisme"', lesson)
        self.assertIn("Describe this drawing", lesson)
        self.assertIn("Pleochroism", c.get("/en/glossaire").text)
        quiz = c.get("/en/lecons/aimant").text
        self.assertIn("Check my answers", quiz)
        from app import catalog
        bank = catalog.quiz("aimant")
        page = quiz
        asked = re.search(r'name="asked" value="([0-9,]+)"', page).group(1)
        data = {"asked": asked}
        for i in map(int, asked.split(",")):
            data["q%d" % i] = str(bank[i]["answer"])
        result = c.post("/en/lecons/aimant/quiz", data)
        self.assertIn("5</strong> / 5", result.text)
        self.assertIn("Passed!", result.text)
        self.assertIn("Correct", result.text)
        mixed = c.get("/en/quiz/jouer").text
        self.assertEqual(mixed.count('<fieldset class="question">'), 20)
        self.assertIn("/en/quiz", mixed)

    def test_english_search(self):
        c = Client()
        r = c.get("/en/recherche?q=cleavage")
        self.assertIn("Cleavage", r.text)
        self.assertIn('href="/en/lecons/tenacite', r.text)
        self.assertIn("<mark>", r.text)

    def test_redirects_keep_the_language(self):
        c = Client()
        r = c.get("/en/compte")
        self.assertEqual(r.status, 303)
        self.assertTrue(r.headers["location"].startswith("/en/connexion"), r.headers["location"])
        self.assertTrue(Client().get("/compte").headers["location"].startswith("/connexion"))

    def test_english_member_flow(self):
        c = Client()
        r = c.post("/en/inscription", {"username": "eve", "email": "eve@example.com", "password": "correct-horse-battery", "password2": "correct-horse-battery"})
        self.assertEqual(r.status, 200)
        self.assertIn("Almost done", r.text)
        mail = self.mails()[-1]
        self.assertIn("Hello eve", mail["body"])
        self.assertIn("/en/verification?token=", mail["body"])

    def test_sitemap_and_robots_list_both_languages(self):
        config.INDEXABLE = True
        try:
            c = Client()
            sitemap = c.get("/sitemap.xml").text
            self.assertIn("/en/encyclopedie/diamant", sitemap)
            self.assertIn('hreflang="en"', sitemap)
            self.assertIn("<loc>http://127.0.0.1:8000/en</loc>", sitemap)
            self.assertIn("Disallow: /en/forum", c.get("/robots.txt").text)
            en = c.get("/en/encyclopedie/diamant")
            self.assertEqual(en.headers.get("x-robots-tag"), "index, follow")
            self.assertIn('rel="canonical" href="http://127.0.0.1:8000/en/encyclopedie/diamant"', en.text)
            self.assertIn("noindex", c.get("/en/forum").headers.get("x-robots-tag", ""))
        finally:
            config.INDEXABLE = False


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


class TestPrices(SiteTestCase):
    SP = "Grenat_Spessartite"
    SLUG = "grenat-spessartite"

    def add_rows(self, weights_prices, kind="asking", status="approved", species=None, color=3, clarity="eye_clean", treatment="none", user=None):
        from app import prices
        conn = db.connect()
        for w, p in weights_prices:
            prices.add_report(conn, user, {"species": species or self.SP, "weight": w, "price": p, "kind": kind, "color": color,
                                           "clarity": clarity, "treatment": treatment, "observed": prices.this_month()}, status)
        conn.commit()
        conn.close()

    def form(self, **kw):
        from app import prices
        data = {"species": self.SLUG, "weight": "1,5", "price": "900", "kind": "asking", "color": "3", "clarity": "eye_clean",
                "treatment": "none", "observed": prices.this_month(), "confirm": "1"}
        data.update(kw)
        return data

    # -- the calculation
    def test_no_price_gives_no_estimate(self):
        from app import prices
        self.assertEqual(prices.estimate([], 1.0)["status"], "none")

    def test_too_few_prices_give_no_range(self):
        from app import prices
        rows = [{"weight": 1.0, "ppc": 500 + i, "price": 500, "kind": "asking", "observed": prices.this_month()} for i in range(4)]
        self.assertEqual(prices.estimate(rows, 1.0)["status"], "few")

    def test_band_estimate_ignores_extreme_values(self):
        from app import prices
        values = [480, 500, 510, 520, 540, 560, 9000]
        rows = [{"weight": 1.2, "ppc": v, "price": v * 1.2, "kind": "asking", "observed": prices.this_month()} for v in values]
        est = prices.estimate(rows, 1.2)
        self.assertEqual(est["status"], "ok")
        self.assertEqual(est["method"], "band")
        low, med, high = est["ppc"]
        self.assertTrue(low <= med <= high)
        self.assertLess(high, 1000)           # the 9000 outlier does not stretch the range
        self.assertAlmostEqual(est["total"][1], med * 1.2)

    def test_trend_when_the_band_is_thin_but_the_whole_is_rich(self):
        from app import prices
        rows = [{"weight": w, "ppc": 100 * w ** 0.4, "price": 0, "kind": "paid", "observed": prices.this_month()}
                for w in (0.3, 0.4, 0.6, 0.8, 1.2, 1.6, 2.5, 3.5, 6.0)]
        est = prices.estimate(rows, 2.0)
        self.assertEqual(est["status"], "ok")
        self.assertEqual(est["method"], "trend")
        self.assertEqual(est["confidence"], "low")
        self.assertAlmostEqual(est["ppc"][1], 100 * 2.0 ** 0.4, delta=3)
        self.assertEqual(prices.estimate(rows, 80.0)["status"], "few")   # far outside the data: no extrapolation

    def test_checks_on_a_contribution(self):
        from app import prices
        ids = {self.SP}
        good = {"species": self.SP, "weight": "1,5", "price": "900", "kind": "paid", "color": "3", "clarity": "eye_clean", "treatment": "none", "observed": prices.this_month()}
        self.assertEqual(prices.clean_report(good, ids)["weight"], 1.5)
        for change in ({"species": "Inconnue"}, {"weight": "0"}, {"weight": "abc"}, {"price": "-5"}, {"price": "5000000"}, {"price": "1", "weight": "400"},
                       {"kind": "x"}, {"color": "9"}, {"observed": "2999-01"}, {"observed": "2001-01"}, {"observed": "oups"}):
            with self.assertRaises(Exception, msg=str(change)):
                prices.clean_report(dict(good, **change), ids)

    # -- the pages
    def test_prices_page_is_public_in_both_languages(self):
        c = Client()
        for path, word in (("/prix", "Estimer le prix"), ("/en/prix", "Estimate the price")):
            r = c.get(path)
            self.assertEqual(r.status, 200, path)
            self.assertIn(word, r.text)
        self.assertNotIn("Grenat Spessartite 1 ct", c.get("/prix").text)

    def test_empty_table_says_no_data(self):
        r = Client().get("/prix?s=" + self.SLUG + "&w=1")
        self.assertIn("Aucun prix n&#x27;est encore enregistré", r.text)
        self.assertNotIn('class="estimate"', r.text)

    def test_range_is_shown_from_approved_prices_only(self):
        self.add_rows([(1.0, 450), (1.1, 500), (1.2, 560), (1.3, 600), (1.4, 640), (1.5, 700)])
        self.add_rows([(1.2, 99999)], status="pending")
        r = Client().get("/prix?s=" + self.SLUG + "&w=1,2")
        self.assertIn('class="estimate"', r.text)
        self.assertIn("par carat", r.text)
        self.assertNotIn("99999", r.text)
        self.assertIn("<svg", r.text)
        self.assertIn('role="img"', r.text)
        en = Client().get("/en/prix?s=" + self.SLUG + "&w=1.2").text
        self.assertIn("per carat", en)
        self.assertIn("This figure is a guide only", en)

    def test_filters_reduce_the_prices_used(self):
        self.add_rows([(1.0, 450), (1.1, 500), (1.2, 560), (1.3, 600), (1.4, 640)], treatment="heated")
        r = Client().get("/prix?s=" + self.SLUG + "&w=1.2&t=none")
        self.assertNotIn('class="estimate"', r.text)

    def test_bad_input_does_not_crash(self):
        c = Client()
        for q in ("?s=nope&w=1", "?s=" + self.SLUG + "&w=abc", "?s=" + self.SLUG + "&w=-3", "?s=" + self.SLUG + "&w=1e999", "?s=" + self.SLUG + "&w=1&c=zz&q=zz&t=zz&k=zz"):
            self.assertEqual(c.get("/prix" + q).status, 200, q)

    def test_synthetics_have_no_price_page(self):
        self.assertNotIn('value="cz"', Client().get("/prix").text)

    # -- adding a price
    def test_visitors_cannot_add(self):
        c = Client()
        self.assertEqual(c.get("/prix/ajouter").status, 303)
        self.assertEqual(c.post("/prix/ajouter", self.form()).status, 303)

    def test_unverified_member_cannot_add(self):
        c = self.member("alice", verified=False)
        self.assertEqual(c.post("/prix/ajouter", self.form()).status, 403)

    def test_a_member_price_waits_for_the_administrator(self):
        c = self.member("alice")
        r = c.post("/prix/ajouter", self.form())
        self.assertEqual(r.status, 303)
        self.assertIn("En attente de vérification", c.get("/prix/ajouter").text)
        self.assertEqual(len(__import__("app.prices", fromlist=["x"]).approved_rows(db.connect(), self.SP)), 0)
        boss = self.member("boss", admin=True)
        page = boss.get("/admin/prix").text
        self.assertIn("1 en attente", page)
        conn = db.connect()
        rid = conn.execute("SELECT id FROM price_reports").fetchone()["id"]
        conn.close()
        self.assertEqual(c.post("/admin/prix/{}/approuver".format(rid)).status, 403)
        self.assertEqual(boss.post("/admin/prix/{}/approuver".format(rid)).status, 303)
        conn = db.connect()
        self.assertEqual(len(__import__("app.prices", fromlist=["x"]).approved_rows(conn, self.SP)), 1)
        conn.close()

    def test_rejected_price_is_not_used(self):
        c = self.member("alice")
        c.post("/prix/ajouter", self.form())
        boss = self.member("boss", admin=True)
        conn = db.connect()
        rid = conn.execute("SELECT id FROM price_reports").fetchone()["id"]
        conn.close()
        boss.post("/admin/prix/{}/rejeter".format(rid))
        conn = db.connect()
        self.assertEqual(conn.execute("SELECT status FROM price_reports").fetchone()["status"], "rejected")
        conn.close()

    def test_administrator_prices_are_published_at_once(self):
        boss = self.member("boss", admin=True)
        boss.post("/prix/ajouter", self.form())
        conn = db.connect()
        self.assertEqual(conn.execute("SELECT status FROM price_reports").fetchone()["status"], "approved")
        conn.close()

    def test_invalid_and_repeated_prices_are_refused(self):
        c = self.member("alice")
        self.assertEqual(c.post("/prix/ajouter", self.form(weight="0")).status, 400)
        self.assertEqual(c.post("/prix/ajouter", self.form(confirm="")).status, 400)
        self.assertEqual(c.post("/prix/ajouter", self.form(species="cz")).status, 400)
        self.assertEqual(c.post("/prix/ajouter", {**self.form(), "website": "http://spam"}).status, 400)
        self.assertEqual(c.post("/prix/ajouter", self.form()).status, 303)
        self.assertEqual(c.post("/prix/ajouter", self.form()).status, 400)       # same price again
        conn = db.connect()
        self.assertEqual(conn.execute("SELECT COUNT(*) AS n FROM price_reports").fetchone()["n"], 1)
        conn.close()

    def test_daily_limit(self):
        c = self.member("alice")
        for i in range(10):
            self.assertEqual(c.post("/prix/ajouter", self.form(price=str(900 + i))).status, 303)
        self.assertEqual(c.post("/prix/ajouter", self.form(price="1500")).status, 429)

    def test_form_needs_csrf(self):
        c = self.member("alice")
        self.assertEqual(c.post("/prix/ajouter", self.form(), csrf=False).status, 403)

    def test_deleting_the_account_keeps_the_price_but_not_the_link(self):
        c = self.member("alice")
        c.post("/prix/ajouter", self.form())
        c.post("/compte/supprimer", {"current": "correct-horse-battery"})
        conn = db.connect()
        row = conn.execute("SELECT * FROM price_reports").fetchone()
        self.assertIsNotNone(row)
        self.assertIsNone(row["user_id"])
        conn.close()

    # -- files
    def test_import_and_export(self):
        from app import prices
        conn = db.connect()
        text = ("species,weight_ct,price_eur,kind,color_tier,clarity,treatment,observed\n"
                "{sp},1.2,600,asking,3,eye_clean,none,{m}\n{sp},1.2,600,asking,3,eye_clean,none,{m}\nInconnue,1,100,asking,3,eye_clean,none,{m}\n"
                "{slug},2,1500,paid,4,loupe_clean,heated,{m}\n").format(sp=self.SP, slug=self.SLUG, m=prices.this_month())
        added, problems = prices.import_csv(conn, text)
        conn.commit()
        self.assertEqual(added, 2)
        self.assertEqual([n for n, _ in problems], [3, 4])
        out = prices.export_csv(conn)
        conn.close()
        self.assertEqual(len(out.strip().splitlines()), 3)
        self.assertNotIn("user", out.lower().split("\n")[0])
        self.assertEqual(prices.import_csv(db.connect(), "a,b\n1,2\n")[0], 0)

    # -- integration
    def test_navigation_and_sitemap(self):
        self.assertIn('href="/prix"', Client().get("/").text)
        self.assertIn('href="/en/prix"', Client().get("/en/").text)
        self.assertIn("/prix?s=grenat-spessartite", Client().get("/encyclopedie/" + self.SLUG).text)
        config.INDEXABLE = True
        try:
            self.assertIn("<loc>http://127.0.0.1:8000/prix</loc>", Client().get("/sitemap.xml").text)
            self.assertEqual(Client().get("/prix").headers.get("x-robots-tag"), "index, follow")
            self.assertIn("noindex", self.member("alice").get("/prix/ajouter").headers.get("x-robots-tag", ""))
        finally:
            config.INDEXABLE = False


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
