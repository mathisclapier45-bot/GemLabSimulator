using UnityEngine;

// How good the stone really is, as the eye sees it. The clarity grade, the species and the history of the stone give each specimen its own look, always the same for the same stone:
// a veil of haze and a lower transparency, the inclusions that show without a loupe, a polish more or less dull with a few scratches, colour that is not even,
// and proportions that are not always right (a shallow pavilion lets the light through like a window, a deep one is dark). StoneQualityFx paints it.
public struct StoneQuality
{
    public float Haze;           // 0..1: milky veil and clouds inside
    public float Dull;           // 0..1: dull polish
    public float Scratch;        // 0..1: visible scratches on the facets
    public float Zoning;         // 0..1: patches and bands of colour of uneven depth
    public float DepthOffset;    // proportions: negative = shallow (a window), positive = deep (dark)
    public int LevelShift;       // steps from the species' diaphaneity toward opaque (0..2)
    public int Tier;             // 0 loupe clean ... 3 heavily included
    public int Seed;
    // The kinds of inclusion that show, by share (the species' own mix): needles, crystals, clouds, feathers, bubbles.
    public float Needles;
    public float Crystals;
    public float Clouds;
    public float Feathers;
    public float Bubbles;
    // The same inclusions as bodies in the volume of the stone (see StoneInclusions), by share (the sum is 1): the shares of the species, sorted by what they are made of.
    public float VNeedles, VCrystals, VCavities, VHealing, VClouds, VFeathers, VBubbles;
    public float SilkShare, TubeShare;                 // among the needles: silk in the basal plane, tubes along the c axis (the rest: loose fibres)
    public float DarkShare, MicaShare, OctaShare;      // among the crystals: dark grains (sulfide, chromite), flakes of mica, octahedra (the rest: pale grains)
    public float Cav3Share, CavJagShare;               // among the cavities: three-phase ones (jagged, with a bubble and a crystal), jagged two-phase ones (the rest: rounded rods)
    public float Silk;                                 // 0..1: how dense the rutile silk is (it makes a stone milky or opaque, gives it a sheen and, in a cabochon, a star)
    public string StyleKey;                            // "species|deposit" in lower case: which inclusion style the stone has (see InclusionStyles)
    public float Scatter;                              // how much the body scatters the light, per unit of the stone: the haze and the silk together (the milky look, in depth)

    public float DepthScale => 1f + DepthOffset;

    public bool HasEffect => Haze > 0.02f || Dull > 0.02f || Scratch > 0.04f || Zoning > 0.04f || Tier >= 1;

    // The diaphaneity the stone shows: the one of its species, lowered by its own haze.
    public Diaphaneity Level(Diaphaneity species)
    {
        var level = (int)species + LevelShift;
        if (species == Diaphaneity.Opaque) level = (int)Diaphaneity.Opaque;
        return (Diaphaneity)Mathf.Clamp(level, 0, (int)Diaphaneity.Opaque);
    }

    // The look of a stone in classes, so that stones that look alike share one icon.
    public string Key =>
        $"q{Mathf.RoundToInt(Haze * 4f)}{Mathf.RoundToInt(Dull * 2f)}{Mathf.RoundToInt(Scratch * 2f)}{Mathf.RoundToInt(Zoning * 2f)}{LevelShift}{Mathf.RoundToInt(DepthOffset * 8f)}{Tier}{Seed % 5}";

    public static int TierOf(string clarityGrade)
    {
        return clarityGrade == "Fortement inclus" ? 3 : clarityGrade == "Inclus" ? 2 : clarityGrade == "Eye-clean" ? 1 : 0;
    }

    private static int SeedOf(string specimenId, string fallback)
    {
        return (int)(StoneOptics.StableHash((specimenId ?? fallback) + "|quality") & 0x7FFFFFFF);
    }

    // Where a stone sits inside its clarity grade: 0 = the best of the grade, 1 = the worst. The look of the stone (haze, opacity) and its price both follow it,
    // so a stone that looks worse is also worth less. Always the same for the same specimen.
    public static float PositionInTier(string specimenId)
    {
        return (float)new System.Random(SeedOf(specimenId, "stone")).NextDouble();
    }

    public static StoneQuality For(GemInstance gem)
    {
        if (gem == null || gem.Definition == null) return default;

        var seed = SeedOf(gem.SpecimenId, gem.Definition.name);
        var rng = new System.Random(seed);
        float R() => (float)rng.NextDouble();

        var tier = TierOf(gem.ClarityGrade);
        var species = gem.Definition.Diaphaneity;
        var opaque = species == Diaphaneity.Opaque;
        var rough = gem.IsRough;
        var dome = gem.CutStyleName == "Cabochon";
        var diamond = gem.Definition.name != null && gem.Definition.name.StartsWith("Diamant");

        var q = new StoneQuality
        {
            Seed = seed,
            Tier = tier,
            StyleKey = (gem.Definition.displayName ?? gem.Definition.name ?? "").ToLowerInvariant() + "|" + (gem.Origin != null && gem.Origin.name != null ? gem.Origin.name : "").ToLowerInvariant(),
        };

        // Where the stone sits in its grade (the first draw, see PositionInTier): the worse it is inside the grade, the hazier it looks and the less it is worth.
        var position = R();

        // The inclusions of the species and of the deposit: a species full of silk and clouds (ruby, sapphire) can turn opaque with them.
        var profile = InclusionCatalog.For(gem.Definition, gem.Origin, gem.IsSynthetic);
        float Share(FeatureKind kind) => profile != null ? profile.Share(kind) : 0f;
        var silky = Mathf.Clamp01((Share(FeatureKind.Needle) + Share(FeatureKind.HorseTail) + Share(FeatureKind.Cloud)) * 1.4f);

        // Haze: nothing in a loupe-clean stone, a faint softness in an eye-clean one, clouds and a milky look in an included one. Heating clears the silk.
        float haze;
        switch (tier)
        {
            case 3: haze = Mathf.Lerp(0.50f, 0.98f, position); break;
            case 2: haze = Mathf.Lerp(0.18f, 0.50f, position); break;
            case 1: haze = Mathf.Lerp(0.02f, 0.14f, position); break;
            default: haze = Mathf.Lerp(0f, 0.04f, position); break;
        }
        if (gem.HasTreatment(TreatmentKind.Heated)) haze *= 0.7f;
        haze *= InclusionStyles.For(q.StyleKey).Haze;
        q.Haze = opaque ? 0f : haze;

        // Silk: the dense needles of rutile of the species that carry them, denser in the worse stones of a grade. Heating dissolves a part of it.
        float silk;
        switch (tier)
        {
            case 3: silk = silky * Mathf.Lerp(0.55f, 1f, position); break;
            case 2: silk = silky * Mathf.Lerp(0.12f, 0.50f, position); break;
            case 1: silk = silky * 0.06f; break;
            default: silk = 0f; break;
        }
        if (gem.HasTreatment(TreatmentKind.Heated)) silk *= 0.6f;
        q.Silk = Mathf.Clamp01(silk);
        q.Scatter = Mathf.Min(3f, q.Haze * 0.9f + q.Silk * 1.8f);

        // Transparency: the stones of a transparent species are not all as clear as glass, and the more included they are, the less. The worst of a heavily
        // included grade turn opaque (a ruby so full of rutile that no light goes through), sooner when the species carries silk.
        var shift = 0;
        switch (tier)
        {
            case 3:
                var opaqueFrom = Mathf.Lerp(0.88f, 0.62f, silky);
                shift = position > opaqueFrom ? (silky > 0.3f ? 3 : 2) : position > 0.30f ? 1 : 0;
                break;
            case 2: shift = position > 0.72f ? 1 : 0; break;
        }
        var room = species == Diaphaneity.Transparent ? (silky > 0.3f ? 3 : 2) : species == Diaphaneity.Opaque ? 0 : 1;
        q.LevelShift = Mathf.Min(shift, room);
        if (diamond) q.LevelShift = Mathf.Min(q.LevelShift, 1);

        // Polish and scratches: a stone cut by the player carries the cut quality it earned (0 best); a stone bought is mostly well polished, a few are dull.
        float dull;
        if (gem.CutQualityIndex >= 0) dull = new[] { 0f, 0.12f, 0.30f, 0.55f }[Mathf.Clamp(gem.CutQualityIndex, 0, 3)] + R() * 0.08f;
        else dull = Mathf.Pow(R(), 2.2f) * 0.6f + tier * 0.03f;
        if (opaque) dull *= 0.7f;
        q.Dull = rough ? 0f : Mathf.Clamp01(dull);
        q.Scratch = rough ? 0f : Mathf.Clamp01(q.Dull * 0.9f + (tier >= 2 ? 0.1f * R() : 0f) + (R() < 0.3f ? 0.15f : 0f));

        // Colour that is not even: bands and patches. Natural stones show them more; flame-fusion and other synthetics little.
        var zoning = Mathf.Pow(R(), 1.6f) * 0.7f;
        q.Zoning = gem.IsSynthetic ? zoning * 0.25f : zoning;

        // Proportions: most stones are close to the ideal; some are shallow (a window under the table) or deep (a dark middle).
        if (!rough && !dome)
        {
            float offset;
            var poor = gem.CutQualityIndex >= 0 ? new[] { 0f, 0f, 0.5f, 1f }[Mathf.Clamp(gem.CutQualityIndex, 0, 3)] : 0.18f;
            if (R() < poor) offset = (R() < 0.5f ? -1f : 1f) * Mathf.Lerp(0.20f, 0.32f, R());
            else offset = (R() - 0.5f) * (gem.CutQualityIndex >= 0 ? 0.06f : 0.16f);
            // A diamond's proportions are graded on its certificate: they stay close to the ideal here.
            q.DepthOffset = diamond ? Mathf.Clamp(offset, -0.06f, 0.06f) : offset;
        }

        // The kinds of inclusion that show: the mix of the species.
        q.Needles = Share(FeatureKind.Needle) + Share(FeatureKind.HorseTail) * 0.6f;
        q.Crystals = Share(FeatureKind.Crystal) + Share(FeatureKind.NegativeCrystal) * 0.5f;
        q.Clouds = Share(FeatureKind.Cloud) + Share(FeatureKind.Healing) + Share(FeatureKind.Fluid) + Share(FeatureKind.TwoPhase) + Share(FeatureKind.ThreePhase);
        q.Feathers = Share(FeatureKind.Fracture) + Share(FeatureKind.Feather) + Share(FeatureKind.Cleavage) + Share(FeatureKind.KmFeather);
        q.Bubbles = Share(FeatureKind.Bubble);
        var total = q.Needles + q.Crystals + q.Clouds + q.Feathers + q.Bubbles;
        if (total < 0.05f)
        {
            q.Crystals = 0.4f; q.Clouds = 0.3f; q.Feathers = 0.3f; q.Needles = 0f; q.Bubbles = 0f;
            total = 1f;
        }
        q.Needles /= total; q.Crystals /= total; q.Clouds /= total; q.Feathers /= total; q.Bubbles /= total;

        // The bodies of the volume: needles, crystals, cavities (liquid, gas, negative crystals), healed fissures (fingerprints), clouds, feathers and cracks, bubbles.
        q.VNeedles = Share(FeatureKind.Needle) + Share(FeatureKind.HorseTail);
        q.VCrystals = Share(FeatureKind.Crystal);
        q.VCavities = Share(FeatureKind.Fluid) + Share(FeatureKind.TwoPhase) + Share(FeatureKind.ThreePhase) + Share(FeatureKind.NegativeCrystal);
        q.VHealing = Share(FeatureKind.Healing);
        q.VClouds = Share(FeatureKind.Cloud);
        q.VFeathers = Share(FeatureKind.Fracture) + Share(FeatureKind.Feather) + Share(FeatureKind.Cleavage) + Share(FeatureKind.KmFeather);
        q.VBubbles = Share(FeatureKind.Bubble);
        var volume = q.VNeedles + q.VCrystals + q.VCavities + q.VHealing + q.VClouds + q.VFeathers + q.VBubbles;
        if (volume < 0.05f)
        {
            q.VCrystals = 0.30f; q.VCavities = 0.30f; q.VFeathers = 0.20f; q.VClouds = 0.20f;
            q.VNeedles = 0f; q.VHealing = 0f; q.VBubbles = 0f;
            volume = 1f;
        }
        q.VNeedles /= volume; q.VCrystals /= volume; q.VCavities /= volume; q.VHealing /= volume; q.VClouds /= volume; q.VFeathers /= volume; q.VBubbles /= volume;

        float Variant(FeatureKind kind, int variant) => profile != null ? profile.Share(kind, variant) : 0f;
        var needleTotal = Share(FeatureKind.Needle) + Share(FeatureKind.HorseTail);
        if (needleTotal > 0.001f)
        {
            q.SilkShare = Variant(FeatureKind.Needle, 0) / needleTotal;
            q.TubeShare = Variant(FeatureKind.Needle, 2) / needleTotal;
        }
        var cavityTotal = Share(FeatureKind.Fluid) + Share(FeatureKind.TwoPhase) + Share(FeatureKind.ThreePhase) + Share(FeatureKind.NegativeCrystal);
        if (cavityTotal > 0.001f)
        {
            q.Cav3Share = Share(FeatureKind.ThreePhase) / cavityTotal;
            q.CavJagShare = (Variant(FeatureKind.TwoPhase, 1) + Variant(FeatureKind.Fluid, 1)) / cavityTotal;
        }
        var crystalTotal = Share(FeatureKind.Crystal);
        if (crystalTotal > 0.001f)
        {
            q.DarkShare = (Variant(FeatureKind.Crystal, (int)CrystalVariant.Sulfide) + Variant(FeatureKind.Crystal, (int)CrystalVariant.Chromite)) / crystalTotal;
            q.MicaShare = Variant(FeatureKind.Crystal, (int)CrystalVariant.Mica) / crystalTotal;
            q.OctaShare = Variant(FeatureKind.Crystal, (int)CrystalVariant.Octahedron) / crystalTotal;
        }
        return q;
    }
}
