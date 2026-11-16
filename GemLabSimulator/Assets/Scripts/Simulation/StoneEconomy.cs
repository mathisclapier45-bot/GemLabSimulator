using UnityEngine;

// What the quality of a stone does to its price. A heavily included ruby full of rutile is worth a few euros, a clean one of fine colour a fortune: the clarity factor
// is wide, and it follows the place of the stone inside its grade (StoneQuality.PositionInTier), so what the eye sees and what the stone costs agree.
public static class StoneEconomy
{
    // The factor at the best and at the worst of each grade (tier 3 = heavily included ... 0 = loupe clean).
    private static readonly float[] Best = { 3.2f, 1.6f, 0.80f, 0.32f };
    private static readonly float[] Worst = { 1.9f, 0.9f, 0.30f, 0.04f };

    // How much clarity matters to the price of a species: a lot for the precious stones, less for the cheap ones.
    public static float Steepness(GemDefinition definition)
    {
        if (definition == null) return 0.75f;
        var value = definition.baseValue;
        return value >= 100f ? 1f : value >= 40f ? 0.75f : 0.5f;
    }

    public static float ClarityFactor(GemDefinition definition, int tier, float position)
    {
        tier = Mathf.Clamp(tier, 0, 3);
        var raw = Mathf.Lerp(Best[tier], Worst[tier], Mathf.Clamp01(position));
        return Mathf.Pow(raw, Steepness(definition));
    }

    // For the encyclopedia: the factor from the worst to the best stone of a grade.
    public static string ClarityRangeLabel(GemDefinition definition, int tier)
    {
        tier = Mathf.Clamp(tier, 0, 3);
        var steep = Steepness(definition);
        var low = Mathf.Pow(Worst[tier], steep);
        var high = Mathf.Pow(Best[tier], steep);
        return "×" + low.ToString("0.0#") + " à ×" + high.ToString("0.0#");
    }
}
