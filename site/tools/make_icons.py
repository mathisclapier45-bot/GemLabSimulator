#!/usr/bin/env python3
"""Makes the icon and the share image of the site (needs Pillow, only for this tool: the site itself uses the standard library only).

  python3 site/tools/make_icons.py "GemLab" "Apprendre à reconnaître les pierres précieuses"

Writes site/static/img/favicon.svg, apple-touch-icon.png and og-image.png (1200 x 630, shown when a link is shared).
Run it again when the name of the site changes."""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent.parent / "static" / "img"
FONTS = ["/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
         "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", "/Library/Fonts/Arial.ttf"]

# The gem of the logo of the site: (x, y) points on a 48 x 48 square.
OUTLINE = [(12, 6), (36, 6), (45, 18), (24, 43), (3, 18)]
FACETS = [([(12, 6), (24, 18), (3, 18)], 0.45), ([(36, 6), (24, 18), (45, 18)], 0.20), ([(12, 6), (36, 6), (24, 18)], 0.70)]
TOP, BOTTOM = (110, 168, 255), (123, 92, 255)


def font(size):
    for path in FONTS:
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default()


def gradient(size, c1, c2, vertical=False):
    w, h = size
    img = Image.new("RGB", size)
    px = img.load()
    for y in range(h):
        for x in range(w):
            t = (y / max(h - 1, 1)) if vertical else ((x + y) / max(w + h - 2, 1))
            px[x, y] = tuple(round(c1[i] + (c2[i] - c1[i]) * t) for i in range(3))
    return img


def draw_gem(img, x0, y0, size):
    """The gem, with its gradient body and three light facets, drawn at 4x and reduced for smooth edges."""
    k = 4
    big = size * k
    layer = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    scale = big / 48.0
    pts = [(x * scale, y * scale) for x, y in OUTLINE]
    body = gradient((big, big), TOP, BOTTOM).convert("RGBA")
    mask = Image.new("L", (big, big), 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    layer.paste(body, (0, 0), mask)
    for poly, alpha in FACETS:
        light = Image.new("RGBA", (big, big), (0, 0, 0, 0))
        ImageDraw.Draw(light).polygon([(x * scale, y * scale) for x, y in poly], fill=(255, 255, 255, round(255 * alpha)))
        layer = Image.alpha_composite(layer, light)
    small = layer.resize((size, size), Image.LANCZOS)
    img.paste(small, (x0, y0), small)


def favicon_svg():
    facets = "".join('<polygon points="{}" fill="#fff" opacity="{}"/>'.format(" ".join("{},{}".format(x, y) for x, y in poly), a) for poly, a in FACETS)
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48"><defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1">'
            '<stop offset="0" stop-color="#6ea8ff"/><stop offset="1" stop-color="#7b5cff"/></linearGradient></defs>'
            '<polygon points="{}" fill="url(#g)"/>{}</svg>\n').format(" ".join("{},{}".format(x, y) for x, y in OUTLINE), facets)


def main(name, tagline):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "favicon.svg").write_text(favicon_svg(), encoding="utf-8")

    # Icon for phones: the gem on a dark square.
    icon = gradient((180, 180), (21, 26, 46), (11, 14, 26), vertical=True)
    draw_gem(icon, 30, 34, 120)
    icon.save(OUT / "apple-touch-icon.png")

    # Share image 1200 x 630.
    w, h = 1200, 630
    img = gradient((w, h), (17, 21, 42), (58, 40, 140))
    glow = Image.new("RGB", (w, h), (0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse((60, 60, 620, 570), fill=(70, 60, 190))
    from PIL import ImageFilter
    glow = glow.filter(ImageFilter.GaussianBlur(90))
    img = Image.blend(img, Image.composite(glow, img, glow.convert("L")), 0.55)
    draw_gem(img, 110, 130, 380)
    d = ImageDraw.Draw(img)
    d.text((560, 210), name, font=font(120), fill=(255, 255, 255))
    words, line, y = tagline.split(), "", 360
    for word in words:
        trial = (line + " " + word).strip()
        if d.textlength(trial, font=font(44)) > 580 and line:
            d.text((562, y), line, font=font(44), fill=(205, 212, 240))
            line, y = word, y + 60
        else:
            line = trial
    d.text((562, y), line, font=font(44), fill=(205, 212, 240))
    img.save(OUT / "og-image.png", optimize=True)
    print("Écrit dans", OUT)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "GemLab", sys.argv[2] if len(sys.argv) > 2 else "Apprendre à reconnaître les pierres précieuses")
