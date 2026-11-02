using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The money of the player, at the top left of the screen: a rounded purse with a gold coin and the amount, which counts up or down when
// it changes and gives a little jump to the coin. The gains and the expenses float away under it. Replaces the old plain text; added to the
// Canvas of every game scene.
public class MoneyHudUI : MonoBehaviour
{
    public static MoneyHudUI Instance { get; private set; }

    private const float Width = 200f;
    private const float Height = 46f;

    private class Floater
    {
        public Text Text;
        public float Age;
        public Color Color;
    }

    private Text amountText;
    private RectTransform coinRect;
    private float shown;
    private int target;
    private bool initialized;
    private float pulse;
    private readonly List<Floater> floaters = new List<Floater>();
    private RectTransform floaterRoot;

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
        if (canvas != null && canvas.GetComponent<MoneyHudUI>() == null) canvas.AddComponent<MoneyHudUI>();
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

    private void Build()
    {
        var root = new GameObject("MoneyHud", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.SetSiblingIndex(1);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(16f, -16f);
        rect.sizeDelta = new Vector2(Width, Height);

        // Gold edge, dark purse, and a faint light along the top.
        RuntimeUIKit.MakeBox("Edge", root.transform, Vector2.zero, new Vector2(Width, Height), new Color(0.80f, 0.64f, 0.26f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Purse", root.transform, new Vector2(2f, -2f), new Vector2(Width - 4f, Height - 4f), new Color(0.07f, 0.08f, 0.12f, 0.93f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Light", root.transform, new Vector2(6f, -4f), new Vector2(Width - 12f, 10f), new Color(1f, 1f, 1f, 0.06f)).raycastTarget = false;

        var coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        coin.transform.SetParent(root.transform, false);
        coinRect = coin.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(coinRect, new Vector2(8f, -7f), new Vector2(32f, 32f));
        coinRect.pivot = new Vector2(0.5f, 0.5f);
        coinRect.anchoredPosition = new Vector2(24f, -23f);
        var coinImage = coin.GetComponent<Image>();
        coinImage.sprite = MakeCoinSprite();
        coinImage.raycastTarget = false;
        var euro = RuntimeUIKit.MakeText("Euro", coin.transform, "€", 17, TextAnchor.MiddleCenter, new Color(0.50f, 0.33f, 0.04f), Vector2.zero, new Vector2(32f, 32f));
        euro.fontStyle = FontStyle.Bold;

        amountText = RuntimeUIKit.MakeText("Amount", root.transform, "0", 24, TextAnchor.MiddleLeft, new Color(1f, 0.94f, 0.74f), new Vector2(50f, -6f), new Vector2(Width - 58f, 34f));
        amountText.fontStyle = FontStyle.Bold;
        var shadow = amountText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        floaterRoot = new GameObject("Floaters", typeof(RectTransform)).GetComponent<RectTransform>();
        floaterRoot.SetParent(root.transform, false);
        floaterRoot.anchorMin = floaterRoot.anchorMax = new Vector2(0f, 1f);
        floaterRoot.pivot = new Vector2(0f, 1f);
        floaterRoot.anchoredPosition = new Vector2(0f, -Height);
        floaterRoot.sizeDelta = new Vector2(Width, 30f);
    }

    // A gold coin: a darker rim, a lighter face with a groove, and a highlight.
    private static Sprite MakeCoinSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        var center = (size - 1) / 2f;
        var radius = size / 2f - 1f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.Clamp01(radius - d + 0.5f);
                if (alpha <= 0f) continue;

                Color color;
                if (d > radius - 5f) color = new Color(0.62f, 0.43f, 0.08f);                              // rim
                else if (d > radius - 7f) color = new Color(0.98f, 0.82f, 0.32f);                         // edge light
                else if (d > radius - 9f) color = new Color(0.72f, 0.52f, 0.10f);                         // groove
                else color = Color.Lerp(new Color(1f, 0.90f, 0.42f), new Color(0.93f, 0.72f, 0.20f), (dy + dx * 0.4f + radius) / (radius * 2.4f));
                // Highlight, up and to the left.
                var highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(center - 12f, center + 14f)) / 16f);
                color = Color.Lerp(color, Color.white, highlight * 0.35f);
                pixels[y * size + x] = new Color32((byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        var wallet = PlayerWallet.Instance;
        if (wallet == null) return;

        var money = wallet.Money;
        if (!initialized)
        {
            initialized = true;
            target = money;
            shown = money;
        }
        else if (money != target)
        {
            SpawnFloater(money - target);
            target = money;
            pulse = 1f;
        }

        // The figure runs towards the real amount.
        if (!Mathf.Approximately(shown, target))
        {
            shown += (target - shown) * Mathf.Min(1f, Time.unscaledDeltaTime * 6f);
            if (Mathf.Abs(target - shown) < 1f) shown = target;
        }
        amountText.text = Format(Mathf.RoundToInt(shown));

        // The coin jumps a little when the amount changes.
        pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 3f);
        coinRect.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(pulse * Mathf.PI));

        for (var i = floaters.Count - 1; i >= 0; i--)
        {
            var floater = floaters[i];
            floater.Age += Time.unscaledDeltaTime;
            var t = floater.Age / 1.5f;
            if (t >= 1f)
            {
                Destroy(floater.Text.gameObject);
                floaters.RemoveAt(i);
                continue;
            }
            var rect = floater.Text.rectTransform;
            rect.anchoredPosition = new Vector2(52f, -t * 22f);
            floater.Text.color = new Color(floater.Color.r, floater.Color.g, floater.Color.b, 1f - t * t);
        }
    }

    private void SpawnFloater(int delta)
    {
        var color = delta > 0 ? new Color(0.50f, 0.92f, 0.50f) : new Color(0.96f, 0.45f, 0.42f);
        var text = RuntimeUIKit.MakeText("Delta", floaterRoot, (delta > 0 ? "+" : "-") + Format(Mathf.Abs(delta)) + " €", 18, TextAnchor.MiddleLeft, color,
            new Vector2(52f, 0f), new Vector2(150f, 26f));
        text.fontStyle = FontStyle.Bold;
        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(1f, -1f);
        floaters.Add(new Floater { Text = text, Color = color });
    }

    // 12345 -> "12 345"
    private static string Format(int amount)
    {
        var digits = Mathf.Abs(amount).ToString();
        var result = "";
        for (var i = 0; i < digits.Length; i++)
        {
            if (i > 0 && (digits.Length - i) % 3 == 0) result += " ";
            result += digits[i];
        }
        return amount < 0 ? "-" + result : result;
    }
}
