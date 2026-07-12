using System.Collections.Generic;
using UnityEngine;

public static class MarketFlagLookup
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite GetFlag(string countryName)
    {
        if (string.IsNullOrEmpty(countryName)) return null;
        if (Cache.TryGetValue(countryName, out var cached)) return cached;

        var slug = MarketCountries.Slug(countryName);
        var sprite = Resources.Load<Sprite>($"Flags/flag_{slug}_pixel");
        Cache[countryName] = sprite;
        return sprite;
    }
}
