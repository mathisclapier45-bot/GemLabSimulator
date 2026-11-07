using System.Collections.Generic;
using UnityEngine;

// Runtime gem icons ("3D pixel" art from GemPixelArt): a stone shows the shape and cut style it really has,
// including the ones chosen at the lapidary.
public static class GemIconFactory
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Get(GemDefinition def, string shape, string cutStyle)
    {
        if (def == null) return null;
        if (def.iconColor.a <= 0f) return def.icon;

        var key = $"{def.name}|{shape}|{cutStyle}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var level = def.Diaphaneity;
        var sprite = cutStyle != "Cabochon"
            ? FacetedIcon(shape, cutStyle, def.iconColor, def.iconFacetColor, level)
            : ToSprite(GemPixelArt.RenderGem(shape, cutStyle, def.iconColor, def.iconFacetColor, level));
        Cache[key] = sprite;
        return sprite;
    }

    // Icon of a stone with its own color. The colors are quantized so near-identical stones share one sprite.
    public static Sprite Get(GemDefinition def, string shape, string cutStyle, Color body, Color facet)
    {
        if (def == null) return null;
        body = Quantize(body);
        facet = Quantize(facet);

        var key = $"{def.name}|{shape}|{cutStyle}|{ColorUtility.ToHtmlStringRGB(body)}|{ColorUtility.ToHtmlStringRGB(facet)}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var level = def.Diaphaneity;
        var sprite = cutStyle != "Cabochon"
            ? FacetedIcon(shape, cutStyle, body, facet, level)
            : ToSprite(GemPixelArt.RenderGem(shape, cutStyle, body, facet, level));
        Cache[key] = sprite;
        return sprite;
    }

    // The icon of a stone whose pavilion carries a thin metal coating: the rainbow sheen of a "mystic" topaz, laid over the facets. The stronger
    // the coating, the more the colours of the rainbow show.
    public static Sprite GetIridescent(GemDefinition def, string shape, string cutStyle, Color body, Color facet, float strength)
    {
        var baseSprite = Get(def, shape, cutStyle, body, facet);
        if (baseSprite == null || baseSprite.texture == null) return baseSprite;

        var level = Mathf.Clamp(Mathf.RoundToInt(strength * 5f), 1, 5);
        var key = $"irid|{def.name}|{shape}|{cutStyle}|{ColorUtility.ToHtmlStringRGB(Quantize(body))}|{ColorUtility.ToHtmlStringRGB(Quantize(facet))}|{level}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        Sprite sprite;
        try
        {
            var source = baseSprite.texture;
            var pixels = source.GetPixels32();
            var width = source.width;
            var mix = 0.18f + 0.09f * level;
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.a == 0) continue;
                var x = i % width;
                var y = i / width;
                var t = (x + y * 0.7f) / (width * 1.7f);
                var rainbow = Color.HSVToRGB(Mathf.Repeat(t * 1.3f + 0.05f, 1f), 0.8f, 1f);
                var light = (pixel.r + pixel.g + pixel.b) / (3f * 255f);
                var tinted = rainbow * (0.5f + 0.7f * light);
                var original = (Color)pixel;
                var mixed = Color.Lerp(original, tinted, mix);
                pixels[i] = new Color32((byte)(mixed.r * 255f), (byte)(mixed.g * 255f), (byte)(mixed.b * 255f), pixel.a);
            }

            var texture = new Texture2D(width, source.height, TextureFormat.RGBA32, false) { filterMode = source.filterMode, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, width, source.height), new Vector2(0.5f, 0.5f), baseSprite.pixelsPerUnit);
        }
        catch (UnityException)
        {
            // The texture cannot be read: the stone keeps its plain icon.
            sprite = baseSprite;
        }
        Cache[key] = sprite;
        return sprite;
    }

    public static Sprite GetRough(GemDefinition def, Color body)
    {
        if (def == null) return null;
        body = Quantize(body);

        var key = $"{def.name}|rough|{ColorUtility.ToHtmlStringRGB(body)}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var sprite = ToSprite(GemPixelArt.RenderRough(body, (int)StoneOptics.StableHash(def.name), def.Diaphaneity));
        Cache[key] = sprite;
        return sprite;
    }

    private static Color Quantize(Color color)
    {
        const float steps = 31f;
        return new Color(Mathf.Round(color.r * steps) / steps, Mathf.Round(color.g * steps) / steps, Mathf.Round(color.b * steps) / steps, 1f);
    }

    // Icon of a faceted cut: the top view of its real facet mesh, with the proportions of its shape, filling the icon.
    private static Sprite FacetedIcon(string shape, string cutStyle, Color body, Color facet, Diaphaneity? level = null)
    {
        var view = new GemView3D(GemPixelArt.Size);
        var aspect = GemMesh.Aspect(shape);
        var half = GemView3D.FitFraction;                      // cancels the margin the viewer keeps around the stone
        view.Draw(GemMesh.Get(shape, cutStyle), new Vector3(aspect.x * half, aspect.y * half, half * 0.6f), Quaternion.identity, body, facet, level);
        return view.Sprite;
    }

    // Icon of a rough crystal with its habit (an octahedron, a prism...): the habit seen from the side that shows it best, with the colours of the stone.
    public static Sprite GetHabit(GemDefinition def, string habitId, Color body, Color facet)
    {
        if (def == null) return null;
        var mesh = GemMesh.GetHabit(habitId);
        if (mesh == null) return GetRough(def, body);

        body = Quantize(body);
        facet = Quantize(facet);
        var key = $"{def.name}|habit|{habitId}|{ColorUtility.ToHtmlStringRGB(body)}|{ColorUtility.ToHtmlStringRGB(facet)}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var view = new GemView3D(GemPixelArt.Size);
        view.Draw(mesh, Vector3.one, HabitMesh.IconOrientation(habitId), body, facet, def.Diaphaneity);
        Cache[key] = view.Sprite;
        return view.Sprite;
    }

    public static Sprite GetRough(GemDefinition def)
    {
        if (def == null) return null;
        if (def.iconColor.a <= 0f) return def.roughIcon;

        // A species that grows in crystals shows its most common habit.
        var primary = CrystalHabits.Primary(def);
        if (primary != null) return GetHabit(def, primary.Id, def.iconColor, def.iconFacetColor);

        var key = $"{def.name}|rough";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var sprite = ToSprite(GemPixelArt.RenderRough(def.iconColor, (int)StoneOptics.StableHash(def.name), def.Diaphaneity));
        Cache[key] = sprite;
        return sprite;
    }

    // Flat-colored version of the habit of a rough crystal, like GetSolid for a cut stone.
    public static Sprite GetHabitSolid(string habitId, Color body, Color facet)
    {
        var mesh = GemMesh.GetHabit(habitId);
        if (mesh == null) return null;

        var key = $"solidhabit|{habitId}|{ColorUtility.ToHtmlStringRGB(body)}|{ColorUtility.ToHtmlStringRGB(facet)}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var view = new GemView3D(GemPixelArt.Size);
        view.Draw(mesh, Vector3.one, HabitMesh.IconOrientation(habitId), body, facet);
        Cache[key] = view.Sprite;
        return view.Sprite;
    }

    // Flat-colored version of a shape/style, used for effects such as UV luminescence (body = main color,
    // facet = highlight color). The facet shading stays, so a white body can be tinted at will.
    public static Sprite GetSolid(string shape, string cutStyle, Color body, Color facet)
    {
        var key = $"solid|{shape}|{cutStyle}|{ColorUtility.ToHtmlStringRGB(body)}|{ColorUtility.ToHtmlStringRGB(facet)}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        Sprite sprite;
        if (cutStyle != "Cabochon") sprite = FacetedIcon(shape, cutStyle, body, facet);
        else sprite = ToSprite(GemPixelArt.RenderGem(shape, cutStyle, body, facet));
        Cache[key] = sprite;
        return sprite;
    }

    private static Sprite ToSprite(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, GemPixelArt.Size, GemPixelArt.Size), new Vector2(0.5f, 0.5f), GemPixelArt.Size);
    }
}
