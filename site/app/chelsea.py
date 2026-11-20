"""What a stone looks like through the Chelsea filter (a port of ChelseaInfo.cs and of ChelseaFilterUI.ReactionChip in the game).

The filter lets only deep red and yellow-green through: a stone colored by chromium shows a red or pink reaction, every other stone stays a dull green. The color shown
on the page is the one the stone takes through the filter: the dull tint (the entry's "dull" color, a dull green by default), brought toward the color of the reaction as far
as the reaction is strong. A stone with no reaction is therefore green or gray-green, never red."""

import unicodedata

DULL_GREEN = (0.14, 0.40, 0.18)
RED = (0.96, 0.10, 0.12)
PINK = (1.0, 0.38, 0.52)


def _plain(text):
    """The text without accents, in lower case (the keywords below are written without accents)."""
    return "".join(ch for ch in unicodedata.normalize("NFKD", text or "") if not unicodedata.combining(ch)).lower()


def _hex(c):
    return "#{:02X}{:02X}{:02X}".format(*[int(round(max(0.0, min(1.0, v)) * 255)) for v in c])


def _parse(value, fallback):
    if isinstance(value, str) and len(value) == 7 and value[0] == "#":
        try:
            return tuple(int(value[i:i + 2], 16) / 255.0 for i in (1, 3, 5))
        except ValueError:
            pass
    return fallback


def _lerp(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def from_text(text):
    """(reacts, intensity, color) read in the French description of a species with no entry of its own (ChelseaInfo.ForGem)."""
    t = _plain(text)
    if not t or "non applicable" in t:
        return False, 0.0, RED
    if not ("rouge" in t or "rosat" in t or "rose" in t):
        return False, 0.0, RED
    if t.startswith("aucune reaction") and "ou reaction rougeatre" not in t:
        return False, 0.0, RED
    if "contrairement" in t:
        intensity = 0.08
    elif "tres faible" in t:
        intensity = 0.15
    elif "faible a moderee" in t:
        intensity = 0.4
    elif "moder" in t:
        intensity = 0.65
    elif "vif" in t:
        intensity = 1.0
    elif "marqu" in t:
        intensity = 0.9
    elif "faible" in t:
        intensity = 0.25
    elif "possible" in t:
        intensity = 0.3
    else:
        intensity = 0.6
    color = _lerp(RED, PINK, 0.55) if "rosat" in t else RED
    return intensity >= 0.1, intensity, color


def seen_color(entry, french_text):
    """The hex color of the stone through the filter (None when the filter does not apply): from the entry of the table of the course when the species has one, from its description otherwise."""
    if not entry and "non applicable" in _plain(french_text):
        return None                                   # an opaque stone: the filter says nothing about it
    if entry:
        intensity = max(0.0, min(1.0, float(entry.get("intensity") or 0)))
        reacts = intensity >= 0.1
        color = _parse(entry.get("color"), RED)
        dull = _parse(entry.get("dull"), DULL_GREEN)
    else:
        reacts, intensity, color = from_text(french_text)
        dull = DULL_GREEN
    return _hex(_lerp(dull, color, intensity) if reacts else dull)
