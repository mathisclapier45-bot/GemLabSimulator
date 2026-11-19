"""Gemstone prices: contributions of members (and files imported by the administrator), checks, storage and robust estimates.

Nothing is invented here: an estimate only comes from the rows of the table `price_reports` that were approved. With too few prices
the answer is "not enough data". The prices are asking or paid prices in euros for a whole stone, never the name of a seller."""
import datetime
import math

from . import catalog, config
from .db import now
from .forum import Denied

COLOR_TIERS = (1, 2, 3, 4)                           # 1 pale, 2 medium, 3 vivid, 4 exceptional
CLARITIES = ("included", "eye_clean", "loupe_clean")
TREATMENTS = ("none", "heated", "filled", "other")
KINDS = ("asking", "paid")
BANDS = [(0.05, 0.2), (0.2, 0.5), (0.5, 1.0), (1.0, 2.0), (2.0, 3.0), (3.0, 5.0), (5.0, 10.0), (10.0, 500.01)]   # weight bands in carats
MIN_N = 5                       # fewest prices needed to give a range
GOOD_N = 12                     # from this many prices the confidence is "high"
MAX_AGE_MONTHS = 24             # older prices are not used
WEIGHT_MIN, WEIGHT_MAX = 0.05, 500.0
PRICE_MIN, PRICE_MAX = 1.0, 2000000.0
PPC_MIN, PPC_MAX = 0.5, 500000.0          # price per carat accepted
TREND_MIN_N = 8                 # fewest prices (over at least three weight bands) for the trend line


# ------------------------------------------------------------------ species and months

def eligible_species():
    """Natural species only: a price of a synthetic stone or of an imitation is not a gem price."""
    return catalog.natural_species(catalog.data()["species"])


def this_month():
    d = datetime.date.today()
    return "{:04d}-{:02d}".format(d.year, d.month)


def month_index(ym):
    y, m = ym.split("-")
    return int(y) * 12 + int(m) - 1


def cutoff_month():
    return month_index(this_month()) - MAX_AGE_MONTHS + 1


def parse_month(text):
    text = (text or "").strip()
    if len(text) != 7 or text[4] != "-" or not (text[:4] + text[5:]).isdigit() or not 1 <= int(text[5:]) <= 12:
        return None
    return text


def parse_number(text):
    try:
        value = float(str(text).replace(",", ".").replace(" ", "").replace(" ", ""))
    except ValueError:
        return None
    return value if math.isfinite(value) else None


# ------------------------------------------------------------------ checks and storage

def clean_report(form, species_ids):
    """The values of a contribution, checked; raises Denied(<message key>) when something is wrong."""
    species = form.get("species", "")
    if species not in species_ids:
        raise Denied("price.err.species", 400)
    weight, price = parse_number(form.get("weight")), parse_number(form.get("price"))
    if weight is None or not WEIGHT_MIN <= weight <= WEIGHT_MAX:
        raise Denied("price.err.weight", 400)
    if price is None or not PRICE_MIN <= price <= PRICE_MAX:
        raise Denied("price.err.price", 400)
    if not PPC_MIN <= price / weight <= PPC_MAX:
        raise Denied("price.err.ppc", 400)
    kind, clarity, treatment = form.get("kind", ""), form.get("clarity", ""), form.get("treatment", "")
    color = form.get("color", "")
    if kind not in KINDS or clarity not in CLARITIES or treatment not in TREATMENTS or not color.isdigit() or int(color) not in COLOR_TIERS:
        raise Denied("price.err.choice", 400)
    observed = parse_month(form.get("observed"))
    if observed is None or month_index(observed) > month_index(this_month()) or month_index(observed) < cutoff_month():
        raise Denied("price.err.month", 400)
    return {"species": species, "weight": round(weight, 3), "price": round(price, 2), "kind": kind, "color": int(color),
            "clarity": clarity, "treatment": treatment, "observed": observed}


def add_report(conn, user_id, values, status="pending", source="member"):
    cur = conn.execute(
        "INSERT INTO price_reports (user_id, species_id, weight_ct, price_eur, kind, color_tier, clarity, treatment, observed_at, source, status, created_at, reviewed_at) "
        "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
        (user_id, values["species"], values["weight"], values["price"], values["kind"], values["color"], values["clarity"], values["treatment"],
         values["observed"], source, status, now(), now() if status == "approved" else None))
    return cur.lastrowid


def is_duplicate(conn, user_id, values):
    row = conn.execute(
        "SELECT 1 FROM price_reports WHERE user_id = ? AND species_id = ? AND weight_ct = ? AND price_eur = ? AND created_at > ?",
        (user_id, values["species"], values["weight"], values["price"], now() - 86400)).fetchone()
    return row is not None


def review(conn, report_id, admin_id, approve):
    conn.execute("UPDATE price_reports SET status = ?, reviewed_at = ?, reviewed_by = ? WHERE id = ? AND status = 'pending'",
                 ("approved" if approve else "rejected", now(), admin_id, report_id))


def approved_rows(conn, species_id, kind=None, color=None, clarity=None, treatment=None):
    """The approved prices of a species that are recent enough, as dicts with the price per carat."""
    sql = "SELECT * FROM price_reports WHERE species_id = ? AND status = 'approved'"
    args = [species_id]
    for column, value in (("kind", kind), ("color_tier", color), ("clarity", clarity), ("treatment", treatment)):
        if value not in (None, ""):
            sql += " AND {} = ?".format(column)
            args.append(value)
    out = []
    for r in conn.execute(sql, args).fetchall():
        if month_index(r["observed_at"]) < cutoff_month():
            continue
        out.append({"id": r["id"], "weight": r["weight_ct"], "ppc": r["price_eur"] / r["weight_ct"], "price": r["price_eur"], "kind": r["kind"], "observed": r["observed_at"]})
    return out


def counts_by_species(conn):
    """species id -> number of approved prices that are recent enough."""
    out = {}
    for r in conn.execute("SELECT species_id, observed_at FROM price_reports WHERE status = 'approved'").fetchall():
        if month_index(r["observed_at"]) >= cutoff_month():
            out[r["species_id"]] = out.get(r["species_id"], 0) + 1
    return out


# ------------------------------------------------------------------ statistics

def percentile(sorted_values, p):
    if not sorted_values:
        return None
    k = (len(sorted_values) - 1) * p
    lo, hi = int(math.floor(k)), int(math.ceil(k))
    return sorted_values[lo] + (sorted_values[hi] - sorted_values[lo]) * (k - lo)


def trimmed(values):
    """The values without the extreme ones (more than 1.5 times the interquartile range outside it); needs at least 5 values."""
    ordered = sorted(values)
    if len(ordered) < 5:
        return ordered
    q1, q3 = percentile(ordered, 0.25), percentile(ordered, 0.75)
    spread = q3 - q1
    return [v for v in ordered if q1 - 1.5 * spread <= v <= q3 + 1.5 * spread] or ordered


def band_index(weight):
    for i, (lo, hi) in enumerate(BANDS):
        if lo <= weight < hi:
            return i
    return None


def band_stats(rows):
    """For each weight band: number of prices, median and quartiles of the price per carat (None when there are none)."""
    out = []
    for lo, hi in BANDS:
        values = trimmed([r["ppc"] for r in rows if lo <= r["weight"] < hi])
        raw = sum(1 for r in rows if lo <= r["weight"] < hi)
        out.append({"lo": lo, "hi": hi, "n": raw, "median": percentile(values, 0.5), "p25": percentile(values, 0.25), "p75": percentile(values, 0.75)})
    return out


def trend(rows):
    """ln(price per carat) = a + b ln(weight): (a, b, residual spread), or None when the data does not allow it."""
    if len(rows) < TREND_MIN_N or len({band_index(r["weight"]) for r in rows}) < 3:
        return None
    xs, ys = [math.log(r["weight"]) for r in rows], [math.log(r["ppc"]) for r in rows]
    mx, my = sum(xs) / len(xs), sum(ys) / len(ys)
    sxx = sum((x - mx) ** 2 for x in xs)
    if sxx < 1e-9:
        return None
    b = sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / sxx
    a = my - b * mx
    resid = [y - (a + b * x) for x, y in zip(xs, ys)]
    spread = max(math.sqrt(sum(e * e for e in resid) / max(len(resid) - 2, 1)), 0.15)
    return a, b, spread


def estimate(rows, weight):
    """The estimate for a stone of `weight` carats from the prices `rows`. Always a dict with a "status":
    "none" (no price at all), "few" (not enough prices) or "ok" (with the range of the price per carat and of the whole stone)."""
    res = {"n_total": len(rows), "asking": sum(1 for r in rows if r["kind"] == "asking"), "paid": sum(1 for r in rows if r["kind"] == "paid"),
           "newest": max((r["observed"] for r in rows), default=None), "bands": band_stats(rows), "weight": weight, "n_band": 0}
    if not rows:
        res["status"] = "none"
        return res
    i = band_index(weight)
    if i is None:
        res["status"] = "few"
        return res
    band = res["bands"][i]
    res["band"] = (band["lo"], band["hi"])
    res["n_band"] = band["n"]
    if band["n"] >= MIN_N:
        low, med, high = band["p25"], band["median"], band["p75"]
        res.update(status="ok", method="band", confidence="high" if band["n"] >= GOOD_N else "medium", ppc=(low, med, high))
    else:
        t = trend(rows)
        weights = [r["weight"] for r in rows]
        if t and min(weights) / 1.5 <= weight <= max(weights) * 1.5:
            a, b, s = t
            mid = math.exp(a + b * math.log(weight))
            res.update(status="ok", method="trend", confidence="low", ppc=(mid * math.exp(-0.67 * s), mid, mid * math.exp(0.67 * s)))
        else:
            res["status"] = "few"
            return res
    low, med, high = res["ppc"]
    res["total"] = (low * weight, med * weight, high * weight)
    return res


def plausibility(conn, species_id, weight, price):
    """True when a price per carat is very far from the approved ones of the same band (to look at it twice), else False."""
    rows = approved_rows(conn, species_id)
    i = band_index(weight)
    if i is None:
        return False
    band = band_stats(rows)[i]
    if band["n"] < MIN_N or not band["median"]:
        return False
    ratio = (price / weight) / band["median"]
    return ratio > 4 or ratio < 0.25


# ------------------------------------------------------------------ display

def round_sig(value, digits=2):
    if value <= 0:
        return 0
    magnitude = 10 ** (math.floor(math.log10(value)) - digits + 1)
    return round(value / magnitude) * magnitude


def fmt_eur(value):
    """A rounded amount in euros: 1 240 (French) or 1,240 (English)."""
    text = "{:,.0f}".format(round_sig(value) if value >= 100 else round(value))
    return text.replace(",", " ") if catalog.current_lang() == "fr" else text


# ------------------------------------------------------------------ files (import / export by the administrator)

CSV_COLUMNS = ("species", "weight_ct", "price_eur", "kind", "color_tier", "clarity", "treatment", "observed")
CSV_TEMPLATE = ",".join(CSV_COLUMNS) + "\n"


def import_csv(conn, text, source="import"):
    """Adds the rows of a CSV text (columns of CSV_COLUMNS, species as its id such as Spessartite or its slug). The rows pass the same
    checks as a member's price and are published at once. Returns (rows added, list of (line number, message key))."""
    import csv
    import io
    species = {s["id"]: s["id"] for s in eligible_species()}
    species.update({s["slug"]: s["id"] for s in eligible_species()})
    added, problems = 0, []
    reader = csv.DictReader(io.StringIO(text))
    if not reader.fieldnames or any(c not in reader.fieldnames for c in CSV_COLUMNS):
        return 0, [(1, "price.err.choice")]
    for n, row in enumerate(reader, start=2):
        form = {"species": species.get((row.get("species") or "").strip(), ""), "weight": row.get("weight_ct"), "price": row.get("price_eur"),
                "kind": (row.get("kind") or "").strip(), "color": (row.get("color_tier") or "").strip(),
                "clarity": (row.get("clarity") or "").strip(), "treatment": (row.get("treatment") or "").strip(), "observed": row.get("observed")}
        try:
            values = clean_report(form, set(species.values()))
        except Denied as d:
            problems.append((n, d.key))
            continue
        if is_duplicate_any(conn, values):
            problems.append((n, "err.duplicate"))
            continue
        add_report(conn, None, values, "approved", source)
        added += 1
    return added, problems


def is_duplicate_any(conn, values):
    row = conn.execute("SELECT 1 FROM price_reports WHERE species_id = ? AND weight_ct = ? AND price_eur = ? AND observed_at = ? AND kind = ? AND color_tier = ? AND clarity = ? AND treatment = ?",
                       (values["species"], values["weight"], values["price"], values["observed"], values["kind"], values["color"], values["clarity"], values["treatment"])).fetchone()
    return row is not None


def export_csv(conn):
    """All approved prices as CSV text, without any user information."""
    import csv
    import io
    out = io.StringIO()
    w = csv.writer(out, lineterminator="\n")
    w.writerow(CSV_COLUMNS)
    for r in conn.execute("SELECT * FROM price_reports WHERE status = 'approved' ORDER BY id"):
        w.writerow([r["species_id"], r["weight_ct"], r["price_eur"], r["kind"], r["color_tier"], r["clarity"], r["treatment"], r["observed_at"]])
    return out.getvalue()
