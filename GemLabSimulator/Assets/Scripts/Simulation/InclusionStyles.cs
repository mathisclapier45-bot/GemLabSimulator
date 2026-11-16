using System.Collections.Generic;
using UnityEngine;

// How the inclusions of a species and of a deposit look as bodies in the volume of the stone: the length and the fineness of the silk, the colour of the grains, the size of the
// feathers and of the fingerprints, the clouds, the zoning. StoneQuality gives what kinds of inclusion a stone has (the shares of the species); the style gives their look.
// Every rule that matches the species (and the deposit) changes the style one after the other, the most general first, so a deposit only has to say what is different.
// To tune a stone from a photo: find its rule here and change a number.
public class InclusionStyle
{
    public float Count = 1f;                                              // multiplier of the number of bodies
    public float Needles = 1f;                                            // how much of the needles of the species are really needles (an emerald has fissures instead: the rest turns into feathers)
    public bool AngularCrystals = false;                                  // crystals with flat faces (a cube of pyrite, a prism) instead of rounded grains
    public Vector2 FeatherRatio = new Vector2(1f, 1f);                    // width of a feather or a fracture as a part of its length (1 = round, small = long and narrow)
    public float FeatherIridescence = 0.55f;                              // chance of the interference colours on a feather
    public int FractureSheets = 1;                                        // fractures come in groups of up to this many sheets that cross each other
    public float FractureWeb = 0f;                                        // share of the sheets that are a web of meandering cracks (like trichites)
    public float PureFractures = 0f;                                      // chance that an inclusion of a very pure stone is a fracture (even the purest emerald has some)
    public int PureFloor = 0;                                             // the least number of inclusions of a very pure (Eye-clean) stone
    public float Haze = 1f;                                               // multiplier of the milky haze of the stone (an emerald stays clear, with its garden)
    public Vector2 SilkLength = new Vector2(0.10f, 0.32f);                // half length of a needle of silk, in the space of the stone (the stone is about 2 wide)
    public Vector2 SilkRadius = new Vector2(0.005f, 0.009f);
    public Color SilkColor = new Color(0.95f, 0.90f, 0.78f);
    public float SilkBrightness = 1.7f;
    public float SilkTint = 0.4f;                                         // 0 = the silk keeps its own colour, 1 = it takes the colour of the stone
    public float SilkGroup = 0.6f;                                        // chance that a needle comes with two parallel ones beside it
    public Vector2 GrainSize = new Vector2(0.020f, 0.050f);               // the pale crystals
    public Color GrainColor = new Color(0.86f, 0.86f, 0.82f);
    public float Halo = 0.1f;                                             // chance that a crystal has a tension halo (a thin sheet around it)
    public Vector2 FeatherSize = new Vector2(0.10f, 0.28f);
    public Vector2 FingerprintSize = new Vector2(0.14f, 0.34f);
    public Vector2 CloudSize = new Vector2(0.07f, 0.20f);
    public Color CloudColor = new Color(0.86f, 0.90f, 0.95f);
    public float Zoning = 1f;                                             // multiplier of the bands of colour
    public Color DarkColor = new Color(0.12f, 0.10f, 0.08f);             // the dark grains (sulfide, chromite): a brassy pyrite is lighter
    public Vector2 FibreLength = new Vector2(0.10f, 0.30f);               // loose fibres (tremolite, actinolite)
    public Vector2 FibreRadius = new Vector2(0.005f, 0.010f);
    public Color FibreColor = new Color(0.88f, 0.86f, 0.80f);
    public float FibreBrightness = 1.2f;
    public float TubeBrightness = 1.0f;
    public Vector2 TubeLength = new Vector2(0.18f, 0.45f);                // tubes along the c axis (beryl, topaz, tourmaline)
    public Vector2 TubeRadius = new Vector2(0.010f, 0.018f);
    public Color TubeColor = new Color(0.80f, 0.88f, 0.92f);
    public Vector2 MicaSize = new Vector2(0.05f, 0.12f);                  // flakes of mica
    public Color MicaColor = new Color(0.72f, 0.62f, 0.40f);
    public Vector2 CavitySize = new Vector2(0.025f, 0.07f);               // cavities with a liquid (the garden of an emerald)
    public Vector2 CavityElongation = new Vector2(1.4f, 3.2f);
    public float CavityBubble = 0.55f;                                    // chance of a gas bubble in a cavity
    public float CavityCrystal = 0.2f;                                    // chance of a small crystal in a cavity (a third phase: halite in a Colombian emerald)
    public bool CavityAlign = false;                                      // the cavities lie along the c axis, as rods in rows (beryl)
    public Vector2 SheetLength = new Vector2(0.12f, 0.30f);               // jagged cavity sheets (three-phase inclusions): half length along the c axis
    public Vector2 SheetWidth = new Vector2(0.18f, 0.34f);                // their half width, as a part of the length
    public int DarkCluster = 1;                                           // dark grains come in clusters of this many
    public int Moss = 0;                                                  // patches of fine curly fibres (the moss of an emerald garden)
    public int Striae = 0;                                                // fine parallel lines along the c axis
    public float Dust = 0.5f;                                             // multiplier of the pinpoints: tiny bright particles in sheets, that sparkle
    public Color DustColor = new Color(0.98f, 0.94f, 0.92f);
}

public static class InclusionStyles
{
    private class Rule
    {
        public readonly string Species;        // a part of the name of the species, in lower case
        public readonly string Origin;         // a part of the name of the deposit ("" = every deposit)
        public readonly System.Action<InclusionStyle> Apply;

        public Rule(string species, string origin, System.Action<InclusionStyle> apply)
        {
            Species = species;
            Origin = origin;
            Apply = apply;
        }
    }

    private static readonly Rule[] Rules =
    {
        // ---- Emerald ----
        // The garden: wide feathers and fissures, cavities with a liquid, a bubble and often a crystal, flakes of mica, fibres of tremolite, a green veil.
        new Rule("émeraude", "", s =>
        {
            s.Haze = 0.45f;
            s.Needles = 0f;                                              // no rutile-like needles: the garden is made of fissures and fractures
            s.AngularCrystals = true;
            s.FeatherRatio = new Vector2(0.35f, 0.70f);
            s.FeatherIridescence = 0.85f;
            s.FractureSheets = 4;
            s.FractureWeb = 0.65f;
            s.PureFractures = 1f;
            s.PureFloor = 3;
            s.FeatherSize = new Vector2(0.16f, 0.45f);
            s.FingerprintSize = new Vector2(0.14f, 0.36f);
            s.CloudColor = new Color(0.80f, 0.92f, 0.84f);
            s.Dust = 0.2f;
            s.Zoning = 0.6f;
            s.GrainColor = new Color(0.85f, 0.88f, 0.80f);
            s.GrainSize = new Vector2(0.020f, 0.060f);
            s.Halo = 0.05f;
            s.DarkColor = new Color(0.30f, 0.26f, 0.12f);
            s.MicaColor = new Color(0.60f, 0.55f, 0.32f);
            s.MicaSize = new Vector2(0.06f, 0.14f);
            s.FibreColor = new Color(0.70f, 0.85f, 0.72f);
            s.FibreBrightness = 0.7f;
            s.TubeColor = new Color(0.70f, 0.88f, 0.78f);
            s.TubeBrightness = 0.6f;
            s.CavityAlign = true;
            s.CavitySize = new Vector2(0.010f, 0.026f);                  // the rods of the two-phase inclusions
            s.CavityElongation = new Vector2(2.5f, 6.0f);
            s.CavityBubble = 0.6f;
            s.CavityCrystal = 0.25f;
            s.DarkCluster = 4;
        }),
        // Colombia (Muzo, Chivor): big jagged three-phase cavities (liquid, bubble, cube of halite), pyrite and calcite, wide feathers; a strongly included garden.
        new Rule("émeraude", "colombie", s =>
        {
            s.Count = 1.2f;
            s.SheetLength = new Vector2(0.16f, 0.38f);
            s.FractureSheets = 5;
            s.CavityBubble = 0.85f;
            s.CavityCrystal = 0.7f;
            s.DarkColor = new Color(0.50f, 0.38f, 0.12f);
            s.DarkCluster = 6;
            s.FeatherSize = new Vector2(0.20f, 0.55f);
            s.Zoning = 0.5f;
        }),
        // Zambia: many flakes of mica and fibres of tremolite, a darker bluish green, more zoning.
        new Rule("émeraude", "zambie", s =>
        {
            s.MicaSize = new Vector2(0.08f, 0.20f);
            s.MicaColor = new Color(0.50f, 0.52f, 0.30f);
            s.FibreColor = new Color(0.72f, 0.85f, 0.74f);
            s.FibreLength = new Vector2(0.14f, 0.36f);
            s.CavityElongation = new Vector2(3.0f, 7.0f);                // many rod-shaped two-phase inclusions, parallel, with fine growth lines
            s.CavityBubble = 0.75f;
            s.Zoning = 0.9f;
        }),
        // Brazil: smaller flakes of mica, two-phase cavities, cleaner stones.
        new Rule("émeraude", "brésil", s =>
        {
            s.Count = 0.85f;
            s.MicaSize = new Vector2(0.04f, 0.10f);
            s.CavityBubble = 0.5f;
            s.CavityCrystal = 0.05f;
        }),
        // Zimbabwe (Sandawana) and Siberia: dense long fibres of tremolite or actinolite.
        new Rule("émeraude", "zimbabwe", s =>
        {
            s.Count = 1.2f;
            s.Needles = 0.15f;
            s.FibreLength = new Vector2(0.20f, 0.50f);
            s.FibreRadius = new Vector2(0.006f, 0.012f);
            s.FibreColor = new Color(0.72f, 0.85f, 0.72f);
        }),
        new Rule("émeraude", "sibérie", s =>
        {
            s.Needles = 0.15f;
            s.FibreLength = new Vector2(0.20f, 0.46f);
            s.FibreRadius = new Vector2(0.006f, 0.011f);
            s.FibreColor = new Color(0.74f, 0.84f, 0.72f);
        }),
        // Afghanistan (Panjshir): like Colombia, with fewer crystals in the cavities.
        new Rule("émeraude", "afghanistan", s =>
        {
            s.SheetLength = new Vector2(0.14f, 0.34f);
            s.CavityBubble = 0.7f;
            s.CavityCrystal = 0.4f;
            s.FeatherSize = new Vector2(0.16f, 0.45f);
        }),

        // ---- Ruby ----
        // The silk of a ruby is rutile: fine needles that run in three directions, golden to silver, in nets. Calcite and apatite grains are rounded, fingerprints are common.
        new Rule("rubis", "", s =>
        {
            s.SilkLength = new Vector2(0.08f, 0.26f);
            s.SilkRadius = new Vector2(0.0025f, 0.0045f);               // very fine: far thinner than a pixel of the small pictures, so it shows as a faint glint
            s.SilkColor = new Color(0.90f, 0.74f, 0.56f);              // golden, not white
            s.SilkBrightness = 1.2f;
            s.SilkTint = 0.55f;
            s.SilkGroup = 0.75f;
            s.GrainSize = new Vector2(0.020f, 0.055f);
            s.GrainColor = new Color(0.90f, 0.88f, 0.84f);
            s.Halo = 0.12f;
            s.FingerprintSize = new Vector2(0.16f, 0.38f);
            s.FeatherSize = new Vector2(0.10f, 0.30f);
            s.CloudColor = new Color(0.90f, 0.86f, 0.90f);
            s.Dust = 0.6f;
        }),
        // Burma (Mogok): dense, very fine and short silk that gives the velvet; white rounded calcite; an even colour.
        new Rule("rubis", "birmanie", s =>
        {
            s.Count = 1.15f;
            s.SilkLength = new Vector2(0.05f, 0.20f);
            s.SilkRadius = new Vector2(0.002f, 0.004f);
            s.SilkGroup = 0.9f;
            s.SilkBrightness = 1.0f;                                      // the needles are seen only as a soft velvet: the eye sees the pinpoints and the haze
            s.GrainSize = new Vector2(0.025f, 0.060f);
            s.GrainColor = new Color(0.92f, 0.92f, 0.90f);
            s.Halo = 0.05f;
            s.Dust = 1.2f;
            s.FingerprintSize = new Vector2(0.18f, 0.42f);                // floury white patches with an irregular edge
            s.Zoning = 0.9f;                                              // angular zones of purple and red with straight edges
        }),
        // Mozambique: less silk, big fingerprints and veils, twin planes, grains of a dull grey.
        new Rule("rubis", "mozambique", s =>
        {
            s.SilkGroup = 0.4f;
            s.Halo = 0.08f;
            s.FingerprintSize = new Vector2(0.20f, 0.45f);
            s.GrainColor = new Color(0.80f, 0.78f, 0.74f);
            s.Dust = 0.4f;
            s.Zoning = 0.8f;
        }),
        // Thailand (Chanthaburi): little and short silk, big feathers and fingerprints, dark grains, strong zoning.
        new Rule("rubis", "thaïlande", s =>
        {
            s.SilkLength = new Vector2(0.05f, 0.16f);
            s.SilkGroup = 0.3f;
            s.FingerprintSize = new Vector2(0.18f, 0.42f);
            s.FeatherSize = new Vector2(0.14f, 0.36f);
            s.GrainColor = new Color(0.35f, 0.30f, 0.28f);
            s.Halo = 0.2f;
            s.Zoning = 1.5f;
        }),
        // Sri Lanka: long silk, zircons with their halos.
        new Rule("rubis", "sri lanka", s =>
        {
            s.SilkLength = new Vector2(0.14f, 0.40f);
            s.SilkRadius = new Vector2(0.004f, 0.006f);
            s.SilkGroup = 0.8f;
            s.Halo = 0.35f;
            s.GrainSize = new Vector2(0.025f, 0.065f);
            s.Zoning = 0.9f;
        }),
    };

    private static readonly Dictionary<string, InclusionStyle> Cache = new Dictionary<string, InclusionStyle>();

    // The style of a stone from its key ("species|deposit", in lower case; see StoneQuality.StyleKey).
    public static InclusionStyle For(string key)
    {
        key = key ?? "";
        if (Cache.TryGetValue(key, out var known)) return known;

        var parts = key.Split('|');
        var species = parts[0];
        var origin = parts.Length > 1 ? parts[1] : "";
        var style = new InclusionStyle();
        foreach (var rule in Rules)
        {
            if (!species.Contains(rule.Species)) continue;
            if (rule.Origin.Length > 0 && !origin.Contains(rule.Origin)) continue;
            rule.Apply(style);
        }
        Cache[key] = style;
        return style;
    }
}
