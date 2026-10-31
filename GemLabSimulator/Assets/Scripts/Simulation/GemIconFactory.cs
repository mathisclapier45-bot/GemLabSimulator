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

        var sprite = cutStyle != "Cabochon"
            ? FacetedIcon(shape, cutStyle, def.iconColor, def.iconFacetColor)
            : ToSprite(GemPixelArt.RenderGem(shape, cutStyle, def.iconColor, def.iconFacetColor));
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

        var sprite = cutStyle != "Cabochon"
            ? FacetedIcon(shape, cutStyle, body, facet)
            : ToSprite(GemPixelArt.RenderGem(shape, cutStyle, body, facet));
        Cache[key] = sprite;
        return sprite;
    }

    public static Sprite GetRough(GemDefinition def, Color body)
    {
        if (def == null) return null;
        body = Quantize(body);

        var key = $"{def.name}|rough|{ColorUtility.ToHtmlStringRGB(body)}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var sprite = ToSprite(GemPixelArt.RenderRough(body, (int)StoneOptics.StableHash(def.name)));
        Cache[key] = sprite;
        return sprite;
    }

    private static Color Quantize(Color color)
    {
        const float steps = 31f;
        return new Color(Mathf.Round(color.r * steps) / steps, Mathf.Round(color.g * steps) / steps, Mathf.Round(color.b * steps) / steps, 1f);
    }

    // Icon of a faceted cut: the top view of its real facet mesh, with the proportions of its shape, filling the icon.
    private static Sprite FacetedIcon(string shape, string cutStyle, Color body, Color facet)
    {
        var view = new GemView3D(GemPixelArt.Size);
        var aspect = GemMesh.Aspect(shape);
        var half = GemView3D.FitFraction;                      // cancels the margin the viewer keeps around the stone
        view.Draw(GemMesh.Get(shape, cutStyle), new Vector3(aspect.x * half, aspect.y * half, half * 0.6f), Quaternion.identity, body, facet);
        return view.Sprite;
    }

    public static Sprite GetRough(GemDefinition def)
    {
        if (def == null) return null;
        if (def.iconColor.a <= 0f) return def.roughIcon;

        var key = $"{def.name}|rough";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var sprite = ToSprite(GemPixelArt.RenderRough(def.iconColor, (int)StoneOptics.StableHash(def.name)));
        Cache[key] = sprite;
        return sprite;
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
