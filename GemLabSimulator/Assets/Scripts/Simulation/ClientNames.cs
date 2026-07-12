using UnityEngine;

public static class ClientNames
{
    private static readonly string[] FirstNames =
    {
        "Léa", "Hugo", "Camille", "Nathan", "Manon", "Louis", "Chloé", "Adam",
        "Inès", "Gabriel", "Jade", "Raphaël", "Alice", "Arthur", "Zoé", "Léon",
        "Emma", "Noah", "Sarah", "Victor",
    };

    private static readonly string[] LastNames =
    {
        "Moreau", "Girard", "Lambert", "Fontaine", "Rousseau", "Blanchard",
        "Marchand", "Dubois", "Lefevre", "Petit", "Roche", "Caron",
        "Beaumont", "Renard", "Dupont", "Leroy",
    };

    public static string GetRandom()
    {
        var first = FirstNames[Random.Range(0, FirstNames.Length)];
        var last = LastNames[Random.Range(0, LastNames.Length)];
        return $"{first} {last}";
    }
}
