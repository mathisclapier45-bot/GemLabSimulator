using UnityEngine;

// How a dye bath did on a stone.
public enum DyeOutcome { Ruined, NoEffect, Uneven, Good, Excellent }

public enum DyeKind { Organic, Mineral, Carbon }

// One of the pots of the dye room: a colour and the chemistry that carries it.
public class DyeBath
{
    public string Name;
    public string Chemistry;
    public string Description;
    public Color Liquid;
    public float Hue;                // the colour the dye gives, in the HSB model of the gem colours
    public float Saturation;
    public float Brightness;
    public int Fee;
    public float DurationFactor;
    public float Strength;           // how much colour the dye carries
    public DyeKind Kind;
}

public struct DyeQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public float Uptake;             // 0..1, how much dye the material takes in
    public float Regularity;         // 0..1, how evenly it spreads
    public float Risk;               // 0..1, chance that the dye is unstable and runs
    public string Advice;
    public bool Bleached;
    public float PreviewHue;
    public float PreviewSaturation;
    public float PreviewBrightness;

    public string UptakeLabel => Uptake < 0.25f ? "faible" : Uptake < 0.5f ? "moyenne" : Uptake < 0.75f ? "forte" : "très forte";
    public string RegularityLabel => Regularity < 0.45f ? "inégale" : Regularity < 0.65f ? "moyenne" : Regularity < 0.8f ? "bonne" : "très régulière";
    public string RiskLabel => Risk < 0.08f ? "très faible" : Risk < 0.2f ? "faible" : Risk < 0.4f ? "modéré" : "élevé";
}

public struct DyeResult
{
    public DyeOutcome Outcome;
    public float Quality;
    public float ValueFactor;
    public float Hue;
    public float Saturation;
    public float Brightness;
    public string Description;
    public string Message;
    public string Bath;              // the pot the stone came out of: what the detection tools read of the dye

    public bool IsRuined => Outcome == DyeOutcome.Ruined;
}

// The dyeing of gems, as taught in the course: any material that is more or less permeable, that is porous or has fissures reaching the
// surface, can be dyed. It changes the colour of some materials, or gives a more even colour to materials that may have been bleached
// first, like jadeite and cultured pearls. The colour follows the porosity: it is uneven and concentrates in fissures and veins, and most
// organic dyes leave a mark on a cotton ball soaked in acetone.
public static class DyeingCatalog
{
    public const int PotCount = 6;

    public static readonly DyeBath[] Baths =
    {
        new DyeBath { Name = "Rouge", Chemistry = "Colorant organique (rhodamine)", Liquid = new Color(0.84f, 0.16f, 0.21f), Hue = 355f, Saturation = 0.85f, Brightness = 0.78f,
            Fee = 12, DurationFactor = 0.8f, Strength = 1.0f, Kind = DyeKind.Organic,
            Description = "Un colorant organique rapide : le rouge du corail, de l'ambre cerise ou du faux rubis." },
        new DyeBath { Name = "Rose", Chemistry = "Colorant organique (fuchsine)", Liquid = new Color(0.94f, 0.47f, 0.65f), Hue = 335f, Saturation = 0.50f, Brightness = 0.95f,
            Fee = 12, DurationFactor = 0.8f, Strength = 0.95f, Kind = DyeKind.Organic,
            Description = "Un rose tendre, très employé sur les perles et le jade lavande." },
        new DyeBath { Name = "Bleu de Prusse", Chemistry = "Ferrocyanure + sel de fer", Liquid = new Color(0.12f, 0.30f, 0.66f), Hue = 222f, Saturation = 0.85f, Brightness = 0.60f,
            Fee = 22, DurationFactor = 1.3f, Strength = 0.85f, Kind = DyeKind.Mineral,
            Description = "Un pigment minéral qui se forme dans la pierre : lent, mais stable. Le bleu des agates et de la turquoise." },
        new DyeBath { Name = "Vert chrome", Chemistry = "Sels de chrome", Liquid = new Color(0.15f, 0.60f, 0.31f), Hue = 140f, Saturation = 0.80f, Brightness = 0.55f,
            Fee = 25, DurationFactor = 1.3f, Strength = 0.85f, Kind = DyeKind.Mineral,
            Description = "Des sels de chrome qui donnent un vert d'émeraude stable : jade, agate, béryl." },
        new DyeBath { Name = "Noir carbone", Chemistry = "Sucre + acide sulfurique", Liquid = new Color(0.12f, 0.12f, 0.15f), Hue = 240f, Saturation = 0.10f, Brightness = 0.10f,
            Fee = 15, DurationFactor = 1.0f, Strength = 0.9f, Kind = DyeKind.Carbon,
            Description = "Le sucre imbibe la pierre, l'acide le carbonise : un noir profond, mais seulement dans une matière poreuse." },
        new DyeBath { Name = "Violet lavande", Chemistry = "Colorant organique", Liquid = new Color(0.55f, 0.35f, 0.75f), Hue = 275f, Saturation = 0.55f, Brightness = 0.75f,
            Fee = 12, DurationFactor = 0.8f, Strength = 1.0f, Kind = DyeKind.Organic,
            Description = "Le violet « lavande » du jade teint, vif mais fragile à la lumière." },
    };

    // What each species takes: how porous it is (0 to 1) and the days a dye needs to go in.
    private struct Stuff
    {
        public float Porosity;
        public int Days;
    }

    private static bool TryMaterial(GemDefinition definition, out Stuff material)
    {
        material = default;
        var name = definition != null ? definition.name : "";
        switch (name)
        {
            case "Perle": material = new Stuff { Porosity = 0.55f, Days = 2 }; return true;
            case "Perle_Tahiti": material = new Stuff { Porosity = 0.50f, Days = 3 }; return true;
            case "Corail": material = new Stuff { Porosity = 0.70f, Days = 2 }; return true;
            case "Jade_Jadeite": material = new Stuff { Porosity = 0.45f, Days = 4 }; return true;
            case "Jade_Nephrite": material = new Stuff { Porosity = 0.35f, Days = 4 }; return true;
            case "Opale_Noble": material = new Stuff { Porosity = 0.55f, Days = 3 }; return true;
            case "Opale_De_Feu": material = new Stuff { Porosity = 0.50f, Days = 3 }; return true;
            case "Turquoise": material = new Stuff { Porosity = 0.80f, Days = 2 }; return true;
            case "Turquoise_Stabilisee": material = new Stuff { Porosity = 0.30f, Days = 3 }; return true;
            case "Agate": material = new Stuff { Porosity = 0.70f, Days = 3 }; return true;
            case "Lapis_Lazuli": material = new Stuff { Porosity = 0.50f, Days = 3 }; return true;
            case "Ambre": material = new Stuff { Porosity = 0.40f, Days = 2 }; return true;
            case "Quartz": material = new Stuff { Porosity = 0.12f, Days = 5 }; return true;
            case "Corindon_Rubis":
            case "Padparadscha":
            case "Emeraude":
            case "Goshenite":
            case "Heliodore":
            case "Morganite":
                material = new Stuff { Porosity = 0.15f, Days = 5 };
                return true;
        }
        if (name.StartsWith("Saphir_") && name != "Saphir_Incolore")
        {
            material = new Stuff { Porosity = 0.15f, Days = 5 };
            return true;
        }
        return false;
    }

    public static bool IsBleached(GemInstance gem) => gem != null && !string.IsNullOrEmpty(gem.TreatmentGrade) && gem.TreatmentGrade.StartsWith("Blanchi");

    public static bool CanDye(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go into a dye bath, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        if (!TryMaterial(gem.Definition, out _))
            return "Ce matériau ne se teint pas ici : il faut une matière poreuse ou fissurée (perle, corail, jade, agate, turquoise...).";
        if (gem.IsSynthetic) return "Inutile de teindre une pierre synthétique.";
        if (!string.IsNullOrEmpty(gem.TreatmentGrade) && gem.TreatmentGrade.Contains("eint"))
            return "Cette pierre a déjà été teinte : un second bain ferait des taches.";
        return null;
    }

    private static float Uptake(Stuff material, DyeBath bath)
    {
        var factor = 1f;
        if (bath.Kind == DyeKind.Carbon && material.Porosity < 0.4f) factor = 0.4f;
        if (bath.Kind == DyeKind.Mineral && material.Porosity < 0.3f) factor = 0.5f;
        return Mathf.Clamp01(material.Porosity * bath.Strength * factor * 1.3f);
    }

    private static float Regularity(Stuff material, DyeBath bath, bool bleached)
    {
        return Mathf.Clamp01(0.35f + 0.55f * material.Porosity + (bleached ? 0.2f : 0f) + (bath.Kind == DyeKind.Mineral ? 0.05f : 0f));
    }

    private static float RiskOf(Stuff material, DyeBath bath)
    {
        if (bath.Kind == DyeKind.Organic) return (1f - material.Porosity) * 0.25f;
        if (bath.Kind == DyeKind.Carbon) return material.Porosity < 0.4f ? 0.30f : 0.08f;
        return 0.05f;
    }

    private static float Quality(float uptake, float regularity) => uptake * (0.45f + 0.55f * regularity);

    public static DyeQuote GetQuote(int bathIndex, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (bathIndex < 0 || bathIndex >= Baths.Length || refusal != null)
            return new DyeQuote { IsValid = false, Reason = refusal ?? "Choisissez un bain." };

        TryMaterial(gem.Definition, out var material);
        var bath = Baths[bathIndex];
        var bleached = IsBleached(gem);
        var uptake = Uptake(material, bath);
        var regularity = Regularity(material, bath, bleached);
        var quality = Quality(uptake, regularity);
        var blend = Mathf.Clamp01(quality * 1.25f);

        return new DyeQuote
        {
            IsValid = true,
            Days = Mathf.Max(1, Mathf.RoundToInt(material.Days * bath.DurationFactor)),
            Fee = bath.Fee + Mathf.RoundToInt(gem.CaratWeight * 2f),
            Uptake = uptake,
            Regularity = regularity,
            Risk = RiskOf(material, bath),
            Bleached = bleached,
            Advice = AdviceFor(material, bath, bleached),
            PreviewHue = Mathf.LerpAngle(gem.ColorHue, bath.Hue, blend),
            PreviewSaturation = Mathf.Lerp(gem.ColorSaturation, bath.Saturation, blend),
            PreviewBrightness = Mathf.Lerp(gem.ColorBrightness, bath.Brightness, blend * 0.9f),
        };
    }

    private static string AdviceFor(Stuff material, DyeBath bath, bool bleached)
    {
        string advice;
        if (material.Porosity < 0.25f) advice = "Peu poreux : la couleur ne prendra que dans les fissures, de façon inégale.";
        else if (bath.Kind == DyeKind.Carbon && material.Porosity < 0.4f) advice = "Le noir se fait par carbonisation du sucre : il prend mal dans une matière compacte.";
        else if (bath.Kind == DyeKind.Mineral && material.Porosity < 0.3f) advice = "Les sels minéraux pénètrent mal un matériau compact : la couleur sera pâle.";
        else if (bath.Kind == DyeKind.Organic) advice = "Colorant organique : rapide et vif, mais il peut déteindre.";
        else if (bath.Kind == DyeKind.Mineral) advice = "Sels minéraux : plus lents, mais la couleur est stable et ne déteint pas.";
        else advice = "Un noir profond et stable dans une matière poreuse.";
        if (bleached) advice += " Déjà blanchie : la couleur prendra plus régulièrement.";
        return advice;
    }

    // What comes out of the bath. The same stone in the same bath always gives the same result.
    public static DyeResult Resolve(int bathIndex, GemInstance gem)
    {
        var quote = GetQuote(bathIndex, gem);
        if (!quote.IsValid) return new DyeResult { Outcome = DyeOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        var bath = Baths[bathIndex];
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|dye|{bathIndex}");
        var rollRisk = (hash % 1000u) / 1000f;
        var quality = Quality(quote.Uptake, quote.Regularity);
        var baseColor = new DyeResult { Hue = gem.ColorHue, Saturation = gem.ColorSaturation, Brightness = gem.ColorBrightness };

        if (rollRisk < quote.Risk && quote.Uptake >= 0.15f)
        {
            return new DyeResult
            {
                Outcome = DyeOutcome.Ruined,
                Quality = 0f,
                ValueFactor = 0.5f,
                Hue = Mathf.LerpAngle(gem.ColorHue, bath.Hue, 0.5f),
                Saturation = gem.ColorSaturation * 0.7f,
                Brightness = Mathf.Max(0.05f, gem.ColorBrightness - 0.1f),
                Description = "Teinture instable : la couleur coule, tache la pierre et laisse une forte marque sur un coton imbibé d'acétone.",
                Message = "Zut : la teinture n'a pas tenu. La couleur coule et la pierre est tachée.",
            };
        }

        if (quote.Uptake < 0.15f)
        {
            baseColor.Outcome = DyeOutcome.NoEffect;
            baseColor.ValueFactor = 1f;
            baseColor.Description = "Bain de teinture sans effet visible.";
            baseColor.Message = "La couleur n'a presque pas pris : la matière est trop compacte pour ce colorant.";
            return baseColor;
        }

        var blend = Mathf.Clamp01(quality * 1.25f);
        var outcome = quality >= 0.6f && quote.Regularity >= 0.7f ? DyeOutcome.Excellent
            : quality >= 0.4f && quote.Regularity >= 0.55f ? DyeOutcome.Good
            : DyeOutcome.Uneven;

        var result = new DyeResult
        {
            Outcome = outcome,
            Quality = quality,
            Hue = Mathf.LerpAngle(gem.ColorHue, bath.Hue, blend),
            Saturation = Mathf.Lerp(gem.ColorSaturation, bath.Saturation, blend) * (outcome == DyeOutcome.Uneven ? 0.85f : 1f),
            Brightness = Mathf.Lerp(gem.ColorBrightness, bath.Brightness, blend * 0.9f),
        };
        switch (outcome)
        {
            case DyeOutcome.Excellent:
                result.ValueFactor = 1.35f;
                result.Description = "Teinture profonde et régulière ; la couleur reste concentrée dans les pores aux forts grossissements et laisse une marque sur un coton imbibé d'acétone.";
                result.Message = "Magnifique : la couleur a pris en profondeur, régulière et franche.";
                break;
            case DyeOutcome.Good:
                result.ValueFactor = 1.15f;
                result.Description = "Teinture assez régulière, plus soutenue dans les pores et les veines ; marque sur un coton imbibé d'acétone.";
                result.Message = "Une belle couleur, un peu plus soutenue dans les veines.";
                break;
            default:
                result.ValueFactor = 0.8f;
                result.Description = "Couleur inégale, concentrée dans les fissures et les veines : signature d'une teinture.";
                result.Message = "La couleur est inégale : elle s'est logée dans les fissures et les veines.";
                break;
        }
        return result;
    }
}
