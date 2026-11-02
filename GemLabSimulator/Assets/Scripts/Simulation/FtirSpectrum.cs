using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public struct FtirBand
{
    public float Center;     // cm-1
    public float Width;      // FWHM, cm-1
    public float Height;
    public string Label;
    public bool Artifact;    // atmospheric CO2, finger grease: seen on the curve but telling nothing about the stone
}

public class FtirResult
{
    public const float MaxCm = 4000f;
    public const float MinCm = 1000f;
    public const int Points = 3001;      // one point per cm-1; index i is wavenumber MaxCm - i

    public float[] Absorbance;
    public float[] Lattice;              // lattice absorption of the host crystal (saturates toward the low wavenumbers)
    public List<FtirBand> Bands = new List<FtirBand>();

    // What the software concludes from the curve.
    public string DiamondType;           // "Ia", "IIa", "IIb" or null
    public List<string> Analysis = new List<string>();
    public bool TreatmentConclusive;     // false: the infrared cannot tell for this stone, nothing is recorded
}

// Infrared absorption spectrum (4000 to 1000 cm-1) of a gem, as an FTIR spectrometer records it. The curve comes from the
// stone's species and its true state: natural or synthetic, filled with oil or resin, heated or not. A given stone always gives
// the same curve.
public static class FtirSpectrum
{
    private const float Saturation = 4.5f;

    private class Ctx
    {
        public GemInstance Gem;
        public string Name;
        public float Path;
        public bool Synthetic;
        public System.Random Rng;
        public float[] A = new float[FtirResult.Points];
        public float[] Lattice = new float[FtirResult.Points];
        public List<FtirBand> Bands = new List<FtirBand>();
        public bool HasCh;       // C-H stretching bands of a filler or of an organic material are already on the curve

        public bool Has(string keyword)
        {
            var origin = Gem.Origin != null ? Gem.Origin.name : null;
            return origin != null && origin.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public uint Hash(string salt) => StoneOptics.StableHash((Gem.SpecimenId ?? "") + "|" + salt);

        public void Add(float center, float fwhm, float height, string label = null, bool artifact = false, bool scaled = true)
        {
            if (scaled) height *= Path;
            if (height <= 0.001f) return;
            Bands.Add(new FtirBand { Center = center, Width = fwhm, Height = height, Label = label, Artifact = artifact });

            var sigma = fwhm / 2.3548f;
            var lo = Mathf.Max(0, Mathf.FloorToInt(FtirResult.MaxCm - (center + 4f * fwhm)));
            var hi = Mathf.Min(A.Length - 1, Mathf.CeilToInt(FtirResult.MaxCm - (center - 4f * fwhm)));
            for (var i = lo; i <= hi; i++)
            {
                var d = (FtirResult.MaxCm - i - center) / sigma;
                A[i] += height * Mathf.Exp(-0.5f * d * d);
            }
        }

        // The crystal itself absorbs below `cut` (cm-1): the detector saturates.
        public void Cutoff(float cut, float width)
        {
            for (var i = 0; i < A.Length; i++)
            {
                var nu = FtirResult.MaxCm - i;
                Lattice[i] += Saturation / (1f + Mathf.Exp((nu - cut) / width));
            }
        }
    }

    public static FtirResult Measure(GemInstance gem)
    {
        var def = gem.Definition;
        var c = new Ctx
        {
            Gem = gem,
            Name = def.name ?? "",
            Synthetic = gem.IsSynthetic || def.forcedSynthetic,
            Rng = new System.Random((int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|ftir") & 0x7FFFFFFF)),
            Path = gem.DepthMm > 0f ? Mathf.Clamp(gem.DepthMm / 5f, 0.6f, 1.6f) : 1f,
        };

        var result = new FtirResult();
        var group = GroupOf(c.Name);

        Host(c, group, result);
        Treatments(c, group, result);
        Contamination(c);

        // Whatever the stone, the sample compartment holds a little CO2.
        c.Add(2361, 8, 0.10f + 0.08f * (float)c.Rng.NextDouble(), "CO2 atmosphérique", true, false);
        c.Add(2338, 8, 0.07f, null, true, false);

        Assemble(c, result);
        return result;
    }

    // ---- Host material --------------------------------------------------------------------------------------

    private enum HostGroup { Corundum, Beryl, Quartz, Diamond, DiamondImitation, Glass, Garnet, Organic, Turquoise, Jade, Opal, Other }

    private static HostGroup GroupOf(string name)
    {
        if (name.StartsWith("Corindon") || name.StartsWith("Saphir") || name == "Padparadscha") return HostGroup.Corundum;
        if (name == "Emeraude" || name == "Goshenite" || name == "Morganite" || name == "Heliodore" || name == "Beryl_Rouge" || name == "Pezzottaite") return HostGroup.Beryl;
        if (name.StartsWith("Quartz") || name == "Amethyste" || name == "Ametrine" || name == "Citrine" || name == "Prasiolite") return HostGroup.Quartz;
        if (name.StartsWith("Diamant")) return HostGroup.Diamond;
        if (name == "CZ" || name == "Moissanite_Synthetique" || name == "YAG" || name == "GGG") return HostGroup.DiamondImitation;
        if (name == "Verre" || name == "Obsidienne") return HostGroup.Glass;
        if (name.StartsWith("Grenat")) return HostGroup.Garnet;
        if (name.StartsWith("Ivoire") || name == "Defense_Narval" || name == "Ecaille_Tortue" || name == "Corne_Rhinoceros") return HostGroup.Organic;
        if (name.StartsWith("Turquoise")) return HostGroup.Turquoise;
        if (name.StartsWith("Jade") || name == "Serpentine_Bowenite") return HostGroup.Jade;
        if (name.StartsWith("Opale")) return HostGroup.Opal;
        return HostGroup.Other;
    }

    private static void Host(Ctx c, HostGroup group, FtirResult result)
    {
        switch (group)
        {
            case HostGroup.Corundum:
                c.Cutoff(2150f, 25f);
                CorundumBands(c);
                break;
            case HostGroup.Beryl:
                c.Cutoff(2200f, 25f);
                BerylWater(c);
                break;
            case HostGroup.Quartz:
                c.Cutoff(1990f, 25f);
                QuartzBands(c);
                break;
            case HostGroup.Diamond:
                DiamondBands(c, result);
                break;
            case HostGroup.DiamondImitation:
                ImitationBands(c);
                break;
            case HostGroup.Glass:
                c.Cutoff(2050f, 60f);
                c.Add(3500, 450, 0.5f, "Eau / Si-OH (verre)");
                c.Add(2850, 220, 0.2f);
                break;
            case HostGroup.Garnet:
                c.Cutoff(2000f, 25f);
                if (c.Name == "Grenat_Hydrogrossulaire")
                {
                    c.Add(3600, 25, 0.7f, "OH (hydrogrenat)");
                    c.Add(3660, 20, 0.35f, "OH (hydrogrenat)");
                }
                else c.Add(3560, 60, 0.1f, "OH");
                break;
            case HostGroup.Organic:
                // Natural organic materials: C-H stretching is part of the material, not a treatment.
                c.Cutoff(900f, 60f);
                c.Add(3300, 220, 1.0f, "Collagène / protéines (N-H, O-H)");
                c.Add(2925, 35, 0.8f, "Collagène (C-H)");
                c.Add(2855, 30, 0.55f, "Collagène (C-H)");
                c.Add(1650, 60, 1.2f, "Amide I");
                c.Add(1540, 55, 1.0f, "Amide II");
                c.HasCh = true;
                break;
            case HostGroup.Turquoise:
                c.Cutoff(1250f, 60f);
                c.Add(3510, 30, 0.6f, "OH (turquoise)");
                c.Add(3465, 28, 0.55f, "OH (turquoise)");
                c.Add(3300, 220, 0.5f, "Eau (turquoise)");
                c.Add(1650, 50, 0.4f, "Eau (turquoise)");
                break;
            case HostGroup.Jade:
                c.Cutoff(2150f, 25f);
                if (c.Name == "Jade_Jadeite")
                {
                    c.Add(3670, 14, 0.5f, "OH (jadéite)");
                    c.Add(3615, 40, 0.7f, "OH (jadéite)");
                    c.Add(3450, 400, 0.3f, "Eau");
                }
                else if (c.Name == "Jade_Nephrite")
                {
                    c.Add(3675, 12, 0.8f, "OH (amphibole)");
                    c.Add(3640, 14, 0.4f, "OH (amphibole)");
                }
                else
                {
                    c.Add(3690, 14, 0.7f, "OH (serpentine)");
                    c.Add(3650, 20, 0.6f, "OH (serpentine)");
                }
                break;
            case HostGroup.Opal:
                c.Cutoff(2100f, 40f);
                c.Add(3400, 380, 0.7f, "Eau (opale)");
                c.Add(3200, 300, 0.3f, "Eau (opale)");
                break;
            default:
                OtherSpecies(c);
                break;
        }
    }

    private static void OtherSpecies(Ctx c)
    {
        var n = c.Name;
        float cut = 2050f;
        if (n == "Zircon" || n == "Spinelle" || n == "Spinelle_Bleu") cut = 1900f;
        else if (n == "Peridot") cut = 1950f;
        else if (n == "Fluorite") cut = 1200f;
        c.Cutoff(cut, 25f);

        if (n == "Topaze")
        {
            c.Add(3650, 18, 0.2f, "OH (topaze)");
            c.Add(3570, 18, 0.15f, "OH (topaze)");
        }
        else if (n.StartsWith("Tourmaline"))
        {
            c.Add(3550, 60, 0.3f, "OH (tourmaline)");
            c.Add(3640, 30, 0.25f, "OH (tourmaline)");
        }
        else if (n == "Apatite") c.Add(3540, 16, 0.4f, "OH (apatite)");
        else if (n == "Tanzanite" || n == "Zoisite") c.Add(3480, 60, 0.4f, "OH (zoïsite)");
        else if (n == "Kyanite" || n == "Andalousite" || n == "Iolite" || n == "Scapolite") c.Add(3560, 80, 0.12f, "OH");
        else c.Add(3450, 400, 0.08f, "Eau");
    }

    // Natural unheated corundum may keep boehmite (3160); a heated one shows the OH peaks at 3309, 3232 and 3185.
    private static void CorundumBands(Ctx c)
    {
        if (c.Synthetic) return;   // anhydrous: nothing in the OH region

        var heated = c.Gem.TreatmentGrade == "Chauffé";
        var lowTemperature = heated && c.Hash("lowtemp") % 3 == 0;
        var boehmite = !heated && c.Hash("boehmite") % 100 < 55;

        if (boehmite || lowTemperature) c.Add(3160, 22, 0.3f, "Boehmite");
        if (heated)
        {
            c.Add(3309, 9, 0.38f, "OH (chauffage)");
            c.Add(3232, 9, 0.30f, "OH (chauffage)");
            c.Add(3185, 9, 0.32f, "OH (chauffage)");
        }
    }

    // Water in the channels of beryl. Natural: type I (3698, 3655) and type II, with alkalis (3595, 3555). A flux synthetic has none;
    // a hydrothermal one only has type I.
    private static void BerylWater(Ctx c)
    {
        if (c.Synthetic)
        {
            if (c.Hash("flux") % 100 < 60) return;   // flux synthetic: anhydrous
            c.Add(3698, 8, 0.8f, "Eau type I");
            c.Add(3655, 10, 0.7f, "Eau type I");
            return;
        }

        var colombia = c.Has("Colombie");
        var type1 = colombia ? 0.9f : 0.5f;
        var type2 = colombia ? 0.3f : 0.9f;
        c.Add(3698, 8, 0.5f * type1, "Eau type I");
        c.Add(3655, 10, 0.45f * type1, "Eau type I");
        c.Add(3595, 10, 0.55f * type2, "Eau type II");
        c.Add(3555, 14, 0.5f * type2, "Eau type II");
        c.Add(3450, 400, 0.12f, "Eau");
    }

    // Quartz. Natural amethyst: a narrow 3595 peak (under 3.3 cm-1 wide) taller than 3585. Hydrothermal synthetic: 3595 absent or wider
    // than 7 cm-1 and lower than 3585.
    private static void QuartzBands(Ctx c)
    {
        // Overtones of the lattice, between 2000 and 1600.
        c.Add(1990, 25, 0.7f);
        c.Add(1870, 25, 0.9f);
        c.Add(1790, 25, 0.9f);

        if (c.Synthetic)
        {
            if (c.Hash("q3595") % 100 < 50) c.Add(3595, 8f, 0.18f, "OH quartz (3595)");
            c.Add(3585, 6f, 0.5f, "OH quartz (3585)");
            c.Add(3400, 350, 0.4f, "Eau");
        }
        else
        {
            c.Add(3595, 2.8f, 0.55f, "OH quartz (3595)");
            c.Add(3585, 4f, 0.28f, "OH quartz (3585)");
            c.Add(3484, 25, 0.25f, "Al-OH");
            c.Add(3440, 40, 0.3f, "Al-OH");
        }
    }

    // Diamond: the two-phonon absorption of the lattice for every stone, and the nitrogen of type I.
    private static void DiamondBands(Ctx c, FtirResult result)
    {
        var g = c.Gem;
        c.Add(2650, 120, 0.12f, "Réseau du diamant");
        c.Add(2450, 90, 0.22f, "Réseau du diamant");
        c.Add(2160, 70, 0.30f, "Réseau du diamant");
        c.Add(2030, 50, 0.55f, "Réseau du diamant");
        c.Add(1990, 40, 0.5f, "Réseau du diamant");
        c.Add(1760, 60, 0.6f, "Réseau du diamant");
        c.Add(2000, 900, 0.3f);

        if (c.Synthetic)
        {
            // Colorless synthetics are type II. A CVD stone often keeps a trace of hydrogen.
            result.DiamondType = "IIa";
            if (c.Hash("synth") % 2 == 0) c.Add(3123, 4, 0.14f, "N-V-H (CVD)");
            return;
        }

        var hue = g.ColorHue;
        if (g.IsFancyDiamond && hue >= 170f && hue < 260f)
        {
            // Blue diamond: boron.
            result.DiamondType = "IIb";
            c.Add(2800, 250, 0.6f, "Bore");
            c.Add(2930, 20, 0.3f, "Bore");
            c.Add(2458, 12, 0.3f, "Bore");
            c.Add(1290, 40, 0.3f, "Bore");
            return;
        }

        if (c.Hash("type") % 100 < 4)
        {
            result.DiamondType = "IIa";   // rare natural type II
            return;
        }

        // Type Ia: aggregated nitrogen, the more the yellower.
        result.DiamondType = "Ia";
        var n = Mathf.Clamp(0.25f + 1.5f * g.ColorSaturation, 0.2f, 1.4f);
        c.Add(1282, 80, 1.2f * n, "Azote (type Ia)");
        c.Add(1215, 40, 0.5f * n, "Azote (type Ia)");
        c.Add(1175, 70, 0.9f * n, "Azote (type Ia)");
        c.Add(1010, 70, 0.8f * n, "Azote (type Ia)");
        c.Add(1370, 25, 0.3f * n, "Plaquettes");
        c.Add(3107, 7, 0.25f, "Hydrogène");
    }

    // CZ, moissanite and garnets: curves that have nothing to do with a diamond's.
    private static void ImitationBands(Ctx c)
    {
        switch (c.Name)
        {
            case "CZ":
                c.Cutoff(1700f, 120f);
                c.Add(3400, 400, 0.1f, "Eau");
                break;
            case "Moissanite_Synthetique":
                c.Cutoff(1500f, 150f);
                c.Add(2150, 40, 0.3f, "SiC (multiphonons)");
                c.Add(1970, 40, 0.45f, "SiC (multiphonons)");
                c.Add(1800, 60, 0.6f, "SiC (multiphonons)");
                c.Add(1620, 60, 0.8f, "SiC (multiphonons)");
                break;
            default:
                c.Cutoff(1800f, 40f);
                c.Add(3500, 300, 0.06f, "Eau");
                break;
        }
    }

    // ---- Treatments and contamination ---------------------------------------------------------------------------

    private static void Treatments(Ctx c, HostGroup group, FtirResult result)
    {
        var g = c.Gem;
        var grade = g.TreatmentGrade;

        if (group == HostGroup.Diamond && grade == "Irradié" && !c.Synthetic)
        {
            c.Add(1450, 6, 0.35f, "H1a (irradiation + recuit)");
            c.Add(1405, 6, 0.15f, "H1b");
        }

        var filled = grade == "Huilé / imprégné" || grade == "Fracture comblée" || c.Name == "Turquoise_Stabilisee";
        if (!filled || group == HostGroup.Organic) return;

        // Corundum filled with glass: water-rich, no C-H.
        if (group == HostGroup.Corundum && grade == "Fracture comblée" && c.Hash("glass") % 100 < 70)
        {
            c.Add(3400, 450, 0.6f, "Verre comblant (eau)");
            return;
        }

        var polymer = group == HostGroup.Jade || group == HostGroup.Turquoise || group == HostGroup.Opal
                      || c.Name == "Turquoise_Stabilisee" || grade == "Fracture comblée" || c.Hash("resin") % 100 < 35;
        if (polymer)
        {
            var s = group == HostGroup.Jade || group == HostGroup.Turquoise ? 1.5f : 1f;
            c.Add(3065, 12, 0.35f * s, "Résine (cycles aromatiques)");
            c.Add(3051, 8, 0.5f * s, "Résine (cycles aromatiques)");
            c.Add(3035, 14, 0.6f * s, "Résine (cycles aromatiques)");
            c.Add(2965, 25, 0.45f * s, "Résine (C-H)");
            c.Add(2930, 30, 0.9f * s, "Résine (C-H)");
            c.Add(2870, 25, 0.5f * s, "Résine (C-H)");
            c.Add(1740, 30, 0.7f * s, "Résine (C=O)");
            c.Add(3400, 300, 0.2f, "Eau");
        }
        else
        {
            c.Add(2960, 25, 0.35f, "Huile (C-H)");
            c.Add(2925, 30, 0.9f, "Huile (C-H)");
            c.Add(2855, 25, 0.6f, "Huile (C-H)");
            c.Add(3400, 300, 0.25f, "Eau");
        }
        c.HasCh = true;
    }

    // Fingerprint grease leaves weak C-H bands on many stones: a classic false alarm (see the jadeite figure of the course).
    private static void Contamination(Ctx c)
    {
        if (c.HasCh || c.Hash("grease") % 100 >= 35) return;
        c.Add(2955, 14, 0.06f, "Gras des doigts", true, false);
        c.Add(2925, 16, 0.10f, "Gras des doigts", true, false);
        c.Add(2855, 14, 0.07f, "Gras des doigts", true, false);
    }

    // ---- Final curve ----------------------------------------------------------------------------------------------

    private static void Assemble(Ctx c, FtirResult result)
    {
        var a = c.A;
        for (var i = 0; i < a.Length; i++)
        {
            // Small baseline, then the saturating lattice; the detector is noisy where almost nothing goes through.
            var lattice = c.Lattice[i];
            var sigma = 0.004f + 0.45f * Mathf.Clamp01(lattice / 3.5f);
            a[i] += 0.03f + 0.00003f * i + lattice + Gauss(c.Rng) * sigma;
            a[i] = Mathf.Clamp(a[i], 0f, 6f);
        }

        result.Absorbance = a;
        result.Lattice = c.Lattice;
        result.Bands = c.Bands;
        Interpret(c, result);
    }

    // Highest absorbance on the part of the curve where the stone still lets the beam through.
    public static float InformativeMax(FtirResult result, float fromCm, float toCm)
    {
        var max = 0f;
        var lo = Mathf.Max(0, Mathf.FloorToInt(FtirResult.MaxCm - toCm));
        var hi = Mathf.Min(result.Absorbance.Length - 1, Mathf.CeilToInt(FtirResult.MaxCm - fromCm));
        for (var i = lo; i <= hi; i++)
        {
            if (result.Lattice[i] > 0.6f) continue;
            max = Mathf.Max(max, result.Absorbance[i]);
        }
        return max;
    }

    // ---- Reading ----------------------------------------------------------------------------------------------------

    // Peaks grouped by assignment, strongest first: "Huile (C-H) : 2960, 2925, 2855 cm-1".
    public static List<string> PeakLines(FtirResult result)
    {
        var lines = new List<string>();
        var labels = new List<string>();
        var centers = new Dictionary<string, List<float>>();
        var strongest = new Dictionary<string, float>();
        foreach (var b in result.Bands)
        {
            if (b.Label == null || b.Height < 0.08f) continue;
            if (!centers.ContainsKey(b.Label))
            {
                centers[b.Label] = new List<float>();
                strongest[b.Label] = 0f;
                labels.Add(b.Label);
            }
            centers[b.Label].Add(b.Center);
            strongest[b.Label] = Mathf.Max(strongest[b.Label], b.Artifact ? b.Height * 0.2f : b.Height);
        }

        labels.Sort((x, y) => strongest[y].CompareTo(strongest[x]));
        foreach (var label in labels)
        {
            var list = centers[label];
            list.Sort((p, q) => q.CompareTo(p));
            var sb = new StringBuilder(label).Append(" : ");
            for (var i = 0; i < list.Count && i < 4; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Mathf.RoundToInt(list[i]));
            }
            sb.Append(" cm-1");
            lines.Add(sb.ToString());
        }

        if (lines.Count == 0) lines.Add("Aucun pic marqué.");
        return lines;
    }

    private static void Interpret(Ctx c, FtirResult result)
    {
        var g = c.Gem;
        var grade = g.TreatmentGrade;
        var group = GroupOf(c.Name);
        var analysis = result.Analysis;

        if (result.DiamondType != null)
        {
            analysis.Add(result.DiamondType == "Ia" ? "Diamant de type Ia (azote agrégé)."
                : result.DiamondType == "IIb" ? "Diamant de type IIb (bore)."
                : "Diamant de type IIa : sans azote, à confirmer par photoluminescence ou DiamondView.");
        }
        else if (group == HostGroup.DiamondImitation)
        {
            analysis.Add("Spectre sans rapport avec celui d'un diamant : simulant.");
        }

        if (group == HostGroup.Beryl)
        {
            var hasWater = false;
            foreach (var b in result.Bands) if (b.Label != null && b.Label.StartsWith("Eau type")) hasWater = true;
            analysis.Add(hasWater ? "Eau de canal présente (béryl naturel ou hydrothermal)." : "Béryl anhydre : aucune eau détectée.");
        }

        if (group == HostGroup.Quartz)
            analysis.Add("Mesurez les pics à 3595 et 3585 cm-1 (zoom : molette).");

        var conclusive = true;
        var filled = grade == "Huilé / imprégné" || grade == "Fracture comblée" || c.Name == "Turquoise_Stabilisee";
        if (filled && group != HostGroup.Organic)
        {
            var oil = false;
            var resin = false;
            var glass = false;
            foreach (var b in result.Bands)
            {
                if (b.Label == null) continue;
                if (b.Label.StartsWith("Huile")) oil = true;
                if (b.Label.StartsWith("Résine")) resin = true;
                if (b.Label.StartsWith("Verre comblant")) glass = true;
            }
            analysis.Add(resin ? "Résine ou polymère détecté : comblement moderne."
                : oil ? "Huile détectée dans les fissures."
                : glass ? "Comblement par un verre (eau en bande large)."
                : "Substance de comblement détectée.");
        }
        else if (group == HostGroup.Corundum && !c.Synthetic)
        {
            var heated = grade == "Chauffé";
            var boehmite = false;
            foreach (var b in result.Bands) if (b.Label == "Boehmite") boehmite = true;
            analysis.Add(heated
                ? (boehmite ? "Pics OH (3309, 3232, 3185) et boehmite : chauffage à basse température."
                            : "Pics OH (3309, 3232, 3185) : traitement thermique.")
                : (boehmite ? "Boehmite (3160) : pas de chauffage au-delà de 1400 °C." : "Aucun indice de traitement thermique ni de comblement."));
        }
        else if (group == HostGroup.Diamond)
        {
            conclusive = grade == "Irradié" && !c.Synthetic;
            analysis.Add(conclusive ? "Pic H1a (1450) : irradiation suivie d'un recuit." : "Aucun comblement : l'infrarouge ne dit rien d'autre sur le traitement.");
        }
        else
        {
            analysis.Add("Aucun comblement détecté.");
            analysis.Add("Chauffage et irradiation ne laissent pas de trace infrarouge sur cette espèce.");
            conclusive = false;
        }

        // Corundum: a stone that is neither filled nor heated is conclusive as "untreated" only when the curve shows it.
        if (group == HostGroup.Corundum && !c.Synthetic) conclusive = grade == "Chauffé" || filled || grade == "Non traité";
        if (group == HostGroup.Corundum && !c.Synthetic && grade == "Irradié") conclusive = false;
        if (filled && group != HostGroup.Organic) conclusive = true;
        result.TreatmentConclusive = conclusive;
    }

    private static float Gauss(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
