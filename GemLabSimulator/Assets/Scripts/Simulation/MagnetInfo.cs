// How strongly a stone is pulled by a strong magnet (0 = no reaction, 1 = rushes to the magnet),
// derived from the species' free-text description.
public static class MagnetInfo
{
    public static float AttractionStrength(GemDefinition def)
    {
        var text = def.magnetism;
        if (string.IsNullOrEmpty(text)) return 0f;

        var t = text.ToLowerInvariant();
        if (t.StartsWith("aucune")) return 0f;
        if (t.Contains("légère réaction possible")) return 0.12f;
        if (t.StartsWith("faible réaction")) return 0.25f;
        if (t.StartsWith("légèrement attiré")) return 0.35f;
        if (t.StartsWith("modérément attiré")) return 0.6f;
        if (t.StartsWith("fortement attiré")) return 1f;
        if (t.StartsWith("attiré")) return 0.8f;
        return 0f;
    }
}
