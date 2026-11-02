using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A small window onto the sky under the watch, in the town: the sun crosses it by day, the moon by night (with the phase of the day),
// the stars twinkle in the dark, and the colour of the sky follows the hour from dawn to dusk. Added to the Canvas of the town.
public class SkyDialUI : MonoBehaviour
{
    private const float FrameWidth = 128f;
    private const float FrameHeight = 56f;
    private const float Inset = 3f;
    private const float WindowWidth = FrameWidth - Inset * 2f;
    private const float WindowHeight = FrameHeight - Inset * 2f;
    private const int StarCount = 18;
    private const int MoonPhases = 8;
    private const int GradientHeight = 32;

    private struct SkyKey
    {
        public float Hour;
        public Color Top;
        public Color Bottom;

        public SkyKey(float hour, Color top, Color bottom)
        {
            Hour = hour;
            Top = top;
            Bottom = bottom;
        }
    }

    private static readonly SkyKey[] Keys =
    {
        new SkyKey(0.0f, new Color(0.03f, 0.05f, 0.15f), new Color(0.08f, 0.10f, 0.24f)),
        new SkyKey(4.8f, new Color(0.03f, 0.05f, 0.15f), new Color(0.08f, 0.10f, 0.24f)),
        new SkyKey(5.8f, new Color(0.16f, 0.18f, 0.42f), new Color(0.62f, 0.40f, 0.52f)),
        new SkyKey(6.6f, new Color(0.38f, 0.50f, 0.78f), new Color(1.00f, 0.66f, 0.46f)),
        new SkyKey(8.0f, new Color(0.38f, 0.66f, 0.95f), new Color(0.78f, 0.88f, 0.98f)),
        new SkyKey(12.0f, new Color(0.26f, 0.58f, 0.95f), new Color(0.70f, 0.86f, 0.99f)),
        new SkyKey(16.5f, new Color(0.32f, 0.60f, 0.93f), new Color(0.80f, 0.88f, 0.96f)),
        new SkyKey(18.2f, new Color(0.42f, 0.55f, 0.82f), new Color(1.00f, 0.80f, 0.52f)),
        new SkyKey(19.3f, new Color(0.30f, 0.28f, 0.58f), new Color(1.00f, 0.44f, 0.30f)),
        new SkyKey(20.4f, new Color(0.14f, 0.14f, 0.38f), new Color(0.58f, 0.30f, 0.46f)),
        new SkyKey(21.6f, new Color(0.05f, 0.07f, 0.20f), new Color(0.14f, 0.14f, 0.32f)),
        new SkyKey(24.0f, new Color(0.03f, 0.05f, 0.15f), new Color(0.08f, 0.10f, 0.24f)),
    };

    private Texture2D skyTexture;
    private Image hills;
    private RectTransform sunRect;
    private Image sunImage;
    private RectTransform moonRect;
    private Image moonImage;
    private readonly Sprite[] moonSprites = new Sprite[MoonPhases];
    private int shownPhase = -1;
    private Image[] stars;
    private Vector2[] starPositions;
    private readonly Color32[] gradient = new Color32[GradientHeight];

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
        if (scene.name != "Town") return;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<SkyDialUI>() == null) canvas.AddComponent<SkyDialUI>();
    }

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        var root = new GameObject("SkyDial", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(1);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.sizeDelta = new Vector2(FrameWidth, FrameHeight);
        // Centred under the watch, which is itself centred under the minimap.
        rootRect.anchoredPosition = new Vector2(-16f - (192f - FrameWidth) / 2f, -292f);
        var frame = root.GetComponent<Image>();
        frame.sprite = RuntimeUIKit.Rounded;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.52f, 0.55f, 0.62f, 1f);
        frame.raycastTarget = false;

        var window = new GameObject("Window", typeof(RectTransform), typeof(RectMask2D));
        window.transform.SetParent(root.transform, false);
        RuntimeUIKit.SetTopLeft(window.GetComponent<RectTransform>(), new Vector2(Inset, -Inset), new Vector2(WindowWidth, WindowHeight));

        skyTexture = new Texture2D(1, GradientHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var sky = new GameObject("Sky", typeof(RectTransform), typeof(RawImage));
        sky.transform.SetParent(window.transform, false);
        RuntimeUIKit.SetTopLeft(sky.GetComponent<RectTransform>(), Vector2.zero, new Vector2(WindowWidth, WindowHeight));
        var skyImage = sky.GetComponent<RawImage>();
        skyImage.texture = skyTexture;
        skyImage.raycastTarget = false;

        // Stars, at fixed places in the upper part of the sky.
        stars = new Image[StarCount];
        starPositions = new Vector2[StarCount];
        var rnd = new System.Random(7);
        for (var i = 0; i < StarCount; i++)
        {
            starPositions[i] = new Vector2(4f + (float)rnd.NextDouble() * (WindowWidth - 8f), 3f + (float)rnd.NextDouble() * (WindowHeight - 22f));
            var size = i % 5 == 0 ? 3f : 2f;
            stars[i] = RuntimeUIKit.MakeBox("Star" + i, window.transform, new Vector2(starPositions[i].x, -starPositions[i].y), new Vector2(size, size), Color.white, false);
            stars[i].raycastTarget = false;
        }

        sunImage = MakeBody("Sun", window.transform, MakeSunSprite());
        sunRect = sunImage.rectTransform;
        moonImage = MakeBody("Moon", window.transform, null);
        moonRect = moonImage.rectTransform;

        // Hills on the horizon.
        var hillsGo = new GameObject("Hills", typeof(RectTransform), typeof(Image));
        hillsGo.transform.SetParent(window.transform, false);
        RuntimeUIKit.SetTopLeft(hillsGo.GetComponent<RectTransform>(), new Vector2(0f, -(WindowHeight - 14f)), new Vector2(WindowWidth, 14f));
        hills = hillsGo.GetComponent<Image>();
        hills.sprite = MakeHillsSprite();
        hills.raycastTarget = false;
    }

    private static Image MakeBody(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), Vector2.zero, new Vector2(16f, 16f));
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    // ---- Sprites -----------------------------------------------------------------------------------------------

    private static Sprite MakeSunSprite()
    {
        const int size = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d <= 4.5f) pixels[y * size + x] = new Color32(255, 238, 140, 255);
                else if (d <= 7.5f) pixels[y * size + x] = new Color32(255, 214, 90, (byte)(Mathf.Clamp01((7.5f - d) / 3f) * 120f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // The moon for one of the eight phases of its cycle: new moon, crescents, quarters, gibbous, full moon, and back.
    private Sprite MoonSprite(int phase)
    {
        if (moonSprites[phase] != null) return moonSprites[phase];

        const int size = 16;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        var center = (size - 1) / 2f;
        const float radius = 6f;
        var p = phase / (float)MoonPhases;
        var m = Mathf.Cos(p * Mathf.PI * 2f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var nx = (x - center) / radius;
                var ny = (y - center) / radius;
                var r2 = nx * nx + ny * ny;
                if (r2 > 1f) continue;
                var s = Mathf.Sqrt(Mathf.Max(0f, 1f - ny * ny));
                var lit = p < 0.5f ? nx > m * s : nx < -m * s;
                pixels[y * size + x] = lit ? new Color32(246, 244, 220, 255) : new Color32(44, 50, 78, 235);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        moonSprites[phase] = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return moonSprites[phase];
    }

    private static Sprite MakeHillsSprite()
    {
        const int w = 122;
        const int h = 14;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (var x = 0; x < w; x++)
        {
            var top = 6f + Mathf.Sin(x * 0.11f) * 2.2f + Mathf.Sin(x * 0.043f + 1f) * 2.6f;
            for (var y = 0; y < h; y++)
                if (y <= top) pixels[y * w + x] = Color.white;
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---- Animation ---------------------------------------------------------------------------------------------

    private void Update()
    {
        var hour = OpeningHours.CurrentHour;
        EvaluateSky(hour, out var top, out var bottom);
        for (var i = 0; i < GradientHeight; i++)
        {
            // Row 0 of the texture is the bottom of the picture.
            var t = i / (float)(GradientHeight - 1);
            gradient[i] = Color.Lerp(bottom, top, t);
        }
        skyTexture.SetPixels32(gradient);
        skyTexture.Apply();

        var darkness = DayNightLighting.Darkness;
        hills.color = Color.Lerp(new Color(0.22f, 0.42f, 0.22f), new Color(0.03f, 0.05f, 0.09f), Mathf.Clamp01(darkness * 1.3f));

        // The sun crosses the sky from 6h to 20h, the moon from 19h to 7h.
        PlaceBody(sunRect, sunImage, (hour - 5.8f) / 14.4f);
        var moonT = Mathf.Repeat(hour - 19f, 24f) / 12f;
        PlaceBody(moonRect, moonImage, moonT <= 1f ? moonT : -1f);

        var day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        var phase = ((day % MoonPhases) + MoonPhases) % MoonPhases;
        if (phase != shownPhase)
        {
            shownPhase = phase;
            moonImage.sprite = MoonSprite(phase);
        }

        var starLevel = Mathf.Clamp01(darkness * 1.4f - 0.2f);
        for (var i = 0; i < stars.Length; i++)
        {
            var twinkle = 0.55f + 0.45f * Mathf.Sin(Time.time * (1.4f + i * 0.17f) + i * 1.7f);
            stars[i].color = new Color(1f, 0.97f, 0.88f, starLevel * twinkle);
        }
    }

    // t from 0 to 1 along the arc; outside that range the body is below the horizon.
    private static void PlaceBody(RectTransform rect, Image image, float t)
    {
        if (t < 0f || t > 1f)
        {
            image.enabled = false;
            return;
        }
        image.enabled = true;
        var x = Mathf.Lerp(10f, WindowWidth - 10f, t);
        var y = WindowHeight - 14f - Mathf.Sin(t * Mathf.PI) * (WindowHeight - 28f);
        rect.anchoredPosition = new Vector2(x - 8f, -(y - 8f));
        var fade = Mathf.Clamp01(Mathf.Min(t, 1f - t) / 0.08f);
        image.color = new Color(1f, 1f, 1f, fade);
    }

    private static void EvaluateSky(float hour, out Color top, out Color bottom)
    {
        for (var i = 0; i < Keys.Length - 1; i++)
        {
            var a = Keys[i];
            var b = Keys[i + 1];
            if (hour < a.Hour || hour > b.Hour) continue;
            var t = Mathf.Clamp01((hour - a.Hour) / (b.Hour - a.Hour));
            top = Color.Lerp(a.Top, b.Top, t);
            bottom = Color.Lerp(a.Bottom, b.Bottom, t);
            return;
        }
        top = Keys[Keys.Length - 1].Top;
        bottom = Keys[Keys.Length - 1].Bottom;
    }
}
