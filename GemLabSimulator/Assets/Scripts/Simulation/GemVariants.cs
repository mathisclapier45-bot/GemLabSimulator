using UnityEngine;

// One species, many colours. A sapphire is a sapphire whatever its colour (and a fluorite too): the game has a single "Saphir" species and a single
// "Fluorite" one, and the colour of each stone decides what the rest of the game does with it. Where a catalogue still thinks in colour groups (the heating
// recipes, the spectra, the diffusion of beryllium...), this class gives the group of a stone: "Saphir" + violet becomes "Saphir_Violet", the name the
// catalogues used when each colour was a species of its own.
public static class GemVariants
{
    public const string Sapphire = "Saphir";

    // The colour group of a sapphire: from the hue wheel of the laboratory. Nearly neutral stones are colourless.
    public static string SapphireGroup(float hue, float saturation)
    {
        if (saturation < 0.10f) return "Incolore";
        hue = Mathf.Repeat(hue, 360f);
        if (hue >= 315f || hue < 8f) return "Rose";
        if (hue < 40f) return "Orange";
        if (hue < 72f) return "Jaune";
        if (hue < 183f) return "Vert";
        if (hue < 255f) return "Bleu";
        return "Violet";
    }

    // What the colour of a stone does to its price, for the species that come in several colours: the base value of the species is the one of its
    // commonest colour (blue sapphire, violet fluorite), the other colours are worth more or less (the values the colours had as separate species).
    public static float ColorValueFactor(string name, float hue, float saturation)
    {
        if (name == Sapphire)
        {
            switch (SapphireGroup(hue, saturation))
            {
                case "Bleu": return 1.00f;
                case "Rose": return 0.79f;
                case "Orange": return 0.64f;
                case "Violet": return 0.46f;
                case "Jaune": return 0.39f;
                case "Vert": return 0.29f;
                default: return 0.16f;           // colourless
            }
        }
        if (name == "Fluorite")
        {
            hue = Mathf.Repeat(hue, 360f);
            if (saturation < 0.10f) return 0.7f;
            if (hue >= 195f && hue < 255f) return 1.45f;        // blue
            if (hue >= 40f && hue < 72f) return 1.2f;            // yellow
            if (hue >= 255f && hue < 305f) return 1f;            // violet
            if (hue >= 72f && hue < 195f) return 1f;             // green
            return 1.6f;                                         // orange, pink, red: the rare ones
        }
        return 1f;
    }

    // The name a catalogue should use for a stone: the species name, or "Saphir_<group>" for a sapphire.
    public static string Key(string name, float hue, float saturation)
    {
        return name == Sapphire ? Sapphire + "_" + SapphireGroup(hue, saturation) : name;
    }

    public static string Key(GemInstance gem)
    {
        if (gem == null || gem.Definition == null) return "";
        return Key(gem.Definition.name ?? "", gem.ColorHue, gem.ColorSaturation);
    }

    // A new stone of a species with the colour group wanted (the technicians' samples: a colourless sapphire to heat, a pink one to diffuse...).
    // The colour is drawn by the species, so the stone is drawn again until it fits; the last draw stands when none does.
    public static GemInstance NewSample(GemDefinition definition, string group)
    {
        GemInstance last = null;
        for (var i = 0; i < 400; i++)
        {
            last = new GemInstance(definition);
            if (definition.name != Sapphire || string.IsNullOrEmpty(group) || SapphireGroup(last.ColorHue, last.ColorSaturation) == group) break;
        }
        return last;
    }

    // True for the sapphire species (the merged one, and the former colour names in case an old save still has one).
    public static bool IsSapphire(string name) => name != null && (name == Sapphire || name.StartsWith("Saphir_"));
}
