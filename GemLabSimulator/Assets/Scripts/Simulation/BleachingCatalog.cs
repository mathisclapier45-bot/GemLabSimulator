using UnityEngine;

// How well a bleaching bath did on a stone.
public enum BleachOutcome { Ruined, NoEffect, Slight, Good, Excellent }

// One of the vats of the treatments laboratory: an acid or an oxidant, with its formula, its price and its speed.
public class BleachBath
{
    public string Name;
    public string Formula;
    public string Description;
    public Color Liquid;
    public int Fee;
    public float DurationFactor;     // multiplies the days a material needs
    public int Bubbles;              // how lively the vat is in the window
}

public struct BleachQuote
{
    public bool IsValid;
    public string Reason;
    public int Days;
    public int Fee;
    public float Effectiveness;      // 0..1, how much of the colour the bath can take out of this material
    public float Risk;               // 0..1, chance that the bath attacks the stone
    public string Advice;

    public string EffectivenessLabel => Effectiveness < 0.3f ? "faible" : Effectiveness < 0.55f ? "moyenne" : Effectiveness < 0.8f ? "forte" : "très forte";
    public string RiskLabel => Risk < 0.08f ? "très faible" : Risk < 0.2f ? "faible" : Risk < 0.4f ? "modéré" : Risk < 0.7f ? "élevé" : "destructeur";
}

public struct BleachResult
{
    public BleachOutcome Outcome;
    public float Quality;            // 0..1
    public float ValueFactor;
    public string Description;       // for the treatment line of the stone sheet
    public string Message;           // what the technician says when the stone comes out of the bath

    public bool IsRuined => Outcome == BleachOutcome.Ruined;
}

// The bleaching of gems, as taught in the course: a bath of chemicals lightens or removes the colour of a gem. It is done with care, because
// a bath that is too strong attacks the material and its durability. Only four materials are bleached: pearls, coral, jadeite and tiger's
// eye (quartz). Pearls and coral are calcium carbonate, so no acid can touch them; jadeite and tiger's eye lose their iron stains in acids.
public static class BleachingCatalog
{
    public const int VatCount = 6;

    public static readonly BleachBath[] Baths =
    {
        new BleachBath { Name = "Eau oxygénée 3 %", Formula = "H2O2 à 3 %", Liquid = new Color(0.59f, 0.84f, 0.90f), Fee = 15, DurationFactor = 1.5f, Bubbles = 4,
            Description = "L'oxydant le plus doux : sans danger pour la nacre et le corail, mais lent." },
        new BleachBath { Name = "Eau oxygénée 30 %", Formula = "H2O2 à 30 %", Liquid = new Color(0.47f, 0.88f, 0.94f), Fee = 25, DurationFactor = 1.0f, Bubbles = 9,
            Description = "Le blanchiment classique des perles : plus rapide, il demande de la surveillance." },
        new BleachBath { Name = "Eau de Javel", Formula = "NaClO", Liquid = new Color(0.77f, 0.89f, 0.36f), Fee = 10, DurationFactor = 0.7f, Bubbles = 6,
            Description = "L'hypochlorite de sodium décolore vite, mais ternit les matières organiques." },
        new BleachBath { Name = "Acide oxalique", Formula = "H2C2O4", Liquid = new Color(0.89f, 0.78f, 0.95f), Fee = 30, DurationFactor = 1.0f, Bubbles = 5,
            Description = "Un acide organique qui dissout les oxydes de fer : jade et quartz s'y éclaircissent." },
        new BleachBath { Name = "Acide chlorhydrique 10 %", Formula = "HCl à 10 %", Liquid = new Color(0.94f, 0.81f, 0.27f), Fee = 40, DurationFactor = 0.5f, Bubbles = 8,
            Description = "Un acide fort, rapide, qui attaque tout carbonate : il dissoudrait une perle." },
        new BleachBath { Name = "Bain mixte", Formula = "H2SO4 + H2O2", Liquid = new Color(0.75f, 0.34f, 0.59f), Fee = 60, DurationFactor = 0.35f, Bubbles = 12,
            Description = "Acide sulfurique et peroxyde : le plus puissant des bains, au prix d'un vrai risque." },
    };

    private enum Stuff { None, Pearl, TahitiPearl, Coral, Jadeite, TigerEye }

    // Days at 1.0 of the bath factor, for each material: pearls and coral are quick, jadeite is a slow stone to bleach.
    private static readonly int[] BaseDays = { 0, 3, 4, 2, 6, 5 };

    // How much value a perfect bleaching adds to each material.
    private static readonly float[] ValueGain = { 0f, 0.55f, 0.50f, 0.45f, 0.40f, 0.35f };

    // [material][bath]: effectiveness and risk of each bath on each material.
    private static readonly float[][] Effectiveness =
    {
        null,
        new[] { 0.55f, 0.85f, 0.60f, 0.20f, 0.10f, 0.10f },     // pearl
        new[] { 0.50f, 0.80f, 0.55f, 0.20f, 0.10f, 0.10f },     // Tahiti pearl
        new[] { 0.60f, 0.85f, 0.70f, 0.25f, 0.10f, 0.10f },     // coral
        new[] { 0.25f, 0.45f, 0.55f, 0.85f, 0.90f, 0.95f },     // jadeite
        new[] { 0.20f, 0.50f, 0.70f, 0.90f, 0.85f, 0.95f },     // tiger's eye
    };

    private static readonly float[][] Risk =
    {
        null,
        new[] { 0.03f, 0.12f, 0.45f, 0.80f, 0.95f, 0.98f },
        new[] { 0.04f, 0.15f, 0.50f, 0.80f, 0.95f, 0.98f },
        new[] { 0.05f, 0.15f, 0.40f, 0.75f, 0.95f, 0.98f },
        new[] { 0.02f, 0.06f, 0.10f, 0.10f, 0.25f, 0.40f },
        new[] { 0.02f, 0.06f, 0.10f, 0.10f, 0.30f, 0.45f },
    };

    private static Stuff StuffOf(GemDefinition definition)
    {
        switch (definition != null ? definition.name : "")
        {
            case "Perle": return Stuff.Pearl;
            case "Perle_Tahiti": return Stuff.TahitiPearl;
            case "Corail": return Stuff.Coral;
            case "Jade_Jadeite": return Stuff.Jadeite;
            case "Oeil_de_Tigre": return Stuff.TigerEye;
            default: return Stuff.None;
        }
    }

    private static bool IsCarbonate(Stuff material) => material == Stuff.Pearl || material == Stuff.TahitiPearl || material == Stuff.Coral;

    public static bool CanBleach(GemInstance gem) => string.IsNullOrEmpty(Refusal(gem));

    // Why a stone cannot go into a bath, or null.
    public static string Refusal(GemInstance gem)
    {
        if (gem == null) return "Aucune pierre.";
        if (StuffOf(gem.Definition) == Stuff.None)
            return "Ce matériau ne se blanchit pas : on ne blanchit que les perles, le corail, la jadéite et l'œil-de-tigre.";
        if (gem.IsSynthetic) return "Inutile de blanchir une pierre synthétique.";
        if (!string.IsNullOrEmpty(gem.TreatmentGrade) && gem.TreatmentGrade.Contains("eint"))
            return "Une pierre teinte ne se blanchit pas : le bain emporterait la couleur. On blanchit d'abord, puis on teint.";
        if (!string.IsNullOrEmpty(gem.TreatmentGrade) && gem.TreatmentGrade.StartsWith("Blanchi"))
            return "Cette pierre a déjà été blanchie : un second bain l'achèverait.";
        return null;
    }

    public static BleachQuote GetQuote(int bathIndex, GemInstance gem)
    {
        var refusal = Refusal(gem);
        if (bathIndex < 0 || bathIndex >= Baths.Length || refusal != null)
            return new BleachQuote { IsValid = false, Reason = refusal ?? "Choisissez un bain." };

        var material = StuffOf(gem.Definition);
        var bath = Baths[bathIndex];
        var eff = Effectiveness[(int)material][bathIndex];
        var risk = Risk[(int)material][bathIndex];
        return new BleachQuote
        {
            IsValid = true,
            Days = Mathf.Max(1, Mathf.RoundToInt(BaseDays[(int)material] * bath.DurationFactor)),
            Fee = bath.Fee + Mathf.RoundToInt(gem.CaratWeight * 3f),
            Effectiveness = eff,
            Risk = risk,
            Advice = AdviceFor(material, bathIndex, risk),
        };
    }

    private static string AdviceFor(Stuff material, int bath, float risk)
    {
        if (IsCarbonate(material))
        {
            var what = material == Stuff.Coral ? "le corail" : "la nacre";
            if (bath >= 3) return $"Déconseillé : les acides dissolvent le carbonate de calcium. {char.ToUpperInvariant(what[0])}{what.Substring(1)} serait détruit{(material == Stuff.Coral ? "" : "e")}.";
            if (bath == 2) return $"Risqué : l'eau de Javel ternit {what} et la rend crayeuse.";
            if (bath == 0) return "Sûr : l'eau oxygénée diluée est le bain des perles, mais il faut de la patience.";
            return "Le bain classique : bon résultat, à surveiller pour ne pas attaquer la surface.";
        }
        if (bath <= 1) return "Bain doux : sans danger, mais peu efficace sur ce matériau.";
        if (bath == 2) return "L'eau de Javel éclaircit le matériau sans l'attaquer trop.";
        if (bath == 3) return "Le bain adapté : l'acide oxalique dissout les oxydes de fer sans abîmer la pierre.";
        if (bath == 4) return "Rapide et très efficace, mais l'acide peut piquer la surface.";
        return risk >= 0.35f ? "Le plus puissant, le plus rapide... et le plus risqué : la pierre peut en sortir fragilisée." : "Très puissant et rapide.";
    }

    // What comes out of the bath. The same stone in the same bath always gives the same result, so it cannot be redone by reloading a save.
    public static BleachResult Resolve(int bathIndex, GemInstance gem)
    {
        var quote = GetQuote(bathIndex, gem);
        if (!quote.IsValid) return new BleachResult { Outcome = BleachOutcome.NoEffect, ValueFactor = 1f, Message = quote.Reason };

        var material = StuffOf(gem.Definition);
        var hash = StoneOptics.StableHash($"{gem.SpecimenId}|bleach|{bathIndex}");
        var rollRisk = (hash % 1000u) / 1000f;
        var rollQuality = ((hash / 1000u) % 1000u) / 1000f;

        if (rollRisk < quote.Risk)
        {
            return new BleachResult
            {
                Outcome = BleachOutcome.Ruined,
                Quality = 0f,
                ValueFactor = 0.45f,
                Description = IsCarbonate(material)
                    ? "Surface attaquée par le bain : aspect terne et crayeux, nacre ou corail piqué, matière fragilisée."
                    : "Surface piquée par l'acide : aspect mat, matière fragilisée en profondeur.",
                Message = IsCarbonate(material)
                    ? "Quel dommage : le bain a rongé la surface. La pierre est terne, crayeuse et fragile."
                    : "Le bain était trop agressif : la surface est piquée et la pierre s'est fragilisée.",
            };
        }

        var quality = Mathf.Clamp01(quote.Effectiveness * (0.85f + 0.3f * rollQuality));
        var outcome = quality >= 0.75f ? BleachOutcome.Excellent : quality >= 0.5f ? BleachOutcome.Good : quality >= 0.3f ? BleachOutcome.Slight : BleachOutcome.NoEffect;
        var gain = ValueGain[(int)material];

        switch (outcome)
        {
            case BleachOutcome.Excellent:
                return new BleachResult { Outcome = outcome, Quality = quality, ValueFactor = 1f + gain * quality,
                    Description = "Couleur nettement éclaircie et homogène après un bain de blanchiment.",
                    Message = "Superbe : la couleur est claire et régulière, sans la moindre attaque de la surface." };
            case BleachOutcome.Good:
                return new BleachResult { Outcome = outcome, Quality = quality, ValueFactor = 1f + gain * quality,
                    Description = "Couleur éclaircie et plus uniforme après un bain de blanchiment.",
                    Message = "Beau travail : la pierre est plus claire et les taches ont disparu." };
            case BleachOutcome.Slight:
                return new BleachResult { Outcome = outcome, Quality = quality, ValueFactor = 1f + gain * quality,
                    Description = "Couleur légèrement éclaircie après un bain de blanchiment.",
                    Message = "Le bain a fait son effet, mais discrètement : la pierre est à peine plus claire." };
            default:
                return new BleachResult { Outcome = outcome, Quality = quality, ValueFactor = 1f,
                    Description = "Bain de blanchiment sans effet visible.",
                    Message = "Rien à signaler : ce bain n'a presque rien changé à la couleur." };
        }
    }
}
