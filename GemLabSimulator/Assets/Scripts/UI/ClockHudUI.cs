using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The clock of the screen, at the top right, in the style of Stardew Valley: a dial whose hand follows the sun and the moon through the day, and three
// cream cells: the weekday and the day, the season, the moon and what the day holds (the gavel on the day of the auctions, a star for a holiday or an
// event), and the time. The purse (MoneyHudUI) hangs under it. Added to the Canvas of every game scene.
public class ClockHudUI : MonoBehaviour
{
    public static ClockHudUI Instance { get; private set; }

    public const float Width = 210f;
    public const float Height = 94f;
    public const float Margin = 12f;

    private const int DialPixels = 32;

    private RectTransform hand;
    private Text dateText;
    private Text timeText;
    private Image seasonIcon;
    private Image moonIcon;
    private Image eventIcon;
    private int shownDay = -1;

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
        if (canvas != null && canvas.GetComponent<ClockHudUI>() == null) canvas.AddComponent<ClockHudUI>();
    }

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        var phase = DayNightOverlay.GetCurrentPhase();
        timeText.text = DayNightOverlay.GetClockLabel();
        // The hand points down at midnight, up at noon, left in the morning and right in the evening.
        hand.localRotation = Quaternion.Euler(0f, 0f, 180f - phase * 360f);

        var clock = GameClock.Instance;
        var day = clock != null ? clock.CurrentDay : 1;
        if (day == shownDay) return;
        shownDay = day;
        ShowDate(GameCalendar.DateOf(day));
    }

    private void ShowDate(DateTime date)
    {
        dateText.text = GameCalendar.DayLabel(date);
        seasonIcon.sprite = SeasonSprite(date.Month);
        moonIcon.sprite = MoonSprite(GameCalendar.MoonPhase(date));

        Sprite special = null;
        if (AuctionCatalog.IsAuctionDay(date)) special = BagTabIcons.For("Enchères");
        else
        {
            foreach (var e in GameCalendar.EventsOn(date))
            {
                if (!e.Notable) continue;
                special = StarSprite();
                break;
            }
        }
        eventIcon.enabled = special != null;
        eventIcon.sprite = special;
    }

    // ---- Construction --------------------------------------------------------------------------------------------

    private void Build()
    {
        var root = new GameObject("ClockHud", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(1);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-Margin, -Margin);
        rect.sizeDelta = new Vector2(Width, Height);

        StardewKit.MakeFrame("Frame", root.transform, Vector2.zero, new Vector2(Width, Height));

        // The dial on the left.
        var dial = new GameObject("Dial", typeof(RectTransform), typeof(Image));
        dial.transform.SetParent(root.transform, false);
        RuntimeUIKit.SetTopLeft(dial.GetComponent<RectTransform>(), new Vector2(8f, -15f), new Vector2(DialPixels * 2f, DialPixels * 2f));
        var dialImage = dial.GetComponent<Image>();
        dialImage.sprite = MakeDial();
        dialImage.raycastTarget = false;

        var handGO = new GameObject("Hand", typeof(RectTransform), typeof(Image));
        handGO.transform.SetParent(dial.transform, false);
        hand = handGO.GetComponent<RectTransform>();
        hand.anchorMin = hand.anchorMax = hand.pivot = new Vector2(0.5f, 0.5f);
        hand.sizeDelta = new Vector2(DialPixels * 2f, DialPixels * 2f);
        hand.anchoredPosition = Vector2.zero;
        var handImage = handGO.GetComponent<Image>();
        handImage.sprite = MakeHand();
        handImage.raycastTarget = false;

        // The three cells on the right.
        const float cellX = 78f;
        const float cellW = 120f;
        const float cellH = 22f;
        StardewKit.MakeInset("DateCell", root.transform, new Vector2(cellX, -10f), new Vector2(cellW, cellH));
        dateText = CellText("Date", root.transform, new Vector2(cellX, -10f), new Vector2(cellW, cellH), 13);

        StardewKit.MakeInset("IconCell", root.transform, new Vector2(cellX, -36f), new Vector2(cellW, cellH));
        seasonIcon = CellIcon("Season", root.transform, new Vector2(cellX + 12f, -36f));
        moonIcon = CellIcon("Moon", root.transform, new Vector2(cellX + 49f, -36f));
        eventIcon = CellIcon("Event", root.transform, new Vector2(cellX + 86f, -36f));
        eventIcon.enabled = false;

        StardewKit.MakeInset("TimeCell", root.transform, new Vector2(cellX, -62f), new Vector2(cellW, cellH));
        timeText = CellText("Time", root.transform, new Vector2(cellX, -62f), new Vector2(cellW, cellH), 14);
    }

    private static Text CellText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        var text = RuntimeUIKit.MakeText(name, parent, "", fontSize, TextAnchor.MiddleCenter, StardewKit.Ink, position, size);
        text.fontStyle = FontStyle.Bold;
        return text;
    }

    private static Image CellIcon(string name, Transform parent, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, new Vector2(22f, 22f));
        var image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    // ---- Pixel-art -------------------------------------------------------------------------------------------------

    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    // The dial: a ring of orange, a day sky above the horizon, a night sky with a few stars below.
    private static Sprite MakeDial()
    {
        const int n = DialPixels;
        var t = StardewKit.BlankTexture(n, n);
        var center = (n - 1) / 2f;
        var clear = new Color(0f, 0f, 0f, 0f);
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                Color c;
                if (d > 15.5f) c = clear;
                else if (d > 14.2f) c = C(0.27f, 0.10f, 0.03f);
                else if (d > 12.6f) c = dx + dy > 0f ? C(0.72f, 0.33f, 0.08f) : C(1.00f, 0.74f, 0.38f);
                else if (d > 11.5f) c = C(0.50f, 0.22f, 0.05f);
                else if (Mathf.Abs(dy) < 0.6f) c = C(0.50f, 0.28f, 0.10f);
                else if (dy > 0f) c = Color.Lerp(C(0.55f, 0.80f, 0.98f), C(0.98f, 0.92f, 0.70f), Mathf.Clamp01(1f - dy / 10f));
                else c = C(0.10f, 0.12f, 0.32f);
                t.SetPixel(x, y, c);
            }
        }
        foreach (var (sx, sy) in new[] { (9, 8), (21, 11), (13, 4), (18, 6), (11, 12) }) t.SetPixel(sx, sy, C(1f, 0.97f, 0.80f));
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    // The hand: an arrow from the middle of the dial to the top, in gold with a red centre.
    private static Sprite MakeHand()
    {
        const int n = DialPixels;
        var t = StardewKit.BlankTexture(n, n);
        var clear = new Color(0f, 0f, 0f, 0f);
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++) t.SetPixel(x, y, clear);

        var cx = n / 2;
        var dark = C(0.27f, 0.10f, 0.03f);
        var gold = C(1.00f, 0.82f, 0.25f);
        for (var y = cx; y < cx + 9; y++)
        {
            t.SetPixel(cx - 2, y, dark);
            t.SetPixel(cx + 1, y, dark);
            t.SetPixel(cx - 1, y, gold);
            t.SetPixel(cx, y, gold);
        }
        // The head of the arrow.
        for (var i = 0; i < 4; i++)
        {
            var half = 3 - i;
            for (var x = cx - 1 - half; x <= cx + half; x++)
            {
                var edge = x == cx - 1 - half || x == cx + half || i == 0;
                t.SetPixel(x, cx + 9 + i, edge ? dark : gold);
            }
        }
        for (var y = -2; y <= 1; y++)
            for (var x = -2; x <= 1; x++)
                t.SetPixel(cx + x, cx + y, (x == -2 || x == 1 || y == -2 || y == 1) ? dark : C(0.85f, 0.18f, 0.14f));
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();

    private static Sprite Cached(string key, Func<Sprite> make)
    {
        if (!IconCache.TryGetValue(key, out var sprite) || sprite == null)
        {
            sprite = make();
            IconCache[key] = sprite;
        }
        return sprite;
    }

    private static Sprite Draw12(Func<int, int, Color?> pixel)
    {
        var t = StardewKit.BlankTexture(12, 12);
        for (var y = 0; y < 12; y++)
            for (var x = 0; x < 12; x++)
                t.SetPixel(x, y, pixel(x, 11 - y) ?? new Color(0f, 0f, 0f, 0f));
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 100f);
    }

    // The season of a month: a flower in spring, a sun in summer, a leaf in autumn, a snowflake in winter.
    private static Sprite SeasonSprite(int month)
    {
        if (month >= 3 && month <= 5) return Cached("spring", () => Draw12((x, y) =>
        {
            var dx = x - 5.5f;
            var dy = y - 5.5f;
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < 1.6f) return C(1f, 0.85f, 0.25f);
            if (d < 5.4f)
            {
                var angle = Mathf.Atan2(dy, dx);
                if (Mathf.Cos(angle * 5f) > 0.25f && d > 2f) return C(0.98f, 0.55f, 0.72f);
            }
            return null;
        }));
        if (month >= 6 && month <= 8) return Cached("summer", () => Draw12((x, y) =>
        {
            var dx = x - 5.5f;
            var dy = y - 5.5f;
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < 3.2f) return d > 2.4f ? C(0.95f, 0.62f, 0.10f) : C(1f, 0.88f, 0.25f);
            var angle = Mathf.Atan2(dy, dx);
            if (d < 5.6f && Mathf.Cos(angle * 8f) > 0.8f) return C(1f, 0.78f, 0.20f);
            return null;
        }));
        if (month >= 9 && month <= 11) return Cached("autumn", () => Draw12((x, y) =>
        {
            var dx = (x - 5.5f) * 0.8f + (y - 5.5f) * 0.6f;
            var dy = -(x - 5.5f) * 0.6f + (y - 5.5f) * 0.8f;
            if (dx * dx / 20f + dy * dy / 7f < 1f) return Mathf.Abs(dy) < 0.5f ? C(0.62f, 0.25f, 0.05f) : C(0.92f, 0.45f, 0.10f);
            if (x >= 8 && y <= 3 && Mathf.Abs(x - 11 + y) <= 1) return C(0.45f, 0.20f, 0.05f);
            return null;
        }));
        return Cached("winter", () => Draw12((x, y) =>
        {
            var dx = Mathf.Abs(x - 5.5f);
            var dy = Mathf.Abs(y - 5.5f);
            var onArm = dx < 0.7f || dy < 0.7f || Mathf.Abs(dx - dy) < 0.7f;
            if (onArm && dx * dx + dy * dy < 30f) return dx + dy < 2f ? C(1f, 1f, 1f) : C(0.70f, 0.88f, 1f);
            return null;
        }));
    }

    // The moon of the day: lit on the right while it grows, on the left while it shrinks.
    private static Sprite MoonSprite(float phase)
    {
        var step = Mathf.RoundToInt(phase * 16f) % 16;
        return Cached("moon" + step, () => Draw12((x, y) =>
        {
            var nx = (x - 5.5f) / 5f;
            var ny = (y - 5.5f) / 5f;
            if (nx * nx + ny * ny > 1.05f) return null;
            var p = step / 16f;
            var terminator = Mathf.Cos(2f * Mathf.PI * p) * Mathf.Sqrt(Mathf.Max(0f, 1f - ny * ny));
            var lit = p < 0.5f ? nx > terminator : nx < -terminator;
            if (nx * nx + ny * ny > 0.8f) return lit ? C(0.95f, 0.93f, 0.75f) : C(0.28f, 0.30f, 0.45f);
            return lit ? C(1f, 0.98f, 0.85f) : C(0.18f, 0.20f, 0.36f);
        }));
    }

    // A star: for a holiday or an event of the day.
    private static Sprite StarSprite()
    {
        return Cached("star", () => Draw12((x, y) =>
        {
            var dx = Mathf.Abs(x - 5.5f);
            var dy = Mathf.Abs(y - 5.5f);
            if (dx * dy <= 3.2f && dx + dy <= 6.2f) return dx + dy < 3f ? C(1f, 0.95f, 0.55f) : C(0.96f, 0.72f, 0.12f);
            return null;
        }));
    }
}
