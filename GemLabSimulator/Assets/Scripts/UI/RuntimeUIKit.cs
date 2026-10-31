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

    private static Sprite gift;

    // Small pixel-art gift box: marks the slots that hold a donated stone.
    public static Sprite Gift
    {
        get
        {
            if (gift == null) gift = MakeGiftSprite();
            return gift;
        }
    }

    private static Sprite reportIcon;

    // Small pixel-art sheet of paper with a gold seal: the laboratory report of a stone.
    public static Sprite ReportIcon
    {
        get
        {
            if (reportIcon == null) reportIcon = MakeReportSprite();
            return reportIcon;
        }
    }

    private static Sprite validated;

    // Small pixel-art green seal with a check mark: marks the slots that hold an identified stone.
    public static Sprite Validated
    {
        get
        {
            if (validated == null) validated = MakeValidatedSprite();
            return validated;
        }
    }

    // Top right: envelope for a client's stone, gift box for a donation. Bottom right: green check once identified.
    public static void AddEnvelopeBadge(Transform slot, GemInstance gem)
    {
        if (gem == null) return;

        if (gem.IsDonation)
            AddBadge(slot, "GiftBadge", Gift, new Vector2(16f, 16f), new Vector2(1f, 1f));
        else if (gem.IsClientStone)
            AddBadge(slot, "EnvelopeBadge", Envelope, new Vector2(20f, 15f), new Vector2(1f, 1f));

        if (gem.IsIdentified)
            AddBadge(slot, "ValidatedBadge", Validated, new Vector2(16f, 16f), new Vector2(1f, 0f));
    }

    private static void AddBadge(Transform slot, string badgeName, Sprite sprite, Vector2 size, Vector2 corner)
    {
        var go = new GameObject(badgeName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(slot, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = new Vector2(-2f, corner.y > 0.5f ? -2f : 2f);
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private static Sprite MakeReportSprite()
    {
        const int size = 12;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var clear = new Color(0f, 0f, 0f, 0f);
        var outline = new Color(0.25f, 0.22f, 0.18f, 1f);
        var paper = new Color(0.97f, 0.95f, 0.88f, 1f);
        var ink = new Color(0.55f, 0.50f, 0.42f, 1f);
        var seal = new Color(0.90f, 0.70f, 0.20f, 1f);

        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);

        void Px(int x, int y, Color c) => texture.SetPixel(x, y, c);

        // Sheet from x 2..9 and y 0..11 (y = 0 is the bottom), outline around a paper-colored body.
        for (var y = 0; y <= 11; y++)
            for (var x = 2; x <= 9; x++)
                Px(x, y, x == 2 || x == 9 || y == 0 || y == 11 ? outline : paper);

        // Cut top-right corner.
        Px(9, 11, clear); Px(8, 11, clear); Px(9, 10, clear);
        Px(8, 10, outline);

        // Lines of text.
        foreach (var row in new[] { 9, 7, 5 })
            for (var x = 4; x <= 7; x++)
                Px(x, row, ink);

        // Seal.
        Px(4, 2, seal); Px(5, 2, seal); Px(4, 3, seal); Px(5, 3, seal);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeValidatedSprite()
    {
        const int size = 12;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var clear = new Color(0f, 0f, 0f, 0f);
        var outline = new Color(0.08f, 0.30f, 0.14f, 1f);
        var green = new Color(0.22f, 0.72f, 0.34f, 1f);
        var light = new Color(0.45f, 0.88f, 0.52f, 1f);
        var check = new Color(1f, 1f, 1f, 1f);

        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                Color c = clear;
                if (distance <= 5.6f) c = distance > 4.6f ? outline : (y >= 7 ? light : green);
                texture.SetPixel(x, y, c);
            }
        }

        // Check mark (y = 0 is the bottom).
        foreach (var (px, py) in new[] { (3, 5), (4, 4), (5, 3), (6, 4), (7, 5), (8, 6), (9, 7), (3, 6), (4, 5), (5, 4), (6, 5), (7, 6), (8, 7) })
            texture.SetPixel(px, py, check);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeGiftSprite()
    {
        const int size = 12;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        var clear = new Color(0f, 0f, 0f, 0f);
        var outline = new Color(0.38f, 0.10f, 0.20f, 1f);
        var box = new Color(0.86f, 0.25f, 0.42f, 1f);
        var boxShade = new Color(0.68f, 0.17f, 0.33f, 1f);
        var ribbon = new Color(0.99f, 0.84f, 0.30f, 1f);
        var ribbonShade = new Color(0.86f, 0.66f, 0.16f, 1f);

        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);

        void Px(int x, int y, Color c) => texture.SetPixel(x, y, c);

        // Box body (rows 0..6) with a lid (rows 7..8); y = 0 is the bottom.
        for (var x = 1; x <= 10; x++)
        {
            for (var y = 0; y <= 8; y++)
            {
                var edge = x == 1 || x == 10 || y == 0 || y == 8 || y == 6;
                Px(x, y, edge ? outline : (y <= 5 ? (x >= 8 ? boxShade : box) : box));
            }
        }
        // Vertical ribbon on the box and lid.
        for (var y = 1; y <= 8; y++)
        {
            Px(5, y, y == 6 ? ribbonShade : ribbon);
            Px(6, y, y == 6 ? ribbonShade : ribbon);
        }
        // Bow: two loops and a knot on top of the lid.
        foreach (var (bx, by) in new[] { (3, 9), (4, 9), (3, 10), (7, 9), (8, 9), (8, 10), (4, 10), (7, 10) })
            Px(bx, by, ribbon);
        foreach (var (bx, by) in new[] { (2, 9), (2, 10), (9, 9), (9, 10), (3, 11), (4, 11), (7, 11), (8, 11), (5, 10), (6, 10) })
            Px(bx, by, outline);
        Px(5, 9, ribbonShade);
        Px(6, 9, ribbonShade);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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
