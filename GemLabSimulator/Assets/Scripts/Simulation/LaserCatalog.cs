using System.Collections.Generic;
using UnityEngine;

public enum LaserMethod { Classic, KM }
public enum LaserOutcome { Ruined, NoEffect, Weak, Good, Excellent }

public class LaserMethodInfo
{
    public string Name;
    public string Detail;
    public string Description;
    public int Fee;
    public float PowerMin, PowerMax;     // the window in which the laser does its job
    public float ValueDiscount;          // what a treated stone is worth against an untreated one of the same grade
    public int Days;
}

public struct LaserQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public int DarkCount;
    public int Treated;
    public float Reaction;           // 0..1, how well the power suits the method
    public float Risk;               // 0..1, chance that the stone is damaged
    public string Advice;

    public string ReactionLabel => Reaction < 0.3f ? "insuffisante" : Reaction < 0.7f ? "partielle" : "complète";
    public string RiskLabel => Risk < 0.08f ? "très faible" : Risk < 0.2f ? "faible" : Risk < 0.4f ? "modéré" : "élevé";
}

public struct LaserResult
{
    public LaserOutcome Outcome;
    public LaserMethod Method;
    public int GainGrades;           // grades of clarity won (negative: lost)
    public int NewClarityIndex;
    public float ValueFactor;
    public string Label;
    public string Description;
    public string Message;

    public bool IsRuined => Outcome == LaserOutcome.Ruined;
}

// The laser treatment of diamonds, as taught in the course: dark inclusions are made less visible. The classic way pierces a tiny hole (under
// 0.02 mm) with a laser beam and cuts a small channel to the inclusion, which is then bleached with a strong acid; the holes can be seen with a 10x
// loupe, looking at the stone from the side, and can be hidden by the setting. The KM laser (kiduah meyuhad, "small hole") heats the dark inclusion
// instead, which makes fissures that come out at the surface; acid then removes the black. It is not rare, and hard to detect without
// magnification: zig-zag and spiral effects. The stone that was treated loses its certificate: it has to be graded again.
public static class LaserCatalog
{
    public const int MinPower = 10;
    public const int MaxPower = 100;
    public const int PowerStep = 5;
    public const int MaxTreated = 3;

    public static readonly LaserMethodInfo[] Methods =
    {
        new LaserMethodInfo { Name = "Laser classique", Detail = "Trou foré + acide", Fee = 120, PowerMin = 40f, PowerMax = 75f, ValueDiscount = 0.90f, Days = 3,
            Description = "Un trou de moins de 0,02 mm, un canal jusqu'à l'inclusion, puis un acide fort qui la blanchit. Propre et bénin, mais les trous se voient à la loupe." },
        new LaserMethodInfo { Name = "Laser KM", Detail = "Fissures + acide", Fee = 60, PowerMin = 55f, PowerMax = 85f, ValueDiscount = 0.78f, Days = 2,
            Description = "Le laser chauffe l'inclusion : elle éclate en fissures qui atteignent la surface, puis l'acide la blanchit. Efficace, violent et désordonné : la pierre en sort fragilisée." },
    };

    private static float ClarityFactorOf(int index)
    {
        var current = DiamondGrading.Value(1f, 4, index);
        var reference = DiamondGrading.Value(1f, 4, 5);
        return current / Mathf.Max(1f, reference);
    }

    // The clarity grade of the stone as an index of the diamond scale (its hidden or certified one).
    public static int ClarityIndexOf(GemInstance gem) => DiamondGradeModel.Official(gem).ClarityIndex;

    private static bool IsDark(PlotKind kind) => kind == PlotKind.Point || kind == PlotKind.Crystal || kind == PlotKind.Knot;

    // The dark inclusions the laser can reach, at the same places as on the plot of the stone.
    public static List<PlotMark> DarkInclusions(GemInstance gem)
    {
        var dark = new List<PlotMark>();
        foreach (var mark in DiamondGradeModel.MakePlot(gem, ClarityIndexOf(gem)))
            if (DiamondGradeModel.IsInternal(mark.Kind) && IsDark(mark.Kind)) dark.Add(mark);
        return dark;
    }

    // All the inclusions the microscope of the station shows (internal and external), for the picture of the stone.
    public static List<PlotMark> AllMarks(GemInstance gem) => DiamondGradeModel.MakePlot(gem, ClarityIndexOf(gem));

    public static bool CanTreat(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go under the laser, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        if (!DiamondGradeModel.CanGrade(gem)) return "Le laser ne traite ici que les diamants taillés.";
        if (gem.IsSynthetic) return "Inutile de traiter un diamant synthétique.";
        if (gem.IsLaserTreated) return "Ce diamant a déjà été traité au laser : on ne perce pas deux fois la même pierre.";
        if (ClarityIndexOf(gem) <= 4) return "Ce diamant est trop pur : il n'a pas d'inclusion foncée qui vaille un traitement.";
        if (DarkInclusions(gem).Count == 0) return "Pas d'inclusion foncée accessible : les inclusions de cette pierre sont claires, le laser n'a rien à blanchir.";
        return null;
    }

    private static float PowerScore(LaserMethodInfo info, int power)
    {
        if (power >= info.PowerMin && power <= info.PowerMax) return 1f;
        if (power < info.PowerMin) return Mathf.Clamp01((power - 10f) / (info.PowerMin - 10f)) * 0.7f;
        return 1f;
    }

    private static float DamageRisk(LaserMethod method, int power, int treated)
    {
        var info = Methods[(int)method];
        var baseRisk = method == LaserMethod.KM ? 0.20f : 0.03f;
        if (power > info.PowerMax) baseRisk += (power - info.PowerMax) * (method == LaserMethod.KM ? 0.035f : 0.025f);
        baseRisk += (treated - 1) * (method == LaserMethod.KM ? 0.08f : 0.01f);
        return Mathf.Clamp01(baseRisk);
    }

    public static LaserQuote GetQuote(LaserMethod method, int power, int treated, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (refusal != null) return new LaserQuote { IsValid = false, Reason = refusal };

        var info = Methods[(int)method];
        var dark = DarkInclusions(gem).Count;
        treated = Mathf.Clamp(treated, 1, Mathf.Min(MaxTreated, dark));
        return new LaserQuote
        {
            IsValid = true,
            Days = info.Days,
            Fee = info.Fee + treated * (method == LaserMethod.KM ? 35 : 60) + Mathf.RoundToInt(gem.CaratWeight * 8f),
            DarkCount = dark,
            Treated = treated,
            Reaction = PowerScore(info, power),
            Risk = DamageRisk(method, power, treated),
            Advice = method == LaserMethod.KM
                ? "Le KM fait éclater l'inclusion : le résultat est plus net, mais la pierre peut se fissurer pour de bon. On voit ensuite des plumes en zig-zag ou en spirale."
                : "Le trou et le canal se voient à la loupe 10× en regardant la pierre par le côté. Le sertissage peut les cacher.",
        };
    }

    // What comes out of the laser. The same stone with the same settings always gives the same result.
    public static LaserResult Resolve(LaserMethod method, int power, int treated, GemInstance gem)
    {
        var quote = GetQuote(method, power, treated, gem);
        if (!quote.IsValid) return new LaserResult { Outcome = LaserOutcome.NoEffect, Method = method, ValueFactor = 1f, Message = quote.Reason };

        var info = Methods[(int)method];
        var current = ClarityIndexOf(gem);
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|laser|{method}|{power}|{quote.Treated}");
        var roll = (hash % 1000u) / 1000f;
        var label = method == LaserMethod.KM ? "Laser KM" : "Laser (trous forés)";

        // An accident: the beam or the fissures went too far.
        if (roll < quote.Risk)
        {
            var lost = method == LaserMethod.KM ? 2 : 1;
            var damaged = Mathf.Min(DiamondGrading.ClarityScale.Length - 1, current + lost);
            return new LaserResult
            {
                Outcome = LaserOutcome.Ruined,
                Method = method,
                GainGrades = -lost,
                NewClarityIndex = damaged,
                ValueFactor = Mathf.Clamp(ClarityFactorOf(damaged) / Mathf.Max(0.01f, ClarityFactorOf(current)) * 0.6f, 0.2f, 0.8f),
                Label = label + " (endommagé)",
                Description = method == LaserMethod.KM
                    ? "Traitement au laser KM raté : les fissures se sont propagées, plumes étendues en zig-zag et en spirale jusqu'à la surface, inclusions encore visibles, pierre fragilisée."
                    : "Traitement au laser raté : un éclat s'est détaché autour du trou, des fissures partent du canal.",
                Message = method == LaserMethod.KM
                    ? "Les fissures se sont propagées : la pierre est abîmée, avec des plumes partout."
                    : "Le faisceau a éclaté la pierre autour du trou : elle est abîmée.",
            };
        }

        // The gain: one grade per inclusion treated at the right power, a little more with the KM, less if the beam was too weak.
        var gain = quote.Treated + (method == LaserMethod.KM ? 1 : 0);
        if (quote.Reaction < 1f) gain = Mathf.FloorToInt(gain * quote.Reaction);
        var target = Mathf.Max(4, current - gain);
        gain = Mathf.Max(0, current - target);

        if (gain <= 0)
        {
            return new LaserResult
            {
                Outcome = LaserOutcome.NoEffect,
                Method = method,
                NewClarityIndex = current,
                ValueFactor = 1f,
                Description = "Passage au laser sans effet visible.",
                Message = "Le laser n'a pas assez de puissance : l'inclusion n'a presque pas bougé, rien n'est blanchi.",
            };
        }

        var outcome = quote.Reaction >= 1f ? LaserOutcome.Excellent : quote.Reaction >= 0.5f ? LaserOutcome.Good : LaserOutcome.Weak;
        var factor = ClarityFactorOf(target) / Mathf.Max(0.01f, ClarityFactorOf(current)) * info.ValueDiscount;
        return new LaserResult
        {
            Outcome = outcome,
            Method = method,
            GainGrades = gain,
            NewClarityIndex = target,
            ValueFactor = Mathf.Max(0.5f, factor),
            Label = label,
            Description = method == LaserMethod.KM
                ? "Traitement au laser KM : des fissures irrégulières en zig-zag ou en spirale affleurent la surface et mènent à une inclusion foncée blanchie à l'acide ; difficile à détecter sans grossissement. Pierre fragilisée."
                : "Traitement au laser : un canal droit de moins de 0,02 mm, visible à la loupe 10× en regardant la pierre par le côté, mène à une inclusion foncée blanchie à l'acide ; le trou peut être caché par le sertissage.",
            Message = outcome == LaserOutcome.Excellent
                ? $"Net : {quote.Treated} inclusion{(quote.Treated > 1 ? "s" : "")} blanchie{(quote.Treated > 1 ? "s" : "")}, la pierre est visiblement plus propre."
                : "Le résultat est partiel : l'inclusion est moins visible, sans avoir disparu.",
        };
    }
}
