using System.Collections.Generic;
using UnityEngine;

// Procedural "3D pixel" gems: every shape (round, oval, emerald, pear...) and cut style (brilliant, step cut,
// mixed, princess, scissor, radiant, cabochon...) is drawn from real facet geometry. Each facet gets a slope and a
// direction, is lit by a light at the upper left, and the result is quantized to a few tones. A dark rim and a
// 2 px edge below and to the right give the stone its thickness.
public static class GemPixelArt
{
    public const int Size = 32;

    private const float CenterX = 15f;
    private const float CenterY = 17f;
    private const float Scale = 14f;
    private const int AngleSteps = 720;

    public static readonly Vector3 Light = new Vector3(-0.45f, 0.55f, 0.70f).normalized;

    private class Outline
    {
        public readonly float[] Radius = new float[AngleSteps];
        public readonly float[] NormalAngle = new float[AngleSteps];
    }

    private static readonly Dictionary<string, Outline> Outlines = new Dictionary<string, Outline>();

    // ---- Public API ------------------------------------------------------------------------------------------

    public static Texture2D RenderGem(string shape, string style, Color body, Color facet)
    {
        var outline = GetOutline(shape);
        var count = Size * Size;
        var inside = new bool[count];
        var tone = new float[count];
        var ids = new int[count];

        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var u = (px + 0.5f - CenterX) / Scale;
                var v = (py + 0.5f - CenterY) / Scale;
                if (!Inside(shape, u, v)) continue;

                var index = py * Size + px;
                inside[index] = true;

                var r = Mathf.Sqrt(u * u + v * v);
                var theta = Mathf.Atan2(v, u);
                var boundary = Mathf.Max(0.05f, BoundaryAt(outline, theta));
                var rho = Mathf.Clamp01(r / boundary);

                Facet(style, u, v, theta, rho, outline, out var intensity, out var id);
                tone[index] = Mathf.Clamp01((intensity + 0.2f) / 1.15f);
                ids[index] = id;
            }
        }

        // Light lines along facet edges.
        var edge = new float[count];
        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var i = py * Size + px;
                if (!inside[i]) continue;
                var leftDiffers = px > 0 && inside[i - 1] && ids[i - 1] != ids[i];
                var belowDiffers = py > 0 && inside[i - Size] && ids[i - Size] != ids[i];
                // Step-cut style facets catch light along their edges, radial (brilliant) facets get a dark crease.
                if (leftDiffers || belowDiffers) edge[i] = ids[i] >= 400 ? 0.12f : -0.16f;
            }
        }

        var pixels = new Color[count];
        for (var i = 0; i < count; i++) pixels[i] = Color.clear;

        // Thickness: the stone's silhouette shifted down and to the right, darker.
        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var i = py * Size + px;
                if (inside[i]) continue;
                var near = Covered(inside, px - 1, py + 1);
                var far = Covered(inside, px - 2, py + 2);
                if (near) pixels[i] = Dim(body, 0.34f);
                else if (far) pixels[i] = Dim(body, 0.22f);
            }
        }

        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var i = py * Size + px;
                if (!inside[i]) continue;

                var isRim = !Covered(inside, px - 1, py) || !Covered(inside, px + 1, py) || !Covered(inside, px, py - 1) || !Covered(inside, px, py + 1);
                pixels[i] = isRim
                    ? Dim(body, px < CenterX && py > CenterY ? 0.55f : 0.20f)      // lit rim at the upper left
                    : Palette(Mathf.Clamp01(tone[i] + edge[i]), body, facet);
            }
        }

        // A couple of sparkles on the table / crown.
        if (!IsCabochon(style)) AddSparkle(pixels, inside, -0.26f, 0.30f);
        AddSparkle(pixels, inside, IsCabochon(style) ? -0.34f : 0.28f, IsCabochon(style) ? 0.36f : -0.20f);

        return ToTexture(pixels);
    }

    // A rough crystal / pebble: lumpy outline, flat angular planes, speckles.
    public static Texture2D RenderRough(Color body, int seed)
    {
        var rng = new System.Random(seed);
        var count = Size * Size;
        var inside = new bool[count];
        var tone = new float[count];
        var ids = new int[count];

        var a = (float)rng.NextDouble() * 6.28f;
        var b = (float)rng.NextDouble() * 6.28f;
        var c = (float)rng.NextDouble() * 6.28f;

        var seeds = new Vector2[8];
        var normals = new Vector2[8];
        for (var i = 0; i < seeds.Length; i++)
        {
            seeds[i] = new Vector2((float)rng.NextDouble() * 1.4f - 0.7f, (float)rng.NextDouble() * 1.4f - 0.7f);
            normals[i] = new Vector2((float)rng.NextDouble() * 1.6f - 0.8f, (float)rng.NextDouble() * 1.6f - 0.8f);
        }

        var dull = Color.Lerp(body, new Color(0.5f, 0.5f, 0.5f), 0.35f) * 0.9f;
        dull.a = 1f;

        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var u = (px + 0.5f - CenterX) / Scale;
                var v = (py + 0.5f - CenterY) / Scale;
                var r = Mathf.Sqrt(u * u + v * v);
                var theta = Mathf.Atan2(v, u);
                var radius = 0.74f + 0.12f * Mathf.Sin(2f * theta + a) + 0.08f * Mathf.Sin(3f * theta + b) + 0.06f * Mathf.Sin(5f * theta + c);
                if (r > radius) continue;

                var index = py * Size + px;
                inside[index] = true;

                var nearest = 0;
                var best = float.MaxValue;
                for (var i = 0; i < seeds.Length; i++)
                {
                    var d = (new Vector2(u, v) - seeds[i]).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        nearest = i;
                    }
                }

                var n = new Vector3(normals[nearest].x * 0.8f + u * 0.5f, normals[nearest].y * 0.8f + v * 0.5f, 1f).normalized;
                var intensity = Vector3.Dot(n, Light);
                tone[index] = Mathf.Clamp01((intensity + 0.1f) / 1.05f);
                ids[index] = nearest + 1;
            }
        }

        var pixels = new Color[count];
        for (var i = 0; i < count; i++) pixels[i] = Color.clear;

        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var i = py * Size + px;
                if (inside[i]) continue;
                if (Covered(inside, px - 1, py + 1)) pixels[i] = Dim(dull, 0.34f);
                else if (Covered(inside, px - 2, py + 2)) pixels[i] = Dim(dull, 0.22f);
            }
        }

        for (var py = 0; py < Size; py++)
        {
            for (var px = 0; px < Size; px++)
            {
                var i = py * Size + px;
                if (!inside[i]) continue;

                var isRim = !Covered(inside, px - 1, py) || !Covered(inside, px + 1, py) || !Covered(inside, px, py - 1) || !Covered(inside, px, py + 1);
                if (isRim)
                {
                    pixels[i] = Dim(dull, 0.22f);
                    continue;
                }

                var leftDiffers = px > 0 && inside[i - 1] && ids[i - 1] != ids[i];
                var edgeBoost = leftDiffers ? 0.10f : 0f;
                var speckle = rng.NextDouble() < 0.07 ? -0.18f : 0f;
                pixels[i] = Palette(Mathf.Clamp01(tone[i] + edgeBoost + speckle), dull, Color.Lerp(dull, Color.white, 0.25f), false);
            }
        }

        return ToTexture(pixels);
    }

    // ---- Facet geometry --------------------------------------------------------------------------------------

    private static bool IsCabochon(string style)
    {
        return style == "Cabochon";
    }

    private static float Intensity(float slope, float direction)
    {
        var normal = new Vector3(Mathf.Cos(direction) * Mathf.Sin(slope), Mathf.Sin(direction) * Mathf.Sin(slope), Mathf.Cos(slope));
        return Vector3.Dot(normal, Light);
    }

    // Quantizes an angle to the middle of one of `sectors` equal sectors; returns the sector center and its index.
    private static float Sector(float angle, int sectors, out int index)
    {
        var step = 2f * Mathf.PI / sectors;
        var normalized = Mathf.Repeat(angle + step * 0.5f, 2f * Mathf.PI);
        index = Mathf.Min(sectors - 1, (int)(normalized / step));
        return index * step;
    }

    private static void Facet(string style, float u, float v, float theta, float rho, Outline outline,
        out float intensity, out int id)
    {
        switch (style)
        {
            case "Cabochon": Cabochon(rho, theta, out intensity, out id); return;
            case "Princesse": Princess(u, v, outline, out intensity, out id); return;
            case "Ciseaux": Scissor(u, v, rho, theta, out intensity, out id); return;
            case "À degrés":
            case "Émeraude":
            case "Émeraude (à degrés)":
                StepCut(theta, rho, outline, new[] { 0.42f, 0.58f, 0.74f, 0.88f }, out intensity, out id); return;
            case "Ascher":
                StepCut(theta, rho, outline, new[] { 0.30f, 0.43f, 0.55f, 0.67f, 0.78f, 0.89f }, out intensity, out id); return;
            case "Baguette":
                StepCut(theta, rho, outline, new[] { 0.45f, 0.65f, 0.85f }, out intensity, out id); return;
            case "Mixte": Mixed(theta, rho, outline, out intensity, out id); return;
            case "Radiant": Radiant(theta, rho, outline, out intensity, out id); return;
            default: Brilliant(theta, rho, out intensity, out id); return;
        }
    }

    // Round-brilliant style: octagonal table, star facets, kite/crown facets, girdle.
    private static void Brilliant(float theta, float rho, out float intensity, out int id)
    {
        if (rho < 0.44f)
        {
            // Table: flat, with a soft gradient and a faint octagon edge.
            intensity = Light.z;
            id = 1;
            return;
        }

        if (rho < 0.62f)
        {
            var direction = Sector(theta, 16, out var sector);
            intensity = Intensity(0.34f, direction);
            id = 100 + sector;
            return;
        }

        if (rho < 0.87f)
        {
            var direction = Sector(theta, 16, out var sector);
            // Main facets alternate between a steeper and a flatter slope.
            intensity = Intensity(sector % 2 == 0 ? 0.80f : 0.40f, direction);
            id = 200 + sector;
            return;
        }

        var girdleDirection = Sector(theta, 16, out var girdleSector);
        intensity = Intensity(0.85f, girdleDirection);
        id = 300 + girdleSector;
    }

    // Step cut: parallel rings (offset copies of the outline) that get steeper toward the edge.
    private static void StepCut(float theta, float rho, Outline outline, float[] rings, out float intensity, out int id)
    {
        var normalAngle = NormalAt(outline, theta);
        var direction = Sector(normalAngle, 24, out var sector);

        var ring = 0;
        while (ring < rings.Length && rho >= rings[ring]) ring++;

        if (ring == 0)
        {
            intensity = Light.z;
            id = 1;
            return;
        }

        var slope = 0.22f + 0.65f * ring / rings.Length;
        intensity = Intensity(slope, direction);
        id = 400 + ring * 100 + sector;
    }

    private static void Mixed(float theta, float rho, Outline outline, out float intensity, out int id)
    {
        if (rho < 0.40f)
        {
            intensity = Light.z;
            id = 1;
            return;
        }

        if (rho < 0.64f)
        {
            var direction = Sector(theta, 16, out var sector);
            intensity = Intensity(0.38f, direction);
            id = 100 + sector;
            return;
        }

        var normalAngle = NormalAt(outline, theta);
        var stepDirection = Sector(normalAngle, 24, out var stepSector);
        var ring = rho < 0.78f ? 1 : rho < 0.90f ? 2 : 3;
        intensity = Intensity(0.46f + 0.18f * ring, stepDirection);
        id = 500 + ring * 100 + stepSector;
    }

    private static void Radiant(float theta, float rho, Outline outline, out float intensity, out int id)
    {
        if (rho < 0.40f)
        {
            intensity = Light.z;
            id = 1;
            return;
        }

        if (rho < 0.70f)
        {
            var direction = Sector(theta, 24, out var sector);
            intensity = Intensity(sector % 2 == 0 ? 0.42f : 0.36f, direction);
            id = 100 + sector;
            return;
        }

        var normalAngle = NormalAt(outline, theta);
        var stepDirection = Sector(normalAngle, 24, out var stepSector);
        var ring = rho < 0.86f ? 1 : 2;
        intensity = Intensity(0.58f + 0.2f * ring, stepDirection);
        id = 500 + ring * 100 + stepSector;
    }

    // Princess cut: four pyramid facets from the corners, chevron bands, small square table.
    private static void Princess(float u, float v, Outline outline, out float intensity, out int id)
    {
        // Work in the outline's own proportions so a rectangle behaves like a square.
        var hx = Mathf.Max(0.2f, BoundaryAt(outline, 0f));
        var hy = Mathf.Max(0.2f, BoundaryAt(outline, Mathf.PI / 2f));
        var qx = u / hx;
        var qy = v / hy;
        var cheb = Mathf.Max(Mathf.Abs(qx), Mathf.Abs(qy));

        if (cheb < 0.34f)
        {
            intensity = Light.z;
            id = 1;
            return;
        }

        var horizontal = Mathf.Abs(qx) > Mathf.Abs(qy);
        var direction = horizontal ? (qx > 0f ? 0f : Mathf.PI) : (qy > 0f ? Mathf.PI / 2f : -Mathf.PI / 2f);
        var band = Mathf.Min(4, (int)((cheb - 0.34f) / 0.14f));
        var slope = 0.40f + 0.10f * (band % 2) + 0.06f * band;
        intensity = Intensity(slope, direction);
        id = 700 + band * 10 + (horizontal ? (qx > 0f ? 0 : 1) : (qy > 0f ? 2 : 3));
    }

    // Scissor cut: crossing diagonal bands.
    private static void Scissor(float u, float v, float rho, float theta, out float intensity, out int id)
    {
        if (rho < 0.28f)
        {
            intensity = Light.z;
            id = 1;
            return;
        }

        if (rho > 0.90f)
        {
            var d = Sector(theta, 16, out var s);
            intensity = Intensity(0.85f, d);
            id = 900 + s;
            return;
        }

        var a = Mathf.FloorToInt((u + v) * 2.0f);
        var b = Mathf.FloorToInt((u - v) * 2.0f);
        var parity = ((a & 1) << 1) | (b & 1);
        // Four band types, each tilted toward a diagonal.
        var direction = (parity * 90f + 45f) * Mathf.Deg2Rad;
        intensity = Intensity(0.46f, direction);
        id = 800 + parity * 100 + ((a + b) & 7);
    }

    // Cabochon: smooth dome lit with a specular highlight.
    private static void Cabochon(float rho, float theta, out float intensity, out int id)
    {
        var edge = Mathf.Min(rho, 0.97f);
        var height = Mathf.Sqrt(1f - edge * edge);
        var slopeX = Mathf.Cos(theta) * edge * 0.95f;
        var slopeY = Mathf.Sin(theta) * edge * 0.95f;
        var normal = new Vector3(slopeX, slopeY, Mathf.Max(0.15f, height)).normalized;

        var diffuse = Vector3.Dot(normal, Light);
        var half = (Light + Vector3.forward).normalized;
        var specular = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(normal, half)), 40f);

        intensity = diffuse * 0.85f + specular * 0.7f - (rho > 0.9f ? 0.15f : 0f);
        // Quantize to flat bands: id changes with the band so facet-edge lines stay off a smooth surface.
        var band = Mathf.Clamp(Mathf.FloorToInt((intensity + 0.2f) / 1.15f * 7f), 0, 7);
        id = 1000 + band;
    }

    // ---- Palette ---------------------------------------------------------------------------------------------

    public static Color Palette(float tone, Color body, Color facet, bool bright = true)
    {
        var level = Mathf.Clamp(Mathf.FloorToInt(tone * 6f), 0, 5);
        Color color;
        switch (level)
        {
            case 0: color = Dim(body, 0.30f); break;
            case 1: color = Dim(body, 0.50f); break;
            case 2: color = Dim(body, 0.74f); break;
            case 3: color = body; break;
            case 4: color = Color.Lerp(body, facet, 0.65f); break;
            default: color = Color.Lerp(facet, Color.white, bright ? 0.55f : 0.25f); break;
        }
        color.a = 1f;
        return color;
    }

    public static Color Dim(Color c, float factor)
    {
        return new Color(c.r * factor, c.g * factor, c.b * factor, 1f);
    }

    private static void AddSparkle(Color[] pixels, bool[] inside, float u, float v)
    {
        var px = Mathf.RoundToInt(u * Scale + CenterX - 0.5f);
        var py = Mathf.RoundToInt(v * Scale + CenterY - 0.5f);
        if (!Covered(inside, px, py)) return;

        pixels[py * Size + px] = Color.white;
        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            var nx = px + dx;
            var ny = py + dy;
            if (!Covered(inside, nx, ny)) continue;
            var existing = pixels[ny * Size + nx];
            pixels[ny * Size + nx] = Color.Lerp(existing, Color.white, 0.35f);
        }
    }

    private static bool Covered(bool[] inside, int x, int y)
    {
        return x >= 0 && y >= 0 && x < Size && y < Size && inside[y * Size + x];
    }

    private static Texture2D ToTexture(Color[] pixels)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // ---- Outlines --------------------------------------------------------------------------------------------

    // Distance from the center to the outline of a shape along `theta`, in the unit coordinates of the shape.
    public static float BoundaryRadius(string shape, float theta)
    {
        return BoundaryAt(GetOutline(shape), theta);
    }

    private static Outline GetOutline(string shape)
    {
        var key = shape ?? "";
        if (Outlines.TryGetValue(key, out var cached)) return cached;

        var outline = new Outline();
        for (var i = 0; i < AngleSteps; i++)
        {
            var theta = i * 2f * Mathf.PI / AngleSteps;
            var dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            var r = 0f;
            while (r < 1.3f && Inside(shape, dir.x * (r + 0.004f), dir.y * (r + 0.004f))) r += 0.004f;
            outline.Radius[i] = r;
        }

        for (var i = 0; i < AngleSteps; i++)
        {
            var prev = (i + AngleSteps - 2) % AngleSteps;
            var next = (i + 2) % AngleSteps;
            var p0 = Polar(outline.Radius[prev], prev);
            var p1 = Polar(outline.Radius[next], next);
            var tangent = p1 - p0;
            outline.NormalAngle[i] = Mathf.Atan2(-tangent.x, tangent.y);      // outward normal for a counter-clockwise outline
        }

        Outlines[key] = outline;
        return outline;
    }

    private static Vector2 Polar(float radius, int step)
    {
        var theta = step * 2f * Mathf.PI / AngleSteps;
        return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * radius;
    }

    private static float BoundaryAt(Outline outline, float theta)
    {
        var t = Mathf.Repeat(theta, 2f * Mathf.PI) / (2f * Mathf.PI) * AngleSteps;
        var i0 = (int)t % AngleSteps;
        var i1 = (i0 + 1) % AngleSteps;
        return Mathf.Lerp(outline.Radius[i0], outline.Radius[i1], t - Mathf.Floor(t));
    }

    private static float NormalAt(Outline outline, float theta)
    {
        var t = Mathf.Repeat(theta, 2f * Mathf.PI) / (2f * Mathf.PI) * AngleSteps;
        return outline.NormalAngle[(int)Mathf.Round(t) % AngleSteps];
    }

    // Shape tests in unit coordinates, long axis along +x.
    private static bool Inside(string shape, float u, float v)
    {
        var au = Mathf.Abs(u);
        var av = Mathf.Abs(v);
        switch (shape)
        {
            case "Ovale":
                return (u / 0.98f) * (u / 0.98f) + (v / 0.72f) * (v / 0.72f) <= 1f;
            case "Coussin":
                return Mathf.Pow(au / 0.90f, 3.2f) + Mathf.Pow(av / 0.84f, 3.2f) <= 1f;
            case "Carré":
                return Chamfered(au, av, 0.84f, 0.84f, 0.14f);
            case "Rectangle":
                return Chamfered(au, av, 0.98f, 0.66f, 0.10f);
            case "Rectangle à pans coupés":
                return Chamfered(au, av, 0.98f, 0.70f, 0.32f);
            case "Octogone":
                return Chamfered(au, av, 0.88f, 0.88f, 0.40f);
            case "Baguette":
                return Chamfered(au, av, 1.00f, 0.40f, 0.07f);
            case "Poire":
            {
                if (u < -0.30f) return (u + 0.30f) * (u + 0.30f) + v * v <= 0.62f * 0.62f;
                if (u > 0.98f) return false;
                var width = 0.62f * Mathf.Pow(1f - (u + 0.30f) / 1.28f, 1.3f);
                return av <= width;
            }
            case "Marquise":
            {
                if (au > 0.98f) return false;
                return av <= 0.50f * Mathf.Pow(1f - (u / 0.98f) * (u / 0.98f), 0.8f);
            }
            case "Shield":
            {
                if (u < -0.82f || u > 0.98f) return false;
                if (u < 0.10f) return av <= 0.74f && (u + 0.82f) + (0.74f - av) >= 0.16f;
                return av <= 0.74f * Mathf.Pow(1f - (u - 0.10f) / 0.88f, 0.9f);
            }
            case "Triangle":
            {
                // Triangle pointing right, corners slightly rounded by shrinking the half-planes.
                var a = new Vector2(1.12f, 0f);
                var b = new Vector2(-0.72f, 0.96f);
                var c = new Vector2(-0.72f, -0.96f);
                var p = new Vector2(u, v);
                var d = Mathf.Max(EdgeDistance(p, a, b), EdgeDistance(p, b, c), EdgeDistance(p, c, a));
                return d <= -0.13f;
            }
            case "Cœur":
            {
                var x = v * 1.35f;
                var y = -(u - 0.12f) * 1.2f;
                var q = x * x + y * y - 1f;
                return q * q * q - x * x * y * y * y <= 0f;
            }
            default:
                return u * u + v * v <= 0.92f * 0.92f;
        }
    }

    private static bool Chamfered(float au, float av, float halfX, float halfY, float chamfer)
    {
        return au <= halfX && av <= halfY && (halfX - au) + (halfY - av) >= chamfer;
    }

    private static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var edge = b - a;
        var normal = new Vector2(edge.y, -edge.x).normalized;     // outward for a counter-clockwise triangle
        return Vector2.Dot(p - a, normal);
    }
}
