using System.Collections.Generic;
using UnityEngine;

// Real facet geometry of a cut stone (crown, girdle, pavilion), used to render gems in 3D. The mesh is
// normalized: x, y and z all run from -1 to 1, ready to be scaled by the half sizes of a stone.
public class GemMesh
{
    public Vector3[] Vertices;
    public int[][] Triangles;       // each: 3 vertex indices
    public int[] FacetIds;          // one id per triangle; triangles of the same flat facet share it

    private struct Ring
    {
        public float Rho;
        public float Z;
        public float Alternate;

        public Ring(float rho, float z, float alternate = 0f)
        {
            Rho = rho;
            Z = z;
            Alternate = alternate;
        }
    }

    private static readonly Dictionary<string, GemMesh> Cache = new Dictionary<string, GemMesh>();

    public static GemMesh Get(string shape, string style)
    {
        var key = $"{shape}|{style}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        // Brilliant-type styles need larger facets (fewer segments); step cuts follow the outline closely.
        var smooth = style == "À degrés" || style == "Émeraude" || style == "Émeraude (à degrés)" || style == "Ascher"
                     || style == "Baguette" || style == "Princesse" || style == "Cabochon";
        var mesh = Build(shape, Profile(style), smooth ? 72 : 32);
        Cache[key] = mesh;
        return mesh;
    }

    // Lumpy, angular rock for rough stones.
    public static GemMesh GetRough(int seed)
    {
        var key = $"rough|{seed}";
        if (Cache.TryGetValue(key, out var cached)) return cached;

        var rng = new System.Random(seed);
        var a = (float)rng.NextDouble() * 6.28f;
        var b = (float)rng.NextDouble() * 6.28f;
        var c = (float)rng.NextDouble() * 6.28f;
        var rings = new[]
        {
            new Ring(0f, 0.42f), new Ring(0.50f, 0.36f), new Ring(0.85f, 0.14f), new Ring(1.0f, 0.0f),
            new Ring(0.86f, -0.26f), new Ring(0.50f, -0.40f), new Ring(0f, -0.46f),
        };

        const int segments = 14;
        var mesh = BuildRings(rings, segments, theta =>
        {
            return 0.78f + 0.14f * Mathf.Sin(2f * theta + a) + 0.08f * Mathf.Sin(3f * theta + b) + 0.06f * Mathf.Sin(5f * theta + c);
        }, rng, 0.16f, 0.09f);

        Cache[key] = mesh;
        return mesh;
    }

    private static Ring[] Profile(string style)
    {
        switch (style)
        {
            case "Cabochon":
                return new[]
                {
                    new Ring(0f, 0.64f), new Ring(0.20f, 0.62f), new Ring(0.40f, 0.56f), new Ring(0.58f, 0.47f), new Ring(0.72f, 0.37f),
                    new Ring(0.84f, 0.26f), new Ring(0.93f, 0.15f), new Ring(1.0f, 0.04f), new Ring(1.0f, -0.02f),
                    new Ring(0.96f, -0.10f), new Ring(0f, -0.10f),
                };
            case "À degrés":
            case "Émeraude":
            case "Émeraude (à degrés)":
                return new[]
                {
                    new Ring(0f, 0.30f), new Ring(0.42f, 0.30f), new Ring(0.58f, 0.24f), new Ring(0.74f, 0.17f), new Ring(0.88f, 0.10f),
                    new Ring(1.0f, 0.04f), new Ring(1.0f, -0.03f), new Ring(0.82f, -0.14f), new Ring(0.64f, -0.27f),
                    new Ring(0.46f, -0.40f), new Ring(0.28f, -0.50f), new Ring(0f, -0.55f),
                };
            case "Ascher":
                return new[]
                {
                    new Ring(0f, 0.34f), new Ring(0.30f, 0.34f), new Ring(0.43f, 0.29f), new Ring(0.55f, 0.24f), new Ring(0.67f, 0.19f),
                    new Ring(0.78f, 0.13f), new Ring(0.89f, 0.07f), new Ring(1.0f, 0.03f), new Ring(1.0f, -0.03f),
                    new Ring(0.84f, -0.14f), new Ring(0.68f, -0.26f), new Ring(0.52f, -0.38f), new Ring(0.36f, -0.48f), new Ring(0f, -0.54f),
                };
            case "Baguette":
                return new[]
                {
                    new Ring(0f, 0.26f), new Ring(0.45f, 0.26f), new Ring(0.65f, 0.17f), new Ring(0.85f, 0.08f), new Ring(1.0f, 0.03f),
                    new Ring(1.0f, -0.03f), new Ring(0.80f, -0.20f), new Ring(0.55f, -0.36f), new Ring(0f, -0.46f),
                };
            case "Mixte":
                return new[]
                {
                    new Ring(0f, 0.32f), new Ring(0.40f, 0.32f), new Ring(0.64f, 0.25f, 0.06f), new Ring(0.78f, 0.18f), new Ring(0.90f, 0.11f),
                    new Ring(1.0f, 0.04f), new Ring(1.0f, -0.03f), new Ring(0.82f, -0.14f), new Ring(0.62f, -0.28f),
                    new Ring(0.40f, -0.42f), new Ring(0f, -0.52f),
                };
            case "Radiant":
                return new[]
                {
                    new Ring(0f, 0.32f), new Ring(0.40f, 0.32f), new Ring(0.70f, 0.22f, 0.06f), new Ring(0.86f, 0.12f), new Ring(1.0f, 0.04f),
                    new Ring(1.0f, -0.03f), new Ring(0.80f, -0.22f), new Ring(0.50f, -0.44f, 0.05f), new Ring(0f, -0.58f),
                };
            case "Princesse":
                return new[]
                {
                    new Ring(0f, 0.30f), new Ring(0.34f, 0.30f), new Ring(0.52f, 0.23f), new Ring(0.70f, 0.16f), new Ring(0.86f, 0.09f),
                    new Ring(1.0f, 0.03f), new Ring(1.0f, -0.03f), new Ring(0.75f, -0.22f), new Ring(0.40f, -0.44f), new Ring(0f, -0.58f),
                };
            case "Ciseaux":
                return new[]
                {
                    new Ring(0f, 0.32f), new Ring(0.30f, 0.32f), new Ring(0.55f, 0.24f, 0.09f), new Ring(0.80f, 0.14f, 0.06f), new Ring(1.0f, 0.04f),
                    new Ring(1.0f, -0.03f), new Ring(0.78f, -0.22f, 0.05f), new Ring(0.45f, -0.44f), new Ring(0f, -0.58f),
                };
            default:        // Brilliant
                return new[]
                {
                    new Ring(0f, 0.34f), new Ring(0.44f, 0.34f), new Ring(0.62f, 0.27f, 0.07f), new Ring(0.88f, 0.12f, 0.03f),
                    new Ring(1.0f, 0.05f), new Ring(1.0f, -0.03f), new Ring(0.78f, -0.22f, 0.04f), new Ring(0.45f, -0.45f, 0.05f), new Ring(0f, -0.60f),
                };
        }
    }

    private static GemMesh Build(string shape, Ring[] rings, int segments)
    {
        return BuildRings(rings, segments, theta => Mathf.Max(0.05f, GemPixelArt.BoundaryRadius(shape, theta)), null, 0f, 0f);
    }

    // Revolves the profile around the outline: ring i sits at `rho * outlineRadius(theta)` and height `z`.
    private static GemMesh BuildRings(Ring[] rings, int segments, System.Func<float, float> outline,
        System.Random jitter, float radiusJitter, float zJitter)
    {
        var vertices = new List<Vector3>();
        var ringStart = new int[rings.Length];
        var ringSize = new int[rings.Length];

        for (var r = 0; r < rings.Length; r++)
        {
            ringStart[r] = vertices.Count;
            if (rings[r].Rho <= 0f)
            {
                ringSize[r] = 1;
                vertices.Add(new Vector3(0f, 0f, rings[r].Z));
                continue;
            }

            ringSize[r] = segments;
            for (var k = 0; k < segments; k++)
            {
                var theta = k * 2f * Mathf.PI / segments;
                var rho = rings[r].Rho + (k % 2 == 0 ? rings[r].Alternate : -rings[r].Alternate);
                var z = rings[r].Z;
                if (jitter != null)
                {
                    rho *= 1f + ((float)jitter.NextDouble() * 2f - 1f) * radiusJitter;
                    z += ((float)jitter.NextDouble() * 2f - 1f) * zJitter;
                }

                var radius = outline(theta) * rho;
                vertices.Add(new Vector3(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius, z));
            }
        }

        var triangles = new List<int[]>();
        var facetIds = new List<int>();
        var nextFacet = 1;

        for (var r = 0; r + 1 < rings.Length; r++)
        {
            var flat = Mathf.Approximately(rings[r].Z, rings[r + 1].Z) && (ringSize[r] == 1 || ringSize[r + 1] == 1);
            var sharedId = nextFacet++;

            if (ringSize[r] == 1 || ringSize[r + 1] == 1)
            {
                // Fan between a single center vertex and a ring.
                var centerRing = ringSize[r] == 1 ? r : r + 1;
                var otherRing = centerRing == r ? r + 1 : r;
                for (var k = 0; k < segments; k++)
                {
                    var next = (k + 1) % segments;
                    AddTriangle(vertices, triangles, facetIds, ringStart[centerRing], ringStart[otherRing] + k, ringStart[otherRing] + next,
                        flat ? sharedId : nextFacet++);
                }
                continue;
            }

            for (var k = 0; k < segments; k++)
            {
                var next = (k + 1) % segments;
                var a = ringStart[r] + k;
                var b = ringStart[r] + next;
                var c = ringStart[r + 1] + next;
                var d = ringStart[r + 1] + k;
                var id = nextFacet++;
                AddTriangle(vertices, triangles, facetIds, a, d, c, id);
                AddTriangle(vertices, triangles, facetIds, a, c, b, id);
            }
        }

        return Normalize(vertices, triangles, facetIds);
    }

    // Adds a triangle wound so that its normal points away from the middle of the stone.
    private static void AddTriangle(List<Vector3> vertices, List<int[]> triangles, List<int> facetIds, int i0, int i1, int i2, int id)
    {
        var p0 = vertices[i0];
        var p1 = vertices[i1];
        var p2 = vertices[i2];
        var normal = Vector3.Cross(p1 - p0, p2 - p0);
        var centroid = (p0 + p1 + p2) / 3f;
        if (Vector3.Dot(normal, centroid) < 0f)
            triangles.Add(new[] { i0, i2, i1 });
        else
            triangles.Add(new[] { i0, i1, i2 });
        facetIds.Add(id);
    }

    // Scales x and y so the outline's bounding box spans -1..1, and z so the stone's height does too.
    private static GemMesh Normalize(List<Vector3> vertices, List<int[]> triangles, List<int> facetIds)
    {
        float maxX = 0.01f, maxY = 0.01f, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var v in vertices)
        {
            maxX = Mathf.Max(maxX, Mathf.Abs(v.x));
            maxY = Mathf.Max(maxY, Mathf.Abs(v.y));
            minZ = Mathf.Min(minZ, v.z);
            maxZ = Mathf.Max(maxZ, v.z);
        }

        var middleZ = (minZ + maxZ) * 0.5f;
        var halfZ = Mathf.Max(0.01f, (maxZ - minZ) * 0.5f);
        var result = new Vector3[vertices.Count];
        for (var i = 0; i < vertices.Count; i++)
            result[i] = new Vector3(vertices[i].x / maxX, vertices[i].y / maxY, (vertices[i].z - middleZ) / halfZ);

        return new GemMesh { Vertices = result, Triangles = triangles.ToArray(), FacetIds = facetIds.ToArray() };
    }
}
