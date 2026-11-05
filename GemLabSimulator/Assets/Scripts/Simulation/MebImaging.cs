using System;
using System.Collections.Generic;
using UnityEngine;

// What the scanning electron microscope records: the secondary-electron image of the surface (topography), the cathodoluminescence
// image (growth structure) or the X-ray spectrum (chemical elements).
public enum MebMode { Surface, Cathodoluminescence, XRay }

public struct MebPeak
{
    public string Symbol;
    public float KeV;
    public float Height;
}

public class MebResult
{
    public const int ImageWidth = 220;
    public const int ImageHeight = 165;
    public const float MaxKeV = 10f;
    public const int SpectrumPoints = 1001;      // one point per 0.01 keV

    public MebMode Mode;
    public int Width;
    public int Height;
    public Color32[] Pixels;                     // image modes
    public float[] Spectrum;                     // X-ray mode, counts
    public List<MebPeak> Peaks = new List<MebPeak>();
    public float FieldWidthMicrons;              // width of the image field, for the scale bar
    public string Observation;
    public string Interpretation;
    public bool RecordGrowth;                    // the image settles natural against synthetic growth
    public bool RecordTreatment;                 // a filler was identified
}

public static class MebImaging
{
    private static readonly Dictionary<string, float> Lines = new Dictionary<string, float>
    {
        { "C", 0.277f }, { "O", 0.525f }, { "F", 0.677f }, { "Na", 1.041f }, { "Mg", 1.254f }, { "Al", 1.487f }, { "Si", 1.740f },
        { "P", 2.013f }, { "K", 3.313f }, { "Ca", 3.691f }, { "Ti", 4.511f }, { "V", 4.952f }, { "Cr", 5.415f }, { "Mn", 5.899f },
        { "Fe", 6.404f }, { "Co", 6.925f }, { "Cu", 8.048f }, { "Ni", 7.478f }, { "Zr", 2.042f }, { "Y", 1.922f }, { "Pb", 2.346f }, { "Au", 2.123f },
        { "Ga", 1.098f }, { "Gd", 1.185f },
    };

    public static MebResult Measure(GemInstance gem, MebMode mode, int magnification)
    {
        var rng = new System.Random((int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|meb|" + mode + magnification) & 0x7FFFFFFF));
        switch (mode)
        {
            case MebMode.Cathodoluminescence: return Luminescence(gem, rng);
            case MebMode.XRay: return XRay(gem, rng);
            default: return Surface(gem, magnification, rng);
        }
    }

    // ---- Species -------------------------------------------------------------------------------------------------

    private static bool IsCorundum(string n) => n.StartsWith("Corindon") || n.StartsWith("Saphir") || n == "Padparadscha";
    private static bool IsQuartz(string n) => n.StartsWith("Quartz") || n == "Amethyste" || n == "Ametrine" || n == "Citrine" || n == "Prasiolite";
    private static bool IsBeryl(string n) => n == "Emeraude" || n == "Goshenite" || n == "Morganite" || n == "Heliodore" || n == "Beryl_Rouge" || n == "Pezzottaite";

    // What fills the fractures of a treated stone (null when none), with the same rule as the Raman spectrometer.
    private static string Filler(GemInstance gem)
    {
        return RamanSpectrum.FillerOf(gem, gem.Definition.name ?? "", StoneOptics.StableHash((gem.SpecimenId ?? "") + "|filler"));
    }

    // ---- Secondary electrons: the surface ---------------------------------------------------------------------------

    private static MebResult Surface(GemInstance gem, int magnification, System.Random rng)
    {
        const int w = MebResult.ImageWidth;
        const int h = MebResult.ImageHeight;
        var name = gem.Definition.name ?? "";
        var fov = 100f / Mathf.Max(1f, magnification / 1000f);       // width of the field in microns
        var umPerPixel = fov / w;
        var seedX = (float)rng.NextDouble() * 500f;
        var seedY = (float)rng.NextDouble() * 500f;

        var result = new MebResult { Mode = MebMode.Surface, Width = w, Height = h, FieldWidthMicrons = fov, Pixels = new Color32[w * h] };
        var height = new float[w * h];

        // Fine roughness of the polished surface and the parallel polishing scratches.
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                height[y * w + x] = (Mathf.PerlinNoise(x * 0.18f + seedX, y * 0.18f + seedY) - 0.5f) * 0.25f;

        var scratchAngle = (float)rng.NextDouble() * Mathf.PI;
        var scratches = 3 + rng.Next(4);
        for (var i = 0; i < scratches; i++)
        {
            var angle = scratchAngle + ((float)rng.NextDouble() - 0.5f) * 0.12f;
            var px = (float)rng.NextDouble() * w;
            var py = (float)rng.NextDouble() * h;
            Scratch(height, w, h, px, py, angle, 0.7f + (float)rng.NextDouble() * 0.7f, 0.1f + (float)rng.NextDouble() * 0.18f);
        }

        for (var i = 0; i < 14 + rng.Next(12); i++)
            Pit(height, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, 1.5f + (float)rng.NextDouble() * 2f, 0.5f + (float)rng.NextDouble() * 0.4f);

        string observation;
        string interpretation;

        // Species-specific structure.
        if (name == "Opale_Noble")
        {
            // Silica spheres of about 0.25 micron in a hexagonal packing: only visible at the strongest magnifications.
            Spheres(height, w, h, 0.25f / umPerPixel, seedX, seedY, true);
            observation = umPerPixel < 0.05f
                ? "Sphérules de silice régulières, en empilement hexagonal, avec quelques défauts d'ordre."
                : "Surface lisse : les sphérules (environ 0,25 µm) ne se résolvent pas à ce grossissement.";
            interpretation = umPerPixel < 0.05f ? "Opale noble : l'arrangement ordonné des sphérules cause le jeu de couleurs." : "Augmentez le grossissement (x20 000) pour voir les sphérules.";
        }
        else if (name == "Opale_De_Feu")
        {
            observation = "Surface amorphe, sans arrangement de sphérules, cassures conchoïdales.";
            interpretation = "Opale de feu : pas de jeu de couleurs, structure désordonnée.";
        }
        else if (name.StartsWith("Diamant"))
        {
            var kind = DiamondViewImage.KindOf(gem);
            if (kind == DiamondViewKind.Cvd)
            {
                Steps(height, w, h, 8f + (float)rng.NextDouble() * 4f, seedX, seedY);
                observation = "Marches de croissance parallèles, serrées et régulières.";
                interpretation = "Surface typique d'une croissance CVD couche par couche.";
            }
            else if (kind == DiamondViewKind.Hpht)
            {
                Hillocks(height, w, h, seedX, seedY);
                observation = "Surface rugueuse à collines de croissance larges.";
                interpretation = "Aspect compatible avec une croissance HPHT.";
            }
            else
            {
                Trigons(height, w, h, rng, 9 + rng.Next(8));
                observation = "Figures de corrosion triangulaires (trigones), toutes orientées dans le même sens.";
                interpretation = "Trigones : dissolution naturelle de la surface d'un diamant.";
            }
        }
        else
        {
            observation = "Surface polie : rayures parallèles de polissage et quelques piqûres.";
            interpretation = "Aucune structure particulière à ce grossissement.";
        }

        // What the treatments of the laboratory leave on the surface.
        var st = TreatmentSurface(gem, height, w, h, rng, ref observation, ref interpretation);

        // Intensity: slopes facing the detector are bright, edges are bright, plus the grain of the signal.
        var intensity = new float[w * h];
        for (var y = 1; y < h - 1; y++)
        {
            for (var x = 1; x < w - 1; x++)
            {
                var gx = height[y * w + x + 1] - height[y * w + x - 1];
                var gy = height[(y + 1) * w + x] - height[(y - 1) * w + x];
                var v = 0.44f + 0.22f * height[y * w + x] + 1.1f * (gx * 0.6f - gy * 0.6f) + 0.35f * (Mathf.Abs(gx) + Mathf.Abs(gy));
                intensity[y * w + x] = v + ((float)rng.NextDouble() - 0.5f) * 0.1f;
            }
        }

        // Fractures: filled (smooth, bright, with gas bubbles) or open (dark, sharp edges), and rutile needles in a natural corundum.
        var filler = Filler(gem);
        var openFracture = filler == null && (gem.ClarityGrade == "Fortement inclus" || gem.ClarityGrade == "Inclus") && rng.Next(100) < 50;
        if ((filler != null || openFracture) && !st.DyeFracture)
        {
            var width = Mathf.Clamp(3f / umPerPixel, 4f, 26f);
            DrawFracture(intensity, w, h, rng, width, filler != null);
            if (filler == "Verre au plomb")
            {
                observation = "Fissure comblée par une substance lisse et brillante, parsemée de bulles de gaz.";
                interpretation = "Verre de comblement (rubis traité) : à confirmer par l'analyse X (plomb).";
            }
            else if (filler != null)
            {
                observation = "Fissure comblée par une substance lisse, contraste sombre (matière organique).";
                interpretation = "Résine ou huile de comblement : à confirmer par le Raman ou le FTIR.";
            }
            else
            {
                observation = "Fissure ouverte : bords nets, fond sombre.";
                interpretation = "Fissure naturelle non comblée.";
            }
        }
        else if (st.DyeFracture)
        {
            DrawFracture(intensity, w, h, rng, Mathf.Clamp(3f / umPerPixel, 4f, 26f), false);
            for (var i = 0; i < 160; i++)
            {
                var gx = rng.Next(w);
                var gy = rng.Next(h);
                if (intensity[gy * w + gx] < 0.2f) intensity[gy * w + gx] = 0.55f + (float)rng.NextDouble() * 0.3f;
            }
            observation = "Fissure ouverte dont le fond est tapissé de grains de pigment, bords soulignés par un dépôt dense.";
            interpretation = "Teinture : le colorant s'est logé dans les fissures. À confirmer par l'analyse X (fer, potassium, chrome).";
        }
        else if (st.DashedNeedles)
        {
            for (var i = 0; i < 3 + rng.Next(3); i++) Needle(intensity, w, h, rng, true);
            observation = "Aiguilles de rutile cassées en pointillés, petits cratères de fusion autour des cristaux.";
            interpretation = "Corindon chauffé : la chaleur a dissous une partie des aiguilles. Compatible avec un chauffage.";
        }
        else if (IsCorundum(name) && !gem.IsSynthetic && rng.Next(100) < 45)
        {
            for (var i = 0; i < 3 + rng.Next(3); i++) Needle(intensity, w, h, rng);
            observation = "Fines aiguilles brillantes (rutile) piégées sous la surface.";
            interpretation = "Soie de rutile : inclusion typique d'un corindon naturel.";
        }

        for (var i = 0; i < intensity.Length; i++)
        {
            var v = Mathf.Clamp01(intensity[i]);
            var b = (byte)Mathf.RoundToInt(v * 255f);
            result.Pixels[i] = new Color32((byte)(b * 0.96f), (byte)(b * 0.99f), b, 255);
        }

        result.Observation = observation;
        result.Interpretation = interpretation;
        result.RecordTreatment = st.Record;
        return result;
    }

    private struct SurfaceTreatment
    {
        public bool Record;             // the picture settles the treatment
        public bool DyeFracture;        // pigment in a fracture, drawn with the intensity map
        public bool DashedNeedles;      // rutile needles broken by the heat
    }

    // The traces of the treatments on the polished surface: a coating film, acid etching, the laser channels or fissures of a diamond, the cooking of a
    // diffusion, pigment in the fractures, broken rutile needles of a heated corundum.
    private static SurfaceTreatment TreatmentSurface(GemInstance gem, float[] hmap, int w, int h, System.Random rng, ref string observation, ref string interpretation)
    {
        var st = new SurfaceTreatment();
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return st;
        var name = gem.Definition.name ?? "";

        if (gem.HasTreatment(TreatmentKind.Bleached))
        {
            for (var i = 0; i < 36; i++)
                Pit(hmap, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, 1.2f + (float)rng.NextDouble() * 1.4f, 0.5f + (float)rng.NextDouble() * 0.4f);
            for (var i = 0; i < 9; i++)
                Scratch(hmap, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, (float)rng.NextDouble() * Mathf.PI, 0.5f, 0.28f + (float)rng.NextDouble() * 0.2f);
            observation = "Nombreuses piqûres de corrosion et micro-fissures entre les grains : la surface a été attaquée par un acide.";
            interpretation = "Blanchiment à l'acide : la dissolution des joints de grains laisse un réseau de fissures. Compatible avec un blanchiment.";
        }

        if (gem.HasTreatment(TreatmentKind.Diffused))
        {
            var seed = (float)rng.NextDouble() * 400f;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    hmap[y * w + x] += 0.55f * (Mathf.PerlinNoise(x * 0.07f + seed, y * 0.07f + seed) - 0.5f);
            for (var i = 0; i < 26; i++)
                Pit(hmap, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, 1.6f + (float)rng.NextDouble() * 1.6f, 0.4f + (float)rng.NextDouble() * 0.3f);
            observation = "Surface piquée de petites cupules de fusion, joints de grains marqués par une fine couche recristallisée.";
            interpretation = "La surface a cuit à très haute température : compatible avec une diffusion. L'analyse X dira l'élément.";
        }

        if (gem.HasTreatment(TreatmentKind.Coated))
        {
            var seed = (float)rng.NextDouble() * 500f;
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    hmap[y * w + x] += 0.6f * (Mathf.PerlinNoise(x * 0.85f + seed, y * 0.85f + seed) - 0.5f);
            for (var i = 0; i < 8; i++)
                Scratch(hmap, w, h, (float)rng.NextDouble() * w, (float)rng.NextDouble() * h, (float)rng.NextDouble() * Mathf.PI, 0.7f, 0.5f + (float)rng.NextDouble() * 0.3f);
            observation = "Couche mince faite de petits grains serrés, découpée par des fissures de retrait : un film recouvre la facette.";
            interpretation = "Revêtement de surface déposé sous vide. À confirmer par l'analyse X (or, titane, cobalt, chrome).";
            st.Record = true;
        }

        if (gem.HasTreatment(TreatmentKind.Laser))
        {
            var km = gem.LaserName != null && gem.LaserName.Contains("KM");
            if (!km)
            {
                var cx = w * (0.3f + 0.4f * (float)rng.NextDouble());
                var cy = h * (0.3f + 0.4f * (float)rng.NextDouble());
                Pit(hmap, w, h, cx, cy, 3.2f, 1.5f);
                Scratch(hmap, w, h, cx, cy, (float)rng.NextDouble() * Mathf.PI, 0.55f, 0.9f);
                observation = "Trou foré de quelques microns, d'où part un canal rectiligne très fin vers l'intérieur de la pierre.";
                interpretation = "Laser classique : le canal mène à une inclusion foncée blanchie à l'acide.";
            }
            else
            {
                for (var f = 0; f < 3; f++)
                {
                    var x = (float)rng.NextDouble() * w;
                    var y = (float)rng.NextDouble() * h;
                    var angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    for (var step = 0; step < 14; step++)
                    {
                        angle += ((float)rng.NextDouble() - 0.5f) * 2.1f;
                        for (var t = 0f; t < 12f; t += 1f)
                        {
                            x += Mathf.Cos(angle);
                            y += Mathf.Sin(angle);
                            if (x < 0 || y < 0 || x >= w || y >= h) break;
                            Pit(hmap, w, h, x, y, 0.9f, 0.55f);
                        }
                    }
                }
                observation = "Fissures irrégulières en zig-zag et en spirale qui affleurent la surface, bords éclatés.";
                interpretation = "Laser KM : l'inclusion a été chauffée jusqu'à éclater, puis blanchie à l'acide. La pierre est fragilisée.";
            }
            st.Record = true;
        }

        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            st.DyeFracture = true;
            st.Record = true;
        }
        if (gem.HasTreatment(TreatmentKind.Heated) && IsCorundum(name) && !gem.IsSynthetic) st.DashedNeedles = true;
        return st;
    }

    private static void Scratch(float[] hmap, int w, int h, float px, float py, float angle, float width, float depth)
    {
        var dx = Mathf.Cos(angle);
        var dy = Mathf.Sin(angle);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var d = Mathf.Abs((x - px) * dy - (y - py) * dx);
                if (d > width * 3f) continue;
                hmap[y * w + x] -= depth * Mathf.Exp(-(d * d) / (width * width));
            }
        }
    }

    private static void Pit(float[] hmap, int w, int h, float cx, float cy, float radius, float depth)
    {
        var reach = Mathf.CeilToInt(radius * 3f);
        for (var y = Mathf.Max(0, (int)cy - reach); y <= Mathf.Min(h - 1, (int)cy + reach); y++)
        {
            for (var x = Mathf.Max(0, (int)cx - reach); x <= Mathf.Min(w - 1, (int)cx + reach); x++)
            {
                var d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                hmap[y * w + x] -= depth * Mathf.Exp(-d2 / (radius * radius));
            }
        }
    }

    // Hexagonal packing of spheres of diameter `diameter` pixels, with domains slightly turned against each other.
    private static void Spheres(float[] hmap, int w, int h, float diameter, float seedX, float seedY, bool defects)
    {
        // Spheres smaller than a few pixels cannot be resolved: they fade into a smooth surface instead of a moire.
        var amplitude = 0.5f * Mathf.Clamp01((diameter - 2.5f) / 6f);
        var k = 2f * Mathf.PI / Mathf.Max(2f, diameter) * (2f / Mathf.Sqrt(3f));
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var turn = defects ? (Mathf.PerlinNoise(x * 0.015f + seedX, y * 0.015f + seedY) - 0.5f) * 0.5f : 0f;
                var rx = x * Mathf.Cos(turn) - y * Mathf.Sin(turn);
                var ry = x * Mathf.Sin(turn) + y * Mathf.Cos(turn);
                var sum = 0f;
                for (var i = 0; i < 3; i++)
                {
                    var a = i * Mathf.PI / 3f;
                    sum += Mathf.Cos(k * (rx * Mathf.Cos(a) + ry * Mathf.Sin(a)));
                }
                hmap[y * w + x] += amplitude * (sum / 3f);
            }
        }
    }

    private static void Steps(float[] hmap, int w, int h, float period, float seedX, float seedY)
    {
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var wobble = (Mathf.PerlinNoise(x * 0.02f + seedX, y * 0.02f + seedY) - 0.5f) * 14f;
                var phase = (y + wobble) / period;
                hmap[y * w + x] += 0.45f * Mathf.Sin(phase * 2f * Mathf.PI) + 0.2f * Mathf.Sin(phase * 4f * Mathf.PI + 0.8f);
            }
        }
    }

    private static void Hillocks(float[] hmap, int w, int h, float seedX, float seedY)
    {
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                hmap[y * w + x] += 1.4f * (Mathf.PerlinNoise(x * 0.045f + seedX, y * 0.045f + seedY) - 0.5f);
    }

    // Triangular etch pits (trigons), all pointing the same way.
    private static void Trigons(float[] hmap, int w, int h, System.Random rng, int count)
    {
        for (var n = 0; n < count; n++)
        {
            var cx = (float)rng.NextDouble() * w;
            var cy = (float)rng.NextDouble() * h;
            var size = 5f + (float)rng.NextDouble() * 9f;
            for (var y = Mathf.Max(0, (int)(cy - size)); y <= Mathf.Min(h - 1, (int)(cy + size)); y++)
            {
                for (var x = Mathf.Max(0, (int)(cx - size)); x <= Mathf.Min(w - 1, (int)(cx + size)); x++)
                {
                    var dy = (y - cy) / size;
                    var dx = (x - cx) / size;
                    // Point-up triangle: |dx| <= (1 - dy) / 2 for dy in [-0.5, 1].
                    if (dy < -0.5f || dy > 1f) continue;
                    if (Mathf.Abs(dx) <= (1f - dy) * 0.5f)
                    {
                        var depth = 1f - (Mathf.Abs(dx) / Mathf.Max(0.01f, (1f - dy) * 0.5f)) * 0.5f - (dy + 0.5f) * 0.3f;
                        hmap[y * w + x] -= 0.9f * Mathf.Clamp01(depth);
                    }
                }
            }
        }
    }

    private static void DrawFracture(float[] intensity, int w, int h, System.Random rng, float width, bool filled)
    {
        var phase = (float)rng.NextDouble() * 6f;
        var amplitude = h * (0.12f + (float)rng.NextDouble() * 0.12f);
        var tilt = ((float)rng.NextDouble() - 0.5f) * 0.4f;
        var bubbles = new List<Vector3>();

        for (var x = 0; x < w; x++)
        {
            var centerY = h * 0.5f + amplitude * Mathf.Sin(x * 0.035f + phase) + (x - w * 0.5f) * tilt;
            for (var y = Mathf.Max(0, (int)(centerY - width)); y <= Mathf.Min(h - 1, (int)(centerY + width)); y++)
            {
                var d = Mathf.Abs(y - centerY) / (width * 0.5f);
                var i = y * w + x;
                if (filled)
                {
                    // Smooth, bright filler, slightly darker toward the walls.
                    if (d <= 1f) intensity[i] = 0.80f - 0.12f * d * d + ((float)rng.NextDouble() - 0.5f) * 0.02f;
                }
                else
                {
                    if (d <= 1f) intensity[i] = 0.07f + 0.05f * d;
                    else if (d <= 1.4f) intensity[i] = Mathf.Max(intensity[i], 0.85f);       // bright edge
                }
            }
            if (filled && rng.Next(100) < 3) bubbles.Add(new Vector3(x, centerY + ((float)rng.NextDouble() - 0.5f) * width * 0.6f, 1.5f + (float)rng.NextDouble() * width * 0.25f));
        }

        foreach (var b in bubbles)
        {
            var reach = Mathf.CeilToInt(b.z + 2f);
            for (var y = Mathf.Max(0, (int)b.y - reach); y <= Mathf.Min(h - 1, (int)b.y + reach); y++)
            {
                for (var x = Mathf.Max(0, (int)b.x - reach); x <= Mathf.Min(w - 1, (int)b.x + reach); x++)
                {
                    var d = Mathf.Sqrt((x - b.x) * (x - b.x) + (y - b.y) * (y - b.y));
                    if (d < b.z - 0.6f) intensity[y * w + x] = 0.45f;
                    else if (d < b.z + 0.6f) intensity[y * w + x] = 0.95f;
                }
            }
        }
    }

    private static void Needle(float[] intensity, int w, int h, System.Random rng, bool dashed = false)
    {
        var x0 = (float)rng.NextDouble() * w;
        var y0 = (float)rng.NextDouble() * h;
        var angle = (rng.Next(3) * 60f + (float)rng.NextDouble() * 4f) * Mathf.Deg2Rad;      // along the three directions of corundum
        var length = 30f + (float)rng.NextDouble() * 80f;
        for (var t = 0f; t < length; t += 0.5f)
        {
            if (dashed && (int)(t / 5f) % 2 == 1) continue;
            var x = Mathf.RoundToInt(x0 + Mathf.Cos(angle) * t);
            var y = Mathf.RoundToInt(y0 + Mathf.Sin(angle) * t);
            if (x < 0 || y < 0 || x >= w || y >= h) continue;
            intensity[y * w + x] = Mathf.Max(intensity[y * w + x], 0.9f);
            if (y + 1 < h) intensity[(y + 1) * w + x] = Mathf.Max(intensity[(y + 1) * w + x], 0.6f);
        }
    }

    // ---- Cathodoluminescence: the growth structure -------------------------------------------------------------------

    private static MebResult Luminescence(GemInstance gem, System.Random rng)
    {
        const int w = MebResult.ImageWidth;
        const int h = MebResult.ImageHeight;
        var name = gem.Definition.name ?? "";

        if (name.StartsWith("Diamant"))
        {
            // The same growth patterns as the DiamondView, seen as a close-up of the crystal.
            var view = DiamondViewImage.Generate(gem, w, h, true);
            var cl = new MebResult { Mode = MebMode.Cathodoluminescence, Width = w, Height = h, Pixels = view.Pixels, RecordGrowth = true };
            switch (view.Kind)
            {
                case DiamondViewKind.Cvd:
                    cl.Observation = "Strates orange fines et concentriques, croissance couche par couche.";
                    break;
                case DiamondViewKind.Hpht:
                    cl.Observation = "Secteurs de croissance en croix, luminescence verte à bleu-vert.";
                    break;
                case DiamondViewKind.NaturalAnnealed:
                    cl.Observation = "Zonation de croissance naturelle, mais émission jaune-vert renforcée.";
                    break;
                default:
                    cl.Observation = "Luminescence bleue répartie de façon irrégulière, zones de croissance courbes ou angulaires.";
                    break;
            }
            cl.Interpretation = gem.GrowthStructureDescription;
            return cl;
        }

        // Other crystals: the emission follows the zoning of the trace elements, tinted by the color of the stone.
        var result = new MebResult { Mode = MebMode.Cathodoluminescence, Width = w, Height = h, Pixels = new Color32[w * h], RecordGrowth = true };
        var synthetic = gem.IsSynthetic || gem.Definition.forcedSynthetic;
        var seedX = (float)rng.NextDouble() * 400f;
        var seedY = (float)rng.NextDouble() * 400f;
        var body = gem.BodyColor;
        var tint = Color.Lerp(new Color(body.r, body.g, body.b), Color.white, 0.25f);
        var arcCenter = new Vector2(-2.8f, 0.4f);

        for (var py = 0; py < h; py++)
        {
            for (var px = 0; px < w; px++)
            {
                var x = (px - w * 0.5f) / (h * 0.5f);
                var y = (h * 0.5f - py) / (h * 0.5f);
                float zoning;
                if (synthetic)
                {
                    // Flame-fusion growth: regular curved striae, arcs of a large circle.
                    var radius = Vector2.Distance(new Vector2(x, y), arcCenter);
                    zoning = 0.5f + 0.5f * Mathf.Sin(radius * 34f);
                }
                else
                {
                    // Natural growth: angular zones of irregular thickness and brightness.
                    var d = Mathf.Max(Mathf.Abs(x), Mathf.Max(Mathf.Abs(x * 0.5f + y * 0.866f), Mathf.Abs(x * 0.5f - y * 0.866f)));
                    var warp = Mathf.PerlinNoise(x * 1.6f + seedX, y * 1.6f + seedY);
                    zoning = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(d * 11f + warp * 6f)) * (0.55f + 0.7f * warp);
                }

                var v = Mathf.Clamp01(0.12f + 0.78f * zoning);
                var grain = ((float)rng.NextDouble() - 0.5f) * 0.05f;
                result.Pixels[py * w + px] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(tint.r * v + grain) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(tint.g * v + grain) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(tint.b * v + grain) * 255f), 255);
            }
        }

        result.Observation = synthetic
            ? "Stries courbes régulières, régulièrement espacées."
            : "Zones de croissance angulaires, d'épaisseur et d'intensité irrégulières.";
        result.Interpretation = gem.GrowthStructureDescription;
        return result;
    }

    // ---- X-ray spectrum: the elements -----------------------------------------------------------------------------------

    private static MebResult XRay(GemInstance gem, System.Random rng)
    {
        var name = gem.Definition.name ?? "";
        var synthetic = gem.IsSynthetic || gem.Definition.forcedSynthetic;
        var elements = new List<KeyValuePair<string, float>>();
        void E(string symbol, float weight) { elements.Add(new KeyValuePair<string, float>(symbol, weight)); }

        Composition(name, synthetic, gem, E);

        // The specimen was coated with a fine layer of gold to carry the electrons away.
        E("Au", 0.18f);

        // A treated stone: the beam also reaches the filler.
        var filler = Filler(gem);
        var lead = false;
        if (filler == "Verre au plomb")
        {
            E("Pb", 0.45f);
            E("Si", 0.35f);
            lead = true;
        }
        else if (filler != null) E("C", 0.5f);

        var treatmentNote = TreatmentXRay(gem, E);

        var result = new MebResult { Mode = MebMode.XRay, Spectrum = new float[MebResult.SpectrumPoints] };
        var scale = 3000f;
        for (var i = 0; i < result.Spectrum.Length; i++)
        {
            var e = i * 0.01f;
            result.Spectrum[i] = 70f * Mathf.Exp(-e / 2.6f) + 12f;       // bremsstrahlung background
        }

        var merged = new Dictionary<string, float>();
        foreach (var pair in elements)
        {
            if (!Lines.ContainsKey(pair.Key)) continue;
            merged[pair.Key] = merged.ContainsKey(pair.Key) ? merged[pair.Key] + pair.Value : pair.Value;
        }

        foreach (var pair in merged)
        {
            var energy = Lines[pair.Key];
            var fwhm = 0.07f + 0.03f * Mathf.Sqrt(energy);
            var sigma = fwhm / 2.3548f;
            var height = pair.Value * scale;
            var lo = Mathf.Max(0, Mathf.FloorToInt((energy - 4f * fwhm) * 100f));
            var hi = Mathf.Min(result.Spectrum.Length - 1, Mathf.CeilToInt((energy + 4f * fwhm) * 100f));
            for (var i = lo; i <= hi; i++)
            {
                var d = (i * 0.01f - energy) / sigma;
                result.Spectrum[i] += height * Mathf.Exp(-0.5f * d * d);
            }
            result.Peaks.Add(new MebPeak { Symbol = pair.Key, KeV = energy, Height = height });
        }

        for (var i = 0; i < result.Spectrum.Length; i++)
            result.Spectrum[i] = Mathf.Max(0f, result.Spectrum[i] + Gauss(rng) * (2f + 0.6f * Mathf.Sqrt(result.Spectrum[i])));

        // What the software lists: the elements without the gold coating, strongest first.
        result.Peaks.Sort((a, b) => b.Height.CompareTo(a.Height));
        var symbols = new List<string>();
        foreach (var peak in result.Peaks)
            if (peak.Symbol != "Au") symbols.Add(peak.Symbol);

        result.Observation = $"Éléments détectés : {string.Join(", ", symbols)}. Le pic de l'or vient du revêtement. Le béryllium, trop léger, n'est pas détecté.";
        if (lead)
        {
            result.Interpretation = "Plomb et silicium dans la fissure : verre de comblement. Le rubis est traité.";
            result.RecordTreatment = true;
        }
        else if (filler != null)
        {
            result.Interpretation = "Excès de carbone dans la fissure : substance organique de comblement (résine ou huile).";
        }
        else if (treatmentNote != null)
        {
            result.Interpretation = treatmentNote;
            result.RecordTreatment = true;
        }
        else
        {
            result.Interpretation = Describe(name, synthetic);
        }
        return result;
    }

    // The elements the treatments brought to the first micrometres, which the beam reaches. The light ones (beryllium...) stay invisible.
    private static string TreatmentXRay(GemInstance gem, Action<string, float> e)
    {
        if (string.IsNullOrEmpty(gem.TreatmentGrade) || gem.TreatmentGrade == "Non traité") return null;

        if (gem.HasTreatment(TreatmentKind.Coated))
        {
            switch (gem.CoatingName)
            {
                case "Aqua aura": e("Au", 0.35f); return "Or en surface bien au-delà de la métallisation du MEB : revêtement d'or (aqua aura).";
                case "Mystic": e("Ti", 0.5f); return "Titane en surface, que la pierre ne contient pas : revêtement de titane.";
                case "Bleu cobalt": e("Co", 0.5f); return "Cobalt en surface, que la pierre ne contient pas : revêtement de cobalt.";
                case "Mystic feu": e("Au", 0.3f); e("Ti", 0.45f); return "Or et titane en surface : revêtement multicouche (mystic feu).";
                case "Aqua profond": e("Au", 0.3f); e("Co", 0.45f); return "Or et cobalt en surface : revêtement multicouche (aqua profond).";
                case "Crépuscule": e("Ti", 0.45f); e("Co", 0.45f); return "Titane et cobalt en surface : revêtement multicouche (crépuscule).";
                case "Émeraude Lechleitner": e("Cr", 0.5f); return "Chrome en surface : surcroissance de béryl au chrome.";
            }
        }

        if (gem.HasTreatment(TreatmentKind.Diffused))
        {
            switch (gem.DiffusionElement)
            {
                case "Fer + titane": e("Fe", 0.35f); e("Ti", 0.4f); return "Fer et titane en excès à la surface : diffusion de fer et de titane.";
                case "Chrome": e("Cr", 0.45f); return "Chrome en excès à la surface : diffusion de chrome.";
                case "Nickel": e("Ni", 0.4f); return "Nickel en surface dans un corindon : diffusion de nickel.";
                case "Titane en excès": e("Ti", 0.45f); return "Titane en excès à la surface : diffusion de titane.";
            }
        }

        if (gem.HasTreatment(TreatmentKind.Dyed))
        {
            switch (gem.DyeName)
            {
                case "Bleu de Prusse": e("Fe", 0.35f); e("K", 0.1f); return "Fer et potassium dans les fissures : bleu de Prusse, teinture minérale.";
                case "Vert chrome": e("Cr", 0.4f); return "Chrome dans les fissures d'une pierre qui n'en contient pas : teinture au chrome.";
                default: e("C", 0.3f); return "Excès de carbone dans les fissures : colorant organique.";
            }
        }
        return null;
    }

    // Main elements of each species (relative weight of their strongest line).
    private static void Composition(string name, bool synthetic, GemInstance gem, Action<string, float> e)
    {
        if (name.StartsWith("Diamant")) { e("C", 1f); return; }

        if (IsCorundum(name))
        {
            e("O", 0.55f);
            e("Al", 1f);
            switch (name)
            {
                case "Corindon_Rubis": case "Corindon_Synthetique_Rubis": e("Cr", 0.09f); if (!synthetic) e("Fe", 0.02f); break;
                case "Saphir_Rose": e("Cr", 0.03f); break;
                case "Saphir_Bleu": case "Corindon_Synthetique_Saphir": e("Ti", 0.04f); if (!synthetic) e("Fe", 0.05f); break;
                case "Saphir_Jaune": case "Saphir_Vert": if (!synthetic) e("Fe", 0.08f); break;
                case "Saphir_Orange": case "Padparadscha": e("Cr", 0.03f); if (!synthetic) e("Fe", 0.03f); break;
            }
            return;
        }

        if (IsBeryl(name))
        {
            e("O", 0.6f);
            e("Al", 0.5f);
            e("Si", 1f);
            if (name == "Emeraude")
            {
                e("Cr", 0.06f);
                e("V", 0.03f);
                if (!synthetic)
                {
                    e("Fe", 0.04f);
                    e("Na", 0.05f);
                }
            }
            else if (name == "Morganite") e("Mn", 0.03f);
            else if (name == "Heliodore") e("Fe", 0.05f);
            return;
        }

        if (IsQuartz(name))
        {
            e("O", 0.6f);
            e("Si", 1f);
            if (name == "Amethyste" || name == "Ametrine") e("Fe", 0.02f);
            return;
        }

        switch (name)
        {
            case "Grenat_Almandin": case "Grenat_Rhodolite": e("O", 0.6f); e("Si", 0.8f); e("Al", 0.7f); e("Fe", 1f); e("Mg", 0.3f); break;
            case "Grenat_Pyrope": e("O", 0.6f); e("Si", 0.8f); e("Al", 0.6f); e("Mg", 1f); e("Cr", 0.05f); break;
            case "Grenat_Spessartite": case "Grenat_Malaya": e("O", 0.6f); e("Si", 0.8f); e("Al", 0.7f); e("Mn", 1f); break;
            case "Grenat_Tsavorite": e("O", 0.6f); e("Si", 0.8f); e("Al", 0.6f); e("Ca", 1f); e("V", 0.05f); break;
            case "Grenat_Hessonite": case "Grenat_Hydrogrossulaire": e("O", 0.6f); e("Si", 0.8f); e("Al", 0.6f); e("Ca", 1f); break;
            case "Grenat_Demantoide": e("O", 0.6f); e("Si", 0.8f); e("Ca", 1f); e("Fe", 0.8f); break;
            case "Spinelle": e("O", 0.6f); e("Mg", 0.8f); e("Al", 1f); e("Cr", 0.05f); break;
            case "Spinelle_Bleu": e("O", 0.6f); e("Mg", 0.8f); e("Al", 1f); e("Co", 0.05f); break;
            case "Topaze": e("O", 0.6f); e("Al", 1f); e("Si", 0.8f); e("F", 0.4f); break;
            case "Tourmaline_Paraiba": e("O", 0.6f); e("Si", 1f); e("Al", 0.8f); e("Na", 0.2f); e("Cu", 0.08f); break;
            case "Tourmaline_Rose": e("O", 0.6f); e("Si", 1f); e("Al", 0.8f); e("Na", 0.2f); e("Mn", 0.05f); break;
            case "Zircon": e("Zr", 1f); e("Si", 0.5f); e("O", 0.4f); break;
            case "Peridot": e("O", 0.6f); e("Mg", 1f); e("Si", 0.9f); e("Fe", 0.25f); break;
            case "Apatite": e("Ca", 1f); e("P", 0.7f); e("O", 0.5f); e("F", 0.2f); break;
            case "Fluorite": e("Ca", 1f); e("F", 0.5f); break;
            case "Opale_Noble": case "Opale_De_Feu": e("Si", 1f); e("O", 0.6f); break;
            case "Turquoise": case "Turquoise_Stabilisee": e("Al", 0.8f); e("P", 0.7f); e("Cu", 0.6f); e("O", 0.5f); e("Fe", 0.1f); break;
            case "Jade_Jadeite": e("Na", 0.4f); e("Al", 0.7f); e("Si", 1f); e("O", 0.6f); break;
            case "Jade_Nephrite": e("Mg", 0.6f); e("Ca", 0.5f); e("Si", 1f); e("Fe", 0.2f); e("O", 0.6f); break;
            case "Moissanite_Synthetique": e("Si", 1f); e("C", 0.8f); break;
            case "CZ": e("Zr", 1f); e("Y", 0.12f); e("O", 0.4f); break;
            case "YAG": e("Y", 1f); e("Al", 0.6f); e("O", 0.4f); break;
            case "GGG": e("Gd", 1f); e("Ga", 0.8f); e("O", 0.4f); break;
            case "Verre": case "Obsidienne": e("Si", 1f); e("O", 0.6f); e("Na", 0.3f); e("Ca", 0.15f); e("Al", 0.1f); break;
            case "Ivoire_Elephant": case "Ivoire_Hippopotame": case "Ivoire_Mammouth": case "Ivoire_Morse": case "Defense_Narval":
            case "Ecaille_Tortue": case "Corne_Rhinoceros": e("C", 1f); e("O", 0.6f); e("Ca", 0.5f); e("P", 0.3f); break;
            default: e("O", 0.6f); e("Si", 1f); e("Al", 0.4f); break;
        }
    }

    private static string Describe(string name, bool synthetic)
    {
        if (name.StartsWith("Diamant")) return "Carbone seul : diamant pur, sans élément trace détectable.";
        if (IsCorundum(name)) return synthetic ? "Aluminium et oxygène, chromophores seuls : aucun fer, profil d'un corindon de synthèse." : "Aluminium et oxygène, avec fer et chromophores : profil d'un corindon naturel.";
        if (name == "Verre" || name == "Obsidienne") return "Silicium, sodium et calcium : composition d'un verre.";
        return "Composition cohérente avec l'espèce.";
    }

    private static float Gauss(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
