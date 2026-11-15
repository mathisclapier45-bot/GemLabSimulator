using System.Collections.Generic;
using UnityEngine;

// The realistic gem: a faceted stone drawn on the graphics card by the shader GemLab/RealGem (ray tracing inside the stone, with dispersion), in a small hidden studio far from
// the town, and read back into the picture that GemView3D hands to the screens. The world of the studio is mirrored along z (the camera sits on the -z side and looks along +z),
// so the mesh and the rotation of the stone are mirrored too: the picture is the one the software renderer would have drawn.
public static class RealGemRenderer
{
    private const int StudioLayer = 31;
    private const float CameraDistance = 20f;
    private static readonly Vector3 StudioPosition = new Vector3(4000f, 4000f, 0f);

    private const int MaxPlanes = 256;          // must match the size of _Planes in GemRealistic.shader

    private class Prepared
    {
        public Mesh Mesh;
        public Vector4[] Planes = new Vector4[MaxPlanes];
        public int PlaneCount;
        public bool Invalid;        // not a convex polyhedron of flat facets (a rough lump, a crystal with too many faces): the pixel renderer draws it
    }

    private static GameObject studio;
    private static Camera camera;
    private static MeshFilter filter;
    private static MeshRenderer meshRenderer;
    private static Material material;
    private static Material domeMaterial;
    private static RenderTexture target;
    private static Texture2D readback;
    private static readonly Dictionary<int, (RenderTexture target, Texture2D readback)> Targets = new Dictionary<int, (RenderTexture, Texture2D)>();
    private static bool unavailable;
    private static readonly Dictionary<string, Prepared> Cache = new Dictionary<string, Prepared>();

    public static bool Available => !unavailable;

    private static bool Ensure(int size)
    {
        if (unavailable) return false;
        if (studio == null)
        {
            var shader = Resources.Load<Shader>("Shaders/GemRealistic");
            if (shader == null || !shader.isSupported)
            {
                unavailable = true;
                Debug.LogWarning("Pierres réalistes indisponibles : shader GemRealistic introuvable ou non pris en charge. Le rendu pixel est utilisé.");
                return false;
            }

            DestroyOldStudios();
            studio = new GameObject("GemStudio") { hideFlags = HideFlags.HideAndDontSave };
            studio.transform.position = StudioPosition;
            studio.layer = StudioLayer;
            Object.DontDestroyOnLoad(studio);

            var stone = new GameObject("Stone") { hideFlags = HideFlags.HideAndDontSave };
            stone.layer = StudioLayer;
            stone.transform.SetParent(studio.transform, false);
            filter = stone.AddComponent<MeshFilter>();
            meshRenderer = stone.AddComponent<MeshRenderer>();
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var domeShader = Resources.Load<Shader>("Shaders/GemDome");
            if (domeShader != null && domeShader.isSupported) domeMaterial = new Material(domeShader) { hideFlags = HideFlags.HideAndDontSave };
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var cameraGO = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave };
            cameraGO.layer = StudioLayer;
            cameraGO.transform.SetParent(studio.transform, false);
            cameraGO.transform.localPosition = new Vector3(0f, 0f, -CameraDistance);
            camera = cameraGO.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 1f / GemView3D.FitFraction;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = CameraDistance * 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.cullingMask = 1 << StudioLayer;
            camera.allowMSAA = true;
            camera.allowHDR = false;
        }

        // One render target and read-back texture for each size in use (the sheet, the loupe and the small gauges draw at different sizes).
        if (!Targets.TryGetValue(size, out var pair) || pair.target == null || pair.readback == null)
        {
            var created = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            created.Create();
            var read = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            pair = (created, read);
            Targets[size] = pair;
        }
        target = pair.target;
        readback = pair.readback;
        return true;
    }

    // A studio left over from before a recompile (the editor keeps hidden objects when the code is reloaded) would draw its old stone over the new one: it is removed.
    private static void DestroyOldStudios()
    {
        foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate == null || candidate.name != "GemStudio" || candidate.transform.parent != null) continue;
            if (Application.isPlaying) Object.Destroy(candidate);
            else Object.DestroyImmediate(candidate);
        }
    }

    // Draws the stone into `output` (size x size pixels, bottom row first). False when the realistic renderer cannot be used.
    public static bool TryRender(GemMesh gem, Vector3 halfExtents, Quaternion orientation, Color body, GemLook look, Diaphaneity? level, int size, Color32[] output,
        int starRays = 0, bool catsEye = false)
    {
        if (gem == null || output == null || output.Length < size * size) return false;
        if (gem.Smooth) return TryRenderDome(gem, halfExtents, orientation, body, look, level, size, output, starRays, catsEye);
        if (gem.FacetIds == null || gem.Triangles == null) return false;
        if (!Ensure(size)) return false;
        meshRenderer.sharedMaterial = material;

        var maxHalf = Mathf.Max(halfExtents.x, halfExtents.y, halfExtents.z, 0.0001f);
        var ratios = halfExtents / maxHalf;
        var prepared = Prepare(gem, ratios);
        if (prepared == null || prepared.Invalid) return false;

        filter.sharedMesh = prepared.Mesh;
        var mirrored = new Quaternion(-orientation.x, -orientation.y, orientation.z, orientation.w);
        filter.transform.localRotation = mirrored;

        var milky = 0f;
        switch (level ?? Diaphaneity.Transparent)
        {
            case Diaphaneity.SemiTransparent: milky = 0.42f; break;
            case Diaphaneity.SemiOpaque: milky = 0.75f; break;
            case Diaphaneity.Opaque: milky = 1.0f; break;      // nothing goes through: only the surface, lit and mirroring the studio
        }

        // A rough crystal: the surface of its habit (frosted or glassy, striated across or along, with bands of colour and specks).
        var surface = SurfaceOf(gem.HabitId);
        var roughness = surface.Rough * (look.RoughScale > 0f ? look.RoughScale : 1f);
        if (gem.HabitId != null)
        {
            // A rough stone is never polished and never clear: its faces are dull and the light is scattered, so it looks cloudy.
            roughness = Mathf.Max(0.55f, roughness);
            milky = Mathf.Max(milky, Mathf.Max(0.34f, surface.Milky));
        }
        material.SetFloat("_Rough", roughness);
        material.SetFloat("_Stria", surface.Stria);
        material.SetFloat("_StriaMode", surface.StriaMode);
        material.SetFloat("_Zoning", surface.Zoning);
        material.SetFloat("_Speck", surface.Speck);
        SetLook(material, look);

        material.SetColor("_Body", new Color(body.r, body.g, body.b, 1f));
        material.SetFloat("_Ior", Mathf.Max(1.2f, look.Ior));
        material.SetFloat("_Dispersion", Mathf.Max(0f, look.Dispersion));
        material.SetFloat("_Density", 0.34f);
        material.SetFloat("_Milky", milky);
        material.SetFloat("_Exposure", 1.35f);
        material.SetVectorArray("_Planes", prepared.Planes);
        material.SetInt("_PlaneCount", prepared.PlaneCount);

        camera.targetTexture = target;
        camera.Render();
        camera.targetTexture = null;

        var previous = RenderTexture.active;
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
        RenderTexture.active = previous;

        var pixels = readback.GetPixels32();
        System.Array.Copy(pixels, output, size * size);
        return true;
    }

    // A dome (cabochon): smooth normals, a star or a cat's eye worked out from the light by the shader GemLab/RealDome.
    private static bool TryRenderDome(GemMesh gem, Vector3 halfExtents, Quaternion orientation, Color body, GemLook look, Diaphaneity? level, int size, Color32[] output,
        int starRays, bool catsEye)
    {
        if (!Ensure(size) || domeMaterial == null || gem.Normals == null) return false;

        var maxHalf = Mathf.Max(halfExtents.x, halfExtents.y, halfExtents.z, 0.0001f);
        var ratios = halfExtents / maxHalf;
        var prepared = PrepareDome(gem, ratios);
        if (prepared == null) return false;

        meshRenderer.sharedMaterial = domeMaterial;
        filter.sharedMesh = prepared.Mesh;
        filter.transform.localRotation = new Quaternion(-orientation.x, -orientation.y, orientation.z, orientation.w);

        var milky = 0.1f;
        switch (level ?? Diaphaneity.Transparent)
        {
            case Diaphaneity.SemiTransparent: milky = 0.45f; break;
            case Diaphaneity.SemiOpaque: milky = 0.7f; break;
            case Diaphaneity.Opaque: milky = 0.9f; break;
        }
        if (starRays > 0 || catsEye) milky = Mathf.Max(milky, 0.5f);

        domeMaterial.SetColor("_Body", new Color(body.r, body.g, body.b, 1f));
        domeMaterial.SetFloat("_Milky", milky);
        domeMaterial.SetFloat("_Exposure", 1.3f);
        domeMaterial.SetFloat("_StarRays", starRays);
        domeMaterial.SetFloat("_StarStrength", starRays > 0 ? 1f : 0f);
        domeMaterial.SetFloat("_CatsEye", catsEye ? 1f : 0f);
        // The direction of the crystal axes in the base of the stone: always the same for a given species.
        domeMaterial.SetFloat("_EyeAxis", (look.Seed % 360) * Mathf.Deg2Rad);
        domeMaterial.SetFloat("_Ior", Mathf.Max(1.2f, look.Ior));
        SetLook(domeMaterial, look);

        camera.targetTexture = target;
        camera.Render();
        camera.targetTexture = null;

        var previous = RenderTexture.active;
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
        RenderTexture.active = previous;
        System.Array.Copy(readback.GetPixels32(), output, size * size);
        return true;
    }

    // The mesh of a dome for the studio: shared vertices, smooth normals (corrected for the proportions of the stone), mirrored along z.
    private static Prepared PrepareDome(GemMesh gem, Vector3 ratios)
    {
        var key = $"dome|{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(gem)}|{ratios.x:F2}|{ratios.y:F2}|{ratios.z:F2}";
        if (Cache.TryGetValue(key, out var known) && known.Mesh != null) return known;

        var vertices = new Vector3[gem.Vertices.Length];
        var normals = new Vector3[gem.Vertices.Length];
        var inverse = new Vector3(1f / ratios.x, 1f / ratios.y, 1f / ratios.z);
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Mirror(Vector3.Scale(gem.Vertices[i], ratios));
            normals[i] = Mirror(Vector3.Scale(gem.Normals[i], inverse)).normalized;
        }

        var triangles = new List<int>();
        foreach (var tri in gem.Triangles) { triangles.Add(tri[0]); triangles.Add(tri[1]); triangles.Add(tri[2]); }

        var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 8f);
        var prepared = new Prepared { Mesh = mesh };
        Cache[key] = prepared;
        return prepared;
    }

    private struct Surface
    {
        public float Rough, Stria, StriaMode, Zoning, Speck, Milky;
    }

    // How the faces of each habit look: quartz and tourmaline are glassy, striated across or along; a diamond octahedron is oily and frosted; a garnet is glossy; a rolled pebble is dull.
    private static Surface SurfaceOf(string habit)
    {
        if (habit == null) return default;
        if (habit.StartsWith("corundum"))
        {
            var cross = habit == "corundum_barrel" || habit == "corundum_bipyramid" || habit == "corundum_prism";
            return new Surface { Rough = 0.85f, Stria = cross ? 1f : 0f, StriaMode = 1f, Zoning = 0.85f, Speck = 0.7f, Milky = 0.18f };
        }
        if (habit.StartsWith("lump_")) return new Surface { Rough = 0.85f, Zoning = 0.3f, Speck = 0.3f, Milky = 0.18f };
        switch (habit)
        {
            case "quartz_prism": case "quartz_double": return new Surface { Rough = 0.16f, Stria = 1f, StriaMode = 1f, Zoning = 0.25f, Speck = 0.15f, Milky = 0.06f };
            case "hex_prism": case "hex_pencil": return new Surface { Rough = 0.32f, Stria = 0.8f, StriaMode = 2f, Zoning = 0.6f, Speck = 0.3f, Milky = 0.08f };
            case "hex_bipyramid": case "hex_tabular": return new Surface { Rough = 0.45f, Zoning = 0.5f, Speck = 0.3f, Milky = 0.12f };
            case "druse": return new Surface { Rough = 0.85f, Stria = 0f, StriaMode = 1f, Zoning = 0.4f, Speck = 1f, Milky = 0.30f };
            case "tourmaline_slice": return new Surface { Rough = 0.10f, Stria = 0.3f, StriaMode = 1f, Zoning = 0.9f, Speck = 0.1f, Milky = 0.03f };
            case "trigonal_prism": case "trigonal_pencil": return new Surface { Rough = 0.22f, Stria = 1f, StriaMode = 2f, Zoning = 0.7f, Speck = 0.2f, Milky = 0.05f };
            case "ortho_prism": case "ortho_wedge": return new Surface { Rough = 0.26f, Stria = 0.75f, StriaMode = 2f, Zoning = 0.3f, Speck = 0.2f, Milky = 0.05f };
            case "monoclinic_prism": return new Surface { Rough = 0.34f, Stria = 0.5f, StriaMode = 2f, Zoning = 0.3f, Speck = 0.2f, Milky = 0.08f };
            case "tetragonal_prism": return new Surface { Rough = 0.28f, Zoning = 0.3f, Speck = 0.2f, Milky = 0.06f };
            case "cleaved_octahedron": return new Surface { Rough = 0.20f, Stria = 0.6f, StriaMode = 1f, Zoning = 0.5f, Speck = 0.1f, Milky = 0.06f };
            case "octahedron": case "octahedron_trigons": case "cuboctahedron": return new Surface { Rough = 0.55f, Zoning = 0.15f, Speck = 0.1f, Milky = 0.10f };
            case "spinel_twin": case "octahedron_modified": return new Surface { Rough = 0.50f, Zoning = 0.25f, Speck = 0.15f, Milky = 0.08f };
            case "beryl_short": case "beryl_prism": case "beryl_long": return new Surface { Rough = 0.24f, Stria = 0.85f, StriaMode = 2f, Zoning = 0.5f, Speck = 0.2f, Milky = 0.06f };
            case "beryl_stubby": return new Surface { Rough = 0.30f, Stria = 0.7f, StriaMode = 2f, Zoning = 0.5f, Speck = 0.25f, Milky = 0.07f };
            case "cube": return new Surface { Rough = 0.24f, Stria = 0.8f, StriaMode = 1f, Zoning = 0.2f, Speck = 0.1f, Milky = 0.05f };
            case "rhombic_dodecahedron": case "trapezohedron": return new Surface { Rough = 0.26f, Stria = 0.3f, StriaMode = 1f, Zoning = 0.2f, Speck = 0.2f, Milky = 0.05f };
            case "rhombohedral_plate": return new Surface { Rough = 0.34f, Zoning = 0.4f, Speck = 0.3f, Milky = 0.14f };
            case "rhombohedron": return new Surface { Rough = 0.30f, Zoning = 0.3f, Speck = 0.2f, Milky = 0.10f };
        }
        return new Surface { Rough = 0.4f, Zoning = 0.2f, Speck = 0.1f, Milky = 0.08f };
    }

    // The pattern of the species (and the chrome of the metals), given to a shader.
    private static void SetLook(Material target, GemLook look)
    {
        var metal = look.Pattern == GemPattern.MetalGray || look.Pattern == GemPattern.MetalBrass;
        target.SetFloat("_Pattern", (int)look.Pattern);
        target.SetFloat("_PatStrength", Mathf.Max(0.05f, look.Strength));
        target.SetColor("_Accent", look.Accent.a > 0f ? look.Accent : new Color(look.Accent.r, look.Accent.g, look.Accent.b, 1f));
        target.SetFloat("_Metal", metal ? 1f : 0f);
        target.SetColor("_MetalTint", look.Pattern == GemPattern.MetalBrass ? new Color(1f, 0.84f, 0.40f, 1f) : new Color(0.80f, 0.82f, 0.88f, 1f));
        target.SetFloat("_Seed", (look.Seed % 100) * 0.37f);
    }

    // For the diagnostic menu: whether the realistic renderer can draw this mesh (otherwise the pixel renderer does), and how many facet planes it needs.
    public static bool CanRender(GemMesh gem, out int planeCount)
    {
        planeCount = 0;
        if (gem == null) return false;
        if (gem.Smooth) return true;
        if (gem.FacetIds == null || gem.Triangles == null) return false;
        var prepared = Prepare(gem, Vector3.one);
        planeCount = prepared != null ? prepared.PlaneCount : 0;
        return prepared != null && !prepared.Invalid;
    }

    // The mesh of the stone for the studio (mirrored along z, one flat-shaded triangle each) and its list of facet planes; kept for each mesh and proportions.
    private static Prepared Prepare(GemMesh gem, Vector3 ratios)
    {
        var key = $"{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(gem)}|{ratios.x:F2}|{ratios.y:F2}|{ratios.z:F2}";
        if (Cache.TryGetValue(key, out var known) && (known.Invalid || known.Mesh != null)) return known;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        var planeOfFacet = new Dictionary<int, int>();
        var prepared = new Prepared();
        var tooMany = false;

        for (var t = 0; t < gem.Triangles.Length; t++)
        {
            var tri = gem.Triangles[t];
            var p0 = Mirror(Vector3.Scale(gem.Vertices[tri[0]], ratios));
            var p1 = Mirror(Vector3.Scale(gem.Vertices[tri[1]], ratios));
            var p2 = Mirror(Vector3.Scale(gem.Vertices[tri[2]], ratios));
            var normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (normal.sqrMagnitude < 1e-12f) continue;
            normal.Normalize();
            if (Vector3.Dot(normal, (p0 + p1 + p2) / 3f) < 0f) normal = -normal;      // outward, whatever the winding

            var first = vertices.Count;
            vertices.Add(p0); vertices.Add(p1); vertices.Add(p2);
            normals.Add(normal); normals.Add(normal); normals.Add(normal);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);

            var id = gem.FacetIds[t];
            if (!planeOfFacet.ContainsKey(id))
            {
                if (prepared.PlaneCount >= prepared.Planes.Length) { tooMany = true; continue; }
                planeOfFacet[id] = prepared.PlaneCount;
                prepared.Planes[prepared.PlaneCount++] = new Vector4(normal.x, normal.y, normal.z, Vector3.Dot(normal, p0));
            }
            else
            {
                // The triangles of a facet should lie in one plane. A facet that is slightly bent (a kite whose corners are not quite flat) gets a plane
                // of its own for each stray triangle: a convex stone is still described exactly by the planes of its triangles.
                var plane = prepared.Planes[planeOfFacet[id]];
                var planeNormal = new Vector3(plane.x, plane.y, plane.z);
                if (Mathf.Abs(Vector3.Dot(planeNormal, p0) - plane.w) > 0.02f || Vector3.Dot(planeNormal, normal) < 0.98f)
                {
                    if (prepared.PlaneCount >= prepared.Planes.Length) { tooMany = true; continue; }
                    prepared.Planes[prepared.PlaneCount++] = new Vector4(normal.x, normal.y, normal.z, Vector3.Dot(normal, p0));
                }
            }
        }

        // The stone must be convex: no vertex beyond any of the planes. Otherwise the rays would leave through the wrong facet and the picture would be black.
        var convex = !tooMany && prepared.PlaneCount > 3;
        if (convex)
        {
            foreach (var v in vertices)
            {
                for (var p = 0; p < prepared.PlaneCount; p++)
                {
                    var plane = prepared.Planes[p];
                    if (v.x * plane.x + v.y * plane.y + v.z * plane.z > plane.w + 0.02f) { convex = false; break; }
                }
                if (!convex) break;
            }
        }
        if (!convex)
        {
            // The stone is not exactly convex (a corner of a cushion, the notch of a heart, the pits of an octahedron): the rays are traced in its convex hull,
            // which keeps the outline and the big facets and only fills the small hollows.
            var points = new List<Vector3>(gem.Vertices.Length);
            foreach (var v in gem.Vertices) points.Add(Mirror(Vector3.Scale(v, ratios)));
            vertices.Clear(); normals.Clear(); triangles.Clear();
            prepared.PlaneCount = 0;
            if (!HullInto(points, prepared, vertices, normals, triangles))
            {
                prepared.Invalid = true;
                Cache[key] = prepared;
                return prepared;
            }
        }

        var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 8f);
        prepared.Mesh = mesh;
        Cache[key] = prepared;
        return prepared;
    }

    private static Vector3 Mirror(Vector3 v) => new Vector3(v.x, v.y, -v.z);

    // ---- Convex hull -----------------------------------------------------------------------------------------------

    private struct HullFace
    {
        public int A, B, C;
        public Vector3 Normal;
        public float D;
        public bool Alive;
    }

    // Builds the convex hull of the points (incremental algorithm) into the mesh lists and the facet planes (the faces lying in one plane share it).
    private static bool HullInto(List<Vector3> raw, Prepared prepared, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        const float Eps = 1e-4f;
        var pts = new List<Vector3>();
        foreach (var point in raw)
        {
            var duplicate = false;
            foreach (var other in pts) if ((other - point).sqrMagnitude < 1e-8f) { duplicate = true; break; }
            if (!duplicate) pts.Add(point);
        }
        if (pts.Count < 4) return false;

        // First tetrahedron: the extreme points.
        var i0 = 0;
        for (var i = 1; i < pts.Count; i++) if (pts[i].x < pts[i0].x) i0 = i;
        var i1 = -1; var best = -1f;
        for (var i = 0; i < pts.Count; i++) { var dist = (pts[i] - pts[i0]).sqrMagnitude; if (dist > best) { best = dist; i1 = i; } }
        var i2 = -1; best = -1f;
        for (var i = 0; i < pts.Count; i++) { var dist = Vector3.Cross(pts[i] - pts[i0], pts[i1] - pts[i0]).sqrMagnitude; if (dist > best) { best = dist; i2 = i; } }
        if (best < 1e-10f) return false;
        var planeNormal = Vector3.Cross(pts[i1] - pts[i0], pts[i2] - pts[i0]).normalized;
        var i3 = -1; best = -1f;
        for (var i = 0; i < pts.Count; i++) { var dist = Mathf.Abs(Vector3.Dot(pts[i] - pts[i0], planeNormal)); if (dist > best) { best = dist; i3 = i; } }
        if (best < 1e-4f) return false;

        var inside = (pts[i0] + pts[i1] + pts[i2] + pts[i3]) * 0.25f;
        var faces = new List<HullFace>();
        System.Action<int, int, int> add = (a, b, c) =>
        {
            var n = Vector3.Cross(pts[b] - pts[a], pts[c] - pts[a]);
            if (n.sqrMagnitude < 1e-14f) return;
            n.Normalize();
            var d = Vector3.Dot(n, pts[a]);
            if (Vector3.Dot(n, inside) - d > 0f) { var swap = b; b = c; c = swap; n = -n; d = -d; }
            faces.Add(new HullFace { A = a, B = b, C = c, Normal = n, D = d, Alive = true });
        };
        add(i0, i1, i2); add(i0, i1, i3); add(i0, i2, i3); add(i1, i2, i3);

        for (var p = 0; p < pts.Count; p++)
        {
            if (p == i0 || p == i1 || p == i2 || p == i3) continue;
            var visible = new List<int>();
            for (var f = 0; f < faces.Count; f++)
                if (faces[f].Alive && Vector3.Dot(faces[f].Normal, pts[p]) - faces[f].D > Eps) visible.Add(f);
            if (visible.Count == 0) continue;

            // The horizon: the edges of the visible faces that no other visible face shares.
            var edges = new Dictionary<long, (int a, int b)>();
            foreach (var f in visible)
            {
                var face = faces[f];
                var ring = new[] { face.A, face.B, face.C };
                for (var e = 0; e < 3; e++)
                {
                    var a = ring[e]; var b = ring[(e + 1) % 3];
                    var reverse = (long)b * 100000L + a;
                    if (edges.ContainsKey(reverse)) edges.Remove(reverse);
                    else edges[(long)a * 100000L + b] = (a, b);
                }
                face.Alive = false;
                faces[f] = face;
            }
            foreach (var edge in edges.Values) add(edge.a, edge.b, p);
        }

        // The faces become the mesh; the ones lying in the same plane share a plane.
        foreach (var face in faces)
        {
            if (!face.Alive) continue;
            var first = vertices.Count;
            vertices.Add(pts[face.A]); vertices.Add(pts[face.B]); vertices.Add(pts[face.C]);
            normals.Add(face.Normal); normals.Add(face.Normal); normals.Add(face.Normal);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);

            var known = false;
            for (var k = 0; k < prepared.PlaneCount; k++)
            {
                var plane = prepared.Planes[k];
                if (Vector3.Dot(new Vector3(plane.x, plane.y, plane.z), face.Normal) > 0.9995f && Mathf.Abs(plane.w - face.D) < 0.01f) { known = true; break; }
            }
            if (known) continue;
            if (prepared.PlaneCount >= prepared.Planes.Length) return false;
            prepared.Planes[prepared.PlaneCount++] = new Vector4(face.Normal.x, face.Normal.y, face.Normal.z, face.D);
        }
        return prepared.PlaneCount > 3;
    }

}
