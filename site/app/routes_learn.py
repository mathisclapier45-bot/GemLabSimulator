"""Learning tools: the glossary, the quiz at the end of each lesson (and the progress of members), the comparison of two species."""
import random
import re

from . import catalog
from .db import now
from .views import csrf_field, esc, fmt_num, gem_svg, page
from .web import HTTPError, Router, redirect

router = Router()

PASS_RATIO = 0.7   # a quiz is passed from 70 % of right answers
QUIZ_LENGTH = 5    # questions asked at each attempt, drawn at random from the bank of the lesson


# ------------------------------------------------------------------ results of the members

def results_of(db, user):
    """lesson id -> row (best, total, last, attempts) of a member; {} for a visitor."""
    if not user:
        return {}
    rows = db.execute("SELECT * FROM quiz_results WHERE user_id = ?", (user["id"],)).fetchall()
    return {r["lesson_id"]: r for r in rows}


def passed(best, total):
    return total > 0 and best / total >= PASS_RATIO


def _save_result(db, user, lesson_id, score, total):
    row = db.execute("SELECT best FROM quiz_results WHERE user_id = ? AND lesson_id = ?", (user["id"], lesson_id)).fetchone()
    if row:
        db.execute("UPDATE quiz_results SET best = MAX(best, ?), total = ?, last = ?, attempts = attempts + 1, updated_at = ? "
                   "WHERE user_id = ? AND lesson_id = ?", (score, total, score, now(), user["id"], lesson_id))
    else:
        db.execute("INSERT INTO quiz_results (user_id, lesson_id, best, total, last, attempts, updated_at) VALUES (?, ?, ?, ?, ?, 1, ?)",
                   (user["id"], lesson_id, score, total, score, now()))


# ------------------------------------------------------------------ quiz form (shown at the end of a lesson page)

def quiz_form(req, lesson_id):
    questions = catalog.quiz(lesson_id)
    if not questions:
        return ""
    t = req.t
    mine = results_of(req.db, req.user).get(lesson_id)
    status = ""
    if mine:
        status = '<p class="quizstatus">{}</p>'.format(esc(t("quiz.best", best=mine["best"], total=mine["total"])))
    elif not req.user:
        status = '<p class="small">{} <a href="/inscription">{}</a></p>'.format(esc(t("quiz.guest")), esc(t("nav.register")))
    # Each attempt: a random draw of questions, in a random order, with the choices shuffled. The fields keep the numbers of the bank
    # (question id, choice id), so that the correction does not depend on the order shown.
    asked = random.sample(range(len(questions)), min(QUIZ_LENGTH, len(questions)))
    blocks = []
    for n, i in enumerate(asked):
        q = questions[i]
        order = list(range(len(q["choices"])))
        random.shuffle(order)
        choices = "".join(
            '<label class="choice"><input type="radio" name="q{i}" value="{k}" {req}> <span>{c}</span></label>'.format(
                i=i, k=k, req="required" if pos == 0 else "", c=esc(q["choices"][k]))
            for pos, k in enumerate(order))
        blocks.append('<fieldset class="question"><legend><span class="num">{n}</span> {q}</legend>{c}</fieldset>'.format(
            n=n + 1, q=esc(q["q"]), c=choices))
    return ('<section class="quiz" id="quiz"><h2>{h}</h2><p class="lead">{lead}</p>{status}'
            '<form method="post" action="/lecons/{id}/quiz" class="form">{csrf}<input type="hidden" name="asked" value="{asked}">{blocks}'
            '<button type="submit" class="btn">{go}</button></form></section>').format(
        h=esc(t("quiz.title")), lead=esc(t("quiz.lead", n=len(asked), bank=len(questions))), status=status, id=esc(lesson_id),
        csrf=csrf_field(req), asked=",".join(str(i) for i in asked), blocks="".join(blocks), go=esc(t("quiz.submit")))


@router.route("POST", "/lecons/<lesson_id>/quiz")
def quiz_answer(req):
    t = req.t
    lesson_id = req.params["lesson_id"]
    lesson = catalog.lesson(lesson_id) if catalog.SAFE_ID.match(lesson_id) else None
    questions = catalog.quiz(lesson_id) if lesson else None
    if not questions:
        raise HTTPError(404)
    # The questions of this attempt are the ones listed in the hidden field (their numbers in the bank of the lesson).
    asked = []
    for part in req.form.get("asked", "").split(","):
        if part.strip().isdigit() and int(part) < len(questions) and int(part) not in asked:
            asked.append(int(part))
    asked = asked[:QUIZ_LENGTH] or list(range(min(QUIZ_LENGTH, len(questions))))
    score, items = 0, []
    for i in asked:
        q = questions[i]
        raw = req.form.get("q{}".format(i), "")
        chosen = int(raw) if raw.isdigit() and int(raw) < len(q["choices"]) else None
        ok = chosen == q["answer"]
        score += 1 if ok else 0
        items.append((q, chosen, ok))
    total = len(asked)
    if req.user:
        _save_result(req.db, req.user, lesson_id, score, total)

    rows = []
    for i, (q, chosen, ok) in enumerate(items):
        yours = esc(q["choices"][chosen]) if chosen is not None else esc(t("quiz.none"))
        rows.append(
            '<article class="answer {cls}"><h3><span class="num">{n}</span> {q}</h3>'
            '<p class="verdict">{verdict}</p><p>{ylabel} {yours}</p>{good}<p class="why">{why}</p></article>'.format(
                cls="ok" if ok else "ko", n=i + 1, q=esc(q["q"]), verdict=esc(t("quiz.right" if ok else "quiz.wrong")),
                ylabel=esc(t("quiz.yours")), yours=yours,
                good="" if ok else '<p>{} <strong>{}</strong></p>'.format(esc(t("quiz.correct")), esc(q["choices"][q["answer"]])),
                why=esc(q["why"])))
    order = [it["id"] for it in catalog.lessons()]
    nxt = order[order.index(lesson_id) + 1] if lesson_id in order and order.index(lesson_id) + 1 < len(order) else None
    nav = ['<a class="btn" href="/lecons/{}#quiz">{}</a>'.format(esc(lesson_id), esc(t("quiz.retry")))]
    if nxt:
        nav.append('<a class="btn ghost" href="/lecons/{}">{}</a>'.format(esc(nxt), esc(t("quiz.next"))))
    nav.append('<a class="btn ghost" href="/lecons/{}">{}</a>'.format(esc(lesson_id), esc(t("quiz.back"))))
    if req.user:
        nav.append('<a class="btn ghost" href="/progression">{}</a>'.format(esc(t("quiz.progress"))))
    guest = "" if req.user else '<div class="banner warn">{} <a href="/inscription">{}</a></div>'.format(esc(t("quiz.guest.saved")), esc(t("nav.register")))
    verdict = t("quiz.passed") if passed(score, total) else t("quiz.notyet")
    content = ('<nav class="crumbs"><a href="/lecons/{id}">{back}</a></nav><h1>{h1}</h1>'
               '<p class="score"><strong>{score}</strong> / {total} — {verdict}</p>{guest}{rows}<p class="actions">{nav}</p>').format(
        id=esc(lesson_id), back=esc(lesson["title"]), h1=esc(t("quiz.result.title", title=lesson["title"])),
        score=score, total=total, verdict=esc(verdict), guest=guest, rows="".join(rows), nav=" ".join(nav))
    return page(req, t("quiz.result.title", title=lesson["title"]), content, section="lessons")


# ------------------------------------------------------------------ progress of a member

@router.route("GET", "/progression")
def progress(req):
    if not req.user:
        return redirect("/connexion?suite=/progression")
    t = req.t
    mine = results_of(req.db, req.user)
    rows, done, quizzes = [], 0, 0
    for item in catalog.lessons():
        if not catalog.quiz(item["id"]):
            continue
        quizzes += 1
        r = mine.get(item["id"])
        if r and passed(r["best"], r["total"]):
            done += 1
            state, cls = t("prog.passed"), "ok"
        elif r:
            state, cls = t("prog.again"), "mid"
        else:
            state, cls = t("prog.todo"), "todo"
        score = "{} / {}".format(r["best"], r["total"]) if r else "—"
        tries = str(r["attempts"]) if r else "—"
        rows.append('<tr class="{cls}"><th scope="row"><a href="/lecons/{id}#quiz">{title}</a></th><td>{score}</td><td>{tries}</td><td>{state}</td></tr>'.format(
            cls=cls, id=esc(item["id"]), title=esc(item["title"]), score=score, tries=tries, state=esc(state)))
    pct = int(100 * done / quizzes) if quizzes else 0
    recent, allrows = mixed_results(req.db, req.user, 8)
    mixed_html = ""
    if allrows:
        average = sum(r["score"] * 100 // r["total"] for r in allrows) // len(allrows)
        best = max(r["score"] * 100 // r["total"] for r in allrows)
        lines = "".join("<tr><td>{} / {}</td><td>{}</td></tr>".format(r["score"], r["total"], esc(t("mixed.theme." + _theme(r["theme"])))) for r in recent)
        mixed_html = ('<h2>{h}</h2><p>{sum}</p><table class="props progtable"><thead><tr><th>{c1}</th><th>{c2}</th></tr></thead><tbody>{lines}</tbody></table>'
                      '<p><a class="btn" href="/quiz">{go}</a></p>').format(
            h=esc(t("mixed.progress.title")), sum=esc(t("mixed.progress.sum", n=len(allrows), avg=average, best=best)), c1=esc(t("prog.col.score")),
            c2=esc(t("mixed.progress.theme")), lines=lines, go=esc(t("mixed.start")))
    else:
        mixed_html = '<h2>{}</h2><p>{} <a href="/quiz">{}</a></p>'.format(esc(t("mixed.progress.title")), esc(t("mixed.progress.none")), esc(t("mixed.start")))
    content = ('<h1>{h1}</h1><p class="lead">{lead}</p>'
               '<p class="summary"><strong>{done}</strong> / {quizzes} {label}</p>'
               '<progress max="{quizzes}" value="{done}" aria-label="{pct} %">{pct} %</progress>'
               '<table class="props progtable"><thead><tr><th>{c1}</th><th>{c2}</th><th>{c3}</th><th>{c4}</th></tr></thead><tbody>{rows}</tbody></table>{mixed}').format(
        mixed=mixed_html,
        h1=esc(t("prog.title")), lead=esc(t("prog.lead", pct=int(PASS_RATIO * 100))), done=done, quizzes=quizzes, label=esc(t("prog.done")), pct=pct,
        c1=esc(t("prog.col.lesson")), c2=esc(t("prog.col.score")), c3=esc(t("prog.col.tries")), c4=esc(t("prog.col.state")), rows="".join(rows))
    return page(req, t("prog.title"), content)


# ------------------------------------------------------------------ glossary

@router.route("GET", "/glossaire")
def glossary_page(req):
    t = req.t
    terms = catalog.glossary()[0]
    groups = {}
    for term in terms:
        letter = catalog.fold(term["term"])[:1].upper()
        groups.setdefault(letter, []).append(term)
    index = " ".join('<a href="#l{0}">{0}</a>'.format(esc(letter)) for letter in sorted(groups))
    blocks = []
    for letter in sorted(groups):
        entries = []
        for term in groups[letter]:
            links = []
            for lid in term.get("lessons", []):
                lesson = catalog.lesson(lid)
                if lesson:
                    links.append('<a href="/lecons/{}">{}</a>'.format(esc(lid), esc(lesson["title"])))
            see = '<p class="small">{} {}</p>'.format(esc(t("gloss.see")), ", ".join(links)) if links else ""
            entries.append('<dt id="{id}">{term}</dt><dd>{d}{see}</dd>'.format(id=esc(term["id"]), term=esc(term["term"]), d=esc(term["def"]), see=see))
        blocks.append('<h2 id="l{0}">{0}</h2><dl class="glossary">{1}</dl>'.format(esc(letter), "".join(entries)))
    content = '<h1>{h1}</h1><p class="lead">{lead}</p><nav class="alpha" aria-label="{h1}">{index}</nav>{blocks}'.format(
        h1=esc(t("gloss.title")), lead=esc(t("gloss.lead", n=len(terms))), index=index, blocks="".join(blocks))
    return page(req, t("gloss.title"), content, section="encyclopedia")


# ------------------------------------------------------------------ comparison of two species

def _num(v):
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def _uv(s, fl):
    """(intensity, colour) under the long and short lamps, from the game table when there is one, else from the words."""
    def one(text, inten, col):
        if inten is not None:
            return (round(float(inten), 2), (col or "").lower() if inten >= 0.2 else "")
        return (0.0 if re.match(r"\s*aucun", text or "", re.I) else 0.5, "")
    if fl:
        return one(fl.get("longText"), fl.get("longIntensity"), fl.get("longColor")), one(fl.get("shortText"), fl.get("shortIntensity"), fl.get("shortColor"))
    return one(s.get("fluorescenceLongWave"), None, None), one(s.get("fluorescenceShortWave"), None, None)


def _uv_differs(a, b):
    for x, y in zip(a, b):
        if abs(x[0] - y[0]) >= 0.3:
            return True
        if x[0] >= 0.3 and y[0] >= 0.3 and x[1] and y[1] and x[1] != y[1]:
            return True
    return False


ELEMENTS = ("chrome", "fer", "manganese", "vanadium", "cobalt", "cuivre", "uranium", "titane", "neodyme")


def _elements(text):
    folded = catalog.fold(text or "")
    return {e for e in ELEMENTS if e in folded}


def _magnetic(s):
    return not re.match(r"\s*aucune", s.get("magnetism") or "", re.I)


def _chelsea(s):
    return not re.match(r"\s*aucune", s.get("chelseaFilterReaction") or "", re.I)


def _verdicts(req, a, b, d):
    """The instruments of the laboratory that tell two species apart, with the figures that make the difference."""
    t = req.t
    A, B = a["displayName"], b["displayName"]
    out = []

    def add(lesson_id, key, **kw):
        out.append((lesson_id, t("cmp.sep." + key, **kw)))

    ra, rb = _num(a.get("refractiveIndex")), _num(b.get("refractiveIndex"))
    if ra and rb and ra <= 1.81 and rb <= 1.81 and abs(ra - rb) >= 0.02:
        add("refractometre", "ri", a=fmt_num(ra, 3), b=fmt_num(rb, 3))
    ba, bb = _num(a.get("birefringence")) or 0.0, _num(b.get("birefringence")) or 0.0
    if ra and rb and ra <= 1.81 and rb <= 1.81 and abs(ba - bb) >= 0.01:
        add("refractometre", "bire", a=fmt_num(ba, 3), b=fmt_num(bb, 3))
    da, db_ = _num(a.get("density")), _num(b.get("density"))
    if da and db_ and abs(da - db_) >= 0.15:
        add("balance_hydrostatique", "density", a=fmt_num(da, 2), b=fmt_num(db_, 2))
    if a.get("opticalCharacterClassName") != b.get("opticalCharacterClassName"):
        add("polariscope", "optic", a=a.get("opticalCharacterClassName") or "", b=b.get("opticalCharacterClassName") or "")
    if bool(a.get("pleochroic")) != bool(b.get("pleochroic")):
        add("dichroscope", "pleo", who=A if a.get("pleochroic") else B)
    if _uv_differs(_uv(a, d["fluorescence"].get(a["id"])), _uv(b, d["fluorescence"].get(b["id"]))):
        add("cabine_uv", "uv")
    if _chelsea(a) != _chelsea(b):
        add("filtre_chelsea", "chelsea", who=A if _chelsea(a) else B)
    ea, eb = _elements(a.get("absorptionSpectrum")), _elements(b.get("absorptionSpectrum"))
    if ea != eb or (a["has_spectrum"] != b["has_spectrum"]):
        add("spectroscope", "spectrum")
    if bool(a.get("isThermallyConductiveLikeDiamond")) != bool(b.get("isThermallyConductiveLikeDiamond")):
        add("testeurs_diamant", "thermal", who=A if a.get("isThermallyConductiveLikeDiamond") else B)
    if bool(a.get("isElectricallyConductive")) != bool(b.get("isElectricallyConductive")):
        add("testeurs_diamant", "electric", who=A if a.get("isElectricallyConductive") else B)
    if _magnetic(a) != _magnetic(b):
        add("aimant", "magnet", who=A if _magnetic(a) else B)
    ha, hb = _num(a.get("hardness")), _num(b.get("hardness"))
    if ha and hb and abs(ha - hb) >= 1.0:
        add("durete", "hardness", a=fmt_num(ha, 1), b=fmt_num(hb, 1))
    return out


def _compare_rows(req, a, b):
    t = req.t
    rows = []

    def row(label, va, vb, gloss=None):
        rows.append((label, va or "—", vb or "—", gloss, catalog.fold(str(va or "")) != catalog.fold(str(vb or ""))))

    def system(s):
        key = (s.get("extra") or {}).get("system", "")
        return t("sys." + key) if key else ""

    def disp(s):
        value, exact = catalog.dispersion(s)
        return ("" if exact else "≈ ") + fmt_num(value, 3)

    def yes(v):
        return t("sp.yes") if v else t("cmp.no")

    row(t("sp.system"), system(a), system(b))
    row(t("sp.formula"), (a.get("extra") or {}).get("formula", ""), (b.get("extra") or {}).get("formula", ""))
    row(t("sp.ri"), fmt_num(a.get("refractiveIndex"), 3), fmt_num(b.get("refractiveIndex"), 3), "indice-de-refraction")
    row(t("sp.birefringence"), fmt_num(a.get("birefringence"), 3) if a.get("birefringence") else t("cmp.zero"),
        fmt_num(b.get("birefringence"), 3) if b.get("birefringence") else t("cmp.zero"), "birefringence")
    row(t("sp.dispersion"), disp(a), disp(b), "dispersion")
    row(t("sp.density"), fmt_num(a.get("density"), 2), fmt_num(b.get("density"), 2), "densite")
    row(t("sp.hardness"), fmt_num(a.get("hardness"), 1), fmt_num(b.get("hardness"), 1), "durete")
    row(t("sp.luster"), a.get("luster", ""), b.get("luster", ""), "eclat")
    row(t("sp.color"), a.get("color", ""), b.get("color", ""))
    row(t("sp.transparency"), a.get("transparency", ""), b.get("transparency", ""), "transparence")
    row(t("sp.optic"), a.get("opticalCharacter", ""), b.get("opticalCharacter", ""))
    row(t("sp.pleochroism"), (a.get("pleochroicDescription") if a.get("pleochroic") else t("sp.none")),
        (b.get("pleochroicDescription") if b.get("pleochroic") else t("sp.none")), "pleochroisme")
    row(t("sp.interference"), a.get("interferenceFigure", ""), b.get("interferenceFigure", ""), "figure-d-interference")
    row(t("sp.magnetism"), a.get("magnetism", ""), b.get("magnetism", ""))
    row(t("sp.thermal"), yes(a.get("isThermallyConductiveLikeDiamond")), yes(b.get("isThermallyConductiveLikeDiamond")), "conductivite-thermique")
    row(t("sp.electric"), yes(a.get("isElectricallyConductive")), yes(b.get("isElectricallyConductive")))
    row(t("sp.uv.long"), a.get("fluorescenceLongWave", ""), b.get("fluorescenceLongWave", ""), "fluorescence")
    row(t("sp.uv.short"), a.get("fluorescenceShortWave", ""), b.get("fluorescenceShortWave", ""))
    row(t("cmp.chelsea"), a.get("chelseaFilterReaction", ""), b.get("chelseaFilterReaction", ""), "chelsea")
    row(t("sp.spectrum"), a.get("absorptionSpectrum", ""), b.get("absorptionSpectrum", ""), "spectre-d-absorption")
    return rows


@router.route("GET", "/comparer")
def compare(req):
    t = req.t
    d = catalog.data()
    sa, sb = req.query.get("a", ""), req.query.get("b", "")
    a, b = d["by_slug"].get(sa), d["by_slug"].get(sb)
    options = sorted(d["species"], key=lambda s: catalog.fold(s["displayName"]))

    def select(name, chosen):
        opts = '<option value="">{}</option>'.format(esc(t("cmp.choose"))) + "".join(
            '<option value="{}"{}>{}</option>'.format(esc(s["slug"]), " selected" if s["slug"] == chosen else "", esc(s["displayName"])) for s in options)
        return '<label class="field">{}<select name="{}">{}</select></label>'.format(esc(t("cmp." + name)), name, opts)

    form = '<form method="get" action="/comparer" class="filters">{}{}<button type="submit" class="btn">{}</button></form>'.format(
        select("a", sa), select("b", sb), esc(t("cmp.go")))
    body = ""
    if a and b and a is b:
        body = '<p class="empty">{}</p>'.format(esc(t("cmp.same")))
    elif a and b:
        head = "".join(
            '<th scope="col"><a class="cmphead" href="/encyclopedie/{slug}">{gem}<span>{name}</span></a></th>'.format(
                slug=esc(s["slug"]), gem=gem_svg(s["iconColor"], s["iconFacetColor"], 52), name=esc(s["displayName"])) for s in (a, b))
        rows = []
        ndiff = 0
        for label, va, vb, gloss, differs in _compare_rows(req, a, b):
            ndiff += 1 if differs else 0
            lab = '<a class="gloss" href="/glossaire#{}">{}</a>'.format(gloss, esc(label)) if gloss else esc(label)
            rows.append('<tr class="{cls}"><th scope="row">{lab}</th><td>{va}</td><td>{vb}</td></tr>'.format(
                cls="diff" if differs else "same", lab=lab, va=esc(va), vb=esc(vb)))
        verdicts = _verdicts(req, a, b, d)
        if verdicts:
            items = "".join('<li><a href="/lecons/{}">{}</a> — {}</li>'.format(
                esc(lid), esc((catalog.lesson(lid) or {"title": lid})["title"]), esc(text)) for lid, text in verdicts)
            sep = '<h2>{}</h2><ul class="sep">{}</ul>'.format(esc(t("cmp.sep.title")), items)
        else:
            sep = '<h2>{}</h2><p>{}</p>'.format(esc(t("cmp.sep.title")), esc(t("cmp.sep.none")))
        body = ('{sep}<h2>{tbl}</h2><p class="small">{legend}</p><table class="props cmp"><thead><tr><th></th>{head}</tr></thead><tbody>{rows}</tbody></table>').format(
            sep=sep, tbl=esc(t("cmp.table")), legend=esc(t("cmp.legend", n=ndiff)), head=head, rows="".join(rows))
    elif sa or sb:
        body = '<p class="empty">{}</p>'.format(esc(t("cmp.pick")))
    else:
        body = '<p class="empty">{}</p>'.format(esc(t("cmp.empty")))
    content = '<h1>{}</h1><p class="lead">{}</p>{}{}'.format(esc(t("cmp.title")), esc(t("cmp.lead")), form, body)
    return page(req, t("cmp.title"), content, section="encyclopedia")


# ------------------------------------------------------------------ search in the whole site

SEARCH_MAX = 80


def _fold_chars(text):
    """Like catalog.fold but character by character, so that positions in the result match positions in the text."""
    return "".join(catalog.fold(c) or c for c in text)


def _plain(html):
    return re.sub(r"\s+", " ", re.sub(r"<[^>]+>", "", html or "")).strip()


def _highlight(text, words, start=0, end=None):
    """The text between two positions, escaped, with the searched words in <mark>."""
    end = len(text) if end is None else end
    folded = _fold_chars(text)
    spans = []
    for w in words:
        pos = folded.find(w, start, end)
        while pos != -1:
            spans.append((pos, pos + len(w)))
            pos = folded.find(w, pos + len(w), end)
    spans.sort()
    out, cursor = [], start
    for a, b in spans:
        if a < cursor:
            continue
        out.append(esc(text[cursor:a]))
        out.append("<mark>{}</mark>".format(esc(text[a:b])))
        cursor = b
    out.append(esc(text[cursor:end]))
    return "".join(out)


def _snippet(text, words, width=170):
    folded = _fold_chars(text)
    hits = [i for i in (folded.find(w) for w in words) if i != -1]
    first = min(hits) if hits else 0
    start = max(0, first - 45)
    if start:
        start = text.find(" ", start) + 1 or start
    end = min(len(text), start + width)
    if end < len(text):
        end = text.rfind(" ", start, end) if text.rfind(" ", start, end) > start else end
    return ("\u2026" if start else "") + _highlight(text, words, start, end) + ("\u2026" if end < len(text) else "")


def search_site(q):
    """Species, lessons and glossary terms that contain every word of the query, the best matches first."""
    words = [w for w in catalog.fold(q).split() if w][:6]
    if not words:
        return [], [], []
    species = catalog.search(q=q)
    species.sort(key=lambda s: (not catalog.fold(s["displayName"]).startswith(words[0]), catalog.fold(s["displayName"])))

    lessons = []
    for item in catalog.lessons():
        tool = item.get("tool") or {}
        head = " ".join([item["title"], tool.get("name", ""), tool.get("measures", "")] + [st["title"] for st in item["steps"]])
        steps = [_plain(st["text"]) for st in item["steps"]]
        everything = _fold_chars(head + " " + " ".join(steps))
        if not all(w in everything for w in words):
            continue
        title_folded = _fold_chars(item["title"])
        score = sum(5 for w in words if w in title_folded) + sum(2 for w in words if w in _fold_chars(head))
        best, best_hits = None, 0
        for i, text in enumerate(steps):
            folded = _fold_chars(text + " " + item["steps"][i]["title"])
            hits = sum(1 for w in words if w in folded)
            if hits > best_hits:
                best, best_hits = i, hits
        lessons.append((score + best_hits, item, best))
    lessons.sort(key=lambda x: -x[0])

    glossary = []
    for term in catalog.glossary()[0]:
        names = _fold_chars(" ".join([term["term"]] + term.get("aliases", [])))
        folded = names + " " + _fold_chars(term["def"])
        if all(w in folded for w in words):
            score = sum(5 for w in words if names.startswith(w)) + sum(3 for w in words if w in names) + 1
            glossary.append((score, term))
    glossary.sort(key=lambda x: (-x[0], catalog.fold(x[1]["term"])))
    return species, lessons, [g[1] for g in glossary]


@router.route("GET", "/recherche")
def search_page(req):
    t = req.t
    q = req.query.get("q", "").strip()[:SEARCH_MAX]
    form = ('<form class="herosearch bigsearch" method="get" action="/recherche" role="search"><input type="search" name="q" value="{q}" '
            'placeholder="{ph}" aria-label="{ph}" autofocus><button class="btn" type="submit">{go}</button></form>').format(
        q=esc(q), ph=esc(t("home.search")), go=esc(t("home.search.go")))
    if not q:
        ideas = " ".join('<a class="chip" href="/recherche?q={w}">{w}</a>'.format(w=esc(w)) for w in t("search.ideas").split(","))
        body = '<p class="empty">{}</p><p class="ideas">{}</p>'.format(esc(t("search.empty")), ideas)
        return page(req, t("search.title"), '<h1>{}</h1>{}{}'.format(esc(t("search.title")), form, body))
    species, lessons, terms = search_site(q)
    total = len(species) + len(lessons) + len(terms)
    words = [w for w in catalog.fold(q).split() if w][:6]
    parts = []
    if species:
        tiles = "".join(
            '<li><a class="tile" style="--gem:{gc}" href="/encyclopedie/{slug}">{gem}<span class="name">{name}</span>'
            '<span class="sub">{sub}</span></a></li>'.format(
                gc=esc(s["iconColor"]), slug=esc(s["slug"]), gem=gem_svg(s["iconColor"], s["iconFacetColor"], 44), name=esc(s["displayName"]),
                sub=esc(t("enc.tile.sub", hardness=fmt_num(s.get("hardness"), 1), ri=fmt_num(s.get("refractiveIndex"), 3)))) for s in species[:24])
        more = ('<p class="side-link"><a href="/encyclopedie?q={}">{}</a></p>'.format(esc(q), esc(t("search.more", n=len(species)))) if len(species) > 24 else "")
        parts.append('<section><h2>{} <span class="count">({})</span></h2><ul class="tiles">{}</ul>{}</section>'.format(
            esc(t("search.species")), len(species), tiles, more))
    if lessons:
        items = []
        for score, item, step in lessons[:12]:
            st = item["steps"][step] if step is not None else None
            snippet = _snippet(_plain(st["text"]), words) if st else ""
            anchor = "#e{}".format(step + 1) if st else ""
            items.append('<li><a href="/lecons/{id}{anchor}"><strong>{title}</strong></a>{where}<p>{snip}</p></li>'.format(
                id=esc(item["id"]), anchor=anchor, title=_highlight(item["title"], words),
                where=' <span class="small">\u2014 {}</span>'.format(esc(st["title"])) if st else "", snip=snippet))
        parts.append('<section><h2>{} <span class="count">({})</span></h2><ul class="results">{}</ul></section>'.format(
            esc(t("search.lessons")), len(lessons), "".join(items)))
    if terms:
        items = "".join('<li><a href="/glossaire#{id}"><strong>{term}</strong></a><p>{d}</p></li>'.format(
            id=esc(tm["id"]), term=_highlight(tm["term"], words), d=_highlight(tm["def"], words)) for tm in terms[:12])
        parts.append('<section><h2>{} <span class="count">({})</span></h2><ul class="results">{}</ul></section>'.format(
            esc(t("search.glossary")), len(terms), items))
    if not parts:
        links = " ".join('<a class="chip" href="{}">{}</a>'.format(h, esc(t(k))) for h, k in (
            ("/encyclopedie", "nav.encyclopedia"), ("/lecons", "nav.lessons"), ("/glossaire", "gloss.title"), ("/identification", "nav.identify")))
        parts.append('<p class="empty">{}</p><p>{}</p>'.format(esc(t("search.none", q=q)), links))
    content = '<h1>{}</h1>{}<p class="count">{}</p>{}'.format(esc(t("search.title")), form, esc(t("search.count", n=total, q=q)), "".join(parts))
    return page(req, t("search.title") + " : " + q, content)


# ------------------------------------------------------------------ the mixed quiz: 20 questions drawn from all the lessons

MIXED_LENGTH = 20
MIXED_PER_LESSON = 2          # at most this many questions of the same lesson, so that the quiz covers the whole course
THEMES = {"all": None, "base": "concept", "instruments": "tool"}


def _theme(value):
    return value if value in THEMES else "all"


def _pool(theme):
    kind = THEMES[theme]
    return [(it["id"], i) for it in catalog.lessons() if kind is None or it["kind"] == kind for i in range(len(catalog.quiz(it["id"]) or []))]


def draw_mixed(theme="all", length=MIXED_LENGTH):
    """(lesson id, question number) pairs: random, spread over the lessons, in a random order."""
    pool = _pool(theme)
    random.shuffle(pool)
    chosen, count = [], {}
    for lid, i in pool:
        if count.get(lid, 0) < MIXED_PER_LESSON:
            chosen.append((lid, i))
            count[lid] = count.get(lid, 0) + 1
    if len(chosen) < length:           # a small theme: relax the limit per lesson
        chosen += [x for x in pool if x not in chosen][:length - len(chosen)]
    chosen = chosen[:length]
    random.shuffle(chosen)
    return chosen


def mixed_results(db, user, limit=5):
    if not user:
        return [], None
    rows = db.execute("SELECT * FROM mixed_quiz WHERE user_id = ? ORDER BY id DESC", (user["id"],)).fetchall()
    return rows[:limit], rows


@router.route("GET", "/quiz")
def mixed_home(req):
    t = req.t
    banks = {th: len(_pool(th)) for th in THEMES}
    recent, allrows = mixed_results(req.db, req.user)
    mine = ""
    if allrows:
        best = max(r["score"] * 100 // r["total"] for r in allrows)
        mine = '<p class="quizstatus">{}</p>'.format(esc(t("mixed.mine", n=len(allrows), best=best)))
    elif not req.user:
        mine = '<p class="small">{} <a href="/inscription">{}</a></p>'.format(esc(t("mixed.guest")), esc(t("nav.register")))
    cards = "".join(
        '<a class="step-card themecard" href="/quiz/jouer?theme={th}"><h3>{title}</h3><p>{text}</p><span class="more">{go} \u2192</span></a>'.format(
            th=th, title=esc(t("mixed.theme." + th)), text=esc(t("mixed.theme.{}.text".format(th), n=banks[th])), go=esc(t("mixed.start")))
        for th in ("all", "base", "instruments"))
    recent_html = ""
    if recent:
        recent_html = '<h2>{}</h2><ul class="recent">{}</ul>'.format(esc(t("mixed.recent")), "".join(
            '<li><strong>{} / {}</strong> <span class="small">{}</span></li>'.format(r["score"], r["total"], esc(t("mixed.theme." + _theme(r["theme"])))) for r in recent))
    content = ('<h1>{h1}</h1><p class="lead">{lead}</p>{mine}<div class="themes">{cards}</div>'
               '<p class="small">{how}</p>{recent}<p class="side-link"><a href="/lecons">{les}</a> \u00b7 <a href="/glossaire">{gl}</a></p>').format(
        h1=esc(t("mixed.title")), lead=esc(t("mixed.lead", n=MIXED_LENGTH)), mine=mine, cards=cards, how=esc(t("mixed.how")),
        recent=recent_html, les=esc(t("les.title")), gl=esc(t("gloss.title")))
    return page(req, t("mixed.title"), content, section="quiz", description=t("mixed.desc"))


@router.route("GET", "/quiz/jouer")
def mixed_play(req):
    t = req.t
    theme = _theme(req.query.get("theme", "all"))
    drawn = draw_mixed(theme)
    blocks, tokens = [], []
    for n, (lid, i) in enumerate(drawn):
        q = catalog.quiz(lid)[i]
        order = list(range(len(q["choices"])))
        random.shuffle(order)
        name = "m_{}_{}".format(lid, i)
        choices = "".join('<label class="choice"><input type="radio" name="{nm}" value="{k}" {req}> <span>{c}</span></label>'.format(
            nm=name, k=k, req="required" if pos == 0 else "", c=esc(q["choices"][k])) for pos, k in enumerate(order))
        blocks.append('<fieldset class="question"><legend><span class="num">{n}</span> {q}</legend>{c}</fieldset>'.format(n=n + 1, q=esc(q["q"]), c=choices))
        tokens.append("{}:{}".format(lid, i))
    content = ('<nav class="crumbs"><a href="/quiz">{back}</a></nav><h1>{h1}</h1><p class="lead">{lead}</p>'
               '<p class="counter" aria-live="polite" data-total="{n}" data-label="{label}"></p>'
               '<form method="post" action="/quiz/jouer" class="form quiz-mixed">{csrf}<input type="hidden" name="theme" value="{theme}">'
               '<input type="hidden" name="asked" value="{asked}">{blocks}<button type="submit" class="btn big">{go}</button></form>'
               '<script src="/static/js/quiz.js" defer></script>').format(
        back=esc(t("mixed.title")), h1=esc(t("mixed.theme." + theme)), lead=esc(t("mixed.play.lead", n=len(drawn))), n=len(drawn),
        label=esc(t("mixed.counter")), csrf=csrf_field(req), theme=theme, asked=",".join(tokens), blocks="".join(blocks), go=esc(t("mixed.submit")))
    return page(req, t("mixed.title"), content, section="quiz")


@router.route("POST", "/quiz/jouer")
def mixed_answer(req):
    t = req.t
    theme = _theme(req.form.get("theme", "all"))
    asked = []
    for part in req.form.get("asked", "").split(","):
        lid, _, num = part.partition(":")
        bank = catalog.quiz(lid) if catalog.SAFE_ID.match(lid) else None
        if bank and num.isdigit() and int(num) < len(bank) and (lid, int(num)) not in asked:
            asked.append((lid, int(num)))
    asked = asked[:MIXED_LENGTH]
    if not asked:
        return redirect("/quiz")
    score, rows, wrong = 0, [], {}
    for n, (lid, i) in enumerate(asked):
        q = catalog.quiz(lid)[i]
        raw = req.form.get("m_{}_{}".format(lid, i), "")
        chosen = int(raw) if raw.isdigit() and int(raw) < len(q["choices"]) else None
        ok = chosen == q["answer"]
        score += 1 if ok else 0
        if not ok:
            wrong[lid] = wrong.get(lid, 0) + 1
        lesson = catalog.lesson(lid)
        see = "" if ok else '<p class="small"><a href="/lecons/{}">{}</a></p>'.format(esc(lid), esc(t("mixed.review", title=lesson["title"])))
        rows.append(
            '<article class="answer {cls}"><h3><span class="num">{n}</span> {q}</h3><p class="verdict">{verdict}</p><p>{yl} {yours}</p>{good}'
            '<p class="why">{why}</p>{see}</article>'.format(
                cls="ok" if ok else "ko", n=n + 1, q=esc(q["q"]), verdict=esc(t("quiz.right" if ok else "quiz.wrong")), yl=esc(t("quiz.yours")),
                yours=esc(q["choices"][chosen]) if chosen is not None else esc(t("quiz.none")),
                good="" if ok else "<p>{} <strong>{}</strong></p>".format(esc(t("quiz.correct")), esc(q["choices"][q["answer"]])),
                why=esc(q["why"]), see=see))
    total = len(asked)
    if req.user:
        req.db.execute("INSERT INTO mixed_quiz (user_id, score, total, theme, created_at) VALUES (?, ?, ?, ?, ?)", (req.user["id"], score, total, theme, now()))
    review = ""
    if wrong:
        items = "".join('<li><a href="/lecons/{id}">{title}</a> <span class="small">({n})</span></li>'.format(
            id=esc(lid), title=esc(catalog.lesson(lid)["title"]), n=esc(t("mixed.errors", n=n))) for lid, n in sorted(wrong.items(), key=lambda x: -x[1]))
        review = '<section class="review"><h2>{}</h2><ul>{}</ul></section>'.format(esc(t("mixed.tosee")), items)
    ratio = score / total
    verdict = t("mixed.v.top") if ratio >= 0.9 else t("mixed.v.good") if ratio >= PASS_RATIO else t("mixed.v.mid") if ratio >= 0.5 else t("mixed.v.low")
    guest = "" if req.user else '<div class="banner warn">{} <a href="/inscription">{}</a></div>'.format(esc(t("quiz.guest.saved")), esc(t("nav.register")))
    nav = ['<a class="btn" href="/quiz/jouer?theme={}">{}</a>'.format(theme, esc(t("mixed.again"))), '<a class="btn ghost" href="/quiz">{}</a>'.format(esc(t("mixed.other")))]
    if req.user:
        nav.append('<a class="btn ghost" href="/progression">{}</a>'.format(esc(t("quiz.progress"))))
    content = ('<nav class="crumbs"><a href="/quiz">{back}</a></nav><h1>{h1}</h1><p class="score"><strong>{score}</strong> / {total} \u2014 {verdict}</p>'
               '{guest}{review}<h2>{corr}</h2>{rows}<p class="actions">{nav}</p>').format(
        back=esc(t("mixed.title")), h1=esc(t("mixed.result")), score=score, total=total, verdict=esc(verdict), guest=guest, review=review,
        corr=esc(t("mixed.correction")), rows="".join(rows), nav=" ".join(nav))
    return page(req, t("mixed.result"), content, section="quiz")
