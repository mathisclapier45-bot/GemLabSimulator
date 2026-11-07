using System.Collections.Generic;
using UnityEngine;

// The geometry of the crystal habits of the rough stones. Each habit is a convex polyhedron given by its planes (a normal and a distance: the solid is what lies
// under all of them); the vertices are where three planes meet, and each plane gives one flat facet. The long axis of a prism runs along x, so the habit shows
// its terminations when it is seen from above; the plates are flat along z. The mesh is centred and fits in the unit sphere, so a stone can turn freely.
public static class HabitMesh
{
    // The view of the icons: the cubic habits seen from a corner, the others turned so that both ends show.
    public static Quaternion IconOrientation(string id)
    {
        switch (id)
        {
            case "octahedron":
            case "octahedron_trigons":
            case "cube":
            case "cuboctahedron":
            case "rhombic_dodecahedron":
            case "trapezohedron":
                return Quaternion.Euler(-38f, 28f, 12f);
            default:
                return Quaternion.Euler(-30f, -20f, 10f);
        }
    }

    private static Vector4 P(float x, float y, float z, float d) => new Vector4(x, y, z, d);

    // The sides of a prism around the x axis: n planes at the given distance, the first one at the given angle.
    private static void Sides(List<Vector4> planes, int n, float distance, float phase = 0f)
    {
        for (var i = 0; i < n; i++)
        {
            var t = (phase + 360f * i / n) * Mathf.Deg2Rad;
            planes.Add(P(0f, Mathf.Cos(t), Mathf.Sin(t), distance));
        }
    }

    // A ring of faces sloping towards the end of a prism (a pyramid or a bevel).
    private static void Cap(List<Vector4> planes, int n, float slope, float phase, float distance)
    {
        for (var end = -1; end <= 1; end += 2)
            for (var i = 0; i < n; i++)
            {
                var t = (phase + 360f * i / n) * Mathf.Deg2Rad;
                planes.Add(P(end, slope * Mathf.Cos(t), slope * Mathf.Sin(t), distance));
            }
    }

    private static void Axes(List<Vector4> planes, float x, float y, float z)
    {
        planes.Add(P(1f, 0f, 0f, x)); planes.Add(P(-1f, 0f, 0f, x));
        planes.Add(P(0f, 1f, 0f, y)); planes.Add(P(0f, -1f, 0f, y));
        planes.Add(P(0f, 0f, 1f, z)); planes.Add(P(0f, 0f, -1f, z));
    }

    private static void Signed(List<Vector4> planes, float a, float b, float c, float distance)
    {
        // every permutation of (a, b, c) with every sign
        var values = new[] { a, b, c };
        var seen = new List<Vector3>();
        var orders = new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
        foreach (var order in orders)
            for (var sx = -1; sx <= 1; sx += 2)
                for (var sy = -1; sy <= 1; sy += 2)
                    for (var sz = -1; sz <= 1; sz += 2)
                    {
                        var n = new Vector3(sx * values[order[0]], sy * values[order[1]], sz * values[order[2]]);
                        var known = false;
                        foreach (var other in seen)
                            if ((other - n).sqrMagnitude < 1e-6f) { known = true; break; }
                        if (known) continue;
                        seen.Add(n);
                        planes.Add(P(n.x, n.y, n.z, distance));
                    }
    }

    private static List<Vector4> Planes(string id)
    {
        var p = new List<Vector4>();
        switch (id)
        {
            case "octahedron":
                Signed(p, 1f, 1f, 1f, 1f);
                return p;
            case "cube":
                Axes(p, 1f, 1f, 1f);
                return p;
            case "cuboctahedron":
                Axes(p, 1f, 1f, 1f);
                Signed(p, 1f, 1f, 1f, 2f);
                return p;
            case "rhombic_dodecahedron":
                Signed(p, 1f, 1f, 0f, 1f);
                return p;
            case "trapezohedron":
                Signed(p, 2f, 1f, 1f, 2f);
                return p;

            case "hex_prism":
                Sides(p, 6, 0.55f);
                p.Add(P(1f, 0f, 0f, 1f)); p.Add(P(-1f, 0f, 0f, 1f));
                Cap(p, 6, 0.62f, 30f, 1.2f);
                return p;
            case "quartz_prism":
                Sides(p, 6, 0.55f);
                Cap(p, 6, 0.62f, 30f, 1.05f);
                return p;
            case "hex_bipyramid":
                Sides(p, 6, 0.62f);
                Cap(p, 6, 0.85f, 0f, 1f);
                return p;
            case "hex_tabular":
            {
                for (var i = 0; i < 6; i++)
                {
                    var t = 60f * i * Mathf.Deg2Rad;
                    p.Add(P(Mathf.Cos(t), Mathf.Sin(t), 0f, 1f));
                }
                p.Add(P(0f, 0f, 1f, 0.30f)); p.Add(P(0f, 0f, -1f, 0.30f));
                for (var sz = -1; sz <= 1; sz += 2)
                    for (var i = 0; i < 6; i++)
                    {
                        var t = (30f + 60f * i) * Mathf.Deg2Rad;
                        p.Add(P(0.7f * Mathf.Cos(t), 0.7f * Mathf.Sin(t), sz, 1.05f));
                    }
                return p;
            }
            case "tetragonal_prism":
                p.Add(P(0f, 1f, 0f, 0.5f)); p.Add(P(0f, -1f, 0f, 0.5f));
                p.Add(P(0f, 0f, 1f, 0.5f)); p.Add(P(0f, 0f, -1f, 0.5f));
                for (var sx = -1; sx <= 1; sx += 2)
                    for (var s = -1; s <= 1; s += 2)
                    {
                        p.Add(P(sx, s * 0.62f, 0f, 1f));
                        p.Add(P(sx, 0f, s * 0.62f, 1f));
                    }
                return p;
            case "ortho_prism":
                p.Add(P(0f, 1f, 0f, 0.55f)); p.Add(P(0f, -1f, 0f, 0.55f));
                p.Add(P(0f, 0f, 1f, 0.40f)); p.Add(P(0f, 0f, -1f, 0.40f));
                for (var sy = -1; sy <= 1; sy += 2)
                    for (var sz = -1; sz <= 1; sz += 2)
                        p.Add(P(0f, sy * 0.7f, sz * 0.7f, 0.78f));
                for (var sx = -1; sx <= 1; sx += 2)
                    for (var s = -1; s <= 1; s += 2)
                    {
                        p.Add(P(sx, s * 0.35f, 0f, 1f));
                        p.Add(P(sx, 0f, s * 0.55f, 1f));
                    }
                return p;
            case "monoclinic_prism":
                p.Add(P(0f, 1f, 0f, 0.62f)); p.Add(P(0f, -1f, 0f, 0.62f));
                p.Add(P(0f, 0f, 1f, 0.26f)); p.Add(P(0f, 0f, -1f, 0.26f));
                p.Add(P(1f, 0.25f, 0.55f, 1f)); p.Add(P(-1f, -0.15f, -0.5f, 1f));
                p.Add(P(1f, -0.35f, 0f, 1.1f)); p.Add(P(-1f, 0.35f, 0f, 1.1f));
                for (var sy = -1; sy <= 1; sy += 2)
                    for (var sz = -1; sz <= 1; sz += 2)
                        p.Add(P(0f, sy * 0.7f, sz * 0.45f, 0.5f));
                return p;
            case "trigonal_prism":
                for (var i = 0; i < 3; i++)
                {
                    var strong = (90f + 120f * i) * Mathf.Deg2Rad;
                    var weak = (270f + 120f * i) * Mathf.Deg2Rad;
                    p.Add(P(0f, Mathf.Cos(strong), Mathf.Sin(strong), 0.58f));
                    p.Add(P(0f, Mathf.Cos(weak), Mathf.Sin(weak), 0.46f));
                }
                p.Add(P(1f, 0f, 0f, 1f)); p.Add(P(-1f, 0f, 0f, 1f));
                for (var sx = -1; sx <= 1; sx += 2)
                    for (var i = 0; i < 3; i++)
                    {
                        var t = (90f + 120f * i) * Mathf.Deg2Rad;
                        p.Add(P(sx, 0.5f * Mathf.Cos(t), 0.5f * Mathf.Sin(t), 1.18f));
                    }
                return p;
            case "rhombohedron":
                for (var i = 0; i < 3; i++)
                {
                    var t = (90f + 120f * i) * Mathf.Deg2Rad;
                    p.Add(P(0.72f, Mathf.Cos(t), Mathf.Sin(t), 1f));
                    p.Add(P(-0.72f, -Mathf.Cos(t), -Mathf.Sin(t), 1f));
                }
                return p;
        }
        return null;
    }

    // An octahedron whose faces carry trigons: the small triangular pits, turned upside down compared with the face, that the corrosion of the diamond leaves on
    // the faces of the octahedron. Each face has three flat sides around its pit, and the pit is three small facets down to a point.
    private static GemMesh BuildTrigons()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int[]>();
        var facetIds = new List<int>();

        int Index(Vector3 v)
        {
            for (var i = 0; i < vertices.Count; i++)
                if ((vertices[i] - v).sqrMagnitude < 1e-10f) return i;
            vertices.Add(v);
            return vertices.Count - 1;
        }

        var face = 0;
        for (var sx = -1; sx <= 1; sx += 2)
            for (var sy = -1; sy <= 1; sy += 2)
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    var x = new Vector3(sx, 0f, 0f);
                    var y = new Vector3(0f, sy, 0f);
                    var z = new Vector3(0f, 0f, sz);
                    var n = new Vector3(sx, sy, sz).normalized;
                    var centre = (x + y + z) / 3f;

                    // The pit: an inverted triangle at the middle of the face (its corners point to the edges), and a point below. Each face has a pit of its own size.
                    var t = 0.72f + 0.08f * (face % 3 - 1);
                    var qx = centre + t * ((y + z) / 2f - centre);
                    var qy = centre + t * ((z + x) / 2f - centre);
                    var qz = centre + t * ((x + y) / 2f - centre);
                    var bottom = centre - n * (0.10f + 0.02f * (face % 2));

                    var ix = Index(x); var iy = Index(y); var iz = Index(z);
                    var a = Index(qx); var b = Index(qy); var c = Index(qz); var d = Index(bottom);

                    var currentFace = face;
                    void Add(int p0, int p1, int p2, int id)
                    {
                        // Anticlockwise seen from outside: the normal looks the same way as the face.
                        var normal = Vector3.Cross(vertices[p1] - vertices[p0], vertices[p2] - vertices[p0]);
                        if (Vector3.Dot(normal, n) < 0f) { var swap = p1; p1 = p2; p2 = swap; }
                        triangles.Add(new[] { p0, p1, p2 });
                        facetIds.Add(id);
                    }

                    // The flat part of the face around the pit.
                    Add(ix, iy, c, currentFace); Add(iy, iz, a, currentFace); Add(iz, ix, b, currentFace);
                    Add(iy, a, c, currentFace); Add(iz, b, a, currentFace); Add(ix, c, b, currentFace);
                    // The three small facets of the pit.
                    Add(a, b, d, 100 + currentFace * 3); Add(b, c, d, 101 + currentFace * 3); Add(c, a, d, 102 + currentFace * 3);
                    face++;
                }

        return new GemMesh { Vertices = vertices.ToArray(), Triangles = triangles.ToArray(), FacetIds = facetIds.ToArray(), FacetCount = 0 };
    }

    // Builds the mesh of a habit, or null if the habit is unknown.
    public static GemMesh Build(string id)
    {
        if (id == "octahedron_trigons") return BuildTrigons();

        var planes = Planes(id);
        if (planes == null) return null;

        var normals = new List<Vector3>();
        var offsets = new List<float>();
        foreach (var plane in planes)
        {
            var n = new Vector3(plane.x, plane.y, plane.z);
            var length = n.magnitude;
            normals.Add(n / length);
            offsets.Add(plane.w / length);
        }

        // The vertices: where three planes meet, inside all the others.
        var raw = new List<Vector3>();
        var count = normals.Count;
        for (var i = 0; i < count; i++)
            for (var j = i + 1; j < count; j++)
                for (var k = j + 1; k < count; k++)
                {
                    var det = Vector3.Dot(normals[i], Vector3.Cross(normals[j], normals[k]));
                    if (Mathf.Abs(det) < 1e-5f) continue;
                    var v = (offsets[i] * Vector3.Cross(normals[j], normals[k])
                             + offsets[j] * Vector3.Cross(normals[k], normals[i])
                             + offsets[k] * Vector3.Cross(normals[i], normals[j])) / det;

                    var inside = true;
                    for (var m = 0; m < count && inside; m++)
                        if (Vector3.Dot(normals[m], v) > offsets[m] + 1e-4f) inside = false;
                    if (!inside) continue;

                    var known = false;
                    foreach (var w in raw)
                        if ((w - v).sqrMagnitude < 1e-8f) { known = true; break; }
                    if (!known) raw.Add(v);
                }
        if (raw.Count < 4) return null;

        // Centred, and scaled to fit in the unit sphere.
        var centre = Vector3.zero;
        foreach (var v in raw) centre += v;
        centre /= raw.Count;
        var radius = 0f;
        foreach (var v in raw) radius = Mathf.Max(radius, (v - centre).magnitude);

        var vertices = new Vector3[raw.Count];
        for (var i = 0; i < raw.Count; i++) vertices[i] = (raw[i] - centre) / radius;

        // One facet for each plane that has at least three vertices on it, turned anticlockwise seen from outside.
        var triangles = new List<int[]>();
        var facetIds = new List<int>();
        for (var f = 0; f < count; f++)
        {
            var on = new List<int>();
            for (var i = 0; i < raw.Count; i++)
                if (Mathf.Abs(Vector3.Dot(normals[f], raw[i]) - offsets[f]) < 1e-3f) on.Add(i);
            if (on.Count < 3) continue;

            var middle = Vector3.zero;
            foreach (var i in on) middle += vertices[i];
            middle /= on.Count;
            var a = (vertices[on[0]] - middle).normalized;
            var b = Vector3.Cross(normals[f], a);
            on.Sort((x, y) =>
            {
                var ax = Mathf.Atan2(Vector3.Dot(vertices[x] - middle, b), Vector3.Dot(vertices[x] - middle, a));
                var ay = Mathf.Atan2(Vector3.Dot(vertices[y] - middle, b), Vector3.Dot(vertices[y] - middle, a));
                return ax.CompareTo(ay);
            });
            for (var q = 1; q < on.Count - 1; q++)
            {
                triangles.Add(new[] { on[0], on[q], on[q + 1] });
                facetIds.Add(f);
            }
        }

        return new GemMesh { Vertices = vertices, Triangles = triangles.ToArray(), FacetIds = facetIds.ToArray(), FacetCount = 0 };
    }
}
