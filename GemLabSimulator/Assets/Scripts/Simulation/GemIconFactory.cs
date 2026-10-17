using System.Collections.Generic;
using UnityEngine;

// Builds gem icons at runtime so that a stone cut at the lapidary shows the chosen shape/style.
// The 16x16 shape patterns live in GemIconPatterns.cs.
public static partial class GemIconFactory
{
    private const int PatternSize = 16;
    private const int Upscale = 2;

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Get(GemDefinition def, string shape, string cutStyle)
    {
        if (def == null) return null;
        if (def.iconColor.a <= 0f || string.IsNullOrEmpty(shape)) return def.icon;

        var key = $"{def.name}|{shape}|{cutStyle}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        if (!PatternsByShape.TryGetValue(shape, out var basePattern)) basePattern = DefaultPattern;
        var pattern = ApplyStyle(basePattern, cutStyle);
        var sprite = Build(pattern, def.iconColor, def.iconFacetColor);
        Cache[key] = sprite;
        return sprite;
    }

    private static string[] ApplyStyle(string[] basePattern, string cutStyle)
    {
        var rows = new string[basePattern.Length];
        for (var y = 0; y < basePattern.Length; y++)
        {
            var chars = basePattern[y].ToCharArray();
            for (var x = 0; x < chars.Length; x++)
            {
                switch (cutStyle)
                {
                    case "Cabochon":
                        // Smooth dome: no facets, one soft highlight near the top-left.
                        if (chars[x] == 'F') chars[x] = 'G';
                        break;
                    case "À degrés":
                    case "Émeraude":
                    case "Ascher":
                        // Step cut: horizontal bands of light.
                        if (chars[x] != '.') chars[x] = (y % 2 == 0) ? 'F' : 'G';
                        break;
                    case "Ciseaux":
                        // Scissor cut: crossing diagonal bands.
                        if (chars[x] != '.') chars[x] = ((x + y) % 4 < 2) ? 'F' : 'G';
                        break;
                    case "Princesse":
                    case "Radiant":
                        // Chevron-like faceting.
                        if (chars[x] != '.') chars[x] = (Mathf.Abs(x - PatternSize / 2) + y) % 4 == 0 ? 'F' : 'G';
                        break;
                }
            }

            rows[y] = new string(chars);
        }

        if (cutStyle == "Cabochon")
        {
            for (var y = 0; y < rows.Length; y++)
            {
                var chars = rows[y].ToCharArray();
                var placed = false;
                for (var x = 0; x < chars.Length && !placed; x++)
                {
                    if (chars[x] != 'G' || y < 3) continue;
                    if (x + 1 < chars.Length && chars[x + 1] == 'G' && y + 1 < rows.Length && rows[y + 1][x] == 'G')
                    {
                        chars[x] = 'F';
                        placed = true;
                    }
                }

                rows[y] = new string(chars);
                if (placed) break;
            }
        }

        return rows;
    }

    private static Sprite Build(string[] pattern, Color body, Color facet)
    {
        var size = PatternSize * Upscale;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (var py = 0; py < PatternSize; py++)
        {
            var row = py < pattern.Length ? pattern[py] : new string('.', PatternSize);
            for (var px = 0; px < PatternSize; px++)
            {
                var c = px < row.Length ? row[px] : '.';
                var color = c == 'G' ? body : c == 'F' ? facet : Color.clear;
                for (var dy = 0; dy < Upscale; dy++)
                for (var dx = 0; dx < Upscale; dx++)
                    texture.SetPixel(px * Upscale + dx, (PatternSize - 1 - py) * Upscale + dy, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
