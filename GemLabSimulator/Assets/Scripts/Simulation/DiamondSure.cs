using UnityEngine;

public enum DiamondSureVerdict { Pass, Refer, NotDiamond }

// DiamondSure: quick screening of a stone by its transmission of deep UV (225 nm). A type Ia natural diamond (aggregated
// nitrogen) absorbs it and passes; a type II diamond lets it through and is referred to further testing: that is where the
// colorless synthetics (all type II) and the rare natural type IIa and IIb stones end up.
public static class DiamondSure
{
    // Diamond type of a stone, with the same rules as the FTIR spectrum: a colorless synthetic is type II, a blue fancy diamond is IIb,
    // a few percent of the natural ones are IIa, every other natural diamond is Ia.
    public static string DiamondTypeOf(GemInstance gem)
    {
        var def = gem.Definition;
        if (def == null || !(def.name ?? "").StartsWith("Diamant")) return null;
        if (gem.IsSynthetic || def.forcedSynthetic) return "IIa";
        if (gem.IsFancyDiamond && gem.ColorHue >= 170f && gem.ColorHue < 260f) return "IIb";
        if (StoneOptics.StableHash((gem.SpecimenId ?? "") + "|type") % 100 < 4) return "IIa";
        return "Ia";
    }

    public static DiamondSureVerdict VerdictOf(GemInstance gem)
    {
        var type = DiamondTypeOf(gem);
        if (type == null) return DiamondSureVerdict.NotDiamond;
        return type == "Ia" ? DiamondSureVerdict.Pass : DiamondSureVerdict.Refer;
    }

    // Share of the 225 nm light that goes through: low for a type I, high for a type II. Stable for a given stone.
    public static int TransmissionPercent(GemInstance gem)
    {
        var type = DiamondTypeOf(gem);
        var roll = StoneOptics.StableHash((gem.SpecimenId ?? "") + "|dstrans") % 1000 / 1000f;
        if (type == null) return 0;
        return type == "Ia" ? Mathf.RoundToInt(3f + 14f * roll) : Mathf.RoundToInt(62f + 30f * roll);
    }

    public static string Label(GemInstance gem)
    {
        switch (VerdictOf(gem))
        {
            case DiamondSureVerdict.Pass: return "Pass";
            case DiamondSureVerdict.Refer: return "Refer";
            default: return "Non applicable (pas un diamant)";
        }
    }
}
