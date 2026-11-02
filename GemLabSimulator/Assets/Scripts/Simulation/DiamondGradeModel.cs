using System.Collections.Generic;
using UnityEngine;

// What the grading office of the Diamond Quarter finds when it classifies a diamond: color (D to Z, or fancy), clarity (FL to I3),
// cut, polish, symmetry, fluorescence and the way the stone grew. Official and fixed for a given stone: the player's job at the
// grading desk is to find it by comparison with the master stones.
public enum PlotKind { Point, Crystal, Cloud, Feather, Needle, Knot, Natural, Chip, Scratch, Abrasion, ExtraFacet }

// One symbol of a clarity plot: where it is on the top view (-1 to 1 from the centre, Y up) and what it is.
public struct PlotMark
{
    public PlotKind Kind;
    public float X;
    public float Y;
}

public class DiamondGrade
{
    public bool Fancy;
    public int ColorIndex;          // 0 = D ... 22 = Z, meaningless for a fancy stone
    public string ColorGrade;       // "G", or "Fancy"
    public string ColorText;        // "Presque incolore", "Fancy Vivid Pink"...
    public int ClarityIndex;        // 0 = FL ... 10 = I3
    public string ClarityGrade;
    public List<PlotMark> Plot;     // the inclusions (red) and surface marks (green) of the stone
    public int FluorescenceIndex;   // 0 = none ... 3 = strong
    public string Fluorescence;
    public string Growth;           // "Naturel" or "Synthétique (CVD)"...

    // Measurements.
    public float Carat;
    public float DiameterMax;
    public float DiameterMin;
    public float Ratio;
    public float DepthMm;
    public float CalculatedCarat;

    // Proportions, in % of the mean diameter.
    public float TablePercent;
    public float CrownPercent;
    public float GirdlePercent;
    public float PavilionPercent;
    public float CuletPercent;
    public int GirdleClass;
    public int CuletClass;
    public float DepthPercent;      // total depth: crown + girdle + pavilion
    public float CrownAngle;        // beta
    public float PavilionAngle;     // alpha
    public int ProportionRank;      // 0 = Excellent ... 3 = Moyen

    // Symmetry and polish: a level (0 none ... 3 strong) and the measure behind it, for each characteristic.
    public int[] SymmetryLevels;
    public float[] SymmetryMeasures;
    public int[] PolishLevels;
    public float[] PolishMeasures;
    public int SymmetryRank;
    public int PolishRank;

    public int CutRank => Mathf.Max(ProportionRank, Mathf.Max(SymmetryRank, PolishRank));
    public string Proportions => DiamondGradeModel.CutScale[ProportionRank];
    public string Symmetry => DiamondGradeModel.CutScale[SymmetryRank];
    public string Polish => DiamondGradeModel.CutScale[PolishRank];
    public string Cut => DiamondGradeModel.CutScale[CutRank];
}

public static class DiamondGradeModel
{
    public static readonly string[] CutScale = { "Excellent", "Très bon", "Bon", "Moyen" };
    public static readonly string[] CutShort = { "EX", "VG", "G", "F" };
    public static readonly string[] FluorescenceScale = { "Aucune", "Faible", "Moyenne", "Forte" };
    public static readonly string[] FluorescenceEnglish = { "None", "Slight", "Medium", "Strong" };
    public static readonly string[] LevelNames = { "Aucune", "Faible", "Moyenne", "Forte" };
    public static readonly string[] GirdleNames = { "Extrêmement mince", "Très mince", "Mince", "Moyenne", "Épaisse", "Très épaisse", "Extrêmement épaisse" };
    public static readonly float[] GirdleValues = { 0.4f, 0.8f, 1.5f, 3.0f, 4.5f, 6.5f, 9.0f };
    public static readonly string[] CuletNames = { "Pointue", "Très petite", "Petite", "Moyenne", "Grande", "Très grande" };
    public static readonly float[] CuletValues = { 0f, 1.0f, 2.0f, 3.5f, 5.0f, 7.0f };

    public static readonly string[] SymmetryItems =
    {
        "Hors rond", "Table décentrée", "Colette décentrée", "Table et colette non parallèles", "Ceinture ondulée",
        "Hauteur de couronne variable", "Angle de couronne variable", "Angle de pavillon variable", "Épaisseur de ceinture variable",
        "Longueur des étoiles variable", "Longueur des facettes de ceinture variable", "Facettes mal alignées", "Table non octogonale",
    };

    public static readonly string[] PolishItems =
    {
        "Traits de polissage", "Rayures", "Abrasion", "Brûlures de polissage", "Ceinture rugueuse", "Cavités de surface", "Écailles", "Peau de lézard",
    };

    private static readonly float[] CutWeights = { 34f, 36f, 22f, 8f };
    private static readonly float[] FluorescenceWeights = { 68f, 16f, 11f, 5f };
    private static readonly int[] InternalCount = { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    private static readonly int[] ExternalCount = { 0, 2, 0, 1, 1, 1, 2, 2, 2, 3, 3 };

    private static readonly float[][] GirdleWeights =
    {
        new[] { 0f, 2f, 30f, 40f, 24f, 4f, 0f },
        new[] { 1f, 6f, 28f, 34f, 22f, 8f, 1f },
        new[] { 3f, 12f, 24f, 28f, 20f, 10f, 3f },
        new[] { 8f, 16f, 18f, 20f, 16f, 14f, 8f },
    };

    private static readonly float[][] CuletWeights =
    {
        new[] { 40f, 40f, 18f, 2f, 0f, 0f },
        new[] { 25f, 35f, 28f, 10f, 2f, 0f },
        new[] { 15f, 25f, 30f, 20f, 8f, 2f },
        new[] { 8f, 15f, 25f, 25f, 17f, 10f },
    };

    private static readonly float[][] LevelWeights =
    {
        new[] { 0.88f, 0.10f, 0.02f, 0.00f },
        new[] { 0.70f, 0.22f, 0.07f, 0.01f },
        new[] { 0.50f, 0.28f, 0.17f, 0.05f },
        new[] { 0.30f, 0.30f, 0.25f, 0.15f },
    };

    public static bool CanGrade(GemInstance gem)
    {
        return gem != null && !gem.IsRough && (gem.Definition.name ?? "").StartsWith("Diamant");
    }

    // Fee of the office: the same for every diamond.
    public const int CertificateFee = 50;

    public static int Fee(GemInstance gem)
    {
        return CertificateFee;
    }

    // ---- Master stones ---------------------------------------------------------------------------------------------

    // The master stone of a color letter, drawn like the diamonds of the dealers: colorless to light yellow.
    public static void MasterColors(int index, out Color body, out Color facet)
    {
        var t = Mathf.Clamp01(index / 22f);
        var value = new GemColorValue(Mathf.Lerp(215f, 52f, Mathf.Clamp01(index / 5f)), Mathf.Lerp(0.02f, 0.62f, t * t), Mathf.Lerp(0.96f, 0.90f, t));
        body = GemColorSystem.BodyColor(value);
        facet = GemColorSystem.FacetColor(value);
    }

    public static Sprite MasterSprite(int index)
    {
        MasterColors(index, out var body, out var facet);
        return GemIconFactory.GetSolid("Rond", "Brillant", body, facet);
    }

    public static string ClarityDescription(string grade)
    {
        switch (grade)
        {
            case "FL": return "Aucune inclusion, aucun défaut de surface à 10×.";
            case "IF": return "Aucune inclusion à 10×, seulement de menus défauts de surface.";
            case "VVS1": return "Une inclusion minuscule, très très difficile à voir à 10×.";
            case "VVS2": return "Une ou deux inclusions minuscules, très difficiles à voir à 10×.";
            case "VS1": return "Quelques petites inclusions, difficiles à voir à 10×.";
            case "VS2": return "Petites inclusions plus nombreuses, assez faciles à voir à 10×.";
            case "SI1": return "Inclusions nettes à 10×, rarement visibles à l'œil nu.";
            case "SI2": return "Inclusions évidentes à 10×, parfois visibles à l'œil nu.";
            case "I1": return "Inclusions visibles à l'œil nu ; l'éclat est un peu altéré.";
            case "I2": return "Inclusions bien visibles à l'œil nu ; l'éclat est altéré.";
            default: return "Inclusions très visibles et fractures qui affectent la solidité.";
        }
    }

    // What the grader sees through the loupe, in a few words, for the grade the stone really has.
    public static List<string> ClarityClues(GemInstance gem, int clarityIndex)
    {
        var places = new[] { "près de la ceinture", "sous la table", "au centre du pavillon", "près de la colette", "sous une facette de la couronne" };
        var kinds = new[] { "un petit cristal", "un nuage de points", "une plume", "un point sombre", "un fin voile" };
        var rnd = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "|clues"));
        string Place() => places[rnd.Next(places.Length)];
        string Kind() => kinds[rnd.Next(kinds.Length)];

        var lines = new List<string>();
        switch (DiamondGrading.Clarity(clarityIndex))
        {
            case "FL":
                lines.Add("À 10× : rien à signaler, ni à l'intérieur ni en surface.");
                break;
            case "IF":
                lines.Add("À 10× : aucune inclusion à l'intérieur.");
                lines.Add("En surface : une petite rayure de poli " + Place() + ".");
                break;
            case "VVS1":
                lines.Add($"À 10× : {Kind()} minuscule {Place()}, vu seulement par le pavillon.");
                break;
            case "VVS2":
                lines.Add($"À 10× : {Kind()} minuscule {Place()}.");
                lines.Add($"Et {Kind()} presque invisible {Place()}.");
                break;
            case "VS1":
                lines.Add($"À 10× : {Kind()} {Place()}.");
                lines.Add($"Et {Kind()} {Place()}, difficile à repérer.");
                break;
            case "VS2":
                lines.Add($"À 10× : {Kind()} {Place()}.");
                lines.Add($"Et {Kind()} {Place()}.");
                lines.Add("Plusieurs autres points, assez faciles à voir.");
                break;
            case "SI1":
                lines.Add($"À 10× : {Kind()} net {Place()}.");
                lines.Add("Il faut une bonne loupe pour le trouver, pas l'œil nu.");
                break;
            case "SI2":
                lines.Add($"À 10× : {Kind()} évident {Place()}.");
                lines.Add("Une inclusion se devine parfois à l'œil nu, sous la table.");
                break;
            case "I1":
                lines.Add($"À l'œil nu : {Kind()} visible {Place()}.");
                lines.Add("L'éclat de la pierre est un peu altéré.");
                break;
            case "I2":
                lines.Add($"À l'œil nu : {Kind()} bien visible {Place()}, et d'autres.");
                lines.Add("L'éclat est altéré.");
                break;
            default:
                lines.Add("À l'œil nu : inclusions très visibles, fractures qui atteignent la surface.");
                lines.Add("La solidité de la pierre est en cause.");
                break;
        }
        return lines;
    }

    // ---- The official grade ------------------------------------------------------------------------------------------

    public static DiamondGrade Official(GemInstance gem)
    {
        var grade = new DiamondGrade();
        uint Hash(string salt) => StoneOptics.StableHash((gem.SpecimenId ?? "") + "|grade|" + salt);

        // Color.
        if (gem.IsFancyDiamond || gem.ColorGrade == "Fancy")
        {
            grade.Fancy = true;
            grade.ColorGrade = "Fancy";
            grade.ColorText = $"Fancy — {gem.ColorName}";
        }
        else
        {
            if (!string.IsNullOrEmpty(gem.ColorGrade) && DiamondGrading.ColorScale.IndexOf(gem.ColorGrade[0]) >= 0)
                grade.ColorIndex = DiamondGrading.ColorScale.IndexOf(gem.ColorGrade[0]);
            else if (gem.RoughColorIndex >= 0)
                grade.ColorIndex = Mathf.Clamp(gem.RoughColorIndex, 0, 22);
            else
                grade.ColorIndex = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(gem.ColorSaturation / 0.05f) * 12f), 0, 22);
            grade.ColorGrade = DiamondGrading.Color(grade.ColorIndex);
            grade.ColorText = DiamondGrading.ColorDescription(grade.ColorIndex);
        }

        // Clarity: a dealer's diamond has its grade; another stone gets one inside the tier it falls in (loupe clean, eye-clean...).
        if (!string.IsNullOrEmpty(gem.DiamondClarity) && System.Array.IndexOf(DiamondGrading.ClarityScale, gem.DiamondClarity) >= 0)
        {
            grade.ClarityIndex = System.Array.IndexOf(DiamondGrading.ClarityScale, gem.DiamondClarity);
        }
        else if (gem.RoughClarityIndex >= 0)
        {
            grade.ClarityIndex = Mathf.Clamp(gem.RoughClarityIndex, 0, DiamondGrading.ClarityScale.Length - 1);
        }
        else
        {
            var roll = Hash("clarity") % 100;
            switch (gem.ClarityGrade)
            {
                case "Loupe clean": grade.ClarityIndex = roll < 8 ? 0 : roll < 30 ? 1 : roll < 54 ? 2 : roll < 78 ? 3 : 4; break;
                case "Eye-clean": grade.ClarityIndex = roll < 50 ? 5 : 6; break;
                case "Inclus": grade.ClarityIndex = roll < 50 ? 7 : 8; break;
                default: grade.ClarityIndex = roll < 50 ? 9 : 10; break;
            }
        }
        grade.ClarityGrade = DiamondGrading.Clarity(grade.ClarityIndex);
        grade.Plot = MakePlot(gem, grade.ClarityIndex);

        // Fluorescence under long-wave ultraviolet.
        grade.FluorescenceIndex = PickIndex(FluorescenceWeights, Hash("fluo"));
        grade.Fluorescence = FluorescenceScale[grade.FluorescenceIndex];
        grade.Growth = !gem.IsSynthetic ? "Naturel"
            : DiamondViewImage.KindOf(gem) == DiamondViewKind.Cvd ? "Synthétique (CVD)"
            : DiamondViewImage.KindOf(gem) == DiamondViewKind.Hpht ? "Synthétique (HPHT)" : "Synthétique";

        // Measurements.
        float Unit(string salt) => (Hash(salt) % 1000) / 1000f;
        float Signed(string salt) => (Hash(salt) % 2001) / 1000f - 1f;
        grade.Carat = Mathf.Round(gem.CaratWeight * 100f) / 100f;
        grade.DiameterMax = Mathf.Round(Mathf.Max(gem.LengthMm, gem.WidthMm) * 100f) / 100f;
        var smaller = Mathf.Min(gem.LengthMm, gem.WidthMm);
        if (gem.ShapeName == "Rond" || grade.DiameterMax - smaller < 0.03f) smaller = grade.DiameterMax * (1f - 0.004f - Unit("round") * 0.012f);
        grade.DiameterMin = Mathf.Round(smaller * 100f) / 100f;
        grade.Ratio = Mathf.Round(grade.DiameterMax / Mathf.Max(grade.DiameterMin, 0.01f) * 100f) / 100f;
        grade.CalculatedCarat = Mathf.Round(grade.Carat * (1f + Signed("calc") * 0.025f) * 100f) / 100f;

        // Proportions: a quality level draws how far from the ideal each of them can stray.
        var q = gem.CutQualityIndex >= 0 ? Mathf.Clamp(gem.CutQualityIndex, 0, 3) : PickIndex(CutWeights, Hash("cut"));
        grade.TablePercent = Round1(57f + Signed("table") * (2.2f + q * 2.3f));
        grade.CrownPercent = Round1(14.4f + Signed("crown") * (0.9f + q * 0.8f));
        grade.GirdleClass = PickIndex(GirdleWeights[q], Hash("girdle"));
        grade.GirdlePercent = GirdleValues[grade.GirdleClass];
        grade.PavilionPercent = Round1(43.2f + Signed("pavilion") * (0.5f + q * 0.6f));
        grade.CuletClass = PickIndex(CuletWeights[q], Hash("culet"));
        grade.CuletPercent = CuletValues[grade.CuletClass];
        grade.DepthPercent = Round1(grade.CrownPercent + grade.GirdlePercent + grade.PavilionPercent);
        grade.CrownAngle = CrownAngle(grade.CrownPercent, grade.TablePercent);
        grade.PavilionAngle = PavilionAngle(grade.PavilionPercent, grade.CuletPercent);
        grade.ProportionRank = ProportionRank(grade.TablePercent, grade.CrownAngle, grade.PavilionAngle, grade.DepthPercent, grade.GirdleClass, grade.CuletClass);
        grade.DepthMm = Mathf.Round(grade.DepthPercent * (grade.DiameterMax + grade.DiameterMin) * 0.5f) / 100f;

        // Symmetry and polish.
        var symmetryQ = gem.CutQualityIndex >= 0 ? Neighbour(q, Hash("symmetry")) : PickIndex(CutWeights, Hash("symmetry"));
        grade.SymmetryLevels = new int[SymmetryItems.Length];
        grade.SymmetryMeasures = new float[SymmetryItems.Length];
        for (var i = 0; i < SymmetryItems.Length; i++)
        {
            grade.SymmetryLevels[i] = PickIndex(LevelWeights[symmetryQ], Hash("sym" + i));
            grade.SymmetryMeasures[i] = MeasureFor(grade.SymmetryLevels[i], Hash("symm" + i));
        }
        grade.SymmetryRank = RankFromLevels(grade.SymmetryLevels, 2, 5, 10);

        var polishQ = gem.CutQualityIndex >= 0 ? Neighbour(q, Hash("polish")) : PickIndex(CutWeights, Hash("polish"));
        grade.PolishLevels = new int[PolishItems.Length];
        grade.PolishMeasures = new float[PolishItems.Length];
        for (var i = 0; i < PolishItems.Length; i++)
        {
            grade.PolishLevels[i] = PickIndex(LevelWeights[polishQ], Hash("pol" + i));
            grade.PolishMeasures[i] = MeasureFor(grade.PolishLevels[i], Hash("polm" + i));
        }
        grade.PolishRank = RankFromLevels(grade.PolishLevels, 1, 3, 7);
        return grade;
    }

    // The polish and the symmetry of a stone follow the cutter's quality, one level better or worse now and then.
    private static int Neighbour(int quality, uint hash)
    {
        var roll = hash % 100;
        var shifted = roll < 15 ? quality - 1 : roll < 75 ? quality : quality + 1;
        return Mathf.Clamp(shifted, 0, 3);
    }

    private static float Round1(float value) => Mathf.Round(value * 10f) / 10f;

    // ---- Proportions -------------------------------------------------------------------------------------------------

    // Crown angle from the crown height and the table width (both in % of the diameter), as the HRD tables give it.
    public static float CrownAngle(float crownPercent, float tablePercent)
    {
        var run = Mathf.Max((100f - tablePercent) * 0.5f, 1f);
        return Mathf.Round(Mathf.Atan(crownPercent / run) * Mathf.Rad2Deg * 10f) / 10f;
    }

    // Pavilion angle from the pavilion depth and the culet size.
    public static float PavilionAngle(float pavilionPercent, float culetPercent)
    {
        var run = Mathf.Max((100f - culetPercent) * 0.5f, 1f);
        return Mathf.Round(Mathf.Atan(pavilionPercent / run) * Mathf.Rad2Deg * 10f) / 10f;
    }

    // Bands of a criterion: Excellent, Très bon and Bon ranges. Anything outside is Moyen.
    public static readonly float[][] TableBands = { new[] { 53f, 62f }, new[] { 51f, 65f }, new[] { 49f, 68f } };
    public static readonly float[][] CrownAngleBands = { new[] { 32f, 36f }, new[] { 30f, 38f }, new[] { 27f, 40f } };
    public static readonly float[][] PavilionAngleBands = { new[] { 40.2f, 41.8f }, new[] { 39.6f, 42.4f }, new[] { 38.8f, 43.4f } };
    public static readonly float[][] DepthBands = { new[] { 58f, 63.5f }, new[] { 56f, 65.5f }, new[] { 53f, 68f } };
    private static readonly int[] GirdleRanks = { 3, 1, 0, 0, 0, 1, 3 };
    private static readonly int[] CuletRanks = { 0, 0, 0, 1, 2, 3 };

    public static int BandRank(float value, float[][] bands)
    {
        for (var i = 0; i < bands.Length; i++)
            if (value >= bands[i][0] && value <= bands[i][1]) return i;
        return bands.Length;
    }

    public static int GirdleRank(int girdleClass) => GirdleRanks[Mathf.Clamp(girdleClass, 0, GirdleRanks.Length - 1)];
    public static int CuletRank(int culetClass) => CuletRanks[Mathf.Clamp(culetClass, 0, CuletRanks.Length - 1)];

    public static int ProportionRank(float table, float crownAngle, float pavilionAngle, float depth, int girdleClass, int culetClass)
    {
        var rank = BandRank(table, TableBands);
        rank = Mathf.Max(rank, BandRank(crownAngle, CrownAngleBands));
        rank = Mathf.Max(rank, BandRank(pavilionAngle, PavilionAngleBands));
        rank = Mathf.Max(rank, BandRank(depth, DepthBands));
        rank = Mathf.Max(rank, GirdleRank(girdleClass));
        return Mathf.Max(rank, CuletRank(culetClass));
    }

    // The girdle and culet classes that a thickness or a size in % falls into.
    public static int GirdleClassOf(float percent)
    {
        var best = 0;
        for (var i = 1; i < GirdleValues.Length; i++)
            if (Mathf.Abs(GirdleValues[i] - percent) < Mathf.Abs(GirdleValues[best] - percent)) best = i;
        return best;
    }

    public static int CuletClassOf(float percent)
    {
        var best = 0;
        for (var i = 1; i < CuletValues.Length; i++)
            if (Mathf.Abs(CuletValues[i] - percent) < Mathf.Abs(CuletValues[best] - percent)) best = i;
        return best;
    }

    // ---- Symmetry and polish -------------------------------------------------------------------------------------

    // The size of a deviation, in %, for a level: below 0.8 none, 0.8 to 2 slight, 2 to 3.5 medium, above strong.
    public static int LevelOfMeasure(float measure)
    {
        return measure < 0.8f ? 0 : measure < 2f ? 1 : measure < 3.5f ? 2 : 3;
    }

    private static float MeasureFor(int level, uint hash)
    {
        var u = (hash % 1000) / 1000f;
        switch (level)
        {
            case 0: return Round1(u * 0.6f);
            case 1: return Round1(1.0f + u * 0.8f);
            case 2: return Round1(2.3f + u * 1.0f);
            default: return Round1(3.8f + u * 1.6f);
        }
    }

    // A grade from the deviations found: a slight one counts 1, a medium one 2, a strong one 4.
    private static int RankFromLevels(int[] levels, int excellent, int veryGood, int good)
    {
        var score = 0;
        foreach (var level in levels) score += level == 3 ? 4 : level;
        return score <= excellent ? 0 : score <= veryGood ? 1 : score <= good ? 2 : 3;
    }

    // ---- Clarity plot ---------------------------------------------------------------------------------------------

    public static bool IsInternal(PlotKind kind) => kind <= PlotKind.Knot;

    public static string KindName(PlotKind kind)
    {
        switch (kind)
        {
            case PlotKind.Point: return "Point";
            case PlotKind.Crystal: return "Cristal";
            case PlotKind.Cloud: return "Nuage";
            case PlotKind.Feather: return "Plume";
            case PlotKind.Needle: return "Aiguille";
            case PlotKind.Knot: return "Nœud";
            case PlotKind.Natural: return "Naturelle";
            case PlotKind.Chip: return "Éclat";
            case PlotKind.Scratch: return "Rayure";
            case PlotKind.Abrasion: return "Abrasion";
            default: return "Facette en plus";
        }
    }

    public static string KindLoupe(PlotKind kind)
    {
        switch (kind)
        {
            case PlotKind.Point: return "un point blanc minuscule, à l'intérieur";
            case PlotKind.Crystal: return "un petit cristal enclavé dans la pierre";
            case PlotKind.Cloud: return "un groupe de points serrés, en nuage";
            case PlotKind.Feather: return "une fissure fine et plate, en forme de plume";
            case PlotKind.Needle: return "un trait fin et droit, comme une aiguille";
            case PlotKind.Knot: return "un cristal qui affleure la surface (nœud)";
            case PlotKind.Natural: return "un reste de peau brute sur la ceinture (naturelle)";
            case PlotKind.Chip: return "un petit éclat sur une arête";
            case PlotKind.Scratch: return "une rayure sur une facette";
            case PlotKind.Abrasion: return "de minuscules arêtes ébréchées (abrasion)";
            default: return "une petite facette en trop";
        }
    }

    // The inclusions (internal) and the surface marks of the stone, always at the same places for the same stone.
    public static List<PlotMark> MakePlot(GemInstance gem, int clarityIndex)
    {
        var rnd = new System.Random((int)StoneOptics.StableHash((gem.SpecimenId ?? "") + "|plot"));
        var marks = new List<PlotMark>();
        var index = Mathf.Clamp(clarityIndex, 0, InternalCount.Length - 1);
        var total = InternalCount[index] + ExternalCount[index];
        for (var i = 0; i < total; i++)
        {
            var internalMark = i < InternalCount[index];
            var kind = internalMark ? (PlotKind)rnd.Next(0, 6) : (PlotKind)rnd.Next(6, 11);
            var position = Vector2.zero;
            for (var attempt = 0; attempt < 60; attempt++)
            {
                var angle = (float)(rnd.NextDouble() * Mathf.PI * 2.0);
                var radius = Mathf.Sqrt((float)rnd.NextDouble()) * 0.78f;
                position = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                var clear = true;
                foreach (var other in marks)
                    if (Vector2.Distance(position, new Vector2(other.X, other.Y)) < 0.34f) clear = false;
                if (clear) break;
            }
            marks.Add(new PlotMark { Kind = kind, X = position.x, Y = position.y });
        }
        return marks;
    }

    private static int PickIndex(float[] weights, uint hash)
    {
        var total = 0f;
        foreach (var w in weights) total += w;
        var roll = (hash % 1000) / 1000f * total;
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f) return i;
        }
        return weights.Length - 1;
    }
}
