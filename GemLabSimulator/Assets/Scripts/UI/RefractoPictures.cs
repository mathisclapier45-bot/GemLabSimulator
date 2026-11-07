using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The pictures of the tabs of the refractometer: what the eyepiece shows on the graduated scale for each result. Each picture is a small copy of the eyepiece (the scale
// from 1,30 to 1,81, the dark zone above the shadow edge, the grey zone between two edges, the bright zone under them), drawn from the same model as the instrument.
public static class RefractoPictures
{
    private const float ScaleMin = 1.30f;
    private const float ScaleMax = 1.81f;
    private const int Width = 70;
    private const int Height = 140;

    private static readonly Color32 Dark = new Color32(33, 33, 38, 255);
    private static readonly Color32 Mid = new Color32(133, 133, 140, 255);
    private static readonly Color32 Bright = new Color32(245, 245, 238, 255);
    private static readonly Color32 InkOnDark = new Color32(224, 224, 230, 255);
    private static readonly Color32 InkOnMid = new Color32(13, 13, 18, 255);
    private static readonly Color32 InkOnBright = new Color32(26, 26, 31, 255);

    // One eyepiece: the lower and the upper shadow edge (0 = none), and what is written under it.
    public struct View
    {
        public float Low;
        public float High;
        public string Caption;
        public bool Live;          // the picture of what the instrument shows now (changes with the lens)
    }

    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, Sprite> LiveSprites = new Dictionary<string, Sprite>();

    private static View V(float low, float high, string caption) => new View { Low = low, High = high, Caption = caption };

    // ---- The edges of the examples, with the model of the instrument ---------------------------------------------------------

    private static View UniaxialView(float nAlpha, float nGamma, bool positive, float degrees, string caption)
    {
        var s = Mathf.Sin(degrees * Mathf.Deg2Rad);
        s *= s;
        var fixedEdge = positive ? nAlpha : nGamma;
        var other = positive ? nGamma : nAlpha;
        var moving = other + (fixedEdge - other) * s;
        return V(Mathf.Min(fixedEdge, moving), Mathf.Max(fixedEdge, moving), caption);
    }

    private static View BiaxialView(float nAlpha, float nBeta, float nGamma, float degrees, string caption)
    {
        const float phase = 0.8f;
        var t = degrees * Mathf.Deg2Rad;
        var s1 = Mathf.Sin(t) * Mathf.Sin(t);
        var s2 = Mathf.Sin(t + phase) * Mathf.Sin(t + phase);
        return V(nAlpha + (nBeta - nAlpha) * s1, nGamma - (nGamma - nBeta) * s2, caption);
    }

    // ---- The pictures ------------------------------------------------------------------------------------------------------

    public static void Build(string id, RectTransform parent, RefractoGuide.Situation s)
    {
        switch (id)
        {
            case "otl":
                Row(parent, V(0f, 0f, "Hors limite : tout le champ est clair"));
                break;
            case "otl-sr":
                Row(parent, V(0f, 0f, "Hors limite : tout est clair"), V(1.540f, 0f, "Une lecture : lentille à 0°"), V(1.540f, 0f, "À 90° : la limite n'a pas bougé"));
                break;
            case "sr":
                Row(parent, V(1.540f, 0f, "Lentille à 0° : une limite à 1,54"), V(1.540f, 0f, "Lentille à 90° : elle n'a pas bougé"));
                break;
            case "dr":
                Row(parent,
                    BiaxialView(1.654f, 1.664f, 1.690f, 0f, "Lentille à 0°"),
                    BiaxialView(1.654f, 1.664f, 1.690f, 40f, "40°"),
                    BiaxialView(1.654f, 1.664f, 1.690f, 80f, "80° : deux limites, qui se déplacent"));
                break;
            case "uni-neg":
                Row(parent,
                    UniaxialView(1.625f, 1.643f, false, 0f, "0°"),
                    UniaxialView(1.625f, 1.643f, false, 30f, "30°"),
                    UniaxialView(1.625f, 1.643f, false, 55f, "55°"),
                    UniaxialView(1.625f, 1.643f, false, 80f, "80° : la haute reste"));
                break;
            case "uni-pos":
                Row(parent,
                    UniaxialView(1.544f, 1.553f, true, 0f, "0°"),
                    UniaxialView(1.544f, 1.553f, true, 30f, "30°"),
                    UniaxialView(1.544f, 1.553f, true, 55f, "55°"),
                    UniaxialView(1.544f, 1.553f, true, 80f, "80° : la basse reste"));
                break;
            case "bi":
                Row(parent,
                    BiaxialView(1.654f, 1.664f, 1.690f, 0f, "0°"),
                    BiaxialView(1.654f, 1.664f, 1.690f, 30f, "30°"),
                    BiaxialView(1.654f, 1.664f, 1.690f, 60f, "60°"),
                    BiaxialView(1.654f, 1.664f, 1.690f, 90f, "90° : les deux bougent"));
                break;
            case "sign-uni":
                Row(parent,
                    UniaxialView(1.625f, 1.643f, false, 0f, "Négatif : 0°"),
                    UniaxialView(1.625f, 1.643f, false, 70f, "Négatif : 70°"),
                    UniaxialView(1.544f, 1.553f, true, 0f, "Positif : 0°"),
                    UniaxialView(1.544f, 1.553f, true, 70f, "Positif : 70°"));
                break;
            case "sign-bi":
                ScaleBars(parent);
                break;
            case "yours":
                if (s.HasStone)
                {
                    var live = V(s.HasLowEdge ? s.CurrentLow : 0f, s.HasHighEdge ? s.CurrentHigh : 0f, $"Votre pierre, lentille à {s.AngleDegrees:F0}°");
                    live.Live = true;
                    Row(parent, live);
                }
                break;
        }
    }

    // ---- The widgets -------------------------------------------------------------------------------------------------------

    private static RectTransform Row(RectTransform parent, params View[] views)
    {
        var go = new GameObject("PictureRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var group = go.GetComponent<HorizontalLayoutGroup>();
        group.spacing = 8f;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
        foreach (var view in views) ViewWidget(go.transform, view);
        return go.GetComponent<RectTransform>();
    }

    private static void ViewWidget(Transform parent, View view)
    {
        var go = new GameObject("View", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var element = go.GetComponent<LayoutElement>();
        element.minWidth = element.preferredWidth = Width + 4f;
        var group = go.GetComponent<VerticalLayoutGroup>();
        group.spacing = 3f;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        // The eyepiece itself, with a frame.
        var frame = new GameObject("Eyepiece", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        frame.transform.SetParent(go.transform, false);
        var frameElement = frame.GetComponent<LayoutElement>();
        frameElement.minWidth = frameElement.preferredWidth = Width + 4f;
        frameElement.minHeight = frameElement.preferredHeight = Height + 4f;
        var frameImage = frame.GetComponent<Image>();
        frameImage.color = new Color(0.02f, 0.02f, 0.03f, 1f);
        frameImage.raycastTarget = false;

        var picture = new GameObject("Scale", typeof(RectTransform), typeof(Image));
        picture.transform.SetParent(frame.transform, false);
        var rect = picture.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(Width, Height);
        var image = picture.GetComponent<Image>();
        image.sprite = EyeSprite(view.Low, view.High, view.Live);
        image.raycastTarget = false;

        // The figures of the scale, in the ink that suits the zone they fall in.
        for (var n = 1.40f; n < 1.805f; n += 0.10f)
        {
            var y = YOf(n);
            var ink = InkAt(n, view.Low, view.High);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(picture.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(30f, -y);
            labelRect.sizeDelta = new Vector2(36f, 12f);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 9;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = ink;
            text.raycastTarget = false;
            text.text = n.ToString("F2", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
        }

        var caption = new GameObject("Caption", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        caption.transform.SetParent(go.transform, false);
        var captionElement = caption.GetComponent<LayoutElement>();
        captionElement.preferredWidth = Width + 4f;
        var captionText = caption.GetComponent<Text>();
        captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        captionText.fontSize = 10;
        captionText.alignment = TextAnchor.UpperCenter;
        captionText.color = new Color(0.78f, 0.82f, 0.90f);
        captionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        captionText.verticalOverflow = VerticalWrapMode.Overflow;
        captionText.raycastTarget = false;
        captionText.text = view.Caption;
    }

    private static float YOf(float n) => Mathf.Clamp01((n - ScaleMin) / (ScaleMax - ScaleMin)) * (Height - 1);

    private static Color32 InkAt(float n, float low, float high)
    {
        if (low <= 0f) return InkOnBright;
        if (n < low) return InkOnDark;
        if (high > low + 0.0004f && n < high) return InkOnMid;
        return InkOnBright;
    }

    // The eyepiece as a small picture: zones, and the graduations every 0,01 (longer every 0,05).
    private static Sprite EyeSprite(float low, float high, bool live)
    {
        var key = $"{low:F3}|{high:F3}";
        var cache = live ? LiveSprites : Sprites;
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        if (live && cache.Count > 200)
        {
            // The pictures of the live view pile up as the lens turns: the oldest go (the page rebuilds itself with each new one).
            foreach (var old in cache.Values)
            {
                if (old == null) continue;
                Object.Destroy(old.texture);
                Object.Destroy(old);
            }
            cache.Clear();
        }

        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[Width * Height];
        for (var row = 0; row < Height; row++)
        {
            var n = ScaleMin + row / (float)(Height - 1) * (ScaleMax - ScaleMin);
            Color32 zone;
            if (low <= 0f) zone = Bright;
            else if (n < low) zone = Dark;
            else if (high > low + 0.0004f && n < high) zone = Mid;
            else zone = Bright;
            var y = Height - 1 - row;
            for (var x = 0; x < Width; x++) pixels[y * Width + x] = zone;
        }

        for (var i = 0; i <= 51; i++)
        {
            var n = ScaleMin + i * 0.01f;
            var row = Mathf.RoundToInt(YOf(n));
            var y = Height - 1 - row;
            var major = i % 5 == 0;
            var length = major ? 24 : 11;
            var ink = InkAt(n, low, high);
            for (var x = 4; x < 4 + length && x < Width; x++)
                if (y >= 0 && y < Height) pixels[y * Width + x] = ink;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100f);
        cache[key] = sprite;
        return sprite;
    }

    // The three indices on a line, for a positive and a negative biaxial stone: the middle one nβ is nearer nα in the first, nearer nγ in the second.
    private static void ScaleBars(RectTransform parent)
    {
        var go = new GameObject("SignBiaxial", typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var group = go.GetComponent<VerticalLayoutGroup>();
        group.spacing = 6f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        Bar(go.transform, "Biaxe positif (+) : nβ plus proche de nα", 0.28f);
        Bar(go.transform, "Biaxe négatif (−) : nβ plus proche de nγ", 0.72f);
    }

    private static void Bar(Transform parent, string title, float betaPosition)
    {
        var holder = new GameObject("Bar", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        holder.transform.SetParent(parent, false);
        var layout = holder.GetComponent<LayoutElement>();
        layout.preferredHeight = 64f;
        var group = holder.GetComponent<VerticalLayoutGroup>();
        group.spacing = 2f;
        group.childControlWidth = true;
        group.childControlHeight = false;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        var label = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        label.transform.SetParent(holder.transform, false);
        label.GetComponent<LayoutElement>().preferredHeight = 16f;
        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 11;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.78f, 0.82f, 0.90f);
        text.raycastTarget = false;
        text.text = title;

        var line = new GameObject("Line", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        line.transform.SetParent(holder.transform, false);
        line.GetComponent<LayoutElement>().preferredHeight = 44f;
        var lineImage = line.GetComponent<Image>();
        lineImage.color = new Color(0.12f, 0.13f, 0.17f, 1f);
        lineImage.raycastTarget = false;

        // The axis and the three marks nα, nβ, nγ.
        void Mark(float position, string name, Color color)
        {
            var mark = new GameObject(name, typeof(RectTransform), typeof(Image));
            mark.transform.SetParent(line.transform, false);
            var rect = mark.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.06f + position * 0.88f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(3f, 22f);
            rect.anchoredPosition = new Vector2(0f, -6f);
            var image = mark.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var caption = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            caption.transform.SetParent(mark.transform, false);
            var captionRect = caption.GetComponent<RectTransform>();
            captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 0.5f);
            captionRect.pivot = new Vector2(0.5f, 0.5f);
            captionRect.sizeDelta = new Vector2(24f, 14f);
            var captionText = caption.GetComponent<Text>();
            captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            captionText.fontSize = 12;
            captionText.fontStyle = FontStyle.Bold;
            captionText.alignment = TextAnchor.MiddleCenter;
            captionText.color = color;
            captionText.raycastTarget = false;
            captionText.text = name;
            captionRect.anchoredPosition = new Vector2(0f, 20f);
        }

        Mark(0f, "nα", new Color(0.55f, 0.85f, 1f));
        Mark(betaPosition, "nβ", new Color(1f, 0.82f, 0.30f));
        Mark(1f, "nγ", new Color(1f, 0.58f, 0.40f));
    }
}
