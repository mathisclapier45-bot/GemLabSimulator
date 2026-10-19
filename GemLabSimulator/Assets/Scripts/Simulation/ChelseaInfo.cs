using UnityEngine;

// What a stone looks like through the Chelsea filter (which only lets deep red and yellow-green through):
// stones colored by chromium show a red/pink reaction, the others stay dull green. Derived from the species'
// free-text description.
public struct ChelseaInfo
{
    public bool Reacts;
    public float Intensity;
    public Color Color;

    private static readonly Color Red = new Color(0.96f, 0.10f, 0.12f);
    private static readonly Color Pink = new Color(1f, 0.38f, 0.52f);

    public static ChelseaInfo ForGem(GemDefinition def)
    {
        var result = new ChelseaInfo { Reacts = false, Intensity = 0f, Color = Red };
        var text = def.chelseaFilterReaction;
        if (string.IsNullOrEmpty(text)) return result;

        var t = text.ToLowerInvariant();
        if (t.Contains("non applicable")) return result;

        var hasRed = t.Contains("rouge") || t.Contains("rosât") || t.Contains("rose");
        if (!hasRed) return result;

        // "No reaction" descriptions that merely mention red ("aucune réaction rouge notable").
        var denies = t.StartsWith("aucune réaction") && !t.Contains("ou réaction rougeâtre");
        if (denies) return result;

        float intensity;
        if (t.Contains("contrairement")) intensity = 0.08f;               // "nulle, unlike imitations"
        else if (t.Contains("très faible")) intensity = 0.15f;
        else if (t.Contains("faible à modérée")) intensity = 0.4f;
        else if (t.Contains("modér")) intensity = 0.65f;
        else if (t.Contains("vif")) intensity = 1f;
        else if (t.Contains("marqu")) intensity = 0.9f;
        else if (t.Contains("faible")) intensity = 0.25f;
        else if (t.Contains("possible")) intensity = 0.3f;
        else intensity = 0.6f;

        result.Reacts = intensity >= 0.1f;
        result.Intensity = intensity;
        result.Color = t.Contains("rosât") ? Color.Lerp(Red, Pink, 0.55f) : Red;
        return result;
    }
}
