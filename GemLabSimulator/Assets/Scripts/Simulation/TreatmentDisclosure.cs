using UnityEngine;

// The resale of a treated stone, as taught in the course: a treatment has to be disclosed to the buyer. A stone sold as an honest treated stone
// goes for its real value, less the stigma of the market (heating is accepted everywhere, a dye or a lead glass filling much less). A stone sold
// without a word is paid as an untreated stone, but the buyer may test it: a treatment found afterwards costs the seller a fine and their reputation.
// The more visible the treatment, the bigger the risk. The rules live here, so the markets, the jeweller and the auctions share them.
public static class TreatmentDisclosure
{
    public const int FinePercent = 20;
    public const int ReputationLoss = 2;

    // The share of the value the market takes off a treated stone that is declared, on top of what the treatment already did to its value.
    public static float Stigma(GemInstance gem)
    {
        if (gem == null || !gem.IsTreated) return 0f;

        var worst = 0f;
        var count = 0;
        void Consider(TreatmentKind kind, float stigma)
        {
            if (!gem.HasTreatment(kind)) return;
            count++;
            worst = Mathf.Max(worst, stigma);
        }

        Consider(TreatmentKind.Heated, 0.02f);
        Consider(TreatmentKind.Irradiated, 0.08f);
        Consider(TreatmentKind.Filled, gem.FillerKind == FillerFamily.Glass ? 0.18f : gem.FillerKind == FillerFamily.Flux ? 0.14f : 0.10f);
        Consider(TreatmentKind.Bleached, 0.12f);
        Consider(TreatmentKind.Dyed, 0.20f);
        Consider(TreatmentKind.Coated, 0.15f);
        Consider(TreatmentKind.Diffused, gem.DiffusionDepthMm >= 0.5f ? 0.08f : 0.18f);
        Consider(TreatmentKind.Laser, gem.LaserName != null && gem.LaserName.Contains("KM") ? 0.12f : 0.05f);
        Consider(TreatmentKind.Hpht, 0.10f);

        // A treatment the stone has that no rule above knows (the old random treatments): a modest stigma.
        if (count == 0) worst = 0.06f;
        return Mathf.Clamp01(worst + 0.02f * Mathf.Max(0, count - 1));
    }

    // How easy the treatment is to find for a buyer who tests the stone with the usual instruments (0 to 1): the most visible one counts.
    public static float Detectability(GemInstance gem)
    {
        if (gem == null || !gem.IsTreated) return 0f;

        var best = 0f;
        var count = 0;
        void Consider(TreatmentKind kind, float visible)
        {
            if (!gem.HasTreatment(kind)) return;
            count++;
            best = Mathf.Max(best, visible);
        }

        Consider(TreatmentKind.Heated, 0.25f);
        Consider(TreatmentKind.Irradiated, 0.45f);
        Consider(TreatmentKind.Filled, gem.FillerKind == FillerFamily.Glass ? 0.85f : 0.65f);
        Consider(TreatmentKind.Bleached, 0.5f);
        Consider(TreatmentKind.Dyed, 0.85f);
        Consider(TreatmentKind.Coated, 0.8f);
        Consider(TreatmentKind.Diffused, gem.DiffusionElement == "Béryllium" ? (gem.DiffusionDepthMm >= 0.5f ? 0.15f : 0.3f) : 0.7f);
        Consider(TreatmentKind.Laser, gem.LaserName != null && gem.LaserName.Contains("KM") ? 0.45f : 0.5f);
        Consider(TreatmentKind.Hpht, 0.2f);

        if (count == 0) best = 0.3f;
        return Mathf.Clamp01(best + 0.03f * Mathf.Max(0, count - 1));
    }

    public static string RiskLabel(float chance)
    {
        return chance < 0.15f ? "faible" : chance < 0.4f ? "moyen" : chance < 0.65f ? "élevé" : "très élevé";
    }

    // The price when the treatment is declared: the real value of the stone, less the stigma.
    public static int DeclaredValue(GemInstance gem)
    {
        return Mathf.Max(1, Mathf.RoundToInt(gem.FinalValue * (1f - Stigma(gem))));
    }

    // The price when nothing is said: an untreated stone.
    public static int HiddenValue(GemInstance gem) => gem.UntreatedValue;

    // The fine of a seller who was caught, on the price they asked.
    public static int FineOn(int price) => Mathf.Max(20, Mathf.RoundToInt(price * FinePercent / 100f));

    // Whether a given buyer finds the treatment: the same stone, the same buyer, always the same answer, so reopening the window never re-rolls it.
    public static bool BuyerFinds(GemInstance gem, int buyerSeed, float expertise)
    {
        var chance = Mathf.Clamp(Detectability(gem) * expertise, 0.05f, 0.95f);
        var hash = StoneOptics.StableHash((gem.SpecimenId ?? "") + "|inspect|" + buyerSeed);
        return (hash % 1000u) / 1000f < chance;
    }

    public static float ChanceFor(GemInstance gem, float expertise) => Mathf.Clamp(Detectability(gem) * expertise, 0.05f, 0.95f);
}
