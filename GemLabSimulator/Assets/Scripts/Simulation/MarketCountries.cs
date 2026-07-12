using System.Text;
using UnityEngine;

public static class MarketCountries
{
    public class Country
    {
        public string Name;
        public string[] MatchKeywords;
        public bool FlagVertical;
        public int[] FlagBandHeights;
        public Color[] FlagColors;
    }

    public static readonly Country[] Pool =
    {
        new Country { Name = "Sri Lanka", MatchKeywords = new[] { "Sri Lanka" },
            FlagVertical = false, FlagBandHeights = new[] { 8, 8 },
            FlagColors = new[] { new Color(0.85f, 0.65f, 0.13f), new Color(0.45f, 0.10f, 0.15f) } },
        new Country { Name = "Inde", MatchKeywords = new[] { "Inde" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { new Color(0.95f, 0.45f, 0.10f), Color.white, new Color(0.10f, 0.55f, 0.25f) } },
        new Country { Name = "Myanmar", MatchKeywords = new[] { "Myanmar", "Birmanie" },
            FlagVertical = false, FlagBandHeights = new[] { 6, 5, 5 },
            FlagColors = new[] { new Color(0.95f, 0.80f, 0.15f), new Color(0.15f, 0.60f, 0.35f), new Color(0.75f, 0.15f, 0.15f) } },
        new Country { Name = "États-Unis", MatchKeywords = new[] { "États-Unis" },
            FlagVertical = false, FlagBandHeights = new[] { 6, 5, 5 },
            FlagColors = new[] { new Color(0.15f, 0.20f, 0.45f), Color.white, new Color(0.70f, 0.10f, 0.15f) } },
        new Country { Name = "Canada", MatchKeywords = new[] { "Canada" },
            FlagVertical = true, FlagBandHeights = new[] { 4, 8, 4 },
            FlagColors = new[] { new Color(0.75f, 0.10f, 0.15f), Color.white, new Color(0.75f, 0.10f, 0.15f) } },
        new Country { Name = "Afrique du Sud", MatchKeywords = new[] { "Afrique du Sud" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { new Color(0.10f, 0.55f, 0.25f), new Color(0.85f, 0.70f, 0.15f), new Color(0.10f, 0.10f, 0.10f) } },
        new Country { Name = "Brésil", MatchKeywords = new[] { "Brésil" },
            FlagVertical = false, FlagBandHeights = new[] { 8, 8 },
            FlagColors = new[] { new Color(0.10f, 0.50f, 0.25f), new Color(0.90f, 0.78f, 0.15f) } },
        new Country { Name = "Colombie", MatchKeywords = new[] { "Colombie" },
            FlagVertical = false, FlagBandHeights = new[] { 8, 4, 4 },
            FlagColors = new[] { new Color(0.95f, 0.80f, 0.10f), new Color(0.15f, 0.25f, 0.65f), new Color(0.75f, 0.10f, 0.15f) } },
        new Country { Name = "Zambie", MatchKeywords = new[] { "Zambie" },
            FlagVertical = false, FlagBandHeights = new[] { 10, 6 },
            FlagColors = new[] { new Color(0.10f, 0.45f, 0.20f), new Color(0.90f, 0.45f, 0.10f) } },
        new Country { Name = "Madagascar", MatchKeywords = new[] { "Madagascar" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { Color.white, new Color(0.80f, 0.10f, 0.15f), new Color(0.10f, 0.55f, 0.25f) } },
        new Country { Name = "Afghanistan", MatchKeywords = new[] { "Afghanistan" },
            FlagVertical = true, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { new Color(0.10f, 0.10f, 0.10f), new Color(0.75f, 0.10f, 0.15f), new Color(0.10f, 0.45f, 0.20f) } },
        new Country { Name = "Pakistan", MatchKeywords = new[] { "Pakistan" },
            FlagVertical = true, FlagBandHeights = new[] { 4, 12 },
            FlagColors = new[] { Color.white, new Color(0.10f, 0.45f, 0.25f) } },
        new Country { Name = "Chine", MatchKeywords = new[] { "Chine" },
            FlagVertical = false, FlagBandHeights = new[] { 16 },
            FlagColors = new[] { new Color(0.75f, 0.10f, 0.10f) } },
        new Country { Name = "Russie", MatchKeywords = new[] { "Russie" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { Color.white, new Color(0.15f, 0.25f, 0.65f), new Color(0.75f, 0.10f, 0.15f) } },
        new Country { Name = "Angleterre", MatchKeywords = new[] { "Angleterre" },
            FlagVertical = false, FlagBandHeights = new[] { 8, 8 },
            FlagColors = new[] { new Color(0.75f, 0.10f, 0.15f), Color.white } },
        new Country { Name = "Tanzanie", MatchKeywords = new[] { "Tanzanie" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { new Color(0.10f, 0.45f, 0.20f), new Color(0.10f, 0.10f, 0.10f), new Color(0.15f, 0.35f, 0.65f) } },
        new Country { Name = "Australie", MatchKeywords = new[] { "Australie" },
            FlagVertical = false, FlagBandHeights = new[] { 16 },
            FlagColors = new[] { new Color(0.08f, 0.15f, 0.40f) } },
        new Country { Name = "Thaïlande", MatchKeywords = new[] { "Thaïlande" },
            FlagVertical = false, FlagBandHeights = new[] { 3, 3, 4, 3, 3 },
            FlagColors = new[] { new Color(0.75f, 0.10f, 0.15f), Color.white, new Color(0.15f, 0.20f, 0.45f), Color.white, new Color(0.75f, 0.10f, 0.15f) } },
        new Country { Name = "Mozambique", MatchKeywords = new[] { "Mozambique" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { new Color(0.10f, 0.45f, 0.20f), new Color(0.10f, 0.10f, 0.10f), new Color(0.90f, 0.78f, 0.15f) } },
        new Country { Name = "Kenya", MatchKeywords = new[] { "Kenya" },
            FlagVertical = false, FlagBandHeights = new[] { 5, 6, 5 },
            FlagColors = new[] { new Color(0.10f, 0.10f, 0.10f), new Color(0.75f, 0.10f, 0.15f), new Color(0.10f, 0.45f, 0.20f) } },
    };

    public static string Slug(string name)
    {
        var sb = new StringBuilder();
        foreach (var c in name.ToLowerInvariant())
        {
            if (c >= 'a' && c <= 'z') sb.Append(c);
            else if (c >= '0' && c <= '9') sb.Append(c);
        }
        return sb.ToString();
    }
}
