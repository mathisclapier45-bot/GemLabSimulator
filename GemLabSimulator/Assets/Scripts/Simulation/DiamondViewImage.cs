using UnityEngine;

public enum DiamondViewKind { NotDiamond, Natural, NaturalAnnealed, Cvd, Hpht }

public class DiamondViewResult
{
    public DiamondViewKind Kind;
    public Color32[] Pixels;
    public int Width;
    public int Height;
    public string Observation;
    public string Interpretation;
}

// Image of a diamond under deep-UV excitation (220-230 nm), as the DiamondView shows it: only a diamond lets this light through.
// A natural stone glows blue, unevenly, sometimes with square concentric growth marks; a CVD stone glows orange in fine concentric
// striations; an HPHT synthetic glows green to blue-green with a cross (hourglass) growth pattern. A given stone always gives the
// same image.
public static class DiamondViewImage
{
    // What the DiamondView shows for a stone: the same rules whenever it is asked (image, stone sheet, report).
    public static DiamondViewKind KindOf(GemInstance gem)
    {
        var def = gem.Definition;
        var name = def.name ?? "";
        if (!name.StartsWith("Diamant")) return DiamondViewKind.NotDiamond;

        if (gem.IsSynthetic || def.forcedSynthetic)
        {
            if (name == "Diamant_Synthetique_CVD") return DiamondViewKind.Cvd;
            if (name == "Diamant_Synthetique_HPHT") return DiamondViewKind.Hpht;
            return StoneOptics.StableHash((gem.SpecimenId ?? "") + "|synth") % 2 == 0 ? DiamondViewKind.Cvd : DiamondViewKind.Hpht;
        }
        return gem.TreatmentGrade == "Chauffé" ? DiamondViewKind.NaturalAnnealed : DiamondViewKind.Natural;
    }

    // One line for the stone sheet and the report.
    public static string Summary(GemInstance gem)
    {
        switch (KindOf(gem))
        {
            case DiamondViewKind.Natural: return "Motifs de croissance naturelle (émission bleue)";
            case DiamondViewKind.NaturalAnnealed: return "Croissance naturelle, émission jaune-vert (recuit HPHT probable)";
            case DiamondViewKind.Cvd: return "Synthèse CVD (stries orange concentriques)";
            case DiamondViewKind.Hpht: return "Synthèse HPHT (croissance en croix, fluorescence verte)";
            default: return "Aucune fluorescence exploitable (pas un diamant)";
        }
    }

    public static DiamondViewResult Generate(GemInstance gem, int width, int height, bool closeUp = false)
    {
        var def = gem.Definition;
        var name = def.name ?? "";
        uint Hash(string salt) => StoneOptics.StableHash((gem.SpecimenId ?? "") + "|dv|" + salt);

        var result = new DiamondViewResult { Width = width, Height = height, Pixels = new Color32[width * height] };

        result.Kind = KindOf(gem);
        switch (result.Kind)
        {
            case DiamondViewKind.NotDiamond:
                result.Observation = "La pierre absorbe l'ultraviolet court : aucune fluorescence exploitable.";
                result.Interpretation = "Le DiamondView ne s'applique qu'au diamant, seul matériau transparent à cette longueur d'onde.";
                break;
            case DiamondViewKind.Cvd:
                result.Observation = "Émission orange, répartie en stries fines et concentriques.";
                result.Interpretation = "Diamant synthétique CVD.";
                break;
            case DiamondViewKind.Hpht:
                result.Observation = "Fluorescence verte à bleu-vert, avec une croissance en croix (sablier).";
                result.Interpretation = "Diamant synthétique HPHT.";
                break;
            case DiamondViewKind.NaturalAnnealed:
                result.Observation = "Marques de croissance naturelles, mais émission bleu-vert à jaune-vert (centres H3) plus forte que la normale.";
                result.Interpretation = "Croissance naturelle ; l'émission jaune-vert évoque un recuit HPHT.";
                break;
            default:
                var squareMarks = Hash("square") % 100 < 45;
                result.Observation = "Émission bleue, luminescence répartie de façon non homogène."
                                     + (squareMarks ? " Marques de croissance carrées concentriques." : "");
                result.Interpretation = "Diamant naturel.";
                break;
        }

        Paint(result, gem, Hash, closeUp);
        return result;
    }

    private static readonly float[] StepRings = { 0.52f, 0.68f, 0.84f };

    private static float Smooth(float edge0, float edge1, float x)
    {
        var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static void Paint(DiamondViewResult result, GemInstance gem, System.Func<string, uint> hash, bool closeUp)
    {
        var w = result.Width;
        var h = result.Height;
        var shape = gem.ShapeName;
        var cutStyle = gem.CutStyleName;

        // Outline radius of the shape for every angle, so the stone's silhouette is exact.
        const int steps = 360;
        var outline = new float[steps];
        for (var i = 0; i < steps; i++) outline[i] = Mathf.Max(0.05f, GemPixelArt.BoundaryRadius(shape, i * 2f * Mathf.PI / steps));

        var rotation = (hash("rot") % 360) * Mathf.Deg2Rad;
        var offsetX = (hash("nx") % 1000) * 0.37f;
        var offsetY = (hash("ny") % 1000) * 0.53f;
        var squareMarks = result.Kind == DiamondViewKind.Natural && hash("square") % 100 < 45;
        var hourglass = hash("hourglass") % 2 == 0;
        var stripes = 9f + (hash("stripes") % 5);
        var typeIIa = hash("t2a") % 100 < 4;

        var cx = (w - 1) * 0.5f;
        var cy = (h - 1) * 0.5f;
        var scale = Mathf.Min(w, h) * 0.45f * (closeUp ? 2.6f : 1f);
        var back = new Color32(4, 5, 9, 255);
        var sector = 16;
        var facetShade = new float[sector * 2];
        for (var i = 0; i < facetShade.Length; i++) facetShade[i] = 0.84f + 0.2f * ((hash("f" + i) % 100) / 100f);

        for (var py = 0; py < h; py++)
        {
            for (var px = 0; px < w; px++)
            {
                var x = (px - cx) / scale;
                var y = (cy - py) / scale;
                var rho = Mathf.Sqrt(x * x + y * y);
                var theta = Mathf.Atan2(y, x);
                if (theta < 0f) theta += 2f * Mathf.PI;
                var edge = outline[Mathf.Min(steps - 1, (int)(theta / (2f * Mathf.PI) * steps))];
                var u = rho / edge;                      // 0 in the middle, 1 on the girdle: follows the shape of the stone

                if (u > 1f)
                {
                    result.Pixels[py * w + px] = back;
                    continue;
                }

                if (result.Kind == DiamondViewKind.NotDiamond)
                {
                    var faint = 6 + Mathf.RoundToInt(10f * Mathf.PerlinNoise(px * 0.2f, py * 0.2f));
                    result.Pixels[py * w + px] = new Color32((byte)faint, (byte)faint, (byte)(faint + 6), 255);
                    continue;
                }

                // Pattern axes turned by a random angle (the stone is never put down the same way twice).
                var rx = x * Mathf.Cos(rotation) - y * Mathf.Sin(rotation);
                var ry = x * Mathf.Sin(rotation) + y * Mathf.Cos(rotation);

                float brightness;
                Color baseColor;
                switch (result.Kind)
                {
                    case DiamondViewKind.Cvd:
                        CvdPattern(u, rx, ry, offsetX, offsetY, stripes, out brightness, out baseColor);
                        break;
                    case DiamondViewKind.Hpht:
                        HphtPattern(u, rx, ry, offsetX, offsetY, hourglass, out brightness, out baseColor);
                        break;
                    default:
                        NaturalPattern(u, rx, ry, offsetX, offsetY, squareMarks, typeIIa, result.Kind == DiamondViewKind.NaturalAnnealed,
                            out brightness, out baseColor);
                        break;
                }

                // Facets: the table is flatter, each facet reflects a little differently, and the edges between them are dark.
                var facet = Mathf.FloorToInt(theta / (2f * Mathf.PI) * sector);
                // (A close-up of the crystal, as the electron microscope sees it, shows no facets.)
                if (!closeUp)
                {
                    var shade = u < 0.55f ? 0.9f : facetShade[facet % facetShade.Length];
                    brightness *= shade * FacetLines(u, theta, sector, cutStyle);
                    brightness *= Mathf.Lerp(1f, 0.55f, Smooth(0.85f, 1f, u));
                    if (u > 0.965f) brightness *= 0.45f;      // girdle
                }

                var c = baseColor * Mathf.Clamp01(brightness);
                result.Pixels[py * w + px] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f), 255);
            }
        }
    }

    // Dark edges between facets: radial lines around the table of a brilliant, rectangles of a step cut, nothing on a cabochon.
    private static float FacetLines(float u, float theta, int sector, string cutStyle)
    {
        if (cutStyle == "Cabochon") return 1f;

        if (cutStyle == "À degrés" || cutStyle == "Ascher")
        {
            foreach (var ring in StepRings)
                if (Mathf.Abs(u - ring) < 0.012f) return 0.35f;
            return 1f;
        }

        if (Mathf.Abs(u - 0.55f) < 0.014f) return 0.35f;            // edge of the table
        if (u > 0.55f)
        {
            var s = theta / (2f * Mathf.PI) * sector;
            var d = Mathf.Abs(s - Mathf.Round(s)) * (2f * Mathf.PI / sector) * u;
            if (d < 0.012f) return 0.4f;                             // line between two facets
        }
        return 1f;
    }

    // Natural: blue emission in uneven patches, sometimes square growth marks around the center.
    private static void NaturalPattern(float u, float x, float y, float ox, float oy, bool square, bool typeIIa, bool annealed,
        out float brightness, out Color color)
    {
        var n1 = Mathf.PerlinNoise(x * 2.3f + ox, y * 2.3f + oy);
        var n2 = Mathf.PerlinNoise(x * 5.1f + oy, y * 5.1f + ox);
        var patches = Mathf.Clamp01(0.15f + 0.95f * (0.65f * n1 + 0.35f * n2));
        brightness = 0.25f + 0.75f * patches;

        if (square)
        {
            // Octahedral zoning: squares standing on their corners.
            var d = (Mathf.Abs(x) + Mathf.Abs(y));
            var rings = Smooth(-0.4f, 0.6f, Mathf.Sin(d * 13f + 3f * n1));
            brightness *= 0.25f + 0.75f * rings;
        }

        color = new Color(0.16f, 0.36f, 1f);
        var tint = Mathf.Clamp01((n2 - 0.35f) * 1.6f);
        color = Color.Lerp(color, new Color(0.15f, 0.72f, 1f), 0.45f * tint);

        if (typeIIa)
        {
            // Rare type II: weaker, bluer-green.
            brightness *= 0.55f;
            color = Color.Lerp(color, new Color(0.1f, 0.8f, 0.85f), 0.5f);
        }
        if (annealed)
        {
            // HPHT anneal: strong H3 emission, green-yellow.
            color = Color.Lerp(color, new Color(0.45f, 1f, 0.1f), 0.55f);
            brightness = Mathf.Clamp01(brightness * 1.15f);
        }
    }

    // CVD: orange, in fine concentric layers that follow the outline of the stone.
    private static void CvdPattern(float u, float x, float y, float ox, float oy, float stripes, out float brightness, out Color color)
    {
        // Layers centered a little off the middle of the stone, wobbling, of uneven thickness and brightness.
        var warp = Mathf.PerlinNoise(x * 1.7f + ox, y * 1.7f + oy) - 0.5f;
        var ex = x - 0.18f;
        var ey = y + 0.12f;
        var distance = Mathf.Sqrt(ex * ex + ey * ey) / 1.15f;
        var phase = (0.55f * u + 0.45f * distance) * stripes * 2f * Mathf.PI + warp * 5f;
        var wave = Mathf.Sin(phase);
        var layer = Smooth(-0.6f, 0.8f, wave);
        var slow = 0.5f + 0.5f * Mathf.PerlinNoise(x * 1.2f + oy, y * 1.2f + ox);
        brightness = (0.4f + 0.6f * layer) * slow;
        color = Color.Lerp(new Color(1f, 0.42f, 0.08f), new Color(1f, 0.62f, 0.18f), layer);
    }

    // HPHT: green to blue-green, bright arms along two axes and dark sectors between them (cross or hourglass).
    private static void HphtPattern(float u, float x, float y, float ox, float oy, bool hourglass, out float brightness, out Color color)
    {
        var theta = Mathf.Atan2(y, x);
        var c2 = Mathf.Cos(2f * theta);                       // +1 along the horizontal axis, -1 along the vertical one
        float arms;
        if (hourglass) arms = Mathf.Pow(Mathf.Clamp01(c2), 0.7f);                    // two arms: a sablier lying on its side
        else arms = Mathf.Pow(Mathf.Abs(c2), 0.7f);                                    // four arms: a cross
        var diagonal = 1f - Mathf.Abs(Mathf.Sin(2f * theta));                          // narrow dark lines on the sector borders
        var growth = 0.9f + 0.1f * Mathf.Sin(u * 38f + 2f * Mathf.PerlinNoise(x * 2f + ox, y * 2f + oy));
        brightness = (0.14f + 0.86f * arms) * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(diagonal * 2.2f)) * growth;
        if (u < 0.05f) brightness *= 0.5f;                    // seed

        var mix = Mathf.Clamp01(0.35f + 0.5f * (1f - arms));
        color = Color.Lerp(new Color(0.15f, 0.95f, 0.4f), new Color(0.1f, 0.8f, 0.85f), mix);
    }
}
