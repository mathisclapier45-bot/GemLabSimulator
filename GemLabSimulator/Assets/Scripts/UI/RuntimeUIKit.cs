using UnityEngine;
using UnityEngine.UI;

// Small helpers to build uGUI widgets from code, with everything anchored to the top-left of its parent.
public static class RuntimeUIKit
{
    private static Sprite rounded;

    public static Sprite Rounded
    {
        get
        {
            if (rounded == null) rounded = MakeRoundedSprite(32, 11);
            return rounded;
        }
    }

    private static Sprite glow;
    private static Sprite envelope;

    // Small pixel-art envelope: marks the slots that hold a client's stone.
    public static Sprite Envelope
    {
        get
        {
            if (envelope == null) envelope = MakeEnvelopeSprite();
            return envelope;
        }
    }

    public static void AddEnvelopeBadge(Transform slot, GemInstance gem)
    {
        if (gem == null || !gem.IsClientStone) return;

        var go = new GameObject("EnvelopeBadge", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(slot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-2f, -2f);
        rect.sizeDelta = new Vector2(20f, 15f);
        var image = go.GetComponent<Image>();
        image.sprite = Envelope;
        image.raycastTarget = false;
    }

    private static Sprite MakeEnvelopeSprite()
    {
        const int w = 16;
        const int h = 12;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var outline = new Color(0.40f, 0.32f, 0.22f, 1f);
        var paper = new Color(0.97f, 0.94f, 0.85f, 1f);
        var shade = new Color(0.86f, 0.80f, 0.66f, 1f);
        var seal = new Color(0.78f, 0.16f, 0.16f, 1f);

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var top = h - 1 - y;                 // 0 at the top edge
                Color c;
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1) c = outline;
                else
                {
                    // Flap: a V going from the top corners down to the middle.
                    var edge = Mathf.Min(x, w - 1 - x) * 0.7f;
                    var onFlapLine = Mathf.Abs(top - edge) < 0.6f && top <= 5;
                    c = onFlapLine ? outline : (top < edge ? shade : paper);
                }
                texture.SetPixel(x, y, c);
            }
        }

        // Wax seal where the flap points meet.
        foreach (var (sx, sy) in new[] { (7, 5), (8, 5), (7, 6), (8, 6) })
            texture.SetPixel(sx, sy, seal);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // Soft white radial gradient, meant to be tinted (halos, glows).
    public static Sprite Glow
    {
        get
        {
            if (glow == null) glow = MakeGlowSprite(64);
            return glow;
        }
    }

    private static Sprite MakeGlowSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, (1f - r) * (1f - r)));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static Image MakeBox(string name, Transform parent, Vector2 topLeft, Vector2 size, Color color, bool roundedCorners = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), topLeft, size);
        var image = go.GetComponent<Image>();
        if (roundedCorners)
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
        }
        image.color = color;
        return image;
    }

    public static Text MakeText(string name, Transform parent, string content, int fontSize, TextAnchor alignment,
        Color color, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    public static Button MakeButton(string name, Transform parent, string label, int fontSize, Color color,
        Vector2 position, Vector2 size)
    {
        var image = MakeBox(name, parent, position, size, color);
        var button = image.gameObject.AddComponent<Button>();

        var text = MakeText("Label", image.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, size);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    // White rounded rectangle meant to be tinted and 9-sliced.
    public static Sprite MakeRoundedSprite(int size, int radius)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f);
                var dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
}
