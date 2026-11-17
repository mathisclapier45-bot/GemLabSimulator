#!/usr/bin/env python3
"""Exports the facet geometry of every cut of the game (GemMesh.cs + the outlines of GemPixelArt.cs) as small JSON
files in static/data/meshes/, for the 3D viewer of the site. A Python port: the game folder is only read.

  python3 site/tools/export_meshes.py
"""
import json
import math
from pathlib import Path

SITE = Path(__file__).resolve().parent.parent
OUT = SITE / "static" / "data" / "meshes"

SHAPES = ["Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Rectangle à pans coupés", "Octogone", "Baguette", "Poire",
          "Marquise", "Shield", "Triangle", "Cœur"]
STYLES = ["Brillant", "Mixte", "À degrés", "Ascher", "Princesse", "Ciseaux", "Radiant", "Cabochon"]
ALLOWED = {
    "Brillant": ["Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Octogone", "Poire", "Marquise", "Shield", "Triangle", "Cœur"],
    "À degrés": ["Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Rectangle à pans coupés", "Octogone", "Baguette", "Poire", "Marquise", "Shield", "Triangle"],
    "Ascher": ["Carré", "Octogone"], "Princesse": ["Carré", "Rectangle"],
    "Ciseaux": ["Rond", "Ovale", "Coussin", "Carré", "Rectangle", "Octogone", "Shield"],
    "Radiant": ["Carré", "Rectangle", "Rectangle à pans coupés", "Octogone"],
}
ANGLE_STEPS = 720
GIRDLE_HALF = 0.02


# ------------------------------------------------------------------ outlines (GemPixelArt)

def chamfered(au, av, hx, hy, c):
    return au <= hx and av <= hy and (hx - au) + (hy - av) >= c


def edge_distance(p, a, b):
    ex, ey = b[0] - a[0], b[1] - a[1]
    n = math.hypot(ex, ey)
    nx, ny = ey / n, -ex / n
    return (p[0] - a[0]) * nx + (p[1] - a[1]) * ny


def inside(shape, u, v):
    au, av = abs(u), abs(v)
    if shape == "Ovale":
        return (u / 0.98) ** 2 + (v / 0.72) ** 2 <= 1
    if shape == "Coussin":
        return (au / 0.90) ** 3.2 + (av / 0.84) ** 3.2 <= 1
    if shape == "Carré":
        return chamfered(au, av, 0.84, 0.84, 0.14)
    if shape == "Rectangle":
        return chamfered(au, av, 0.98, 0.66, 0.10)
    if shape == "Rectangle à pans coupés":
        return chamfered(au, av, 0.98, 0.70, 0.32)
    if shape == "Octogone":
        return chamfered(au, av, 0.88, 0.88, 0.40)
    if shape == "Baguette":
        return chamfered(au, av, 1.00, 0.40, 0.07)
    if shape == "Poire":
        if u < -0.30:
            return (u + 0.30) ** 2 + v * v <= 0.66 * 0.66
        if u > 0.98:
            return False
        return av <= 0.66 * (1 - (u + 0.30) / 1.28) ** 0.85
    if shape == "Marquise":
        if au > 0.98:
            return False
        return av <= 0.50 * (1 - (u / 0.98) ** 2) ** 0.8
    if shape == "Shield":
        if u < -0.82 or u > 0.98:
            return False
        if u < 0.10:
            return av <= 0.74 and (u + 0.82) + (0.74 - av) >= 0.16
        return av <= 0.74 * (1 - (u - 0.10) / 0.88) ** 0.9
    if shape == "Triangle":
        a, b, c = (1.12, 0.0), (-0.72, 0.96), (-0.72, -0.96)
        d = max(edge_distance((u, v), a, b), edge_distance((u, v), b, c), edge_distance((u, v), c, a))
        return d <= -0.13
    if shape == "Cœur":
        x, y = v * 1.35, -(u - 0.12) * 1.2
        q = x * x + y * y - 1
        return q ** 3 - x * x * y ** 3 <= 0
    return u * u + v * v <= 0.92 * 0.92


_outlines = {}


def boundary_radius(shape, theta):
    if shape not in _outlines:
        radii = []
        for i in range(ANGLE_STEPS):
            th = i * 2 * math.pi / ANGLE_STEPS
            dx, dy = math.cos(th), math.sin(th)
            r = 0.0
            while r < 1.3 and inside(shape, dx * (r + 0.004), dy * (r + 0.004)):
                r += 0.004
            radii.append(r)
        _outlines[shape] = radii
    radii = _outlines[shape]
    t = (theta % (2 * math.pi)) / (2 * math.pi) * ANGLE_STEPS
    i0 = int(t) % ANGLE_STEPS
    i1 = (i0 + 1) % ANGLE_STEPS
    f = t - math.floor(t)
    return radii[i0] + (radii[i1] - radii[i0]) * f


def aspect(shape):
    max_x = max_y = 0.01
    for i in range(180):
        th = i * 2 * math.pi / 180
        r = boundary_radius(shape, th)
        max_x = max(max_x, abs(math.cos(th) * r))
        max_y = max(max_y, abs(math.sin(th) * r))
    largest = max(max_x, max_y)
    return max_x / largest, max_y / largest


# ------------------------------------------------------------------ mesh builder

class Mesh:
    def __init__(self):
        self.v, self.tris, self.ids = [], [], []
        self.next_id = 1
        self.counted = 0

    def add_at(self, p):
        self.v.append(tuple(p))
        return len(self.v) - 1

    def add_triangle(self, i0, i1, i2, fid):
        p0, p1, p2 = self.v[i0], self.v[i1], self.v[i2]
        e1 = [p1[k] - p0[k] for k in range(3)]
        e2 = [p2[k] - p0[k] for k in range(3)]
        n = (e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0])
        c = [(p0[k] + p1[k] + p2[k]) / 3 for k in range(3)]
        if sum(n[k] * c[k] for k in range(3)) < 0:
            self.tris.append((i0, i2, i1))
        else:
            self.tris.append((i0, i1, i2))
        self.ids.append(fid)

    def facet(self, count, *poly):
        fid = self.next_id
        self.next_id += 1
        if count:
            self.counted += 1
        for i in range(1, len(poly) - 1):
            self.add_triangle(poly[0], poly[i], poly[i + 1], fid)

    def normalize(self):
        max_x = max([0.01] + [abs(p[0]) for p in self.v])
        max_y = max([0.01] + [abs(p[1]) for p in self.v])
        zs = [p[2] for p in self.v]
        mid = (min(zs) + max(zs)) / 2
        half = max(0.01, (max(zs) - min(zs)) / 2)
        self.v = [(p[0] / max_x, p[1] / max_y, (p[2] - mid) / half) for p in self.v]
        return self


def evenly(n):
    return [360.0 * i / n for i in range(n)]


def chamfer_angles(hx, hy, c):
    a1 = math.degrees(math.atan2(hy - c, hx))
    a2 = math.degrees(math.atan2(hy, hx - c))
    return [a1, a2, 180 - a2, 180 - a1, 180 + a1, 180 + a2, 360 - a2, 360 - a1]


def corner_angles(hx, hy):
    a = math.degrees(math.atan2(hy, hx))
    return [a, 180 - a, 180 + a, 360 - a]


def main_angles(shape, style):
    if shape == "Coussin" and style == "Brillant":
        return [29, 61, 119, 151, 209, 241, 299, 331]
    if not (style in ("À degrés", "Ascher")):
        return evenly(6) if shape == "Triangle" else evenly(8)
    return {"Carré": corner_angles(0.84, 0.84), "Rectangle": corner_angles(0.98, 0.66), "Baguette": corner_angles(1.0, 0.40),
            "Rectangle à pans coupés": chamfer_angles(0.98, 0.70, 0.32), "Octogone": chamfer_angles(0.88, 0.88, 0.40),
            "Triangle": evenly(3)}.get(shape, evenly(8))


CROWN_FR = [None, [0, 1], [0, 0.40, 1], [0, 0.22, 0.55, 1], [0, 0.14, 0.32, 0.62, 1]]
PAV_FR = [None, [0, 1], [0, 0.55, 1], [0, 0.30, 0.62, 1]]


def plan_for(shape, style):
    p = dict(crown_steps=0, pav_steps=0, split=False, star=0.80, table=0.53, crown=0.324, depth=0.862)
    if style == "Mixte":
        p.update(crown_steps=2, table=0.56, crown=0.30)
    elif style == "À degrés":
        p.update(crown_steps=3, pav_steps=3, table=0.58, crown=0.26, depth=0.72)
    elif style == "Ascher":
        p.update(crown_steps=4, pav_steps=3, table=0.50, crown=0.40, depth=0.90)
    elif style == "Princesse":
        p.update(table=0.62, crown=0.28, depth=0.95)
    elif style == "Radiant":
        p.update(split=True, table=0.60, crown=0.30, depth=0.90)
    elif style == "Brillant" and shape == "Coussin":
        p.update(table=0.58, crown=0.30, depth=0.80)
    elif style == "Ciseaux":
        p.update(star=0.93, table=0.44, crown=0.34)
    if shape == "Baguette" and style == "À degrés":
        p.update(crown_steps=1, pav_steps=2, table=0.62, crown=0.22, depth=0.62)
    return p


def build_faceted(shape, style):
    if style == "Princesse":
        return build_princess(shape)
    plan = plan_for(shape, style)
    angles = main_angles(shape, style)
    n = len(angles)
    m = Mesh()

    def outline(deg):
        return 1.0 if shape == "Ovale" else max(0.05, boundary_radius(shape, math.radians(deg)))

    def nxt(k):
        return angles[k + 1] if k + 1 < n else angles[0] + 360

    def mid_angle(k):
        return (angles[k] + nxt(k)) * 0.5

    def half_sector(k):
        return math.radians((nxt(k) - angles[k]) * 0.5)

    def add(radius, deg, z):
        r = radius * outline(deg)
        return m.add_at((math.cos(math.radians(deg)) * r, math.sin(math.radians(deg)) * r, z))

    table_z = GIRDLE_HALF + plan["crown"]
    slope = plan["crown"] / (1 - plan["table"])
    culet_z = -GIRDLE_HALF - plan["depth"]
    b_crown, b_pav = plan["crown_steps"] == 0, plan["pav_steps"] == 0
    has_mids = b_crown or b_pav
    girdle = [add(1.0, angles[k], GIRDLE_HALF) for k in range(n)]
    girdle_mid = [0] * n
    if has_mids:
        for k in range(n):
            if b_crown and b_pav:
                girdle_mid[k] = add(1.0, mid_angle(k), GIRDLE_HALF)
            else:
                a, b = m.v[girdle[k]], m.v[girdle[(k + 1) % n]]
                girdle_mid[k] = m.add_at(((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, GIRDLE_HALF))
    # crown
    if b_crown:
        table, star = [], []
        for k in range(n):
            table.append(add(plan["table"], angles[k], table_z))
            z = table_z - slope * (plan["star"] * math.cos(half_sector(k)) - plan["table"])
            star.append(add(plan["star"], mid_angle(k), z))
        m.facet(True, *table)
        for k in range(n):
            nx, pv = (k + 1) % n, (k + n - 1) % n
            m.facet(True, table[k], table[nx], star[k])
            m.facet(True, table[k], star[pv], girdle[k], star[k])
            m.facet(True, star[k], girdle[k], girdle_mid[k])
            m.facet(True, star[k], girdle_mid[k], girdle[nx])
    else:
        rows = plan["crown_steps"]
        fr = CROWN_FR[rows]
        ring = [None] * (rows + 1)
        ring[rows] = girdle
        for j in range(rows):
            radius = plan["table"] + (1 - plan["table"]) * j / rows
            z = table_z + (GIRDLE_HALF - table_z) * fr[j]
            ring[j] = [add(radius, angles[k], z) for k in range(n)]
        m.facet(True, *ring[0])
        for j in range(rows):
            for k in range(n):
                m.facet(True, ring[j][k], ring[j][(k + 1) % n], ring[j + 1][(k + 1) % n], ring[j + 1][k])
    # girdle band
    lower = []
    lower_mid = [0] * n
    for k in range(n):
        top = m.v[girdle[k]]
        lower.append(m.add_at((top[0], top[1], -GIRDLE_HALF)))
        if has_mids:
            tm = m.v[girdle_mid[k]]
            lower_mid[k] = m.add_at((tm[0], tm[1], -GIRDLE_HALF))
    for k in range(n):
        nx = (k + 1) % n
        if has_mids:
            m.facet(False, girdle[k], girdle_mid[k], lower_mid[k], lower[k])
            m.facet(False, girdle_mid[k], girdle[nx], lower[nx], lower_mid[k])
        else:
            m.facet(False, girdle[k], girdle[nx], lower[nx], lower[k])
    # pavilion
    if b_pav:
        side_r = 0.68
        side = []
        for k in range(n):
            z = culet_z + plan["depth"] * (side_r * math.cos(half_sector(k)))
            side.append(add(side_r, mid_angle(k), z))
        tip = m.add_at((0, 0, culet_z))
        halfway = [0] * n
        if plan["split"]:
            for k in range(n):
                a, b = m.v[tip], m.v[side[k]]
                halfway[k] = m.add_at(tuple((a[i] + b[i]) / 2 for i in range(3)))
        for k in range(n):
            pv = (k + n - 1) % n
            if plan["split"]:
                m.facet(True, side[pv], lower[k], side[k], halfway[k], halfway[pv])
                m.facet(True, tip, halfway[pv], halfway[k])
            else:
                m.facet(True, tip, side[pv], lower[k], side[k])
        for k in range(n):
            nx = (k + 1) % n
            m.facet(True, side[k], lower[k], lower_mid[k])
            m.facet(True, side[k], lower_mid[k], lower[nx])
    else:
        rows = plan["pav_steps"]
        fr = PAV_FR[rows]
        ring = [None] * (rows + 1)
        ring[0] = lower
        for j in range(1, rows + 1):
            radius = 1 - (1 - 0.07) * j / rows
            z = -GIRDLE_HALF + (culet_z + GIRDLE_HALF) * fr[j]
            ring[j] = [add(radius, angles[k], z) for k in range(n)]
        for j in range(rows):
            for k in range(n):
                m.facet(True, ring[j][k], ring[j][(k + 1) % n], ring[j + 1][(k + 1) % n], ring[j + 1][k])
        m.facet(True, *ring[rows])
    return m.normalize()


def build_princess(shape):
    asp = aspect(shape)
    corner = math.degrees(math.atan2(asp[1], asp[0]))
    angles = [corner, 180 - corner, 180 + corner, 360 - corner]
    n, thirds = 4, 3
    table_r, crown_h, pav_d, chev_r = 0.64, 0.28, 0.95, 0.52
    m = Mesh()
    corners = []
    for a in angles:
        r = max(0.05, boundary_radius(shape, math.radians(a)))
        corners.append((math.cos(math.radians(a)) * r, math.sin(math.radians(a)) * r))
    ring = n * thirds
    gxy = []
    for k in range(n):
        a, b = corners[k], corners[(k + 1) % n]
        for j in range(thirds):
            t = j / thirds
            gxy.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    table_z = GIRDLE_HALF + crown_h
    culet_z = -GIRDLE_HALF - pav_d
    upper, lower, table = [], [], []
    for i in range(ring):
        upper.append(m.add_at((gxy[i][0], gxy[i][1], GIRDLE_HALF)))
        lower.append(m.add_at((gxy[i][0], gxy[i][1], -GIRDLE_HALF)))
        table.append(m.add_at((gxy[i][0] * table_r, gxy[i][1] * table_r, table_z)))
    m.facet(True, *table)
    for i in range(ring):
        nx = (i + 1) % ring
        m.facet(True, table[i], table[nx], upper[nx], upper[i])
        m.facet(False, upper[i], upper[nx], lower[nx], lower[i])
    pav = []
    for k in range(n):
        mid = ((corners[k][0] + corners[(k + 1) % n][0]) / 2, (corners[k][1] + corners[(k + 1) % n][1]) / 2)
        z = culet_z + 0.5 * chev_r * (-GIRDLE_HALF - culet_z)
        pav.append(m.add_at((mid[0] * chev_r, mid[1] * chev_r, z)))
    tip = m.add_at((0, 0, culet_z))
    for k in range(n):
        for j in range(thirds):
            a = k * thirds + j
            m.facet(True, lower[a], lower[(a + 1) % ring], pav[k])
    for k in range(n):
        pv = (k + n - 1) % n
        m.facet(True, lower[k * thirds], pav[k], tip, pav[pv])
    return m.normalize()


CABOCHON = [(0, 0.64), (0.20, 0.62), (0.40, 0.56), (0.58, 0.47), (0.72, 0.37), (0.84, 0.26), (0.93, 0.15), (1.0, 0.04),
            (1.0, -0.02), (0.96, -0.10), (0, -0.10)]


def build_cabochon(shape, segments=72):
    m = Mesh()
    starts, sizes = [], []
    for rho, z in CABOCHON:
        starts.append(len(m.v))
        if rho <= 0:
            sizes.append(1)
            m.add_at((0, 0, z))
            continue
        sizes.append(segments)
        for k in range(segments):
            th = k * 2 * math.pi / segments
            r = max(0.05, boundary_radius(shape, th)) * rho
            m.add_at((math.cos(th) * r, math.sin(th) * r, z))
    fid = 1
    for r in range(len(CABOCHON) - 1):
        if sizes[r] == 1 or sizes[r + 1] == 1:
            c, o = (r, r + 1) if sizes[r] == 1 else (r + 1, r)
            for k in range(segments):
                m.add_triangle(starts[c], starts[o] + k, starts[o] + (k + 1) % segments, 1)
            continue
        for k in range(segments):
            nx = (k + 1) % segments
            a, b = starts[r] + k, starts[r] + nx
            c, d = starts[r + 1] + nx, starts[r + 1] + k
            m.add_triangle(a, d, c, 1)
            m.add_triangle(a, c, b, 1)
    m.normalize()
    normals = [[0.0, 0.0, 0.0] for _ in m.v]
    for t in m.tris:
        p0, p1, p2 = (m.v[i] for i in t)
        e1 = [p1[k] - p0[k] for k in range(3)]
        e2 = [p2[k] - p0[k] for k in range(3)]
        f = (e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0])
        for i in t:
            for k in range(3):
                normals[i][k] += f[k]
    for nrm in normals:
        ln = math.sqrt(sum(c * c for c in nrm))
        if ln > 1e-5:
            nrm[:] = [c / ln for c in nrm]
        else:
            nrm[:] = [0, 0, 1]
    m.ids = [1] * len(m.ids)
    m.normals = normals
    return m


def slug(name):
    t = name.lower()
    for a, b in (("é", "e"), ("è", "e"), ("à", "a"), ("œ", "oe"), (" ", "-")):
        t = t.replace(a, b)
    return t


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    index = {"shapes": {}, "styles": {}, "allowed": {}, "aspect": {}}
    count = 0
    for style in STYLES:
        shapes = SHAPES if style in ("Cabochon", "Mixte") else ALLOWED[style]
        index["allowed"][style] = shapes
        for shape in shapes:
            m = build_cabochon(shape) if style == "Cabochon" else build_faceted(shape, style)
            data = {"v": [round(c, 4) for p in m.v for c in p], "t": [i for t in m.tris for i in t], "f": m.ids}
            if style == "Cabochon":
                data["n"] = [round(c, 4) for p in m.normals for c in p]
            (OUT / "{}__{}.json".format(slug(shape), slug(style))).write_text(json.dumps(data, separators=(",", ":")), encoding="utf-8")
            count += 1
    for shape in SHAPES:
        ax, ay = aspect(shape)
        index["aspect"][slug(shape)] = round(min(ax, ay) / max(ax, ay), 4)
        index["shapes"][slug(shape)] = shape
    index["styles"] = {slug(s): s for s in STYLES}
    index["allowed"] = {slug(s): [slug(x) for x in v] for s, v in index["allowed"].items()}
    (OUT / "index.json").write_text(json.dumps(index, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print("{} formes de taille écrites dans {}".format(count, OUT))


if __name__ == "__main__":
    main()
