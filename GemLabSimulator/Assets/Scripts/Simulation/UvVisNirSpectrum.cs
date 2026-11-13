using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// One absorption band of a stone: where it sits, how wide it is (full width at half height) and how much it absorbs.
public struct UvVisBand
{
    public float Center;
    public float Width;
    public float Height;
    public string Label;      // chromophore or color center ("Cr3+", "Fe3+", "N3"...); null for an unlabelled background hump
    public string Element;    // chemical symbol(s) for the trace-element summary
}

public class UvVisNirResult
{
    public const float MinNm = 300f;
    public const float MaxNm = 1000f;
    public const int Points = 701;     // one point per nm

    public float[] Absorbance;
    public float YMax;
    public List<UvVisBand> Bands = new List<UvVisBand>();
    public bool Opaque;
    public string IronLevel;           // only for species whose iron content tells something (corundum, beryl, spinel)
}

// Absorbance curve of a gem between 300 and 1000 nm, as a UV-Vis-NIR spectrophotometer records it. The curve is built from
// what colors the stone: the chromophores of the species, the iron of the deposit, the nature (natural or synthetic) and the
// treatment. A given stone always gives the same curve.
public static class UvVisNirSpectrum
{
    private const float Saturation = 3.6f;

    private class Ctx
    {
        public GemInstance Gem;
        public GemDefinition Def;
        public string Name;
        public bool Synthetic;
        public float Path;       // longer path through the stone, more absorption
        public float Tone;       // 0.5 (pale) to 1.4 (vivid): scales the chromophore bands
        public float Iron;       // iron content of the deposit (0 = none, as in a synthetic corundum)
        public bool UsesIron;
        public bool SkipColor;   // the recipe of the species already carries its own bands
        public float EdgeNm = 340f;
        public System.Random Rng;
        public float[] A = new float[UvVisNirResult.Points];
        public List<UvVisBand> Bands = new List<UvVisBand>();

        public bool Has(string keyword)
        {
            var origin = Gem.Origin != null ? Gem.Origin.name : null;
            return origin != null && origin.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Add(float center, float fwhm, float height, string label = null, string element = null)
        {
            height *= Path;
            if (height <= 0.001f) return;
            Bands.Add(new UvVisBand { Center = center, Width = fwhm, Height = height, Label = label, Element = element });

            var sigma = fwhm / 2.3548f;
            var lo = Mathf.Max(0, Mathf.FloorToInt(center - 4f * fwhm - UvVisNirResult.MinNm));
            var hi = Mathf.Min(A.Length - 1, Mathf.CeilToInt(center + 4f * fwhm - UvVisNirResult.MinNm));
            for (var i = lo; i <= hi; i++)
            {
                var d = (UvVisNirResult.MinNm + i - center) / sigma;
                A[i] += height * Mathf.Exp(-0.5f * d * d);
            }
        }

        // Absorption that falls off from `fromNm` toward the red (a brown or yellow body color, an absorption edge shifted to the visible).
        public void Slope(float height, float fromNm, float scale)
        {
            height *= Path;
            for (var i = 0; i < A.Length; i++)
            {
                var x = UvVisNirResult.MinNm + i;
                A[i] += height * Mathf.Exp(-Mathf.Max(0f, x - fromNm) / scale);
            }
        }

        public void Edge(float nm, float width)
        {
            for (var i = 0; i < A.Length; i++)
            {
                var x = UvVisNirResult.MinNm + i;
                A[i] += 3.4f / (1f + Mathf.Exp((x - nm) / width));
            }
        }
    }

    public static UvVisNirResult Measure(GemInstance gem)
    {
        var def = gem.Definition;
        var c = new Ctx
        {
            Gem = gem,
            Def = def,
            Name = def.name ?? "",
            Synthetic = gem.IsSynthetic || def.forcedSynthetic,
            Rng = new System.Random((int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|uvvisnir") & 0x7FFFFFFF)),
            Path = gem.DepthMm > 0f ? Mathf.Clamp(gem.DepthMm / 5f, 0.6f, 1.7f) : 1f,
            Tone = Mathf.Lerp(0.5f, 1.4f, Mathf.Clamp01(gem.ColorSaturation * 1.3f)),
        };
        c.Iron = c.Synthetic ? 0f : IronOfDeposit(c);

        var result = new UvVisNirResult();
        var diaphaneity = def.Diaphaneity;

        if (diaphaneity == Diaphaneity.Opaque)
        {
            // Nothing goes through: the detector saturates.
            result.Opaque = true;
            for (var i = 0; i < c.A.Length; i++) c.A[i] = Saturation - 0.15f;
        }
        else
        {
            c.Edge(EdgeOf(c), 9f);
            Recipe(c);
            if (!c.SkipColor) ColorBands(c);
            Treatment(c);

            // Light scattered by a translucent stone lifts the whole curve and flattens the bands.
            if (diaphaneity == Diaphaneity.SemiOpaque || diaphaneity == Diaphaneity.SemiTransparent)
            {
                var semiOpaque = diaphaneity == Diaphaneity.SemiOpaque;
                var lift = semiOpaque ? 1.3f : 0.35f;
                var keep = semiOpaque ? 0.75f : 0.9f;
                for (var i = 0; i < c.A.Length; i++) c.A[i] = c.A[i] * keep + lift;
            }

            // Reflection losses and a slightly tilted baseline.
            for (var i = 0; i < c.A.Length; i++) c.A[i] += 0.04f + 0.00006f * i;
        }

        AddNoise(c);

        var max = 0f;
        for (var i = 0; i < c.A.Length; i++)
        {
            c.A[i] = Mathf.Clamp(c.A[i], 0f, Saturation);
            max = Mathf.Max(max, c.A[i]);
        }

        result.Absorbance = c.A;
        result.Bands = c.Bands;
        result.YMax = Mathf.Max(1f, Mathf.Ceil(max * 1.12f * 2f) / 2f);
        if (c.UsesIron) result.IronLevel = IronLevelText(c.Iron);
        return result;
    }

    // Iron of the deposit of a stone (0 for a synthetic), shared with the other laboratory instruments.
    public static float DepositIron(GemInstance gem)
    {
        if (gem.IsSynthetic || gem.Definition.forcedSynthetic) return 0f;
        return IronOfDeposit(new Ctx { Gem = gem });
    }

    // ---- Reading of the curve ----------------------------------------------------------------------------------

    // Bands grouped by chromophore: "Cr3+ : 410, 555, 694 nm".
    public static List<string> BandLines(UvVisNirResult result)
    {
        var lines = new List<string>();
        if (result.Opaque)
        {
            lines.Add("Aucune transmission : l'échantillon est opaque.");
            return lines;
        }

        var labels = new List<string>();
        var centers = new Dictionary<string, List<float>>();
        var strongest = new Dictionary<string, float>();
        foreach (var b in result.Bands)
        {
            if (b.Label == null || b.Height < 0.1f) continue;
            if (!centers.ContainsKey(b.Label))
            {
                centers[b.Label] = new List<float>();
                strongest[b.Label] = 0f;
                labels.Add(b.Label);
            }
            centers[b.Label].Add(b.Center);
            strongest[b.Label] = Mathf.Max(strongest[b.Label], b.Height);
        }

        labels.Sort((a, b) => strongest[b].CompareTo(strongest[a]));
        foreach (var label in labels)
        {
            var list = centers[label];
            list.Sort();
            var sb = new StringBuilder(label).Append(" : ");
            for (var i = 0; i < list.Count && i < 5; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Mathf.RoundToInt(list[i]));
            }
            sb.Append(" nm");
            lines.Add(sb.ToString());
        }

        if (lines.Count == 0) lines.Add("Aucune bande marquée : spectre plat dans le visible.");
        return lines;
    }

    // Trace elements read from the bands, strongest first.
    public static string TraceElements(UvVisNirResult result)
    {
        var order = new List<string>();
        var strongest = new Dictionary<string, float>();
        foreach (var b in result.Bands)
        {
            if (string.IsNullOrEmpty(b.Element) || b.Height < 0.1f) continue;
            foreach (var symbol in b.Element.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!strongest.ContainsKey(symbol))
                {
                    strongest[symbol] = 0f;
                    order.Add(symbol);
                }
                strongest[symbol] = Mathf.Max(strongest[symbol], b.Height);
            }
        }

        if (order.Count == 0) return "aucun détecté";
        order.Sort((a, b) => strongest[b].CompareTo(strongest[a]));
        return string.Join(", ", order);
    }

    private static string IronLevelText(float iron)
    {
        if (iron < 0.05f) return "non détecté";
        if (iron < 0.3f) return "très faible";
        if (iron < 0.6f) return "faible";
        if (iron < 0.9f) return "modéré";
        return "élevé";
    }

    // ---- Physics of the stones ---------------------------------------------------------------------------------

    // Iron of the deposit: Kashmir, Mogok and Muzo are poor in it, basalt-related deposits (Thailand, Australia, Nigeria) are rich.
    private static float IronOfDeposit(Ctx c)
    {
        if (c.Has("Cachemire")) return 0.12f;
        if (c.Has("Colombie")) return 0.08f;
        if (c.Has("Birmanie")) return 0.15f;
        if (c.Has("Russie")) return 0.25f;
        if (c.Has("Sri Lanka")) return 0.35f;
        if (c.Has("Vietnam")) return 0.4f;
        if (c.Has("Afghanistan")) return 0.45f;
        if (c.Has("Br")) return 0.5f;       // Brésil
        if (c.Has("Tanzanie")) return 0.6f;
        if (c.Has("Madagascar")) return 0.7f;
        if (c.Has("Mozambique")) return 0.75f;
        if (c.Has("Zambie")) return 0.9f;
        if (c.Has("Nigeria")) return 0.95f;
        if (c.Has("Tha")) return 1.0f;      // Thaïlande
        if (c.Has("Cambodge")) return 1.0f;
        if (c.Has("Australie")) return 1.15f;
        return 0.5f;
    }

    // Fundamental absorption edge: the wavelength under which the host crystal itself absorbs.
    private static float EdgeOf(Ctx c)
    {
        var n = c.Name;
        if (n.StartsWith("Diamant")) return 230f;
        if (n.StartsWith("Corindon") || n.StartsWith("Saphir") || n == "Padparadscha" || n.StartsWith("Spinelle")) return 300f;
        if (n == "Emeraude" || n == "Goshenite" || n == "Morganite" || n == "Heliodore" || n == "Beryl_Rouge" || n == "Pezzottaite") return 310f;
        if (n.StartsWith("Quartz") || n == "Amethyste" || n == "Citrine" || n == "Ametrine" || n == "Prasiolite") return 275f;
        if (n == "Topaze" || n == "Fluorite") return 260f;
        if (n == "CZ") return 290f;
        if (n == "YAG") return 250f;
        if (n == "GGG") return 270f;
        if (n == "Moissanite_Synthetique") return 385f;
        if (n.StartsWith("Grenat")) return n == "Grenat_Demantoide" ? 410f : 385f;
        if (n.StartsWith("Tourmaline")) return 380f;
        if (n == "Zircon") return 330f;
        if (n == "Peridot") return 345f;
        if (n.StartsWith("Jade")) return 380f;
        if (n.StartsWith("Ivoire") || n == "Defense_Narval" || n == "Ecaille_Tortue" || n == "Corne_Rhinoceros") return 400f;
        if (n == "Opale_Noble" || n == "Opale_De_Feu") return 380f;
        return 340f;
    }

    private static void Recipe(Ctx c)
    {
        var t = c.Tone;
        switch (c.Name)
        {
            // ---- Corundum ----
            case "Corindon_Rubis":
            case "Corindon_Synthetique_Rubis":
                Ruby(c, 1f);
                break;
            case "Saphir_Rose":
                Ruby(c, 0.45f);
                break;
            case "Saphir_Bleu":
            case "Corindon_Synthetique_Saphir":
                c.UsesIron = true;
                c.Add(575, 150, 1.3f * t, "Fe2+/Ti4+", "Fe, Ti");
                CorundumIron(c, 1f);
                c.Add(880, 220, 0.8f * c.Iron * c.Iron, "Fe2+/Fe3+", "Fe");
                c.SkipColor = true;
                break;
            case "Saphir_Jaune":
                c.UsesIron = true;
                if (c.Synthetic) c.Add(430, 70, 1.0f * t, "Ni3+", "Ni");
                else CorundumIron(c, 1.9f);
                c.Slope(0.5f * t, 300f, 110f);
                c.SkipColor = true;
                break;
            case "Saphir_Orange":
            case "Padparadscha":
                c.UsesIron = true;
                c.Add(555, 70, 0.5f * t, "Cr3+", "Cr");
                c.Add(694, 3.5f, 0.25f * t, "Cr3+", "Cr");
                c.Add(450, 60, 0.9f * t, "Centres colorés", null);
                CorundumIron(c, 0.8f);
                c.SkipColor = true;
                break;
            case "Saphir_Vert":
                c.UsesIron = true;
                CorundumIron(c, 1.8f);
                c.Add(600, 140, 0.6f * t, "Fe2+/Ti4+", "Fe, Ti");
                c.SkipColor = true;
                break;
            case "Saphir_Violet":
                c.UsesIron = true;
                c.Add(570, 130, 0.7f * t, "Fe2+/Ti4+", "Fe, Ti");
                c.Add(550, 70, 0.4f * t, "Cr3+", "Cr");
                CorundumIron(c, 0.9f);
                c.SkipColor = true;
                break;
            case "Saphir_Incolore":
                c.UsesIron = true;
                CorundumIron(c, 0.4f);
                c.SkipColor = true;
                break;

            // ---- Beryl ----
            case "Emeraude":
                c.UsesIron = true;
                c.Add(430, 75, 0.9f * t, "Cr3+/V3+", "Cr, V");
                c.Add(610, 100, 1.15f * t, "Cr3+/V3+", "Cr, V");
                c.Add(683, 4, 0.35f * t, "Cr3+", "Cr");
                c.Add(662, 6, 0.25f * t, "Cr3+", "Cr");
                c.Add(810, 130, 1.0f * c.Iron, "Fe2+", "Fe");
                c.Add(370, 18, 0.5f * c.Iron, "Fe3+", "Fe");
                c.SkipColor = true;
                break;
            case "Goshenite":
                c.UsesIron = true;
                c.Add(370, 20, 0.3f * c.Iron, "Fe3+", "Fe");
                c.SkipColor = true;
                break;
            case "Morganite":
            case "Pezzottaite":
                c.Add(500, 90, 0.8f * t, "Mn3+", "Mn");
                c.SkipColor = true;
                break;
            case "Beryl_Rouge":
                c.Add(550, 110, 1.2f * t, "Mn3+", "Mn");
                c.SkipColor = true;
                break;
            case "Heliodore":
                c.UsesIron = true;
                c.Add(375, 30, 0.7f * (0.4f + c.Iron), "Fe3+", "Fe");
                c.Add(430, 25, 0.35f * (0.4f + c.Iron), "Fe3+", "Fe");
                c.SkipColor = true;
                break;

            // ---- Diamond ----
            case "Diamant":
                Diamond(c);
                break;
            case "Diamant_Synthetique_HPHT":
                SyntheticDiamond(c, false);
                break;
            case "Diamant_Synthetique_CVD":
                SyntheticDiamond(c, true);
                break;
            case "Moissanite_Synthetique":
                c.Slope(0.35f, 385f, 90f);
                c.SkipColor = true;
                break;

            // ---- Spinel ----
            case "Spinelle":
                c.UsesIron = true;
                c.Add(400, 60, 0.6f * t, "Cr3+", "Cr");
                c.Add(545, 55, 1.1f * t, "Cr3+", "Cr");
                c.Add(685, 3, 0.4f * t, "Cr3+", "Cr");
                c.Add(675, 3, 0.3f * t, "Cr3+", "Cr");
                c.Add(460, 25, 0.3f * c.Iron, "Fe2+", "Fe");
                c.SkipColor = true;
                break;
            case "Spinelle_Bleu":
            {
                c.UsesIron = true;
                // Cobalt blue spinels (Vietnam, Tanzania) against the iron-colored ones of Sri Lanka.
                var cobalt = c.Has("Sri Lanka") ? 0.4f : 1f;
                c.Add(545, 26, 0.9f * t * cobalt, "Co2+", "Co");
                c.Add(585, 28, 1.1f * t * cobalt, "Co2+", "Co");
                c.Add(625, 26, 1.0f * t * cobalt, "Co2+", "Co");
                c.Add(460, 20, 0.6f * (0.3f + c.Iron), "Fe2+", "Fe");
                c.Add(480, 20, 0.45f * (0.3f + c.Iron), "Fe2+", "Fe");
                c.SkipColor = true;
                break;
            }

            // ---- Garnet ----
            case "Grenat_Almandin":
                AlmandineBands(c, 1f);
                break;
            case "Grenat_Rhodolite":
                AlmandineBands(c, 0.7f);
                c.Add(420, 60, 0.3f * t, "Cr3+/V3+", "Cr, V");
                break;
            case "Grenat_Pyrope":
                c.Add(570, 90, 0.9f * t, "Cr3+", "Cr");
                c.Add(410, 60, 0.6f * t, "Cr3+", "Cr");
                c.Add(527, 18, 0.25f * t, "Fe2+", "Fe");
                c.Add(576, 18, 0.25f * t, "Fe2+", "Fe");
                c.SkipColor = true;
                break;
            case "Grenat_Spessartite":
            case "Grenat_Malaya":
            {
                var mn = c.Name == "Grenat_Spessartite" ? 1f : 0.55f;
                c.Add(410, 6, 0.8f * t * mn, "Mn2+", "Mn");
                c.Add(420, 6, 0.9f * t * mn, "Mn2+", "Mn");
                c.Add(432, 6, 1.0f * t * mn, "Mn2+", "Mn");
                c.Add(462, 8, 0.55f * t * mn, "Mn2+", "Mn");
                c.Add(483, 10, 0.5f * t * mn, "Mn2+", "Mn");
                c.Add(504, 10, 0.3f * t * mn, "Mn2+", "Mn");
                c.Add(527, 18, 0.25f * t, "Fe2+", "Fe");
                c.Add(576, 18, 0.25f * t, "Fe2+", "Fe");
                c.SkipColor = true;
                break;
            }
            case "Grenat_Tsavorite":
                c.Add(420, 70, 0.9f * t, "V3+/Cr3+", "V, Cr");
                c.Add(610, 100, 1.0f * t, "V3+/Cr3+", "V, Cr");
                c.Add(695, 3, 0.15f * t, "Cr3+", "Cr");
                c.SkipColor = true;
                break;
            case "Grenat_Demantoide":
                c.Add(440, 55, 1.5f * t, "Fe3+", "Fe");
                c.Add(690, 5, 0.25f * t, "Cr3+", "Cr");
                c.Add(701, 4, 0.2f * t, "Cr3+", "Cr");
                c.SkipColor = true;
                break;
            case "Grenat_Hessonite":
                c.Add(407, 10, 0.5f * t, "Fe3+", "Fe");
                c.Add(430, 14, 0.6f * t, "Fe3+", "Fe");
                c.Add(480, 90, 0.3f * t, "Mn2+", "Mn");
                break;
            case "Grenat_Hydrogrossulaire":
                c.Add(610, 100, 0.9f * t, "Cr3+", "Cr");
                c.SkipColor = true;
                break;

            // ---- Others ----
            case "Peridot":
                c.Add(453, 12, 0.55f, "Fe2+", "Fe");
                c.Add(473, 12, 0.75f, "Fe2+", "Fe");
                c.Add(493, 12, 0.5f, "Fe2+", "Fe");
                c.Add(1050, 330, 0.9f, "Fe2+", "Fe");
                c.SkipColor = true;
                break;
            case "Chrysoberyl":
                c.Add(440, 20, 1.1f * t, "Fe3+", "Fe");
                c.Slope(0.4f * t, 300f, 90f);
                c.SkipColor = true;
                break;
            case "Chrysoberyl_Alexandrite":
            {
                var cr = c.Has("Russie") ? 1.2f : 1f;
                c.Add(420, 90, 1.3f * t * cr, "Cr3+", "Cr");
                c.Add(585, 75, 1.8f * t * cr, "Cr3+", "Cr");
                c.Add(680, 3.5f, 0.5f * t, "Cr3+", "Cr");
                c.Add(655, 10, 0.3f * t, "Cr3+", "Cr");
                c.Add(445, 15, 0.3f * t, "Fe3+", "Fe");
                c.SkipColor = true;
                break;
            }
            case "Diopside_Chromifere":
                c.Add(505, 60, 0.9f * t, "Cr3+", "Cr");
                c.Add(690, 8, 0.5f * t, "Cr3+", "Cr");
                c.Add(655, 20, 0.3f * t, "Cr3+", "Cr");
                c.SkipColor = true;
                break;
            case "Apatite":
                c.Add(512, 6, 0.45f, "Nd3+", "Nd");
                c.Add(525, 6, 0.35f, "Nd3+", "Nd");
                c.Add(580, 6, 0.7f, "Nd3+", "Nd");
                c.Add(740, 8, 0.4f, "Nd3+", "Nd");
                c.Add(800, 10, 0.5f, "Nd3+", "Nd");
                c.Add(865, 12, 0.45f, "Nd3+", "Nd");
                break;
            case "Iolite":
                c.Add(435, 30, 0.7f * t, "Fe2+/Fe3+", "Fe");
                c.Add(585, 40, 1.0f * t, "Fe2+/Fe3+", "Fe");
                c.Add(880, 180, 0.7f * t, "Fe2+", "Fe");
                c.SkipColor = true;
                break;
            case "Kyanite":
                c.Add(600, 150, 1.0f * t, "Fe2+/Ti4+", "Fe, Ti");
                c.SkipColor = true;
                break;
            case "Tanzanite":
                c.Add(595, 90, 1.2f * t, "V3+", "V");
                c.Add(455, 40, 0.5f * t, "V3+", "V");
                c.Add(528, 40, 0.4f * t, "V3+", "V");
                c.SkipColor = true;
                break;
            case "Zircon":
                c.Add(653.5f, 1.5f, 0.8f, "U4+", "U");
                c.Add(660, 2f, 0.3f, "U4+", "U");
                c.Add(621.5f, 2f, 0.45f, "U4+", "U");
                c.Add(589, 2f, 0.4f, "U4+", "U");
                c.Add(562, 2f, 0.35f, "U4+", "U");
                c.Add(537.5f, 2f, 0.3f, "U4+", "U");
                c.Add(484, 2f, 0.25f, "U4+", "U");
                break;
            case "Tourmaline_Paraiba":
            {
                // The Brazilian Paraiba is richer in manganese than the African ones.
                var mn = c.Has("Br") ? 1f : 0.25f;
                c.Add(695, 170, 1.1f * t, "Cu2+", "Cu");
                c.Add(900, 250, 0.6f * t, "Cu2+", "Cu");
                c.Add(520, 60, 0.9f * t * mn, "Mn3+", "Mn");
                c.SkipColor = true;
                break;
            }
            case "Tourmaline_Rose":
                c.Add(520, 70, 1.1f * t, "Mn3+", "Mn");
                c.SkipColor = true;
                break;
            case "Jade_Jadeite":
                c.Add(437, 6, 0.5f, "Fe3+", "Fe");
                c.Add(630, 15, 0.4f * t, "Cr3+", "Cr");
                c.Add(655, 10, 0.4f * t, "Cr3+", "Cr");
                c.Add(691, 8, 0.3f * t, "Cr3+", "Cr");
                c.SkipColor = true;
                break;
            case "Jade_Nephrite":
                c.Add(720, 350, 0.9f * t, "Fe2+", "Fe");
                c.Add(450, 60, 0.5f, "Fe3+", "Fe");
                c.SkipColor = true;
                break;
            case "Amethyste":
            case "Ametrine":
                c.Add(545, 120, 1.0f * t, "Fe4+ (centre coloré)", "Fe");
                if (c.Name == "Ametrine") c.Slope(0.7f * t, 300f, 120f);
                c.SkipColor = true;
                break;
            case "Citrine":
                c.Add(420, 110, 1.0f * t, "Fe3+", "Fe");
                c.Slope(0.5f * t, 300f, 100f);
                c.SkipColor = true;
                break;
            case "Quartz_Fume":
                c.Add(450, 250, 0.9f * t, "Centre à aluminium", "Al");
                c.SkipColor = true;
                break;
            case "Quartz_Rose":
                c.Add(500, 100, 0.4f * t, null, null);
                c.SkipColor = true;
                break;
            case "Prasiolite":
                c.Add(740, 150, 0.5f * t, "Fe2+", "Fe");
                c.Add(450, 80, 0.3f * t, "Fe3+", "Fe");
                c.SkipColor = true;
                break;
            case "Rhodonite":
                c.Add(503, 10, 0.4f, "Mn2+", "Mn");
                c.Add(545, 50, 0.7f * t, "Mn2+", "Mn");
                c.Add(412, 15, 0.5f, "Mn2+", "Mn");
                c.SkipColor = true;
                break;
            case "Rhodocrosite":
                c.Add(410, 20, 0.35f, "Mn2+", "Mn");
                c.Add(550, 60, 0.45f * t, "Mn2+", "Mn");
                c.SkipColor = true;
                break;
            case "Verre":
                // Red glass is colored by colloidal particles: an absorption edge instead of bands.
                if (c.Gem.ColorHue < 25f || c.Gem.ColorHue > 335f)
                {
                    for (var i = 0; i < c.A.Length; i++)
                        c.A[i] += 2.5f / (1f + Mathf.Exp((UvVisNirResult.MinNm + i - 585f) / 10f));
                    c.SkipColor = true;
                }
                break;
            default:
                // No recipe: the lines the handheld spectroscope shows, and the body color.
                foreach (var line in SpectrumLibrary.Lines(c.Def))
                    c.Add(line.wavelengthNm, Mathf.Max(3f, line.widthNm), 0.4f, null, null);
                break;
        }
    }

    private static void CorundumIron(Ctx c, float scale)
    {
        var fe = c.Iron * scale;
        c.Add(377, 7, 0.5f * fe, "Fe3+", "Fe");
        c.Add(388, 7, 0.55f * fe, "Fe3+", "Fe");
        c.Add(450, 10, 0.95f * fe, "Fe3+", "Fe");
    }

    private static void Ruby(Ctx c, float strength)
    {
        c.UsesIron = true;
        var cr = 0.85f * c.Tone * strength * (c.Has("Birmanie") ? 1.2f : 1f);
        c.Add(410, 70, 1.1f * cr, "Cr3+", "Cr");
        c.Add(555, 65, 1.4f * cr, "Cr3+", "Cr");
        c.Add(694, 3.5f, 0.55f * cr, "Cr3+", "Cr");
        c.Add(692, 3f, 0.4f * cr);
        c.Add(668, 7f, 0.35f * cr, "Cr3+", "Cr");
        c.Add(659, 5f, 0.25f * cr);
        CorundumIron(c, 1f);
        c.Add(880, 220, 0.25f * c.Iron * c.Iron, "Fe2+/Fe3+", "Fe");
        c.SkipColor = true;
    }

    private static void AlmandineBands(Ctx c, float scale)
    {
        var t = c.Tone * scale;
        c.Add(504, 18, 0.8f * t, "Fe2+", "Fe");
        c.Add(527, 18, 1.2f * t, "Fe2+", "Fe");
        c.Add(576, 18, 1.4f * t, "Fe2+", "Fe");
        c.Add(617, 14, 0.35f * t, "Fe2+", "Fe");
        c.Add(680, 12, 0.2f * t, "Fe2+", "Fe");
        c.Add(740, 18, 0.15f * t, "Fe2+", "Fe");
        c.Add(900, 350, 0.5f * t, "Fe2+", "Fe");
        c.SkipColor = true;
    }

    // Natural diamond: cape series (N3 and its neighbours) in the violet, none in the rare type IIa. Fancy colors have their own
    // signatures, and the treatments leave centers behind (H3 after an HPHT anneal, GR1 after irradiation).
    private static void Diamond(Ctx c)
    {
        var g = c.Gem;
        c.SkipColor = true;
        if (c.Synthetic)
        {
            SyntheticDiamond(c, StoneOptics.StableHash((g.SpecimenId ?? "") + "|synth") % 2 == 0);
            return;
        }

        var typeIIa = DiamondSure.DiamondTypeOf(g) == "IIa";
        var annealed = g.HasTreatment(TreatmentKind.Heated) || g.HasTreatment(TreatmentKind.Hpht);

        if (g.IsFancyDiamond)
        {
            var hue = g.ColorHue;
            if (hue < 20f || hue > 330f)
            {
                c.Add(550, 110, 0.7f * c.Tone, "Centres colorés", null);
                c.Add(575, 5, 0.25f, "NV0", "N");
                c.Add(637, 5, 0.3f, "NV-", "N");
            }
            else if (hue < 50f)
            {
                // Brown to reddish brown: absorption growing toward the blue, and the faint band at 776 nm.
                c.Slope(14f, 450f, 110f);
                c.Add(776, 12, 0.15f, "776 nm", null);
            }
            else if (hue < 75f)
            {
                c.Slope(1.6f * c.Tone, 300f, 150f);
            }
            else if (hue < 170f)
            {
                c.Add(741, 3, 0.5f, "GR1", "V");
                c.Add(503, 3, 0.3f, "H3", "N");
                c.Slope(0.6f, 300f, 120f);
            }
            else if (hue < 260f)
            {
                // Blue: boron, an absorption that grows toward the infrared.
                c.Add(1150, 600, 1.2f * c.Tone, "Bore", "B");
            }
            else
            {
                c.Add(560, 100, 0.8f * c.Tone, "Centres colorés", null);
            }
        }

        if (!typeIIa)
        {
            var cape = (0.25f + 1.8f * g.ColorSaturation) * (annealed ? 0.35f : 1f);
            c.Add(415.2f, 3f, 0.9f * cape, "N3 (cape)", "N");
            c.Add(423f, 3f, 0.3f * cape, "N3 (cape)", "N");
            c.Add(435.8f, 3f, 0.5f * cape, "N3 (cape)", "N");
            c.Add(452f, 4f, 0.2f * cape, "N3 (cape)", "N");
            c.Add(465f, 6f, 0.18f * cape, "N3 (cape)", "N");
            c.Add(478f, 6f, 0.25f * cape, "N3 (cape)", "N");
            c.Slope(0.18f * cape, 300f, 90f);
        }

        if (annealed)
        {
            c.Add(503.2f, 3f, 0.55f, "H3 (recuit HPHT)", "N");
            c.Add(986f, 5f, 0.22f, "H2", "N");
        }
        else if (g.HasTreatment(TreatmentKind.Irradiated))
        {
            c.Add(741f, 3f, 0.55f, "GR1 (irradiation)", "V");
            c.Add(594f, 5f, 0.2f, "Centre à 594 nm", null);
            c.Add(503.2f, 3f, 0.25f, "H3", "N");
        }
    }

    private static void SyntheticDiamond(Ctx c, bool cvd)
    {
        c.SkipColor = true;
        if (cvd)
        {
            c.Add(737f, 2.5f, 0.4f, "SiV", "Si");
            c.Slope(0.3f, 300f, 110f);
        }
        else
        {
            c.Add(658f, 4f, 0.18f, "Nickel", "Ni");
            c.Add(885f, 8f, 0.28f, "Nickel", "Ni");
            c.Slope(0.5f, 300f, 130f);
        }
    }

    // Body color as broad absorption where the complementary light goes.
    private static void ColorBands(Ctx c)
    {
        var g = c.Gem;
        if (!g.HasColor || g.ColorSaturation < 0.06f) return;

        var h = g.ColorHue;
        var amount = (0.15f + 1.6f * g.ColorSaturation) * (1.1f - 0.45f * g.ColorBrightness);
        if (h < 15f || h >= 345f) c.Add(525, 90, amount);
        else if (h < 45f) c.Add(475, 100, amount);
        else if (h < 75f) c.Slope(amount * 1.4f, 300f, 110f);
        else if (h < 105f)
        {
            c.Add(430, 80, amount);
            c.Add(700, 150, amount * 0.3f);
        }
        else if (h < 165f)
        {
            c.Add(435, 70, amount);
            c.Add(645, 80, amount);
        }
        else if (h < 200f)
        {
            c.Add(650, 100, amount);
            c.Add(430, 70, amount * 0.4f);
        }
        else if (h < 255f) c.Add(610, 110, amount);
        else if (h < 285f) c.Add(570, 90, amount);
        else c.Add(540, 90, amount);
    }

    // Treatments seen in the visible: color centers made by irradiation.
    private static void Treatment(Ctx c)
    {
        if (c.Name.StartsWith("Diamant")) return;   // handled with the diamond signatures
        // The treatments that leave a band: a dye, the colour centres of an irradiation, the layer of a coating.
        var lines = TreatmentSignatures.TreatmentLines(c.Gem);
        if (lines.Count > 0)
        {
            foreach (var line in lines) c.Add(line.Nm, Mathf.Max(4f, line.Width), 0.45f, line.Label, null);
            return;
        }
        if (c.Gem.HasTreatment(TreatmentKind.Irradiated) && !c.Synthetic)
        {
            c.Add(c.Gem.ColorHue > 200f && c.Gem.ColorHue < 280f ? 620f : 470f, 130f, 0.6f, "Centres colorés", null);
        }
    }

    private static void AddNoise(Ctx c)
    {
        var raw = new float[c.A.Length];
        for (var i = 0; i < raw.Length; i++)
        {
            var x = UvVisNirResult.MinNm + i;
            var sigma = 0.006f + (x < 360f ? 0.035f * (360f - x) / 60f : 0f) + (x > 960f ? 0.01f * (x - 960f) / 40f : 0f);
            raw[i] = c.A[i] + Gauss(c.Rng) * sigma;
        }

        // The instrument averages neighbouring points (boxcar).
        for (var i = 0; i < raw.Length; i++)
        {
            var a = raw[Mathf.Max(0, i - 1)];
            var b = raw[i];
            var d = raw[Mathf.Min(raw.Length - 1, i + 1)];
            c.A[i] = (a + b + d) / 3f;
        }
    }

    private static float Gauss(System.Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}
