"""The Prices tab: an estimate of the price of a coloured gemstone from the prices that members and the administrator have recorded,
the form to add a price (members), and the moderation of the new prices (administrator)."""
import math

from . import catalog, forum, prices, security
from .forum import Denied
from .routes_forum import members_only
from .views import csrf_field, esc, fmt_num, gem_svg, honeypot, page
from .web import HTTPError, Router, redirect

router = Router()


# ------------------------------------------------------------------ small helpers

def _options(t, prefix, values, selected, any_label=None):
    opts = ['<option value="">{}</option>'.format(esc(any_label))] if any_label else []
    opts += ['<option value="{}"{}>{}</option>'.format(esc(str(v)), " selected" if str(v) == str(selected) else "", esc(t(prefix + str(v)))) for v in values]
    return "".join(opts)


def _species_select(species, selected_id, blank):
    opts = '<option value="">{}</option>'.format(esc(blank)) + "".join(
        '<option value="{}"{}>{}</option>'.format(esc(s["slug"]), " selected" if s["id"] == selected_id else "", esc(s["displayName"]))
        for s in sorted(species, key=lambda s: catalog.fold(s["displayName"])))
    return opts


def _ticks(lo, hi, candidates):
    return [v for v in candidates if lo <= v <= hi]


NICE_W = [0.1, 0.2, 0.3, 0.5, 1, 2, 3, 5, 10, 20, 50, 100, 200]
NICE_P = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000, 200000]


def chart_svg(t, rows, est, weight):
    """Scatter of the recorded prices (weight against price per carat, both on a log scale), the median of each weight band and the
    range asked for. Drawn with the CSS classes of .pricechart."""
    W, H, left, right, top, bottom = 560, 300, 62, 14, 14, 46
    values = [r["ppc"] for r in rows]
    if est.get("ppc"):
        values += [est["ppc"][0], est["ppc"][2]]
    wlo, whi = min([r["weight"] for r in rows] + [weight]) / 1.2, max([r["weight"] for r in rows] + [weight]) * 1.2
    plo, phi = min(values) / 1.4, max(values) * 1.4

    def x(w):
        return left + (math.log(w) - math.log(wlo)) / (math.log(whi) - math.log(wlo)) * (W - left - right)

    def y(p):
        return H - bottom - (math.log(p) - math.log(plo)) / (math.log(phi) - math.log(plo)) * (H - top - bottom)

    out = ['<svg class="pricechart" viewBox="0 0 {} {}" role="img" aria-label="{}">'.format(W, H, esc(t("price.chart.label")))]
    for p in _ticks(plo, phi, NICE_P):
        out.append('<line class="grid" x1="{l}" x2="{r}" y1="{y:.1f}" y2="{y:.1f}"/><text class="tick" x="{tx}" y="{y:.1f}" text-anchor="end" dy="4">{p}</text>'.format(
            l=left, r=W - right, y=y(p), tx=left - 6, p=esc(prices.fmt_eur(p))))
    for w in _ticks(wlo, whi, NICE_W):
        out.append('<line class="grid" y1="{t}" y2="{b}" x1="{x:.1f}" x2="{x:.1f}"/><text class="tick" x="{x:.1f}" y="{ty}" text-anchor="middle">{w}</text>'.format(
            t=top, b=H - bottom, x=x(w), ty=H - bottom + 16, w=esc(fmt_num(w, 2))))
    out.append('<text class="axis" x="{}" y="{}" text-anchor="middle">{}</text>'.format((left + W - right) / 2, H - 6, esc(t("price.chart.x"))))
    out.append('<text class="axis" x="12" y="{}" text-anchor="middle" transform="rotate(-90 12 {})">{}</text>'.format((top + H - bottom) / 2, (top + H - bottom) / 2, esc(t("price.chart.y"))))
    # the median of each band
    points = []
    for b in est["bands"]:
        if b["n"] >= prices.MIN_N and b["median"]:
            points.append((x(math.sqrt(max(b["lo"], wlo) * min(b["hi"], whi))), y(b["median"])))
    if len(points) >= 2:
        out.append('<polyline class="median" points="{}"/>'.format(" ".join("{:.1f},{:.1f}".format(*p) for p in points)))
    for r in rows:
        out.append('<circle class="pt {}" cx="{:.1f}" cy="{:.1f}" r="3.6"/>'.format(r["kind"], x(r["weight"]), y(r["ppc"])))
    # the weight asked for, and its range
    out.append('<line class="ask" x1="{x:.1f}" x2="{x:.1f}" y1="{t}" y2="{b}"/>'.format(x=x(weight), t=top, b=H - bottom))
    if est.get("ppc"):
        out.append('<line class="range" x1="{x:.1f}" x2="{x:.1f}" y1="{a:.1f}" y2="{b:.1f}"/>'.format(x=x(weight), a=y(est["ppc"][2]), b=y(est["ppc"][0])))
        out.append('<circle class="mid" cx="{:.1f}" cy="{:.1f}" r="5"/>'.format(x(weight), y(est["ppc"][1])))
    out.append("</svg>")
    return "".join(out)


def _estimate_block(req, species, est, rows, weight):
    t = req.t
    if est["status"] == "none":
        body = '<p class="empty">{}</p>'.format(esc(t("price.none")))
    elif est["status"] == "few":
        body = '<p class="empty">{}</p>'.format(esc(t("price.few", n=est["n_total"], nmin=prices.MIN_N)))
    else:
        low, med, high = est["total"]
        plow, pmed, phigh = est["ppc"]
        method = (t("price.method.band", lo=fmt_num(est["band"][0], 2), hi=fmt_num(min(est["band"][1], 500), 2), n=est["n_band"]) if est["method"] == "band"
                  else t("price.method.trend"))
        body = (
            '<div class="estimate"><p class="range">{total}</p><p class="mid">{median}</p><p>{ppc}</p>'
            '<p class="conf conf-{conf}">{confidence}</p><p class="small">{method}</p></div>').format(
            total=esc(t("price.result.total", low=prices.fmt_eur(low), high=prices.fmt_eur(high))), median=esc(t("price.result.median", median=prices.fmt_eur(med))),
            ppc=esc(t("price.result.ppc", low=prices.fmt_eur(plow), high=prices.fmt_eur(phigh), median=prices.fmt_eur(pmed))),
            conf=est["confidence"], confidence=esc(t("price.conf." + est["confidence"])), method=esc(method))
    sources = ""
    if est["n_total"]:
        sources = '<p class="small">{}</p>'.format(esc(t("price.result.n", n=est["n_total"], asking=est["asking"], paid=est["paid"], date=est["newest"], months=prices.MAX_AGE_MONTHS)))
    chart = ""
    if rows:
        lines = "".join("<tr><th scope=\"row\">{lo} – {hi}</th><td>{n}</td><td>{med}</td><td>{rng}</td></tr>".format(
            lo=esc(fmt_num(b["lo"], 2)), hi=esc(fmt_num(min(b["hi"], 500), 2)), n=b["n"],
            med=esc(prices.fmt_eur(b["median"])) if b["n"] >= prices.MIN_N and b["median"] else "—",
            rng="{} – {}".format(prices.fmt_eur(b["p25"]), prices.fmt_eur(b["p75"])) if b["n"] >= prices.MIN_N and b["median"] else "—")
            for b in est["bands"] if b["n"])
        chart = ('<h3>{ht}</h3><div class="chartbox">{svg}</div><p class="small">{legend}</p>'
                 '<table class="props bands"><caption class="sr-only">{ht}</caption><thead><tr><th>{c1}</th><th>{c2}</th><th>{c3}</th><th>{c4}</th></tr></thead><tbody>{lines}</tbody></table>').format(
            ht=esc(t("price.bands.title")), svg=chart_svg(t, rows, est, weight), legend=esc(t("price.chart.legend")),
            c1=esc(t("price.col.band")), c2=esc(t("price.col.n")), c3=esc(t("price.col.median")), c4=esc(t("price.col.range")), lines=lines)
    invite = '<p><a class="btn" href="/prix/ajouter?s={}">{}</a></p>'.format(esc(species["slug"]), esc(t("price.invite")))
    return ('<section class="result"><h2>{title}</h2>{body}{sources}{chart}{invite}</section>').format(
        title=esc(t("price.result.title", species=species["displayName"], weight=fmt_num(weight, 2))), body=body, sources=sources, chart=chart, invite=invite)


# ------------------------------------------------------------------ the public tab

@router.route("GET", "/prix")
def prices_page(req):
    t = req.t
    q = req.query
    species_list = prices.eligible_species()
    by_slug = {s["slug"]: s for s in species_list}
    species = by_slug.get(q.get("s", ""))
    weight = prices.parse_number(q.get("w", "")) if q.get("w") else None
    color, clarity, treatment, kind = q.get("c", ""), q.get("q", ""), q.get("t", ""), q.get("k", "")
    color = color if color.isdigit() and int(color) in prices.COLOR_TIERS else ""
    clarity = clarity if clarity in prices.CLARITIES else ""
    treatment = treatment if treatment in prices.TREATMENTS else ""
    kind = kind if kind in prices.KINDS else ""
    form = (
        '<form class="filters pricesform" method="get" action="/prix" role="search">'
        '<label class="grow">{ls}<select name="s" required>{so}</select></label>'
        '<label>{lw}<input type="text" inputmode="decimal" name="w" value="{wv}" placeholder="1,5" required></label>'
        '<label>{lc}<select name="c">{co}</select></label><label>{lq}<select name="q">{qo}</select></label>'
        '<label>{lt}<select name="t">{to}</select></label><label>{lk}<select name="k">{ko}</select></label>'
        '<button type="submit" class="btn">{go}</button></form>').format(
        ls=esc(t("price.form.species")), so=_species_select(species_list, species["id"] if species else "", t("price.choose")),
        lw=esc(t("price.form.weight")), wv=esc(q.get("w", "")), lc=esc(t("price.form.color")), co=_options(t, "price.color.", prices.COLOR_TIERS, color, t("price.any")),
        lq=esc(t("price.form.clarity")), qo=_options(t, "price.clarity.", prices.CLARITIES, clarity, t("price.any")),
        lt=esc(t("price.form.treatment")), to=_options(t, "price.treat.", prices.TREATMENTS, treatment, t("price.any")),
        lk=esc(t("price.form.kind")), ko=_options(t, "price.kind.", prices.KINDS, kind, t("price.kind.all")), go=esc(t("price.go")))
    result = ""
    if species and weight is not None and prices.WEIGHT_MIN <= weight <= prices.WEIGHT_MAX:
        rows = prices.approved_rows(req.db, species["id"], kind or None, int(color) if color else None, clarity or None, treatment or None)
        result = _estimate_block(req, species, prices.estimate(rows, weight), rows, weight)
    elif q.get("s") or q.get("w"):
        result = '<p class="empty">{}</p>'.format(esc(t("price.pick")))
    top = ""
    counts = prices.counts_by_species(req.db)
    ranked = sorted(((counts[s["id"]], s) for s in species_list if counts.get(s["id"])), key=lambda x: (-x[0], catalog.fold(x[1]["displayName"])))[:12]
    if not result:
        if ranked:
            tiles = "".join('<li><a class="tile" style="--gem:{gc}" href="/prix?s={slug}&amp;w=1">{gem}<span class="name">{name}</span><span class="sub">{n}</span></a></li>'.format(
                gc=esc(s["iconColor"]), slug=esc(s["slug"]), gem=gem_svg(s["iconColor"], s["iconFacetColor"], 44, s.get("iconStyle")), name=esc(s["displayName"]),
                n=esc(t("price.top.count", n=n))) for n, s in ranked)
            top = '<h2>{}</h2><ul class="tiles">{}</ul>'.format(esc(t("price.top.title")), tiles)
        else:
            top = '<p class="empty">{}</p>'.format(esc(t("price.top.empty")))
    content = ('<h1>{h1}</h1><p class="lead">{lead}</p><div class="banner warn pricewarn"><strong>{wt}</strong> {wx}</div>{form}{result}{top}'
               '{how}<p class="side-link"><a href="/prix/ajouter">{add}</a></p>').format(
        h1=esc(t("price.title")), lead=esc(t("price.lead")), wt=esc(t("price.disclaimer.title")), wx=esc(t("price.disclaimer.text")), form=form,
        result=result, top=top, add=esc(t("price.add.link")),
        how='<details class="how"><summary>{}</summary><p>{}</p><p>{}</p></details>'.format(esc(t("price.how.title")), esc(t("price.how.p1")), esc(t("price.how.p2"))))
    return page(req, t("price.title"), content, section="prices", description=t("price.desc"))


# ------------------------------------------------------------------ adding a price (members)

def _my_reports(req):
    t = req.t
    rows = req.db.execute("SELECT * FROM price_reports WHERE user_id = ? ORDER BY id DESC LIMIT 10", (req.user["id"],)).fetchall()
    if not rows:
        return ""
    names = {s["id"]: s["displayName"] for s in catalog.data()["species"]}
    lines = "".join("<tr><td>{sp}</td><td>{w} ct</td><td>{p} €</td><td>{st}</td></tr>".format(
        sp=esc(names.get(r["species_id"], r["species_id"])), w=esc(fmt_num(r["weight_ct"], 2)), p=esc(prices.fmt_eur(r["price_eur"])), st=esc(t("price.status." + r["status"]))) for r in rows)
    return '<h2>{}</h2><table class="props"><tbody>{}</tbody></table>'.format(esc(t("price.mine")), lines)


def _add_form(req, values=None, error=None):
    t = req.t
    v = values or {}
    species_list = prices.eligible_species()
    by_slug = {s["slug"]: s for s in species_list}
    chosen = by_slug.get(v.get("s", ""))
    month = v.get("observed") or prices.this_month()
    err = '<div class="banner err" role="alert">{}</div>'.format(esc(t(error))) if error else ""
    form = (
        '{err}<form method="post" action="/prix/ajouter" class="form">{csrf}{hp}'
        '<label class="field">{ls}<select name="species" required>{so}</select></label>'
        '<label class="field">{lw}<input type="text" inputmode="decimal" name="weight" value="{wv}" required placeholder="1,5"></label>'
        '<label class="field">{lp}<input type="text" inputmode="decimal" name="price" value="{pv}" required placeholder="450"></label>'
        '<label class="field">{lk}<select name="kind" required>{ko}</select></label>'
        '<label class="field">{lc}<select name="color" required>{co}</select></label>'
        '<p class="hint">{hc}</p>'
        '<label class="field">{lq}<select name="clarity" required>{qo}</select></label>'
        '<label class="field">{lt}<select name="treatment" required>{to}</select></label>'
        '<label class="field">{lm}<input type="month" name="observed" value="{mv}" required></label>'
        '<label class="check"><input type="checkbox" name="confirm" value="1" required> {cf}</label>'
        '<button type="submit" class="btn">{go}</button></form>').format(
        err=err, csrf=csrf_field(req), hp=honeypot(), ls=esc(t("price.add.species")), so=_species_select(species_list, chosen["id"] if chosen else "", t("price.choose")),
        lw=esc(t("price.add.weight")), wv=esc(v.get("weight", "")), lp=esc(t("price.add.price")), pv=esc(v.get("price", "")),
        lk=esc(t("price.add.kind")), ko=_options(t, "price.kind.", prices.KINDS, v.get("kind", "")),
        lc=esc(t("price.add.color")), co=_options(t, "price.color.", prices.COLOR_TIERS, v.get("color", ""), t("price.choose")), hc=esc(t("price.add.color.help")),
        lq=esc(t("price.add.clarity")), qo=_options(t, "price.clarity.", prices.CLARITIES, v.get("clarity", ""), t("price.choose")),
        lt=esc(t("price.add.treatment")), to=_options(t, "price.treat.", prices.TREATMENTS, v.get("treatment", ""), t("price.choose")),
        lm=esc(t("price.add.month")), mv=esc(month), cf=esc(t("price.add.confirm")), go=esc(t("price.add.submit")))
    return form


@router.route("GET", "/prix/ajouter")
def add_page(req):
    r = members_only(req)
    if r:
        return r
    t = req.t
    sel = req.query.get("s", "")
    values = {"species": sel}
    content = '<nav class="crumbs"><a href="/prix">{back}</a></nav><h1>{h1}</h1><p class="lead">{lead}</p><div class="banner warn">{rules}</div>{form}{mine}'.format(
        back=esc(t("price.title")), h1=esc(t("price.add.title")), lead=esc(t("price.add.lead")), rules=esc(t("price.add.rules")),
        form=_add_form(req, {"s": sel}), mine=_my_reports(req))
    return page(req, t("price.add.title"), content, section="prices")


@router.route("POST", "/prix/ajouter")
def add_submit(req):
    r = members_only(req)
    if r:
        return r
    f = req.form
    if f.get("website"):
        raise HTTPError(400)
    t = req.t
    species_by_slug = {s["slug"]: s for s in prices.eligible_species()}
    form = dict(f)
    form["species"] = species_by_slug[f.get("species", "")]["id"] if f.get("species") in species_by_slug else ""
    try:
        forum.require_verified(req.user)
        if not f.get("confirm"):
            raise Denied("price.err.confirm", 400)
        values = prices.clean_report(form, {s["id"] for s in species_by_slug.values()})
        if security.over_limit(req.db, "price_user_day", req.user["id"]):
            raise Denied("err.rate_hour", 429)
        if prices.is_duplicate(req.db, req.user["id"], values):
            raise Denied("err.duplicate", 400)
        security.record(req.db, "price_user_day", req.user["id"])
        approved = forum.is_admin(req.user)
        prices.add_report(req.db, req.user["id"], values, "approved" if approved else "pending")
    except Denied as d:
        content = '<nav class="crumbs"><a href="/prix">{back}</a></nav><h1>{h1}</h1>{form}'.format(
            back=esc(t("price.title")), h1=esc(t("price.add.title")), form=_add_form(req, {"s": f.get("species", ""), **f}, d.key))
        return page(req, t("price.add.title"), content, section="prices", status=d.status)
    req.flash("flash.price_thanks" if not approved else "flash.price_added")
    return redirect("/prix/ajouter")


# ------------------------------------------------------------------ moderation (administrator)

@router.route("GET", "/admin/prix")
def admin_page(req):
    r = members_only(req)
    if r:
        return r
    if not forum.is_admin(req.user):
        raise HTTPError(403, "err.admin_only")
    t = req.t
    names = {s["id"]: s["displayName"] for s in catalog.data()["species"]}
    pending = req.db.execute("SELECT * FROM price_reports WHERE status = 'pending' ORDER BY id LIMIT 50").fetchall()
    cards = []
    for p in pending:
        flag = prices.plausibility(req.db, p["species_id"], p["weight_ct"], p["price_eur"])
        cards.append(
            '<article class="post"><header><strong>{sp}</strong> <span class="meta">{w} ct · {p} € · {ppc} €/ct</span></header>'
            '<p class="small">{kind} · {color} · {clarity} · {treat} · {month}</p>{flag}'
            '<footer><form method="post" action="/admin/prix/{id}/approuver" class="inline">{csrf}<button type="submit" class="btn small">{ok}</button></form> '
            '<form method="post" action="/admin/prix/{id}/rejeter" class="inline">{csrf}<button type="submit" class="btn small danger">{ko}</button></form></footer></article>'.format(
                sp=esc(names.get(p["species_id"], p["species_id"])), w=esc(fmt_num(p["weight_ct"], 2)), p=esc(prices.fmt_eur(p["price_eur"])),
                ppc=esc(prices.fmt_eur(p["price_eur"] / p["weight_ct"])), kind=esc(t("price.kind." + p["kind"])), color=esc(t("price.color." + str(p["color_tier"]))),
                clarity=esc(t("price.clarity." + p["clarity"])), treat=esc(t("price.treat." + p["treatment"])), month=esc(p["observed_at"]),
                flag='<p class="banner warn">{}</p>'.format(esc(t("price.admin.flag"))) if flag else "", id=p["id"], csrf=csrf_field(req),
                ok=esc(t("price.admin.approve")), ko=esc(t("price.admin.reject"))))
    counts = {row["status"]: row["n"] for row in req.db.execute("SELECT status, COUNT(*) AS n FROM price_reports GROUP BY status")}
    content = '<h1>{h1}</h1><p>{sum}</p>{body}<p class="side-link"><a href="/admin">{back}</a></p>'.format(
        h1=esc(t("price.admin.title")), sum=esc(t("price.admin.summary", pending=counts.get("pending", 0), approved=counts.get("approved", 0), rejected=counts.get("rejected", 0))),
        body="".join(cards) or '<p class="empty">{}</p>'.format(esc(t("price.admin.none"))), back=esc(t("admin.title")))
    return page(req, t("price.admin.title"), content)


def _review(req, approve):
    if not req.user or not forum.is_admin(req.user):
        raise HTTPError(403, "err.admin_only")
    prices.review(req.db, req.params["report_id"], req.user["id"], approve)
    return redirect("/admin/prix")


@router.route("POST", "/admin/prix/<int:report_id>/approuver")
def approve(req):
    return _review(req, True)


@router.route("POST", "/admin/prix/<int:report_id>/rejeter")
def reject(req):
    return _review(req, False)
