using System.Collections.Generic;
using UnityEngine;

// How a species catches the light on its sprite, and nothing else: the brilliance of its luster (a diamond flashes, a quartz glints, a jade only glows, a hematite is flat)
// and the fire of the stones that split the light in rainbow colours. Derived from the luster written on the species, so every place that draws a stone shows the same one.
public enum GemPattern
{
    None, Banded, Flecks, Veins, Mottled, Chatoyant, Adular, Opal, Labrador, Pearl, MetalGray, MetalBrass, Tortoise, Amber, Grain, Streaks, Bicolour, ColorChange, Gradient, Cleavage, Bubbles, Horsetail,
}

public struct GemLook
{
    public float Brilliance;        // 0 = matte, 1 = adamantine: how much the facets mirror the light instead of just being lit
    public float Fire;              // 0..1: rainbow flashes on the brightest facets (diamond, CZ, moissanite...)
    public GemPattern Pattern;      // what is seen inside or on the stone: bands, veins, flecks, a sheen, play of colour...
    public float Strength;          // how marked the pattern is, 0..1
    public Color Accent;            // the second colour of a bicolour or colour-change stone, the tint of a gradient
    public int Seed;                // makes two species with the same pattern differ
    public float Contrast;          // 1 = normal; more = harder differences between bright and dark facets (a diamond is made of mirrors)
    public float Ior;               // refractive index, for the realistic renderer
    public float Dispersion;        // half the spread of the index between red and blue light
    public float RoughScale;        // how dull the faces of its rough crystals are, compared with the habit's usual: 0 or 1 = as usual, less = glossier (spinel, garnet)
    public float ToneCap;           // > 0: no facet gets brighter than this (a rough crystal has big flat faces, which must not turn white)

    public static readonly GemLook Default = new GemLook { Brilliance = 0.6f, Fire = 0f, Contrast = 1f, Ior = 1.6f, Dispersion = 0.012f };

    // The pattern of each species that has one: (pattern, strength, accent colour).
    private static readonly Dictionary<string, (GemPattern pattern, float strength, Color accent)> Patterns = new Dictionary<string, (GemPattern, float, Color)>
    {
        { "Agate", (GemPattern.Banded, 0.9f, default) },
        { "Rhodocrosite", (GemPattern.Banded, 0.75f, default) },
        { "Lapis_Lazuli", (GemPattern.Flecks, 0.9f, default) },
        { "Turquoise", (GemPattern.Veins, 0.85f, new Color(0.32f, 0.22f, 0.14f)) },
        { "Turquoise_Stabilisee", (GemPattern.Veins, 0.35f, new Color(0.32f, 0.22f, 0.14f)) },
        { "Rhodonite", (GemPattern.Veins, 0.75f, new Color(0.18f, 0.12f, 0.12f)) },
        { "Serpentine_Bowenite", (GemPattern.Mottled, 0.55f, default) },
        { "Jade_Jadeite", (GemPattern.Mottled, 0.5f, default) },
        { "Jade_Nephrite", (GemPattern.Mottled, 0.6f, default) },
        { "Quartz_Rose", (GemPattern.Mottled, 0.3f, default) },
        { "Oeil_de_Tigre", (GemPattern.Chatoyant, 0.9f, new Color(1f, 0.86f, 0.45f)) },
        { "Pierre_De_Lune", (GemPattern.Adular, 0.9f, new Color(0.55f, 0.72f, 1f)) },
        { "Opale_Noble", (GemPattern.Opal, 0.9f, default) },
        { "Opale_De_Feu", (GemPattern.Amber, 0.7f, new Color(1f, 0.55f, 0.15f)) },
        { "Labradorite", (GemPattern.Labrador, 0.9f, new Color(0.15f, 0.72f, 0.95f)) },
        { "Perle", (GemPattern.Pearl, 0.9f, new Color(1f, 0.86f, 0.92f)) },
        { "Perle_Tahiti", (GemPattern.Pearl, 0.9f, new Color(0.55f, 0.85f, 0.70f)) },
        { "Hematite", (GemPattern.MetalGray, 1f, default) },
        { "Pyrite", (GemPattern.MetalBrass, 1f, default) },
        { "Ecaille_Tortue", (GemPattern.Tortoise, 0.9f, new Color(0.10f, 0.06f, 0.04f)) },
        { "Ambre", (GemPattern.Amber, 0.9f, new Color(1f, 0.70f, 0.20f)) },
        { "Ivoire_Elephant", (GemPattern.Grain, 0.6f, default) },
        { "Ivoire_Hippopotame", (GemPattern.Grain, 0.45f, default) },
        { "Ivoire_Mammouth", (GemPattern.Grain, 0.7f, default) },
        { "Ivoire_Morse", (GemPattern.Grain, 0.6f, default) },
        { "Defense_Narval", (GemPattern.Grain, 0.8f, default) },
        { "Corail", (GemPattern.Streaks, 0.4f, new Color(1f, 0.80f, 0.76f)) },
        { "Corne_Rhinoceros", (GemPattern.Streaks, 0.45f, new Color(0.62f, 0.55f, 0.46f)) },
        { "Ametrine", (GemPattern.Bicolour, 1f, new Color(0.96f, 0.76f, 0.26f)) },
        { "Chrysoberyl_Alexandrite", (GemPattern.ColorChange, 0.85f, new Color(0.82f, 0.20f, 0.36f)) },
        { "Padparadscha", (GemPattern.Gradient, 0.8f, new Color(1f, 0.55f, 0.28f)) },
        { "Fluorite", (GemPattern.Cleavage, 0.3f, default) },
        { "Fluorite_Verte", (GemPattern.Cleavage, 0.3f, default) },
        { "Fluorite_Bleue", (GemPattern.Cleavage, 0.3f, default) },
        { "Fluorite_Jaune", (GemPattern.Cleavage, 0.3f, default) },
        { "Fluorite_Bandee", (GemPattern.Banded, 0.95f, new Color(0.55f, 0.85f, 0.65f)) },
        { "Fluorite_Blue_John", (GemPattern.Banded, 0.95f, new Color(0.96f, 0.84f, 0.42f)) },
        { "Iolite_Bloodshot", (GemPattern.Flecks, 0.9f, new Color(0.88f, 0.28f, 0.14f)) },
        { "Iolite", (GemPattern.Gradient, 0.5f, new Color(0.82f, 0.78f, 0.58f)) },
        { "Tourmaline_Paraiba", (GemPattern.Gradient, 0.6f, new Color(0.25f, 0.85f, 0.55f)) },
        { "Kyanite", (GemPattern.Cleavage, 0.8f, default) },
        { "Verre", (GemPattern.Bubbles, 0.9f, default) },
        { "Grenat_Demantoide", (GemPattern.Horsetail, 0.85f, new Color(0.64f, 0.50f, 0.10f)) },
    };

    private static readonly Dictionary<string, GemLook> cache = new Dictionary<string, GemLook>();

    private static readonly HashSet<string> Fiery = new HashSet<string>
    {
        "Diamant", "Diamant_Synthetique_CVD", "Diamant_Synthetique_HPHT", "CZ", "GGG", "YAG", "Moissanite_Synthetique", "Zircon", "Grenat_Demantoide",
    };

    // The look of a rough crystal of a species: the same, with calm faces.
    public static GemLook ForCrystal(GemDefinition def)
    {
        var look = For(def);
        look.ToneCap = 0.74f;
        return look;
    }

    public static readonly GemLook Crystal = new GemLook { Brilliance = 0.45f, ToneCap = 0.74f, Contrast = 1f, Ior = 1.6f, Dispersion = 0.012f };

    public static GemLook For(GemDefinition def)
    {
        if (def == null) return Default;
        if (cache.TryGetValue(def.name ?? "", out var known)) return known;

        var look = new GemLook { Brilliance = LusterBrilliance(def.luster), Fire = Fiery.Contains(def.name ?? "") ? (def.name == "Zircon" ? 0.6f : def.name == "Grenat_Demantoide" ? 0.95f : 1f) : 0f };
        look.Seed = (int)(StoneOptics.StableHash(def.name ?? "") & 0xFFFF);
        look.Ior = def.refractiveIndex > 1.2f ? def.refractiveIndex : 1.6f;
        // The demantoid: a brilliance and a fire to rival the diamond's, and glossy crystals.
        if (def.name == "Grenat_Demantoide") look.Brilliance = Mathf.Max(look.Brilliance, 0.97f);
        look.RoughScale = def.name == "Spinelle" || def.name == "Spinelle_Bleu" ? 0.35f : def.name == "Grenat_Demantoide" ? 0.4f : (def.name ?? "").StartsWith("Grenat_") ? 0.6f : 1f;
        look.Dispersion = DispersionOf(def.name ?? "", look.Ior);
        look.Contrast = look.Fire > 0.8f ? 1.7f : look.Fire > 0f ? 1.35f : look.Brilliance >= 0.85f ? 1.25f : 1f;
        if (Patterns.TryGetValue(def.name ?? "", out var pattern))
        {
            look.Pattern = pattern.pattern;
            look.Strength = pattern.strength;
            look.Accent = pattern.accent;
        }
        // A stone that lets little light in cannot flash.
        switch (def.Diaphaneity)
        {
            case Diaphaneity.SemiTransparent: look.Brilliance *= 0.8f; break;
            case Diaphaneity.SemiOpaque: look.Brilliance *= 0.55f; break;
            case Diaphaneity.Opaque: look.Brilliance *= 0.4f; break;
        }
        cache[def.name ?? ""] = look;
        return look;
    }

    // Half the spread of the refractive index from red to blue light: huge for the stones that are known for their fire, small for the others.
    private static float DispersionOf(string name, float ior)
    {
        switch (name)
        {
            case "Diamant": case "Diamant_Synthetique_CVD": case "Diamant_Synthetique_HPHT": return 0.022f;
            case "CZ": return 0.030f;
            case "Moissanite_Synthetique": return 0.052f;
            case "YAG": return 0.014f;
            case "GGG": return 0.022f;
            case "Zircon": return 0.020f;
            case "Grenat_Demantoide": return 0.036f;
        }
        return 0.004f + Mathf.Max(0f, ior - 1.4f) * 0.012f;
    }

    // "Vitreux à résineux" is the mean of the two; "sub-adamantin" is a little below "adamantin".
    private static float LusterBrilliance(string luster)
    {
        if (string.IsNullOrEmpty(luster)) return 0.6f;
        var t = luster.ToLowerInvariant();
        var total = 0f;
        var count = 0;
        foreach (var piece in t.Split(new[] { " à ", " ou ", "/", "," }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            var p = piece.Trim();
            float value;
            if (p.Contains("sub-adamantin") || p.Contains("subadamantin")) value = 0.88f;
            else if (p.Contains("adamantin")) value = 1f;
            else if (p.Contains("métallique")) value = 0.9f;
            else if (p.Contains("vitreux")) value = 0.72f;
            else if (p.Contains("résineux")) value = 0.55f;
            else if (p.Contains("nacré")) value = 0.5f;
            else if (p.Contains("soyeux")) value = 0.42f;
            else if (p.Contains("cireux") || p.Contains("gras")) value = 0.38f;
            else if (p.Contains("mat") || p.Contains("terne")) value = 0.15f;
            else value = 0.6f;
            total += value;
            count++;
        }
        return count > 0 ? total / count : 0.6f;
    }
}
