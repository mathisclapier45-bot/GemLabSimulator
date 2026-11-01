using System.Collections.Generic;
using UnityEngine;

public enum CrystalVariant { Generic, Apatite, Zircon, Rutile, Sulfide, Mica, Octahedron, Calcite, Garnet, Chromite }

// One drawing element of an inclusion, in the inclusion's own frame (u along its direction, v across), in mm.
public class InclusionPart
{
    public enum Shape { Line, Dot, Ring, Polygon, Band }

    public Shape Type;
    public Vector2 A;
    public Vector2 B;
    public float Radius;
    public float Width;
    public Vector2[] Points;
    public bool Dark;                // drawn dark in reflected light (pits, chips)
    public bool HasColor;            // its own color (a mineral's, a liquid's...), blended lightly with the host's
    public Color Tint = Color.white;
    public float Fill = -1f;         // polygon fill strength, < 0 = default
    public int Iridescence;          // 1 = interference colors of a thin film, 2 = the blue/orange flash of a filled fracture
    public float Strength = 1f;
}

// What every kind of inclusion looks like, drawn from the real shapes of the Photo Atlas of Inclusions in Gemstones:
// one-, two- and three-phase inclusions, silk, negative crystals, feathers, fingerprints, growth marks, horsetails...
// The same parts feed the loupe and the microscope.
public static class InclusionShapes
{
    private static readonly Color PaleWhite = new Color(0.92f, 0.93f, 0.90f);
    private static readonly Color Salt = new Color(0.97f, 0.98f, 1f);
    private static readonly Color Gold = new Color(0.90f, 0.76f, 0.38f);
    private static readonly Color Brass = new Color(0.86f, 0.74f, 0.34f);
    private static readonly Color Brown = new Color(0.58f, 0.40f, 0.24f);
    private static readonly Color Silver = new Color(0.80f, 0.82f, 0.87f);
    private static readonly Color Rose = new Color(0.88f, 0.52f, 0.58f);
    private static readonly Color Black = new Color(0.04f, 0.04f, 0.05f);
    private static readonly Color Liquid = new Color(0.78f, 0.90f, 1f);
    private static readonly Color Green = new Color(0.50f, 0.62f, 0.48f);

    public static List<InclusionPart> Build(LoupeFeature f, System.Random rng)
    {
        var parts = new List<InclusionPart>();
        switch (f.Kind)
        {
            case FeatureKind.Crystal: BuildCrystal(parts, f, rng); break;
            case FeatureKind.Bubble: BuildBubble(parts, f, rng); break;
            case FeatureKind.Fluid: BuildLiquid(parts, f, rng); break;
            case FeatureKind.TwoPhase: BuildPhases(parts, f, rng, false); break;
            case FeatureKind.ThreePhase: BuildPhases(parts, f, rng, true); break;
            case FeatureKind.NegativeCrystal: BuildNegative(parts, f, rng); break;
            case FeatureKind.Needle: BuildNeedles(parts, f, rng); break;
            case FeatureKind.HorseTail: BuildHorseTail(parts, f, rng); break;
            case FeatureKind.Fracture: BuildFracture(parts, f, rng); break;
            case FeatureKind.Cleavage: BuildCleavage(parts, f, rng); break;
            case FeatureKind.Feather: BuildFeather(parts, f, rng); break;
            case FeatureKind.Healing: BuildHealing(parts, f, rng); break;
            case FeatureKind.Zoning: BuildZoning(parts, f, rng); break;
            case FeatureKind.Growth: BuildGrowth(parts, f, rng); break;
            case FeatureKind.Striae: BuildStriae(parts, f, rng); break;
            case FeatureKind.Twinning: BuildTwinning(parts, f, rng); break;
            case FeatureKind.Cloud: BuildCloud(parts, f, rng); break;
            case FeatureKind.Scratch:
            {
                var length = 0.3f + 0.8f * f.Size;
                parts.Add(Line(new Vector2(-length / 2f, 0f), new Vector2(length / 2f, R(rng, -0.03f, 0.03f)), 0.006f));
                break;
            }
            case FeatureKind.Chip:
            {
                var s = 0.1f + 0.15f * f.Size;
                parts.Add(new InclusionPart
                {
                    Type = InclusionPart.Shape.Polygon,
                    Points = new[] { new Vector2(-s, 0f), new Vector2(s, 0.3f * s), new Vector2(0f, -s) },
                    Dark = true,
                });
                break;
            }
            case FeatureKind.Pit:
                parts.Add(new InclusionPart { Type = InclusionPart.Shape.Dot, A = Vector2.zero, Radius = 0.02f + 0.03f * f.Size, Dark = true });
                break;
            default:    // Abrasion
                parts.Add(new InclusionPart { Type = InclusionPart.Shape.Band, A = new Vector2(-0.15f, 0f), B = new Vector2(0.15f, 0f), Width = 0.14f });
                break;
        }
        return parts;
    }

    // ---- Names -------------------------------------------------------------------------------------------------

    public static string Label(FeatureKind kind, int variant)
    {
        switch (kind)
        {
            case FeatureKind.Crystal:
                switch ((CrystalVariant)variant)
                {
                    case CrystalVariant.Apatite: return "cristaux d'apatite";
                    case CrystalVariant.Zircon: return "cristaux de zircon à halo de tension";
                    case CrystalVariant.Rutile: return "cristaux de rutile";
                    case CrystalVariant.Sulfide: return "cristaux de sulfure (pyrite, pyrrhotite)";
                    case CrystalVariant.Mica: return "paillettes de mica";
                    case CrystalVariant.Octahedron: return "cristaux octaédriques";
                    case CrystalVariant.Calcite: return "cristaux de calcite";
                    case CrystalVariant.Garnet: return "cristaux arrondis de grenat";
                    case CrystalVariant.Chromite: return "cristaux de chromite en « feuille de nénuphar »";
                    default: return "cristaux inclus (monophasés)";
                }
            case FeatureKind.Bubble: return variant == 1 ? "bulles de gaz en virgule" : variant == 2 ? "amas de bulles de gaz" : "bulles de gaz (monophasées)";
            case FeatureKind.Fluid: return "cavités remplies de liquide (monophasées)";
            case FeatureKind.TwoPhase: return "inclusions biphasées (liquide + gaz)";
            case FeatureKind.ThreePhase: return "inclusions triphasées (cristal + liquide + gaz)";
            case FeatureKind.NegativeCrystal: return "cristaux négatifs";
            case FeatureKind.Needle:
                return variant == 1 ? "fibres parallèles" : variant == 2 ? "tubes parallèles" : variant == 3 ? "aiguilles de trémolite" : "soie de rutile (aiguilles à 60°)";
            case FeatureKind.HorseTail: return "« queue de cheval » (fibres de chrysotile)";
            case FeatureKind.Fracture: return variant == 1 ? "fractures comblées" : "fractures";
            case FeatureKind.Cleavage: return "clivages";
            case FeatureKind.Feather: return "plumes (fissures cicatrisées)";
            case FeatureKind.Healing: return variant == 1 ? "voiles de fines gouttelettes" : variant == 2 ? "résidus de flux" : "empreintes digitales";
            case FeatureKind.Zoning: return variant == 1 ? "zonation hexagonale" : variant == 2 ? "zonation en secteurs" : "zonation de couleur";
            case FeatureKind.Growth: return "marques de croissance";
            case FeatureKind.Striae: return "stries de croissance courbes";
            case FeatureKind.Twinning: return "macles polysynthétiques";
            case FeatureKind.Cloud: return "nuages de fines particules";
            default: return kind.ToString();
        }
    }

    // ---- Solid crystals ----------------------------------------------------------------------------------------

    private static void BuildCrystal(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var length = Mathf.Lerp(0.10f, 0.55f, f.Size);
        var variant = (CrystalVariant)f.Variant;
        var halo = f.Heated;

        switch (variant)
        {
            case CrystalVariant.Apatite:
            {
                var w = length * 0.5f;
                parts.Add(Tinted(Poly(Prism(length, w, 0.28f), 0.4f), PaleWhite));
                parts.Add(Tinted(Line(new Vector2(-length * 0.4f, 0f), new Vector2(length * 0.4f, 0f), 0.004f, 0.5f), PaleWhite));
                break;
            }
            case CrystalVariant.Zircon:
            {
                var l = length * 0.7f;
                parts.Add(Tinted(Poly(Prism(l, l * 0.7f, 0.3f), 0.8f), Brown));
                AddTensionHalo(parts, l, rng);
                halo = false;
                break;
            }
            case CrystalVariant.Rutile:
            {
                var l = length * 1.4f;
                var w = l * 0.2f;
                parts.Add(Tinted(Poly(Prism(l, w, 0.18f), 0.85f), Gold));
                parts.Add(Tinted(Line(new Vector2(-l * 0.4f, w * 0.15f), new Vector2(l * 0.4f, w * 0.15f), 0.003f, 0.6f), Brown));
                parts.Add(Tinted(Line(new Vector2(-l * 0.3f, -w * 0.2f), new Vector2(l * 0.35f, -w * 0.2f), 0.003f, 0.5f), Brown));
                break;
            }
            case CrystalVariant.Sulfide:
            {
                var s = length * 0.5f;
                parts.Add(Tinted(Poly(Square(Vector2.zero, s, R(rng, 5f, 35f)), 0.9f), Brass));
                parts.Add(Tinted(Line(new Vector2(-s * 0.4f, -s * 0.1f), new Vector2(s * 0.4f, s * 0.1f), 0.004f, 0.7f), Brown));
                parts.Add(Tinted(Line(new Vector2(-s * 0.3f, s * 0.25f), new Vector2(s * 0.3f, s * 0.3f), 0.004f, 0.7f), Brown));
                break;
            }
            case CrystalVariant.Mica:
            {
                var plates = 2 + rng.Next(3);
                for (var i = 0; i < plates; i++)
                {
                    var center = new Vector2(R(rng, -0.12f, 0.12f), R(rng, -0.08f, 0.08f)) * (0.5f + f.Size);
                    var r = length * R(rng, 0.3f, 0.55f);
                    var pts = new Vector2[6];
                    var turn = R(rng, 0f, 60f);
                    for (var k = 0; k < 6; k++)
                    {
                        var a = (turn + 60f * k) * Mathf.Deg2Rad;
                        pts[k] = center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.5f);
                    }
                    parts.Add(Tinted(Poly(pts, 0.55f, 0.9f), Silver));
                }
                break;
            }
            case CrystalVariant.Octahedron:
            {
                var r = length * 0.5f;
                if (rng.NextDouble() < 0.5)
                {
                    parts.Add(Tinted(Poly(new[] { new Vector2(0f, r), new Vector2(-r * 0.87f, -r * 0.5f), new Vector2(r * 0.87f, -r * 0.5f) }, 0.3f), PaleWhite));
                    parts.Add(Tinted(Poly(new[] { new Vector2(0f, r * 0.45f), new Vector2(-r * 0.4f, -r * 0.22f), new Vector2(r * 0.4f, -r * 0.22f) }, 0f, 0.55f), PaleWhite));
                }
                else
                {
                    parts.Add(Tinted(Poly(new[] { new Vector2(0f, r), new Vector2(-r * 0.75f, 0f), new Vector2(0f, -r), new Vector2(r * 0.75f, 0f) }, 0.3f), PaleWhite));
                    parts.Add(Tinted(Line(new Vector2(0f, r), new Vector2(0f, -r), 0.004f, 0.5f), PaleWhite));
                    parts.Add(Tinted(Line(new Vector2(-r * 0.75f, 0f), new Vector2(r * 0.75f, 0f), 0.004f, 0.5f), PaleWhite));
                }
                break;
            }
            case CrystalVariant.Calcite:
            {
                var r = length * 0.55f;
                parts.Add(Tinted(Poly(new[] { new Vector2(-r, -r * 0.3f), new Vector2(-r * 0.3f, r * 0.55f), new Vector2(r, r * 0.3f), new Vector2(r * 0.3f, -r * 0.55f) }, 0.35f), Salt));
                parts.Add(Tinted(Line(new Vector2(-r * 0.6f, 0f), new Vector2(r * 0.6f, 0f), 0.004f, 0.5f), Salt));
                break;
            }
            case CrystalVariant.Garnet:
            {
                var r = length * 0.38f;
                parts.Add(Tinted(Poly(Ellipse(r, r * R(rng, 0.85f, 1f), 10, rng, 0.08f), 0.6f), Rose));
                parts.Add(Tinted(Dot(new Vector2(-r * 0.3f, r * 0.3f), r * 0.2f, 0.8f), Salt));
                break;
            }
            case CrystalVariant.Chromite:
            {
                // A tiny dark octahedron and the flat disc fracture around it: the "lily pad" of peridot.
                var r = Mathf.Max(0.03f, length * 0.2f);
                parts.Add(Tinted(Poly(new[] { new Vector2(0f, r), new Vector2(-r, 0f), new Vector2(0f, -r), new Vector2(r, 0f) }, 1f), Black));
                var a = length * 1.7f;
                var b = length * 0.85f;
                parts.Add(Poly(Ellipse(a, b, 18, rng, 0.06f), 0.1f, 0.8f));
                parts.Add(Poly(Ellipse(a * 0.62f, b * 0.62f, 14, rng, 0.05f), 0f, 0.55f));
                for (var k = 0; k < 9; k++)
                {
                    var angle = k * 40f * Mathf.Deg2Rad + R(rng, -0.1f, 0.1f);
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    parts.Add(Line(new Vector2(dir.x * a * 0.3f, dir.y * b * 0.3f), new Vector2(dir.x * a * 0.85f, dir.y * b * 0.85f), 0.003f, 0.45f));
                }
                break;
            }
            default:
            {
                var width = length * R(rng, 0.45f, 0.7f);
                parts.Add(Tinted(Poly(new[]
                {
                    new Vector2(length / 2f, 0f), new Vector2(length / 4f, width / 2f), new Vector2(-length / 4f, width / 2f),
                    new Vector2(-length / 2f, 0f), new Vector2(-length / 4f, -width / 2f), new Vector2(length / 4f, -width / 2f),
                }, 0.45f), PaleWhite));
                break;
            }
        }

        // Heating leaves a discoid fracture ("halo") around the crystals.
        if (halo) AddTensionHalo(parts, length * 0.8f, rng);
    }

    private static void AddTensionHalo(List<InclusionPart> parts, float radius, System.Random rng)
    {
        parts.Add(Ring(Vector2.zero, radius * 1.15f, 0.006f, 0.8f));
        parts.Add(Ring(Vector2.zero, radius * 1.6f, 0.005f, 0.45f));
        for (var k = 0; k < 7; k++)
        {
            var angle = (k * 51f + R(rng, -12f, 12f)) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            parts.Add(Line(dir * radius * 0.7f, dir * radius * R(rng, 1.3f, 1.7f), 0.003f, 0.5f));
        }
    }

    // ---- Gas, liquid and multi-phase cavities ------------------------------------------------------------------

    private static void BuildBubble(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var radius = Mathf.Lerp(0.03f, 0.15f, f.Size);
        switch (f.Variant)
        {
            case 1:
            {
                // Comet: a round head and a tapering tail, typical of flame-fusion synthetics.
                var tail = radius * R(rng, 3.5f, 6f);
                parts.Add(Tinted(Poly(new[] { new Vector2(radius * 0.6f, radius * 0.7f), new Vector2(radius * 0.6f + tail, 0f), new Vector2(radius * 0.6f, -radius * 0.7f) }, 0.4f, 0.8f), Salt));
                AddBubble(parts, Vector2.zero, radius);
                break;
            }
            case 2:
            {
                var count = 4 + rng.Next(3);
                for (var i = 0; i < count; i++)
                    AddBubble(parts, new Vector2(R(rng, -0.12f, 0.12f), R(rng, -0.08f, 0.08f)), radius * R(rng, 0.3f, 0.8f));
                break;
            }
            default:
                AddBubble(parts, Vector2.zero, radius);
                break;
        }
    }

    // A gas bubble: a thick dark rim (the gas bends the light away), a clear center and a bright glint.
    private static void AddBubble(List<InclusionPart> parts, Vector2 center, float radius)
    {
        parts.Add(Ring(center, radius, Mathf.Max(0.006f, radius * 0.35f)));
        parts.Add(Tinted(Ring(center, radius * 0.78f, Mathf.Max(0.004f, radius * 0.25f), 0.45f), Black));
        parts.Add(Tinted(Dot(center + new Vector2(-radius * 0.3f, radius * 0.3f), Mathf.Max(0.004f, radius * 0.22f)), Color.white));
    }

    // Monophase liquid cavity: an irregular drop, almost as transparent as the host.
    private static void BuildLiquid(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var length = Mathf.Lerp(0.10f, 0.55f, f.Size);
        var width = length * R(rng, 0.4f, 0.8f);
        var outline = f.Variant == 1 ? Jagged(length, width, rng) : Blob(length, width, 9, rng);
        parts.Add(Tinted(Poly(outline, 0.18f, 0.8f), Liquid));
        if (f.Size > 0.5f)
            parts.Add(Tinted(Dot(new Vector2(R(rng, -0.2f, 0.2f) * length, 0f), width * 0.06f, 0.5f), Salt));
    }

    // Two-phase cavity (liquid + gas bubble) or three-phase (+ a solid crystal): the signature of Colombian emerald.
    private static void BuildPhases(List<InclusionPart> parts, LoupeFeature f, System.Random rng, bool crystalToo)
    {
        var length = Mathf.Lerp(0.16f, 0.75f, f.Size);
        var width = length * R(rng, 0.28f, 0.48f);
        var jagged = f.Variant == 1;
        var outline = jagged ? Jagged(length, width, rng) : Blob(length, width, 10, rng);
        parts.Add(Tinted(Poly(outline, 0.16f, 0.9f), Liquid));

        var side = rng.NextDouble() < 0.5 ? -1f : 1f;
        var bubbleRadius = Mathf.Min(width * 0.3f, length * 0.16f);
        AddBubble(parts, new Vector2(side * (length * 0.5f - bubbleRadius * 1.9f), R(rng, -0.1f, 0.1f) * width), bubbleRadius);

        if (crystalToo)
        {
            var cube = Mathf.Max(0.02f, width * 0.3f);
            var center = new Vector2(-side * (length * 0.5f - cube * 2.2f), R(rng, -0.12f, 0.12f) * width);
            parts.Add(Tinted(Poly(Square(center, cube, R(rng, 0f, 25f)), 0.85f), Salt));
        }
    }

    // Hollow faceted cavity ("negative crystal"), with a bubble when it is large enough.
    private static void BuildNegative(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var r = Mathf.Lerp(0.08f, 0.34f, f.Size);
        var stretch = R(rng, 1f, 1.7f);
        Vector2[] pts;
        if (f.Variant == 1)
        {
            pts = new[] { new Vector2(r * stretch, 0f), new Vector2(0f, r), new Vector2(-r * stretch, 0f), new Vector2(0f, -r) };
        }
        else
        {
            pts = new Vector2[6];
            for (var k = 0; k < 6; k++)
            {
                var a = (60f * k) * Mathf.Deg2Rad;
                pts[k] = new Vector2(Mathf.Cos(a) * r * stretch, Mathf.Sin(a) * r);
            }
        }

        parts.Add(Tinted(Poly(pts, 0.1f, 1f), Liquid));
        for (var k = 0; k < pts.Length; k += 2)
            parts.Add(Line(Vector2.zero, pts[k] * 0.55f, 0.003f, 0.4f));

        if (r > 0.14f)
        {
            AddBubble(parts, new Vector2(r * 0.35f, -r * 0.2f), r * 0.26f);
            parts.Add(Tinted(Dot(new Vector2(-r * 0.4f, r * 0.15f), r * 0.07f, 0.7f), Salt));
        }
    }

    // ---- Needles, fibers and tubes -----------------------------------------------------------------------------

    private static void BuildNeedles(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var size = f.Size;
        switch (f.Variant)
        {
            case 1:
            {
                var count = 7 + rng.Next(6);
                var length = Mathf.Lerp(0.4f, 1.4f, size);
                for (var i = 0; i < count; i++)
                {
                    var v = (i - count / 2f) * R(rng, 0.018f, 0.032f);
                    var l = length * R(rng, 0.55f, 1f);
                    var offset = R(rng, -0.12f, 0.12f);
                    var bend = R(rng, -0.02f, 0.02f);
                    var mid = new Vector2(offset, v + bend);
                    parts.Add(Tinted(Line(new Vector2(offset - l / 2f, v), mid, 0.004f, 0.55f), PaleWhite));
                    parts.Add(Tinted(Line(mid, new Vector2(offset + l / 2f, v), 0.004f, 0.55f), PaleWhite));
                }
                break;
            }
            case 2:
            {
                var count = 4 + rng.Next(5);
                var length = Mathf.Lerp(0.4f, 1.3f, size);
                for (var i = 0; i < count; i++)
                {
                    var v = (i - count / 2f) * R(rng, 0.035f, 0.05f);
                    var l = length * R(rng, 0.5f, 1f);
                    var offset = R(rng, -0.15f, 0.15f);
                    parts.Add(Tinted(Poly(new[]
                    {
                        new Vector2(offset - l / 2f, v - 0.009f), new Vector2(offset + l / 2f, v - 0.009f),
                        new Vector2(offset + l / 2f, v + 0.009f), new Vector2(offset - l / 2f, v + 0.009f),
                    }, 0.22f, 0.7f), Liquid));
                }
                break;
            }
            case 3:
            {
                var count = 5 + rng.Next(5);
                for (var i = 0; i < count; i++)
                {
                    var l = R(rng, 0.12f, 0.35f) * (0.6f + size);
                    var center = new Vector2(R(rng, -0.35f, 0.35f), R(rng, -0.2f, 0.2f));
                    var dir = Rot(Vector2.right, R(rng, -25f, 25f));
                    parts.Add(Tinted(Line(center - dir * l / 2f, center + dir * l / 2f, 0.012f, 0.85f), Green));
                }
                break;
            }
            default:
            {
                // Rutile silk: sets of fine needles crossing at 60°. Heating dissolves them into dashes and dots.
                var sets = size > 0.55f ? 3 : size > 0.25f ? 2 : 1;
                var span = Mathf.Lerp(0.35f, 0.95f, size);
                var perSet = 3 + (int)(size * 6f);
                for (var s = 0; s < sets; s++)
                {
                    var angle = s * 60f + R(rng, -3f, 3f);
                    for (var i = 0; i < perSet; i++)
                    {
                        var v = (i - perSet / 2f) * R(rng, 0.03f, 0.055f);
                        var l = span * R(rng, 0.55f, 1f);
                        var u = R(rng, -0.1f, 0.1f) * span;
                        var a = Rot(new Vector2(u - l / 2f, v), angle);
                        var b = Rot(new Vector2(u + l / 2f, v), angle);
                        if (f.Heated) AddDashes(parts, a, b, rng);
                        else parts.Add(Tinted(Line(a, b, 0.004f, 0.62f), Gold));
                    }
                }
                break;
            }
        }
    }

    private static void AddDashes(List<InclusionPart> parts, Vector2 a, Vector2 b, System.Random rng)
    {
        var pieces = 3 + rng.Next(4);
        for (var i = 0; i < pieces; i++)
        {
            var t0 = (i + R(rng, 0f, 0.25f)) / pieces;
            var t1 = (i + R(rng, 0.45f, 0.8f)) / pieces;
            parts.Add(Tinted(Line(Vector2.Lerp(a, b, t0), Vector2.Lerp(a, b, t1), 0.004f, 0.5f), Gold));
            if (rng.NextDouble() < 0.5)
                parts.Add(Tinted(Dot(Vector2.Lerp(a, b, t1 + 0.04f), 0.006f, 0.55f), Gold));
        }
    }

    // A fan of fine curved fibers growing from a tiny dark crystal: the "horsetail" of demantoid garnet.
    private static void BuildHorseTail(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var count = 14 + (int)(f.Size * 10f);
        var length = Mathf.Lerp(0.35f, 1.1f, f.Size);
        for (var i = 0; i < count; i++)
        {
            var spread = Mathf.Lerp(-58f, 58f, i / (count - 1f)) + R(rng, -4f, 4f);
            var l = length * R(rng, 0.6f, 1f);
            var bend = R(rng, -0.25f, 0.25f);
            var previous = Vector2.zero;
            for (var s = 1; s <= 4; s++)
            {
                var t = s / 4f;
                var angle = spread + bend * t * 60f;
                var point = Rot(new Vector2(l * t, 0f), angle);
                parts.Add(Tinted(Line(previous, point, 0.004f, 0.62f), Gold));
                previous = point;
            }
        }
        parts.Add(Tinted(Poly(Ellipse(0.045f, 0.03f, 8, rng, 0.2f), 1f), Black));
    }

    // ---- Fractures, cleavage, feathers and fingerprints --------------------------------------------------------

    private static void BuildFracture(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var length = 0.5f + 1.5f * f.Size;
        var path = JaggedPath(Vector2.zero, length, 6, 0.35f, rng, true);
        AddPath(parts, path, 0.008f, 1f);
        if (f.Filled) AddStrip(parts, path, 0.014f, 2);
        if (rng.NextDouble() < 0.5)
        {
            var branch = JaggedPath(path[1], length * 0.45f, 4, 0.35f, rng, false);
            AddPath(parts, branch, 0.006f, 0.8f);
        }
    }

    private static void BuildCleavage(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var length = 0.8f + 1.2f * f.Size;
        var lines = 3 + rng.Next(2);
        for (var i = 0; i < lines; i++)
        {
            var v = (i - lines / 2f) * R(rng, 0.06f, 0.1f);
            var jog = R(rng, -0.015f, 0.015f);
            var mid = R(rng, -0.2f, 0.2f) * length;
            parts.Add(Line(new Vector2(-length / 2f, v), new Vector2(mid, v + jog), 0.006f, 0.9f));
            parts.Add(Line(new Vector2(mid, v + jog), new Vector2(length / 2f, v + jog * 0.4f), 0.006f, 0.9f));
        }

        // A thin film in the gap shows interference colors.
        var half = lines * 0.04f;
        parts.Add(new InclusionPart
        {
            Type = InclusionPart.Shape.Polygon,
            Points = new[] { new Vector2(-length / 2f, -half), new Vector2(length / 2f, -half), new Vector2(length / 2f, half), new Vector2(-length / 2f, half) },
            Fill = 0.1f,
            Iridescence = 1,
            Strength = 0.4f,
        });

        if (f.Variant == 1)
        {
            // A second cleavage direction: the stone breaks along a grid.
            for (var i = 0; i < 2; i++)
            {
                var u = (i - 0.5f) * length * 0.5f;
                parts.Add(Line(new Vector2(u, -length * 0.25f), new Vector2(u + R(rng, -0.03f, 0.03f), length * 0.25f), 0.005f, 0.7f));
            }
        }
    }

    // A feather: a healing fracture filled with fluid, a veil of interference colors dotted with droplets and tubes.
    private static void BuildFeather(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var length = Mathf.Lerp(0.35f, 1.5f, f.Size);
        var width = length * R(rng, 0.35f, 0.6f);
        var outline = Blob(length, width, 14, rng, 0.3f);
        parts.Add(new InclusionPart { Type = InclusionPart.Shape.Polygon, Points = outline, Fill = 0.2f, Iridescence = 1, Strength = 0.7f });

        for (var i = 0; i < 5; i++)
        {
            var start = new Vector2(R(rng, -0.35f, 0.35f) * length, R(rng, -0.3f, 0.3f) * width);
            var path = JaggedPath(start, length * R(rng, 0.2f, 0.4f), 3, 0.5f, rng, false);
            AddPath(parts, path, 0.003f, 0.45f);
        }

        var drops = 12 + (int)(f.Size * 14f);
        for (var i = 0; i < drops; i++)
        {
            var p = new Vector2(R(rng, -0.4f, 0.4f) * length, R(rng, -0.35f, 0.35f) * width);
            parts.Add(Dot(p, R(rng, 0.007f, 0.02f), 0.6f));
            if (rng.NextDouble() < 0.25)
                parts.Add(Line(p, p + Rot(Vector2.right, R(rng, 0f, 180f)) * R(rng, 0.03f, 0.08f), 0.01f, 0.5f));
        }
    }

    private static void BuildHealing(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var span = 0.9f * (0.6f + f.Size);
        switch (f.Variant)
        {
            case 1:
            {
                // Veil: a thin sheet of tiny droplets.
                var count = 40 + (int)(f.Size * 40f);
                for (var i = 0; i < count; i++)
                {
                    var p = new Vector2(R(rng, -0.5f, 0.5f) * span, (R(rng, -1f, 1f) + R(rng, -1f, 1f)) * 0.12f);
                    parts.Add(Dot(p, R(rng, 0.006f, 0.014f), 0.5f));
                }
                break;
            }
            case 2:
            {
                // Flux residue of a synthetic: white wisps with angular turns.
                var wisps = 4 + rng.Next(3);
                for (var w = 0; w < wisps; w++)
                {
                    var p = new Vector2(R(rng, -0.35f, 0.35f) * span, R(rng, -0.2f, 0.2f));
                    var heading = R(rng, -50f, 50f);
                    for (var s = 0; s < 4; s++)
                    {
                        heading += R(rng, -40f, 40f);
                        var next = p + Rot(Vector2.right, heading) * R(rng, 0.08f, 0.2f);
                        parts.Add(Tinted(Line(p, next, 0.016f, 0.6f), PaleWhite));
                        p = next;
                    }
                    parts.Add(Tinted(Dot(p, 0.014f, 0.6f), PaleWhite));
                }
                break;
            }
            default:
            {
                // Fingerprint: droplets, short tubes and loops strung along a wavy ribbon.
                var count = 16 + (int)(f.Size * 14f);
                var frequency = R(rng, 4f, 7f);
                var previous = Vector2.zero;
                for (var i = 0; i < count; i++)
                {
                    var t = i / (count - 1f) - 0.5f;
                    var p = new Vector2(t * span, Mathf.Sin(t * frequency) * 0.09f + R(rng, -0.03f, 0.03f));
                    var roll = rng.NextDouble();
                    if (roll < 0.5) parts.Add(Dot(p, R(rng, 0.008f, 0.022f), 0.8f));
                    else if (roll < 0.8)
                    {
                        var dir = Rot(Vector2.right, R(rng, -50f, 50f));
                        var half = R(rng, 0.02f, 0.045f);
                        parts.Add(Line(p - dir * half, p + dir * half, 0.014f, 0.7f));
                    }
                    else parts.Add(Ring(p, R(rng, 0.015f, 0.03f), 0.004f, 0.8f));

                    if (i > 0) parts.Add(Line(previous, p, 0.003f, 0.25f));
                    previous = p;
                }
                break;
            }
        }
    }

    // ---- Growth structures -------------------------------------------------------------------------------------

    private static void BuildZoning(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var size = f.Size;
        switch (f.Variant)
        {
            case 1:
            {
                // Hexagonal growth zones.
                for (var ring = 0; ring < 3; ring++)
                {
                    var r = (0.25f + ring * 0.22f) * (0.6f + size);
                    var pts = new Vector2[6];
                    for (var k = 0; k < 6; k++)
                    {
                        var a = 60f * k * Mathf.Deg2Rad;
                        pts[k] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                    }
                    var zone = Poly(pts, ring % 2 == 0 ? 0.12f : 0.05f, 0.7f);
                    zone.Dark = ring % 2 == 0;
                    parts.Add(zone);
                }
                break;
            }
            case 2:
            {
                // Sector zoning: a wedge of a different color.
                var r = 0.5f * (0.6f + size);
                var wedge = Poly(new[] { Vector2.zero, new Vector2(r, -r * 0.5f), new Vector2(r, r * 0.5f) }, 0.12f, 0.8f);
                wedge.Dark = true;
                parts.Add(wedge);
                break;
            }
            default:
            {
                var bands = 2 + rng.Next(2);
                for (var i = 0; i < bands; i++)
                {
                    var v = (i - bands / 2f) * R(rng, 0.14f, 0.24f);
                    parts.Add(new InclusionPart
                    {
                        Type = InclusionPart.Shape.Band,
                        A = new Vector2(-1.2f, v),
                        B = new Vector2(1.2f, v),
                        Width = (0.07f + 0.18f * size) * R(rng, 0.6f, 1.2f),
                        Strength = R(rng, 0.7f, 1.2f),
                    });
                }
                break;
            }
        }
    }

    // Straight growth marks, with the sharp angles of natural growth.
    private static void BuildGrowth(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var lines = 6 + rng.Next(5);
        var length = Mathf.Lerp(0.8f, 1.6f, f.Size);
        var kinkAt = R(rng, -0.2f, 0.2f) * length;
        var kink = R(rng, 25f, 55f) * (rng.NextDouble() < 0.5 ? -1f : 1f);
        var v = -lines * 0.03f;
        for (var i = 0; i < lines; i++)
        {
            v += R(rng, 0.025f, 0.075f);
            var a = new Vector2(-length / 2f, v);
            var corner = new Vector2(kinkAt, v);
            var b = corner + Rot(Vector2.right, kink) * (length / 2f - kinkAt);
            parts.Add(Line(a, corner, 0.004f, 0.5f));
            parts.Add(Line(corner, b, 0.004f, 0.5f));
        }
    }

    // Curved striae: concentric arcs, the mark of a flame-fusion synthetic.
    private static void BuildStriae(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var radius = R(rng, 0.8f, 1.4f);
        var arcs = 5 + rng.Next(3);
        for (var arc = 0; arc < arcs; arc++)
        {
            var r = radius + arc * R(rng, 0.04f, 0.07f);
            var previous = Vector2.zero;
            for (var i = 0; i <= 8; i++)
            {
                var angle = (-35f + i * (70f / 8f)) * Mathf.Deg2Rad;
                var point = new Vector2(Mathf.Sin(angle) * r, Mathf.Cos(angle) * r - radius);
                if (i > 0) parts.Add(Line(previous, point, 0.005f, 0.7f));
                previous = point;
            }
        }
    }

    // Polysynthetic twinning: two families of very fine parallel planes.
    private static void BuildTwinning(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var length = Mathf.Lerp(0.5f, 1.2f, f.Size);
        var lines = 6 + (int)(f.Size * 6f);
        for (var set = 0; set < 2; set++)
        {
            var angle = set == 0 ? 0f : 60f;
            for (var i = 0; i < lines; i++)
            {
                var v = (i - lines / 2f) * R(rng, 0.02f, 0.034f);
                var a = Rot(new Vector2(-length / 2f, v), angle);
                var b = Rot(new Vector2(length / 2f, v), angle);
                parts.Add(Tinted(Line(a, b, 0.004f, set == 0 ? 0.55f : 0.4f), PaleWhite));
            }
        }
    }

    // A cloud of pinpoint particles: the velvety "silk" in suspension of a Kashmir sapphire.
    private static void BuildCloud(List<InclusionPart> parts, LoupeFeature f, System.Random rng)
    {
        var span = Mathf.Lerp(0.4f, 1.0f, f.Size);
        parts.Add(new InclusionPart
        {
            Type = InclusionPart.Shape.Band,
            A = new Vector2(-span * 0.5f, 0f),
            B = new Vector2(span * 0.5f, 0f),
            Width = span * 0.45f,
            Strength = 0.5f,
        });
        var count = 45 + (int)(f.Size * 40f);
        for (var i = 0; i < count; i++)
        {
            var p = new Vector2((R(rng, -1f, 1f) + R(rng, -1f, 1f)) * 0.25f * span, (R(rng, -1f, 1f) + R(rng, -1f, 1f)) * 0.12f * span);
            parts.Add(Tinted(Dot(p, R(rng, 0.005f, 0.012f), 0.6f), PaleWhite));
        }
    }

    // ---- Geometry helpers --------------------------------------------------------------------------------------

    private static float R(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static Vector2 Rot(Vector2 v, float degrees)
    {
        var c = Mathf.Cos(degrees * Mathf.Deg2Rad);
        var s = Mathf.Sin(degrees * Mathf.Deg2Rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    private static InclusionPart Line(Vector2 a, Vector2 b, float width, float strength = 1f)
    {
        return new InclusionPart { Type = InclusionPart.Shape.Line, A = a, B = b, Width = width, Strength = strength };
    }

    private static InclusionPart Dot(Vector2 center, float radius, float strength = 1f)
    {
        return new InclusionPart { Type = InclusionPart.Shape.Dot, A = center, Radius = radius, Strength = strength };
    }

    private static InclusionPart Ring(Vector2 center, float radius, float width, float strength = 1f)
    {
        return new InclusionPart { Type = InclusionPart.Shape.Ring, A = center, Radius = radius, Width = width, Strength = strength };
    }

    private static InclusionPart Poly(Vector2[] points, float fill, float strength = 1f)
    {
        return new InclusionPart { Type = InclusionPart.Shape.Polygon, Points = points, Fill = fill, Strength = strength };
    }

    private static InclusionPart Tinted(InclusionPart part, Color color)
    {
        part.HasColor = true;
        part.Tint = color;
        return part;
    }

    private static void AddPath(List<InclusionPart> parts, Vector2[] path, float width, float strength)
    {
        for (var i = 1; i < path.Length; i++) parts.Add(Line(path[i - 1], path[i], width, strength));
    }

    // A thin band along a path, filled with a thin film's colors (the flash of a filled fracture).
    private static void AddStrip(List<InclusionPart> parts, Vector2[] path, float half, int iridescence)
    {
        var points = new List<Vector2>();
        foreach (var p in path) points.Add(p + new Vector2(0f, half));
        for (var i = path.Length - 1; i >= 0; i--) points.Add(path[i] - new Vector2(0f, half));
        parts.Add(new InclusionPart { Type = InclusionPart.Shape.Polygon, Points = points.ToArray(), Fill = 0.55f, Iridescence = iridescence, Strength = 0.8f });
    }

    private static Vector2[] JaggedPath(Vector2 origin, float length, int segments, float jitter, System.Random rng, bool centered)
    {
        var start = centered ? origin + new Vector2(-length / 2f, 0f) : origin;
        var path = new Vector2[segments + 1];
        path[0] = start;
        for (var i = 1; i <= segments; i++)
            path[i] = start + new Vector2(length * i / segments, R(rng, -0.1f, 0.1f) * length * jitter);
        return path;
    }

    // Elongated hexagon: the outline of a prismatic crystal seen from the side.
    private static Vector2[] Prism(float length, float width, float tip)
    {
        var l = length * 0.5f;
        var w = width * 0.5f;
        var t = l * (1f - tip);
        return new[] { new Vector2(l, 0f), new Vector2(t, w), new Vector2(-t, w), new Vector2(-l, 0f), new Vector2(-t, -w), new Vector2(t, -w) };
    }

    private static Vector2[] Square(Vector2 center, float side, float degrees)
    {
        var h = side * 0.5f;
        return new[]
        {
            center + Rot(new Vector2(-h, -h), degrees), center + Rot(new Vector2(h, -h), degrees),
            center + Rot(new Vector2(h, h), degrees), center + Rot(new Vector2(-h, h), degrees),
        };
    }

    private static Vector2[] Ellipse(float a, float b, int count, System.Random rng, float jitter)
    {
        var pts = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var angle = i * 2f * Mathf.PI / count;
            var j = 1f + R(rng, -jitter, jitter);
            pts[i] = new Vector2(Mathf.Cos(angle) * a * j, Mathf.Sin(angle) * b * j);
        }
        return pts;
    }

    private static Vector2[] Blob(float length, float width, int count, System.Random rng, float jitter = 0.18f)
    {
        return Ellipse(length * 0.5f, width * 0.5f, count, rng, jitter);
    }

    // The stepped, angular outline of a cavity in emerald: straight sides with notches and pointed ends.
    private static Vector2[] Jagged(float length, float width, System.Random rng)
    {
        var points = new List<Vector2>();
        const int steps = 6;
        for (var i = 0; i <= steps; i++)
        {
            var x = -length / 2f + length * i / steps;
            points.Add(new Vector2(x, width / 2f * (1f + R(rng, -0.3f, 0.5f) * (i % 2 == 0 ? 1f : 0.4f))));
        }
        points.Add(new Vector2(length / 2f + width * 0.5f * R(rng, 0.2f, 1f), R(rng, -0.2f, 0.2f) * width));
        for (var i = steps; i >= 0; i--)
        {
            var x = -length / 2f + length * i / steps;
            points.Add(new Vector2(x, -width / 2f * (1f + R(rng, -0.3f, 0.5f) * (i % 2 == 0 ? 1f : 0.4f))));
        }
        points.Add(new Vector2(-length / 2f - width * 0.4f * R(rng, 0.2f, 1f), R(rng, -0.2f, 0.2f) * width));
        return points.ToArray();
    }
}
