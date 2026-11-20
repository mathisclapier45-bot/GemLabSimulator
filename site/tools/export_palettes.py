#!/usr/bin/env python3
"""Exports the colour palette of every species, as the game's encyclopedia computes it, to data/palettes.json.

It is a Python port of GemColorSystem.cs (hue window, eight tone rows, colour names), reading the curated colour
tables straight from the C# file so they stay in sync. The game folder is only read, never changed.

  python3 site/tools/export_palettes.py
"""
import colorsys
import json
import math
import re
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
REPO = SITE.parent
SRC = REPO / "GemLabSimulator" / "Assets" / "Scripts" / "Simulation" / "GemColorSystem.cs"

ROW_SAT = [0.2, 0.4, 0.6, 0.8, 1, 1, 1, 1]
ROW_BRI = [1, 1, 1, 1, 1, 0.8, 0.6, 0.4]
ROW_WORDS = ["clair", "pastel", "", "intense", "vif", "vif", "profond", "sombre"]
ROW_LABELS = ["Clair", "Pastel", "Moyen", "Intense", "Vif", "Vif foncé", "Profond", "Sombre"]
HUE_BANDS = [(15, 20, "rouge orangé"), (20, 40, "orange"), (40, 65, "jaune"), (65, 80, "vert jaunâtre"),
             (80, 165, "vert"), (165, 175, "vert bleuâtre"), (175, 185, "bleu verdâtre"), (185, 255, "bleu"),
             (255, 265, "bleu violacé"), (265, 280, "violet"), (280, 330, "pourpre"), (330, 340, "rouge pourpré")]
ROWS = 8


def repeat(t, length):
    return t - math.floor(t / length) * length


def delta_angle(a, b):
    d = repeat(b - a, 360)
    return d - 360 if d > 180 else d


def clamp01(v):
    return max(0.0, min(1.0, v))


def mode(w, h0, h1, s0, s1, b0, b1):
    return {"w": w, "h0": h0, "h1": h1, "s0": s0, "s1": s1, "b0": b0, "b1": b1}


# ------------------------------------------------------------------ read the C# tables

def read_source():
    text = SRC.read_text(encoding="utf-8")
    limits = {}
    block = text[text.index("Dictionary<string, float[]> Limits"):text.index("private static Mode[] Restrict")]
    for m in re.finditer(r'\{\s*"(\w+)",\s*new\[\]\s*\{([^}]*)\}', block):
        limits[m.group(1)] = [float(x.strip().rstrip("f")) for x in m.group(2).split(",")]
    overrides = {}
    body = text[text.index("private static Mode[] Overrides"):text.index("private static Mode[] Expand")]
    labels = []
    for line in body.split("\n"):
        c = re.match(r'\s*case "(\w+)":', line)
        if c:
            labels.append(c.group(1))
            continue
        m = re.match(r"\s*M\(([^)]*)\)", line)
        if m and labels:
            vals = [float(x.strip().rstrip("f")) for x in m.group(1).split(",")]
            for lab in labels:
                overrides.setdefault(lab, []).append(mode(*vals))
        elif "default:" in line:
            labels = []
        elif "return new[]" in line:
            pass
        elif re.match(r"\s*\};", line):
            labels = []
    return limits, overrides


# ------------------------------------------------------------------ port of the profile logic

def any_color_profile():
    modes = []
    row_weight = [0.8, 1.0, 1.2, 1.2, 1.2, 1.0, 0.8, 0.5]
    bands = 24
    for band in range(bands):
        hue = band * 360 / bands
        for row in range(ROWS):
            modes.append(mode(row_weight[row], hue - 7.5, hue + 7.5, clamp01(ROW_SAT[row] - 0.10), clamp01(ROW_SAT[row] + 0.07),
                              clamp01(ROW_BRI[row] - 0.10), clamp01(ROW_BRI[row] + 0.07)))
    modes.append(mode(bands * 0.8, 0, 360, 0, 0.05, 0.92, 1))
    return modes


def any_color_except_ruby():
    out = []
    for m in any_color_profile():
        hue = repeat((m["h0"] + m["h1"]) / 2, 360)
        sat = (m["s0"] + m["s1"]) / 2
        from_red = abs(delta_angle(hue, 352))
        ruby = m["h1"] - m["h0"] < 300 and ((from_red <= 10 and sat >= 0.5) or (from_red <= 25 and sat >= 0.7))
        if not ruby:
            out.append(m)
    return out


def derived(icon_rgb):
    h, s, v = colorsys.rgb_to_hsv(*icon_rgb)
    hue = h * 360
    if s < 0.25:
        if v < 0.5:
            return [mode(1, hue - 10, hue + 10, 0, max(0.05, s * 1.2), max(0.04, v * 0.8), min(1, v * 1.2 + 0.04))]
        return [mode(1, hue - 8, hue + 8, s * 0.6, s * 1.4, max(0, v - 0.07), min(1, v + 0.05))]
    center, best = 0, 1e9
    for row in range(ROWS):
        d = (s - ROW_SAT[row]) ** 2 + (v - ROW_BRI[row]) ** 2
        if d < best:
            best, center = d, row
    modes = []
    for off, wt in zip([-3, -2, -1, 0, 1, 2, 3], [0.03, 0.09, 0.21, 0.34, 0.21, 0.09, 0.03]):
        row = center + off
        if 0 <= row < ROWS:
            modes.append(mode(wt, hue - 6, hue + 6, ROW_SAT[row] - 0.10, min(1, ROW_SAT[row] + 0.07),
                              ROW_BRI[row] - 0.10, min(1, ROW_BRI[row] + 0.07)))
    return modes


def expand(curated):
    if not curated:
        return curated
    total = sx = sy = mean_sat = 0.0
    for m in curated:
        rad = math.radians((m["h0"] + m["h1"]) / 2)
        total += m["w"]
        sx += m["w"] * math.cos(rad)
        sy += m["w"] * math.sin(rad)
        mean_sat += m["w"] * (m["s0"] + m["s1"]) / 2
    if total <= 0 or mean_sat / total < 0.12:
        return curated
    if math.hypot(sx, sy) / total < 0.80:
        return curated
    mean_hue = repeat(math.degrees(math.atan2(sy, sx)), 360)
    spread = 0.0
    hist = [0.0] * ROWS
    for m in curated:
        centre = (m["h0"] + m["h1"]) / 2
        spread += m["w"] * (abs(delta_angle(centre, mean_hue)) + (m["h1"] - m["h0"]) / 2)
        s = (m["s0"] + m["s1"]) / 2
        b = (m["b0"] + m["b1"]) / 2
        best, bd = 0, 1e9
        for row in range(ROWS):
            d = (s - ROW_SAT[row]) ** 2 + (b - ROW_BRI[row]) ** 2
            if d < bd:
                bd, best = d, row
        hist[best] += m["w"] / total
    spread = max(8.0, min(24.0, spread / total + 4))
    low, high = ROWS, -1
    for row in range(ROWS):
        if hist[row] > 0.02:
            low, high = min(low, row), max(high, row)
    if high < 0:
        return curated
    low, high = max(0, low - 2), min(ROWS - 1, high + 2)
    row_w = [0.0] * ROWS
    row_total = 0.0
    for row in range(low, high + 1):
        left = hist[row - 1] if row > 0 else 0
        right = hist[row + 1] if row < ROWS - 1 else 0
        row_w[row] = hist[row] + 0.5 * (left + right) + 0.04
        row_total += row_w[row]
    bands = 13
    cloud, cloud_total = [], 0.0
    for band in range(bands):
        offset = (band - (bands - 1) * 0.5) / ((bands - 1) * 0.5)
        hue = mean_hue + offset * spread
        half = spread / (bands - 1) * 1.6
        band_w = math.exp(-(offset * offset) / 0.45)
        for row in range(low, high + 1):
            w = band_w * row_w[row] / row_total
            cloud.append(mode(w, hue - half, hue + half, clamp01(ROW_SAT[row] - 0.10), clamp01(ROW_SAT[row] + 0.07),
                              clamp01(ROW_BRI[row] - 0.10), clamp01(ROW_BRI[row] + 0.07)))
            cloud_total += w
            if row <= 6 and band % 2 == 0:
                mw = w * 0.30
                cloud.append(mode(mw, hue - half, hue + half, clamp01(ROW_SAT[row] * 0.55 - 0.06), clamp01(ROW_SAT[row] * 0.55 + 0.08),
                                  clamp01(ROW_BRI[row] * 0.80 - 0.06), clamp01(ROW_BRI[row] * 0.80 + 0.06)))
                cloud_total += mw
    result = []
    for m in curated:
        n = dict(m)
        n["w"] = m["w"] / total * 0.65
        result.append(n)
    for m in cloud:
        n = dict(m)
        n["w"] = m["w"] / cloud_total * 0.35
        result.append(n)
    return result


def restrict(modes, key, limits):
    if not modes or key not in limits:
        return modes
    lim = limits[key]
    width = repeat(lim[1] - lim[0], 360)
    centre = lim[0] + width * 0.5
    result = []
    for m in modes:
        mc = delta_angle(centre, (m["h0"] + m["h1"]) / 2)
        mh = max(0.5, (m["h1"] - m["h0"]) * 0.5)
        lo = max(mc - mh, -width * 0.5)
        hi = min(mc + mh, width * 0.5)
        if hi <= lo:
            continue
        s0, s1 = max(m["s0"], lim[2]), min(m["s1"], lim[3])
        b0, b1 = max(m["b0"], lim[4]), min(m["b1"], lim[5])
        if s1 <= s0 or b1 <= b0:
            continue
        frac = (hi - lo) / (2 * mh) * (s1 - s0) / max(0.001, m["s1"] - m["s0"]) * (b1 - b0) / max(0.001, m["b1"] - m["b0"])
        result.append(mode(m["w"] * clamp01(frac), centre + lo, centre + hi, s0, s1, b0, b1))
    return result or modes


def grown_in_any_color(name, forced, synthetic):
    if name == "Corindon_Synthetique_Rubis":
        return False
    if name in ("YAG", "GGG", "CZ", "Titanate_Strontium") or name.startswith("Corindon_Synthetique"):
        return True
    if not synthetic and not forced:
        return False
    return name.startswith("Saphir") or name == "Padparadscha" or name.startswith("Spinelle")


class Palettes:
    def __init__(self):
        self.limits, self.overrides = read_source()

    # The sapphire is one species: the modes of each colour group (cut to its window), scaled by the share of the colour, and some colourless stones
    SAPPHIRE_GROUPS = [("Saphir_Bleu", 0.50), ("Saphir_Jaune", 0.13), ("Saphir_Vert", 0.09), ("Saphir_Rose", 0.08), ("Saphir_Orange", 0.05), ("Saphir_Violet", 0.05)]

    def sapphire(self):
        out = []
        for group, share in self.SAPPHIRE_GROUPS:
            modes = restrict(expand(self.overrides[group]), group, self.limits)
            total = sum(m["w"] for m in modes)
            out += [dict(m, w=(m["w"] / total * share if total > 0 else 0)) for m in modes]
        out.append(mode(0.10, 0, 360, 0, 0.07, 0.90, 1))
        return out

    def natural(self, key, icon):
        if key == "Saphir":
            return restrict(self.sapphire(), key, self.limits)
        modes = self.overrides.get(key) or derived(icon)
        return restrict(expand(modes), key, self.limits)

    def profile(self, sp, synthetic):
        key, forced = sp["id"], bool(sp.get("forcedSynthetic"))
        grown = synthetic or forced
        icon = hex_rgb(sp["iconColor"])
        if key == "Corindon_Synthetique_Rubis" or (key == "Corindon_Rubis" and grown):
            return self.natural("Corindon_Rubis", icon)
        if key == "Corindon_Synthetique_Saphir" or ((key.startswith("Saphir") or key == "Padparadscha") and grown):
            return any_color_except_ruby()
        if grown_in_any_color(key, forced, synthetic):
            return any_color_profile()
        return self.natural(key, icon)


def tone_row(s, b):
    if b >= 0.9:
        return 0 if s < 0.3 else 1 if s < 0.5 else 2 if s < 0.7 else 3 if s < 0.9 else 4
    if b >= 0.7:
        return 5 if s >= 0.85 else (3 if s >= 0.55 else 2)
    if b >= 0.5:
        return 6 if s >= 0.65 else 2
    return 7


def color_name(h, s, b):
    if s < 0.10:
        return "Noir" if b < 0.18 else ("Gris" if b < 0.72 else "Incolore")
    row = tone_row(s, b)
    noun = next((n for lo, hi, n in HUE_BANDS if lo <= h < hi), "rouge")
    word = ROW_WORDS[row]
    neon = 190 <= h <= 212 and s >= 0.60 and b >= 0.85
    pink = (295 <= h < 335 and b >= 0.80) or ((h >= 335 or h < 12) and s < 0.70 and b >= 0.90)
    warm = 18 <= h < 66
    if neon:
        noun = "bleu néon"
        word = "vif" if s >= 0.9 else ("intense" if s >= 0.75 else "")
    elif pink:
        noun = "rose violacé" if 295 <= h < 315 else "rose"
    elif 12 <= h < 40 and s < 0.70 and b >= 0.90:
        noun = "rose orangé" if s < 0.55 else "orange rosé"
    elif warm and b < 0.85 and s >= 0.35:
        noun = noun + " brunâtre" if b >= 0.7 else "brun"
    elif s < 0.5 and b < 0.85 and not (h >= 335 or h < 18):
        noun += " grisâtre"
    text = noun if not word else noun + " " + word
    return text[:1].upper() + text[1:]


def palette_cells(modes, steps=12):
    if not modes:
        return []
    sx = sy = total = 0.0
    for m in modes:
        rad = math.radians((m["h0"] + m["h1"]) / 2)
        sx += m["w"] * math.cos(rad)
        sy += m["w"] * math.sin(rad)
        total += m["w"]
    mean = repeat(math.degrees(math.atan2(sy, sx)), 360)
    lo = hi = 0.0
    for m in modes:
        centre = delta_angle(mean, (m["h0"] + m["h1"]) / 2)
        half = (m["h1"] - m["h0"]) / 2
        lo, hi = min(lo, centre - half), max(hi, centre + half)
    if hi - lo < 12:
        lo -= 6
        hi += 6
    cells, weight_sum = [], 0.0
    for step in range(steps):
        offset = lo + (hi - lo) * (step + 0.5) / steps
        hue = repeat(mean + offset, 360)
        for row in range(ROWS):
            sat, bri = ROW_SAT[row], ROW_BRI[row]
            weight = 0.0
            for m in modes:
                centre = delta_angle(mean, (m["h0"] + m["h1"]) / 2)
                half = (m["h1"] - m["h0"]) / 2 + (hi - lo) / steps * 0.5
                if abs(offset - centre) > half:
                    continue
                if sat < m["s0"] - 0.06 or sat > m["s1"] + 0.06:
                    continue
                if bri < m["b0"] - 0.06 or bri > m["b1"] + 0.06:
                    continue
                weight += m["w"]
            if weight <= 0:
                continue
            cells.append([hue, sat, bri, weight, row, step])
            weight_sum += weight
    if not cells:
        for m in modes:
            s, b = (m["s0"] + m["s1"]) / 2, (m["b0"] + m["b1"]) / 2
            cells.append([(m["h0"] + m["h1"]) / 2, s, b, m["w"], tone_row(s, b), len(cells)])
            weight_sum += m["w"]
    out = []
    for hue, sat, bri, weight, row, step in cells:
        hue = repeat(hue, 360)
        out.append([round(hue, 1), round(clamp01(sat), 3), round(clamp01(bri), 3), round(weight / weight_sum if weight_sum else 0, 4),
                    row, step, color_name(hue, clamp01(sat), clamp01(bri))])
    return out


# Stones that change color: the color seen under an incandescent light is added to every cell of their chart.
# By daylight these stones are blue or green; under a warm light they turn purple, red or orange. Each one has its own map from the daylight hue
# (from, to) to the incandescent hue (at the start, at the end). The map is linear; the saturation stays, the brightness drops a little.
# (The game today gives every stone one pinkish shift, GemLook.Incandescent; these maps keep the order of the colors.)
COLOR_CHANGE = {
    "Chrysoberyl_Alexandrite": (120, 185, 20, -45),       # yellowish green -> orange-red, blue-green -> purple
    "Grenat_Change_Couleur": (165, 205, 350, 285),        # blue-green -> red-pink, blue -> violet
    "Saphir_Change_Couleur": (235, 320, 280, 350),        # blue -> violet, violet -> red-pink
    "Diaspore_Zultanite": (60, 85, 25, -15),              # yellowish green -> orange, greener -> pink
}


def incandescent(key, h, s, b):
    """(hue, saturation, brightness) seen under an incandescent light, from the color seen by daylight."""
    lo, hi, inc_lo, inc_hi = COLOR_CHANGE[key]
    t = clamp01((h - lo) / float(hi - lo))
    return repeat(inc_lo + (inc_hi - inc_lo) * t, 360.0), s, clamp01(b * 0.93)


def with_incandescent(key, cells):
    out = []
    for cell in cells:
        ih, is_, ib = incandescent(key, cell[0], cell[1], cell[2])
        out.append(cell + [round(ih, 1), round(is_, 3), round(ib, 3), color_name(ih, is_, ib)])
    return out


def hex_rgb(h):
    return tuple(int(h[i:i + 2], 16) / 255 for i in (1, 3, 5))


def letter_scale():
    out = []
    for i in range(23):
        t = clamp01(i / 22)
        hue = 215 + (52 - 215) * clamp01(i / 5)
        out.append([round(hue, 1), round(0.02 + (0.62 - 0.02) * t * t, 3), round(0.96 + (0.90 - 0.96) * t, 3), chr(ord("D") + i) if i < 23 else ""])
    return out


def main():
    data = json.load(open(SITE / "data" / "gemlab_data.json", encoding="utf-8"))
    extra_path = SITE / "data" / "extra_species.json"
    pal = Palettes()
    if extra_path.exists():                       # species of the site that the game data does not have (yet), with their own colour window
        known = {s["id"] for s in data["species"]}
        for e in json.load(open(extra_path, encoding="utf-8"))["species"]:
            if e["id"] not in known:
                data["species"].append(e)
                if e.get("colorModes") and e["id"] not in pal.overrides:      # its own characteristic colours (a species the game code does not have)
                    pal.overrides[e["id"]] = [mode(*row) for row in e["colorModes"]]
                if e.get("colorLimits") and e["id"] not in pal.limits:
                    pal.limits[e["id"]] = [float(v) for v in e["colorLimits"]]
    out = {"_rows": ROW_LABELS}
    for sp in data["species"]:
        key = sp["id"]
        if key.startswith("Diamant") and not key.startswith("Diamant_Synth"):
            out[key] = {"diamond": letter_scale()}
            continue
        entry = {"natural": palette_cells(pal.profile(sp, False)), "any": grown_in_any_color(key, bool(sp.get("forcedSynthetic")), False)}
        if key in COLOR_CHANGE:
            entry["natural"] = with_incandescent(key, entry["natural"])
        if not sp.get("forcedSynthetic") and key not in COLOR_CHANGE and grown_in_any_color(key, False, True):
            entry["synthetic"] = palette_cells(pal.profile(sp, True))
        out[key] = entry
    (SITE / "data" / "palettes.json").write_text(json.dumps(out, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    sizes = sorted(len(v.get("natural", [])) for k, v in out.items() if k != "_rows")
    print("Palettes écrites : {} espèces, de {} à {} couleurs.".format(len(out) - 1, sizes[0], sizes[-1]))


if __name__ == "__main__":
    main()
