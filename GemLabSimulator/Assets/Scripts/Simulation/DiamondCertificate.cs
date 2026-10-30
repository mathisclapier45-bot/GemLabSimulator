using System;
using UnityEngine;

// Certificate delivered by a diamond dealer with each stone. Data only for now: its design comes later.
[Serializable]
public class DiamondCertificate
{
    public string number;        // e.g. "GL-482913"
    public string issuer;        // the dealer house that issued it
    public int issueDay;         // in-game day of the sale
    public string colorGrade;    // D..Z
    public string clarityGrade;  // FL..I3
    public float caratWeight;
    public string shape;
    public string cutStyle;
}

// Grading scales and value model shared by every diamond dealer.
public static class DiamondGrading
{
    // Colourless to light yellow, D (best) to Z.
    public const string ColorScale = "DEFGHIJKLMNOPQRSTUVWXYZ";

    public static readonly string[] ClarityScale =
    {
        "FL", "IF", "VVS1", "VVS2", "VS1", "VS2", "SI1", "SI2", "I1", "I2", "I3",
    };

    private static readonly float[] ColorFactor =
    {
        1.40f, 1.30f, 1.20f, 1.10f, 1.00f, 0.90f, 0.80f, 0.70f, 0.62f, 0.55f, 0.48f, 0.43f,
        0.38f, 0.34f, 0.31f, 0.28f, 0.26f, 0.24f, 0.22f, 0.20f, 0.18f, 0.17f, 0.16f,
    };

    private static readonly float[] ClarityFactor =
    {
        1.50f, 1.35f, 1.20f, 1.12f, 1.05f, 1.00f, 0.88f, 0.75f, 0.55f, 0.40f, 0.28f,
    };

    public static string Color(int index) => ColorScale[Mathf.Clamp(index, 0, ColorScale.Length - 1)].ToString();

    public static string Clarity(int index) => ClarityScale[Mathf.Clamp(index, 0, ClarityScale.Length - 1)];

    // Value of a stone in the game's currency: reference 1 ct, G colour, VS2 = about 1100.
    public static int Value(float carat, int colorIndex, int clarityIndex)
    {
        var size = 1100f * Mathf.Pow(carat, 1.45f) * (carat >= 3f ? 1f + 0.04f * (carat - 3f) : 1f);
        var value = size * ColorFactor[Mathf.Clamp(colorIndex, 0, ColorFactor.Length - 1)]
                         * ClarityFactor[Mathf.Clamp(clarityIndex, 0, ClarityFactor.Length - 1)];
        return Mathf.Max(10, Mathf.RoundToInt(value));
    }

    // The game's four clarity tiers (used by the loupe and microscope) each cover several diamond grades.
    public static string GameClarityTier(int clarityIndex)
    {
        if (clarityIndex <= 4) return "Loupe clean";       // FL .. VS1
        if (clarityIndex <= 6) return "Eye-clean";         // VS2, SI1
        if (clarityIndex <= 8) return "Inclus";            // SI2, I1
        return "Fortement inclus";                         // I2, I3
    }

}
