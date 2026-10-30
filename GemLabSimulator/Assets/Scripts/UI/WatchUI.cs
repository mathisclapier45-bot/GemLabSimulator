using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A small wristwatch dial at the top right of the screen (below the minimap in the town): white face, black numerals,
// steel bezel, and hour and minute hands that show the in-game time. Under the mouse it gets a magnifying-glass effect;
// a click shows the character's wrist with the watch on it, large. Added to the Canvas of every game scene.
public class WatchUI : MonoBehaviour
{
    private const float DialSize = 92f;               // diameter of the watch, in canvas units
    private const int TextureSize = 184;

    private const float HoverZoom = 1.16f;

    private RectTransform hourHand;
    private RectTransform minuteHand;
    private RectTransform bigHourHand;
    private RectTransform bigMinuteHand;
    private RectTransform dialHolder;
    private Image lens;
    private GameObject wristPanel;
    private bool hovered;
    private float hoverAmount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    private static void Attach(Scene scene)
    {
        if (scene.name == "MainMenu") return;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<WatchUI>() == null) canvas.AddComponent<WatchUI>();
    }

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        var isTown = SceneManager.GetActiveScene().name == "Town";

        var root = new GameObject("Watch", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(1);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.sizeDelta = new Vector2(DialSize + 8f, DialSize);
        // Centred under the minimap (which is 192 wide and 16 from the corner) in the town; in the corner elsewhere.
        rootRect.anchoredPosition = isTown ? new Vector2(-16f - (192f - DialSize) / 2f + 4f, -220f) : new Vector2(-16f, -16f);

        // Crown on the right of the case.
        var crown = new GameObject("Crown", typeof(RectTransform), typeof(Image));
        crown.transform.SetParent(root.transform, false);
        var crownRect = crown.GetComponent<RectTransform>();
        crownRect.anchorMin = crownRect.anchorMax = new Vector2(1f, 0.5f);
        crownRect.pivot = new Vector2(1f, 0.5f);
        crownRect.sizeDelta = new Vector2(8f, 12f);
        crownRect.anchoredPosition = Vector2.zero;
        crown.GetComponent<Image>().color = new Color(0.62f, 0.64f, 0.70f, 1f);
        crown.GetComponent<Image>().raycastTarget = false;

        // Dial holder (scales under the mouse), with the dial and the magnifying lens.
        var holder = new GameObject("DialHolder", typeof(RectTransform));
        holder.transform.SetParent(root.transform, false);
        dialHolder = holder.GetComponent<RectTransform>();
        dialHolder.anchorMin = dialHolder.anchorMax = new Vector2(0f, 0.5f);
        dialHolder.pivot = new Vector2(0.5f, 0.5f);
        dialHolder.sizeDelta = new Vector2(DialSize, DialSize);
        dialHolder.anchoredPosition = new Vector2(DialSize / 2f, 0f);

        BuildDial(dialHolder, DialSize, out hourHand, out minuteHand);

        // Magnifying glass, shown while the mouse is over the watch.
        var lensGO = new GameObject("Lens", typeof(RectTransform), typeof(Image));
        lensGO.transform.SetParent(root.transform, false);
        var lensRect = lensGO.GetComponent<RectTransform>();
        lensRect.anchorMin = lensRect.anchorMax = new Vector2(0f, 0.5f);
        var lensSide = DialSize * 2f;
        lensRect.sizeDelta = new Vector2(lensSide, lensSide);
        lensRect.pivot = new Vector2(0.44f, 0.56f);                          // the glass centre of the texture
        lensRect.anchoredPosition = new Vector2(DialSize / 2f, 0f);
        lens = lensGO.GetComponent<Image>();
        lens.sprite = MakeLensSprite();
        lens.raycastTarget = false;
        lens.color = new Color(1f, 1f, 1f, 0f);

        // Invisible hit area over the dial: hover and click.
        var hit = new GameObject("Hit", typeof(RectTransform), typeof(Image));
        hit.transform.SetParent(root.transform, false);
        var hitRect = hit.GetComponent<RectTransform>();
        hitRect.anchorMin = hitRect.anchorMax = new Vector2(0f, 0.5f);
        hitRect.pivot = new Vector2(0f, 0.5f);
        hitRect.sizeDelta = new Vector2(DialSize, DialSize);
        hitRect.anchoredPosition = Vector2.zero;
        var hitImage = hit.GetComponent<Image>();
        hitImage.color = new Color(0f, 0f, 0f, 0f);
        var handler = hit.AddComponent<WatchPointerHandler>();
        handler.OnHover = value => hovered = value;
        handler.OnClick = ToggleWristView;

        BuildWristView();
    }

    // Round dial: bezel/face texture, numerals, hands and centre cap, for any diameter.
    private static void BuildDial(Transform parent, float diameter, out RectTransform hour, out RectTransform minute)
    {
        var dial = new GameObject("Dial", typeof(RectTransform), typeof(RawImage));
        dial.transform.SetParent(parent, false);
        var dialRect = dial.GetComponent<RectTransform>();
        dialRect.anchorMin = dialRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialRect.pivot = new Vector2(0.5f, 0.5f);
        dialRect.sizeDelta = new Vector2(diameter, diameter);
        dialRect.anchoredPosition = Vector2.zero;
        var raw = dial.GetComponent<RawImage>();
        raw.texture = MakeDialTexture();
        raw.raycastTarget = false;

        // Numerals 1 to 12, bold black.
        var numeralRadius = diameter * 0.5f * 0.66f;
        var fontSize = Mathf.RoundToInt(diameter * 0.13f);
        for (var hourNumber = 1; hourNumber <= 12; hourNumber++)
        {
            var angle = hourNumber * 30f * Mathf.Deg2Rad;
            var text = RuntimeUIKit.MakeText($"N{hourNumber}", dial.transform, hourNumber.ToString(), fontSize, TextAnchor.MiddleCenter,
                new Color(0.05f, 0.05f, 0.08f), Vector2.zero, new Vector2(diameter * 0.24f, diameter * 0.17f));
            text.fontStyle = FontStyle.Bold;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * numeralRadius;
        }

        var handSprite = MakeHandSprite();
        var handColor = new Color(0.04f, 0.04f, 0.07f);
        hour = MakeHand("HourHand", dial.transform, handSprite, diameter * 0.06f, diameter * 0.5f * 0.46f, handColor);
        minute = MakeHand("MinuteHand", dial.transform, handSprite, diameter * 0.043f, diameter * 0.5f * 0.70f, handColor);

        // Centre cap.
        var cap = new GameObject("Cap", typeof(RectTransform), typeof(Image));
        cap.transform.SetParent(dial.transform, false);
        var capRect = cap.GetComponent<RectTransform>();
        capRect.anchorMin = capRect.anchorMax = new Vector2(0.5f, 0.5f);
        capRect.pivot = new Vector2(0.5f, 0.5f);
        capRect.sizeDelta = new Vector2(diameter * 0.075f, diameter * 0.075f);
        capRect.anchoredPosition = Vector2.zero;
        var capImage = cap.GetComponent<Image>();
        capImage.sprite = RuntimeUIKit.MakeRoundedSprite(32, 16);
        capImage.color = new Color(0.78f, 0.80f, 0.86f, 1f);
        capImage.raycastTarget = false;
    }

    private static RectTransform MakeHand(string name, Transform parent, Sprite sprite, float width, float length, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.12f);
        rect.sizeDelta = new Vector2(width, length);
        rect.anchoredPosition = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    // Leaf-shaped hand pointing up: wide near the centre, tapering to a point.
    private static Sprite MakeHandSprite()
    {
        const int width = 16;
        const int height = 64;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        for (var y = 0; y < height; y++)
        {
            var t = y / (float)(height - 1);
            // Narrow tail below the pivot, fullest at about a quarter of the length, then a taper to the tip.
            var half = t < 0.12f ? Mathf.Lerp(1.5f, 6f, t / 0.12f) : Mathf.Lerp(6.5f, 0.6f, Mathf.Pow((t - 0.12f) / 0.88f, 0.8f));
            for (var x = 0; x < width; x++)
            {
                var d = Mathf.Abs(x + 0.5f - width / 2f);
                var alpha = Mathf.Clamp01(half - d + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Texture2D MakeDialTexture()
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = (TextureSize - 1) / 2f;
        var outer = TextureSize / 2f - 1f;
        var bezelInner = outer * 0.86f;
        var faceEdge = outer * 0.80f;
        var pixels = new Color[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var color = new Color(0f, 0f, 0f, 0f);

                if (r <= outer)
                {
                    if (r > bezelInner)
                    {
                        // Polished steel: light from the upper left.
                        var shade = 0.62f + 0.30f * Mathf.Cos(Mathf.Atan2(dy, dx) - 2.3f);
                        color = new Color(shade, shade + 0.01f, shade + 0.04f, 1f);
                    }
                    else if (r > faceEdge)
                    {
                        color = new Color(0.32f, 0.33f, 0.38f, 1f);                    // dark step between bezel and face
                    }
                    else
                    {
                        var face = 0.99f - 0.05f * (r / faceEdge) * (r / faceEdge);   // white face, slightly shaded at the rim
                        color = new Color(face, face, face, 1f);
                    }

                    // Soft anti-aliased edge.
                    color.a *= Mathf.Clamp01(outer - r + 0.5f);
                }

                pixels[y * TextureSize + x] = color;
            }
        }

        // Minute dots just inside the face edge; the five-minute ones are bigger.
        for (var minute = 0; minute < 60; minute++)
        {
            var angle = minute * 6f * Mathf.Deg2Rad;
            var big = minute % 5 == 0;
            var ringRadius = faceEdge * (big ? 0.93f : 0.94f);
            var cx = center + Mathf.Sin(angle) * ringRadius;
            var cy = center + Mathf.Cos(angle) * ringRadius;
            var dotRadius = big ? 1.6f : 0.9f;
            for (var y = Mathf.FloorToInt(cy - 2); y <= Mathf.CeilToInt(cy + 2); y++)
            {
                for (var x = Mathf.FloorToInt(cx - 2); x <= Mathf.CeilToInt(cx + 2); x++)
                {
                    if (x < 0 || y < 0 || x >= TextureSize || y >= TextureSize) continue;
                    var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    var a = Mathf.Clamp01(dotRadius - d + 0.5f);
                    if (a <= 0f) continue;
                    var dark = new Color(0.25f, 0.26f, 0.30f, 1f);
                    var existing = pixels[y * TextureSize + x];
                    pixels[y * TextureSize + x] = Color.Lerp(existing, dark, a);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // ---- Magnifying glass -----------------------------------------------------------------------------------

    // Ring, glass sheen and handle; the glass is centred on (70, 90) of the 160 x 160 texture.
    private static Sprite MakeLensSprite()
    {
        const int size = 160;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        const float cx = 70f;
        const float cy = 90f;
        const float ringOuter = 58f;
        const float ringInner = 50f;
        var handleStart = new Vector2(cx + ringOuter * 0.7071f, cy - ringOuter * 0.7071f);
        var handleEnd = new Vector2(150f, 10f);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var color = new Color(0f, 0f, 0f, 0f);
                var dx = x + 0.5f - cx;
                var dy = y + 0.5f - cy;
                var r = Mathf.Sqrt(dx * dx + dy * dy);

                // Handle: a thick segment going down to the right, darker towards the tip.
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var seg = handleEnd - handleStart;
                var t = Mathf.Clamp01(Vector2.Dot(p - handleStart, seg) / seg.sqrMagnitude);
                var distance = (p - (handleStart + seg * t)).magnitude;
                var handleWidth = Mathf.Lerp(5.5f, 4.5f, t);
                if (distance < handleWidth && t > 0.05f)
                {
                    var shade = 0.30f + 0.18f * (1f - distance / handleWidth);
                    color = new Color(shade + 0.12f, shade * 0.75f, shade * 0.45f, Mathf.Clamp01(handleWidth - distance + 0.5f));
                    if (t < 0.22f) color = new Color(0.78f, 0.62f, 0.28f, color.a);          // metal collar
                }

                if (r <= ringOuter + 0.5f && r >= ringInner)
                {
                    // Gold ring, lit from the upper left.
                    var light = 0.55f + 0.40f * Mathf.Cos(Mathf.Atan2(dy, dx) - 2.3f);
                    color = new Color(0.50f * light + 0.35f, 0.38f * light + 0.26f, 0.12f * light + 0.10f, Mathf.Clamp01(ringOuter - r + 0.7f));
                }
                else if (r < ringInner)
                {
                    // Glass: faint tint with a bright sheen in the upper left.
                    var sheen = Mathf.Clamp01(1f - Mathf.Sqrt((dx + 20f) * (dx + 20f) + (dy - 20f) * (dy - 20f)) / 34f);
                    color = new Color(0.85f, 0.93f, 1f, 0.06f + 0.28f * sheen * sheen);
                    if (r > ringInner - 2f) color = new Color(0.25f, 0.20f, 0.10f, 0.5f);       // inner shadow line
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---- Wrist view -----------------------------------------------------------------------------------------

    private void ToggleWristView()
    {
        wristPanel.SetActive(!wristPanel.activeSelf);
        if (wristPanel.activeSelf) wristPanel.transform.SetAsLastSibling();
    }

    private void BuildWristView()
    {
        // Full-screen dim layer; a click anywhere closes the view.
        wristPanel = new GameObject("WristView", typeof(RectTransform), typeof(Image), typeof(Button));
        wristPanel.transform.SetParent(transform, false);
        var panelRect = wristPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        wristPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
        wristPanel.GetComponent<Button>().onClick.AddListener(() => wristPanel.SetActive(false));

        // Gold frame around the picture.
        var frame = RuntimeUIKit.MakeBox("Frame", wristPanel.transform, Vector2.zero, new Vector2(500f, 360f), new Color(0.80f, 0.64f, 0.30f, 1f));
        var frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        frame.raycastTarget = false;

        var pictureGO = new GameObject("Wrist", typeof(RectTransform), typeof(RawImage));
        pictureGO.transform.SetParent(wristPanel.transform, false);
        var pictureRect = pictureGO.GetComponent<RectTransform>();
        pictureRect.anchorMin = pictureRect.anchorMax = new Vector2(0.5f, 0.5f);
        pictureRect.pivot = new Vector2(0.5f, 0.5f);
        pictureRect.sizeDelta = new Vector2(480f, 340f);
        pictureRect.anchoredPosition = Vector2.zero;
        var picture = pictureGO.GetComponent<RawImage>();
        picture.texture = MakeWristTexture();
        picture.raycastTarget = false;

        // The live dial sits exactly on the case drawn in the picture (centre of the picture, 2 px per texel).
        var bigHolder = new GameObject("BigDial", typeof(RectTransform));
        bigHolder.transform.SetParent(pictureGO.transform, false);
        var bigRect = bigHolder.GetComponent<RectTransform>();
        bigRect.anchorMin = bigRect.anchorMax = new Vector2(0.5f, 0.5f);
        bigRect.pivot = new Vector2(0.5f, 0.5f);
        bigRect.sizeDelta = new Vector2(148f, 148f);
        bigRect.anchoredPosition = Vector2.zero;
        BuildDial(bigRect, 148f, out bigHourHand, out bigMinuteHand);

        var title = RuntimeUIKit.MakeText("Title", wristPanel.transform, "Votre montre", 18, TextAnchor.UpperCenter, new Color(0.96f, 0.88f, 0.66f),
            Vector2.zero, new Vector2(300f, 26f));
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 205f);

        var hint = RuntimeUIKit.MakeText("Hint", wristPanel.transform, "Cliquez pour fermer", 12, TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.82f),
            Vector2.zero, new Vector2(300f, 18f));
        var hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, -205f);

        wristPanel.SetActive(false);
    }

    private static void FillRect(Color32[] px, int w, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
                px[y * w + x] = c;
    }

    // Wrist seen from above, in pixel-art style (240 x 170, shown at 2x): shirt sleeve with a cuff, forearm, hand,
    // steel bracelet and the watch case (the live dial is drawn over the case).
    private static Texture2D MakeWristTexture()
    {
        const int w = 240;
        const int h = 170;
        var px = new Color32[w * h];

        // Background: dark plum gradient with a soft vignette.
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var vignette = 1f - 0.35f * Mathf.Pow(Mathf.Abs(x - w / 2f) / (w / 2f), 2f);
                var shade = Mathf.Lerp(0.42f, 0.78f, y / (float)h) * vignette;
                px[y * w + x] = new Color(0.46f * shade + 0.04f, 0.36f * shade + 0.03f, 0.44f * shade + 0.05f, 1f);
            }
        }

        // Shirt sleeve with folds.
        for (var y = 22; y <= 148; y++)
        {
            for (var x = 0; x <= 62; x++)
            {
                var fold = 0.88f + 0.12f * Mathf.Sin(x * 0.32f + y * 0.06f);
                var lit = Mathf.Lerp(0.78f, 1.04f, (y - 22) / 126f);
                var c = new Color(0.30f * fold * lit + 0.04f, 0.42f * fold * lit + 0.04f, 0.62f * fold * lit + 0.04f, 1f);
                px[y * w + x] = c;
            }
        }
        // Cuff.
        FillRect(px, w, 60, 24, 77, 146, new Color32(232, 234, 240, 255));
        FillRect(px, w, 60, 24, 61, 146, new Color32(150, 154, 168, 255));
        FillRect(px, w, 74, 24, 77, 146, new Color32(190, 194, 206, 255));
        FillRect(px, w, 60, 24, 77, 25, new Color32(150, 154, 168, 255));
        FillRect(px, w, 60, 145, 77, 146, new Color32(150, 154, 168, 255));

        // Forearm, palm, fingers and thumb share the same skin ramp (lit from above).
        for (var y = 28; y <= 133; y++)
        {
            var t = Mathf.Clamp01((y - 37f) / 96f);
            var skin = Color.Lerp(new Color(0.78f, 0.56f, 0.43f), new Color(0.97f, 0.82f, 0.68f), Mathf.Pow(t, 0.8f));
            Color32 c = skin;
            if (y >= 37) FillRect(px, w, 78, y, 205, y, c);
            if (y >= 40 && y <= 130) FillRect(px, w, 170, y, 205, y, c);
            // Thumb under the hand.
            if (y >= 28 && y <= 42) FillRect(px, w, 180, y, 216, y, new Color32((byte)(c.r - 14), (byte)(c.g - 18), (byte)(c.b - 16), 255));
        }
        var fingerCenters = new[] { 118, 98, 78, 58 };
        foreach (var fy in fingerCenters)
        {
            for (var y = fy - 8; y <= fy + 8; y++)
            {
                var t = Mathf.Clamp01((y - 37f) / 96f);
                var skin = Color.Lerp(new Color(0.78f, 0.56f, 0.43f), new Color(0.97f, 0.82f, 0.68f), Mathf.Pow(t, 0.8f));
                var edge = (y == fy - 8 || y == fy + 8) ? 0.82f : 1f;
                var c = new Color(skin.r * edge, skin.g * edge, skin.b * edge, 1f);
                for (var x = 205; x <= 233; x++) px[y * w + x] = c;
                // Rounded fingertip.
                for (var x = 234; x <= 238; x++)
                {
                    var inset = (x - 233) * (x - 233) * 0.35f;
                    if (Mathf.Abs(y - fy) <= 8 - inset) px[y * w + x] = c;
                }
            }
        }
        // Knuckle line between palm and fingers.
        for (var y = 50; y <= 126; y++) px[y * w + 205] = new Color32(196, 146, 112, 255);

        // Soft shadow of the watch on the skin.
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var dx = x - 117f;
                var dy = y - 79f;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 46f) continue;
                var c = (Color)px[y * w + x];
                px[y * w + x] = Color.Lerp(c, Color.black, 0.38f * (1f - d / 46f) + 0.1f);
            }
        }

        // Steel bracelet crossing the wrist: rows of links.
        for (var y = 30; y <= 140; y++)
        {
            var link = (y - 30) / 8;
            var inRow = (y - 30) % 8;
            for (var x = 102; x <= 138; x++)
            {
                var side = Mathf.Min(x - 102, 138 - x);
                var baseTone = link % 2 == 0 ? 0.74f : 0.62f;
                var tone = baseTone - (side < 2 ? 0.18f : 0f) + (side > 15 && side < 20 ? 0.06f : 0f) - (inRow == 0 ? 0.28f : 0f) + (inRow == 1 ? 0.10f : 0f);
                var edge = (y < 33 || y > 137) ? 0.8f : 1f;
                px[y * w + x] = new Color(tone * 0.95f * edge, tone * 0.97f * edge, tone * edge + 0.03f, 1f);
            }
        }

        // Lugs and crown.
        FillRect(px, w, 112, 120, 128, 134, new Color32(176, 180, 192, 255));
        FillRect(px, w, 112, 36, 128, 50, new Color32(150, 154, 166, 255));
        FillRect(px, w, 158, 80, 168, 90, new Color32(180, 184, 196, 255));
        FillRect(px, w, 158, 88, 168, 90, new Color32(214, 218, 228, 255));
        FillRect(px, w, 158, 80, 168, 81, new Color32(112, 116, 128, 255));

        // Case: polished steel disc (the live dial covers all but its outer ring).
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var dx = x - 120f;
                var dy = y - 85f;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 40f) continue;
                var shade = 0.62f + 0.34f * Mathf.Cos(Mathf.Atan2(dy, dx) - 2.3f);
                var c = new Color(shade, shade + 0.01f, shade + 0.04f, Mathf.Clamp01(40.5f - d));
                px[y * w + x] = Color.Lerp((Color)px[y * w + x], new Color(c.r, c.g, c.b, 1f), c.a);
            }
        }

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(px);
        texture.Apply();
        return texture;
    }

    private void Update()
    {
        // The in-game day lasts 14 real minutes: the phase (0..1) of the day gives the time of day.
        var minutesOfDay = DayNightOverlay.GetCurrentPhase() * 24f * 60f;
        var hourAngle = -(minutesOfDay / 60f % 12f) * 30f;
        var minuteAngle = -(minutesOfDay % 60f) * 6f;
        hourHand.localEulerAngles = new Vector3(0f, 0f, hourAngle);
        minuteHand.localEulerAngles = new Vector3(0f, 0f, minuteAngle);
        if (wristPanel.activeSelf)
        {
            bigHourHand.localEulerAngles = new Vector3(0f, 0f, hourAngle);
            bigMinuteHand.localEulerAngles = new Vector3(0f, 0f, minuteAngle);
            if (Input.GetKeyDown(KeyCode.Escape)) wristPanel.SetActive(false);
        }

        // Magnifying-glass effect under the mouse: the dial swells inside a lens that fades in.
        hoverAmount = Mathf.MoveTowards(hoverAmount, hovered ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        dialHolder.localScale = Vector3.one * Mathf.Lerp(1f, HoverZoom, hoverAmount);
        lens.color = new Color(1f, 1f, 1f, hoverAmount);
    }
}

// Hover and click on the small watch.
public class WatchPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public System.Action<bool> OnHover;
    public System.Action OnClick;

    public void OnPointerEnter(PointerEventData eventData) { OnHover?.Invoke(true); }
    public void OnPointerExit(PointerEventData eventData) { OnHover?.Invoke(false); }
    public void OnPointerClick(PointerEventData eventData) { OnClick?.Invoke(); }

    private void OnDisable() { OnHover?.Invoke(false); }
}
