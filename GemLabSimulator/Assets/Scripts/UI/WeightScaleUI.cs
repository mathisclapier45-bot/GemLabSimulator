using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Precision pocket scale: drag a gem onto the pan and the LCD shows its weight in carats.
// Built entirely at runtime (including the 7-segment digits), so it needs no scene wiring.
public class WeightScaleUI : MonoBehaviour
{
    public static WeightScaleUI Instance { get; private set; }

    private const float PanelWidth = 280f;
    private const float PanelHeight = 566f;
    private const int DigitCount = 5;          // XXX.XX in carats, XX.XXX in grams
    private const float CaratInGrams = 0.2f;
    private const float DigitWidth = 28f;
    private const float DigitHeight = 52f;
    private const float DigitGap = 10f;
    private const float DotSize = 6f;
    private const float SegmentThickness = 5f;

    private static readonly Color BodyColor = new Color(0.07f, 0.07f, 0.08f, 1f);
    private static readonly Color LcdColor = new Color(0.78f, 0.85f, 0.87f, 1f);
    private static readonly Color LcdOffColor = new Color(0.55f, 0.60f, 0.62f, 1f);
    private static readonly Color InkColor = new Color(0.08f, 0.12f, 0.30f, 1f);
    private static readonly Color GhostColor = new Color(0.08f, 0.12f, 0.30f, 0.07f);
    private static readonly Color ButtonColor = new Color(0.16f, 0.16f, 0.18f, 1f);

    // Segment order: a (top), b (top right), c (bottom right), d (bottom), e (bottom left), f (top left), g (middle).
    private static readonly bool[][] DigitSegments =
    {
        new[] { true, true, true, true, true, true, false },     // 0
        new[] { false, true, true, false, false, false, false }, // 1
        new[] { true, true, false, true, true, false, true },    // 2
        new[] { true, true, true, true, false, false, true },    // 3
        new[] { false, true, true, false, false, true, true },   // 4
        new[] { true, false, true, true, false, true, true },    // 5
        new[] { true, false, true, true, true, true, true },     // 6
        new[] { true, true, true, false, false, false, false },  // 7
        new[] { true, true, true, true, true, true, true },      // 8
        new[] { true, true, true, true, false, true, true },     // 9
    };

    private GameObject panel;
    private Image lcdImage;
    private Image gemImage;
    private Text hintText;
    private Text unitText;
    private Text maxText;
    private Text resolutionText;
    private Image dotImage;
    private float digitsOriginX;
    private float digitsOriginY;
    private bool grams;
    private readonly Image[][] segments = new Image[DigitCount][];

    private GemInstance stone;
    private bool poweredOn = true;
    private Coroutine settleRoutine;
    private Sprite roundedSprite;

    // The scale is added to the canvas on first use, so existing scenes don't need to be regenerated.
    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<WeightScaleUI>();
        }

        if (Instance == null) return false;
        Instance.Open();
        return true;
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Open()
    {
        stone = null;
        poweredOn = true;
        grams = false;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        StopSettling();
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        if (gem != null && gem.IsSet)
        {
            ToolFeedbackUI.Instance?.ShowMessage(JewelCatalog.SetRefusal);
            return;
        }
        if (!poweredOn)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Balance éteinte : appuyez sur ON/OFF.");
            return;
        }

        stone = gem;
        Refresh();
        StopSettling();
        settleRoutine = StartCoroutine(Settle());
    }

    private void ClearStone()
    {
        StopSettling();
        stone = null;
        Refresh();
    }

    private void TogglePower()
    {
        poweredOn = !poweredOn;
        if (!poweredOn) stone = null;
        StopSettling();
        Refresh();
    }

    // MODE switches the unit between carats (default) and grams (1 ct = 0.2 g).
    private void ToggleMode()
    {
        if (!poweredOn) return;
        grams = !grams;
        UpdateUnitLabels();
        if (stone != null && settleRoutine == null) ShowValue(stone.CaratWeight);
        else if (stone == null) ShowValue(0f);
    }

    private void UpdateUnitLabels()
    {
        unitText.text = grams ? "g" : "ct";
        maxText.text = grams ? "Max: 10 g" : "Max: 50 ct";
        resolutionText.text = grams ? "d: 0.001 g" : "d: 0.01 ct";
    }

    private void StopSettling()
    {
        if (settleRoutine != null) StopCoroutine(settleRoutine);
        settleRoutine = null;
    }

    // The reading flickers around the real value for a moment, like a real balance settling.
    private IEnumerator Settle()
    {
        var target = stone.CaratWeight;
        const float duration = 0.9f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            var remaining = 1f - elapsed / duration;
            ShowValue(Mathf.Max(0f, target + Random.Range(-1f, 1f) * Mathf.Max(0.5f, target * 0.3f) * remaining));
            yield return new WaitForSeconds(0.06f);
            elapsed += 0.06f;
        }

        ShowValue(target);
        stone.RecordObservation(ExaminationTool.MeasurementType.Weight.ToString());
        ToolFeedbackUI.Instance?.ShowMessage(grams
            ? $"Balance à carats — Poids : {target * CaratInGrams:F3} g ({target:F2} ct)"
            : $"Balance à carats — Poids : {target:F2} ct");
        settleRoutine = null;
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            StopSettling();
        }

        lcdImage.color = poweredOn ? LcdColor : LcdOffColor;
        unitText.enabled = poweredOn;
        UpdateUnitLabels();

        if (stone == null)
        {
            gemImage.enabled = false;
            hintText.text = poweredOn ? "Déposez une gemme sur le plateau" : "";
            if (poweredOn) ShowValue(0f); else ShowBlank();
            return;
        }

        gemImage.enabled = true;
        gemImage.sprite = stone.DisplaySprite;
        hintText.text = "";
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, gem)) return true;
        }
        return false;
    }

    // ---- 7-segment display -----------------------------------------------------------------------------------

    private void ShowValue(float carats)
    {
        // Carats show 2 decimals (XXX.XX), grams 3 decimals (XX.XXX).
        var decimalAfter = grams ? 1 : 2;
        var scaled = grams ? carats * CaratInGrams * 1000f : carats * 100f;
        var text = Mathf.Clamp(Mathf.RoundToInt(scaled), 0, 99999).ToString("D5");
        for (var i = 0; i < DigitCount; i++)
        {
            // Leading zeros are blank, except the units digit before the decimal point.
            var blank = i < decimalAfter && AllLeadingZeros(text, i);
            SetDigit(i, blank ? -1 : text[i] - '0');
        }

        PlaceDot(decimalAfter);
    }

    private void PlaceDot(int afterDigit)
    {
        var digitEnd = digitsOriginX + afterDigit * (DigitWidth + DigitGap) + DigitWidth;
        var rect = dotImage.rectTransform;
        rect.anchoredPosition = new Vector2(digitEnd + (DigitGap - DotSize) / 2f, digitsOriginY - DigitHeight + 8f);
        dotImage.color = InkColor;
    }

    private static bool AllLeadingZeros(string text, int upTo)
    {
        for (var i = 0; i <= upTo; i++)
        {
            if (text[i] != '0') return false;
        }
        return true;
    }

    private void ShowBlank()
    {
        for (var i = 0; i < DigitCount; i++) SetDigit(i, -1);
        dotImage.color = Color.clear;
    }

    private void SetDigit(int index, int value)
    {
        for (var s = 0; s < 7; s++)
        {
            var lit = value >= 0 && DigitSegments[value][s];
            segments[index][s].color = lit ? InkColor : GhostColor;
        }
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        roundedSprite = MakeRoundedSprite(32, 11);

        panel = new GameObject("WeightScalePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-130f, 0f);

        MakeBox("Body", panel.transform, new Vector2(0f, 0f), new Vector2(PanelWidth, 520f), BodyColor);

        // Hinged glass lid over a dark weighing platform.
        MakeBox("Lid", panel.transform, new Vector2(16f, -14f), new Vector2(248f, 254f), new Color(0.80f, 0.86f, 0.92f, 0.22f));
        var platform = MakeBox("Platform", panel.transform, new Vector2(26f, -24f), new Vector2(228f, 234f), new Color(0.04f, 0.04f, 0.05f, 1f));
        platform.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        platform.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        var pan = MakeDisc("Pan", panel.transform, new Vector2(26f + 114f - 75f, -24f - 117f + 75f), 150f);
        pan.raycastTarget = false;

        var gemGO = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        gemGO.transform.SetParent(panel.transform, false);
        SetTopLeft(gemGO.GetComponent<RectTransform>(), new Vector2(26f + 114f - 38f, -24f - 117f + 38f), new Vector2(76f, 76f));
        gemImage = gemGO.GetComponent<Image>();
        gemImage.preserveAspect = true;
        gemImage.raycastTarget = false;
        gemImage.enabled = false;

        hintText = MakeText("Hint", panel.transform, "", 11, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.65f),
            new Vector2(26f, -24f - 234f + 24f), new Vector2(228f, 18f));
        hintText.raycastTarget = false;

        MakeText("Brand", panel.transform, "GEMLAB", 17, TextAnchor.MiddleLeft, Color.white,
            new Vector2(26f, -274f), new Vector2(120f, 24f)).fontStyle = FontStyle.Bold;

        BuildLcd();

        maxText = MakeText("Max", panel.transform, "Max: 50 ct", 12, TextAnchor.MiddleLeft, Color.white,
            new Vector2(26f, -392f), new Vector2(110f, 18f));
        resolutionText = MakeText("Resolution", panel.transform, "d: 0.01 ct", 12, TextAnchor.MiddleRight, Color.white,
            new Vector2(144f, -392f), new Vector2(110f, 18f));

        BuildButtons();

        var close = MakeButton("Close", panel.transform, "Fermer", 14, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2((PanelWidth - 160f) / 2f, -PanelHeight + 34f), new Vector2(160f, 30f));
        close.onClick.AddListener(Close);
    }

    private void BuildLcd()
    {
        MakeBox("LcdFrame", panel.transform, new Vector2(22f, -300f), new Vector2(236f, 86f), new Color(0.02f, 0.02f, 0.03f, 1f));
        lcdImage = MakeBox("Lcd", panel.transform, new Vector2(28f, -306f), new Vector2(224f, 74f), LcdColor);

        digitsOriginX = 40f;
        digitsOriginY = -306f - 11f;
        for (var i = 0; i < DigitCount; i++)
            segments[i] = BuildDigit(new Vector2(digitsOriginX + i * (DigitWidth + DigitGap), digitsOriginY));

        dotImage = MakeBox("Dot", panel.transform, Vector2.zero, new Vector2(DotSize, DotSize), InkColor);

        unitText = MakeText("Unit", panel.transform, "ct", 13, TextAnchor.UpperRight, InkColor,
            new Vector2(222f, -310f), new Vector2(28f, 16f));
        unitText.fontStyle = FontStyle.Bold;
    }

    private Image[] BuildDigit(Vector2 topLeft)
    {
        var w = DigitWidth;
        var h = DigitHeight;
        var t = SegmentThickness;
        var halfH = (h - 3f * t) / 2f;

        var result = new Image[7];
        result[0] = MakeSegment(topLeft + new Vector2(t, 0f), new Vector2(w - 2f * t, t));                         // a
        result[1] = MakeSegment(topLeft + new Vector2(w - t, -t), new Vector2(t, halfH));                          // b
        result[2] = MakeSegment(topLeft + new Vector2(w - t, -(2f * t + halfH)), new Vector2(t, halfH));           // c
        result[3] = MakeSegment(topLeft + new Vector2(t, -(h - t)), new Vector2(w - 2f * t, t));                   // d
        result[4] = MakeSegment(topLeft + new Vector2(0f, -(2f * t + halfH)), new Vector2(t, halfH));              // e
        result[5] = MakeSegment(topLeft + new Vector2(0f, -t), new Vector2(t, halfH));                             // f
        result[6] = MakeSegment(topLeft + new Vector2(t, -(t + halfH)), new Vector2(w - 2f * t, t));               // g
        foreach (var segment in result) segment.color = GhostColor;
        return result;
    }

    private Image MakeSegment(Vector2 topLeft, Vector2 size)
    {
        var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        SetTopLeft(go.GetComponent<RectTransform>(), topLeft, size);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void BuildButtons()
    {
        var labels = new[] { "ZERO\nTARE", "PCS", "MODE", "ON\nOFF" };
        const float buttonSize = 52f;
        var gap = (224f - 4f * buttonSize) / 3f;
        for (var i = 0; i < labels.Length; i++)
        {
            var button = MakeButton("Key" + i, panel.transform, labels[i], 10, ButtonColor,
                new Vector2(28f + i * (buttonSize + gap), -420f), new Vector2(buttonSize, buttonSize));
            if (i == 0) button.onClick.AddListener(ClearStone);
            else if (i == 2) button.onClick.AddListener(ToggleMode);
            else if (i == 3) button.onClick.AddListener(TogglePower);
            else button.interactable = false;
        }
    }

    private Image MakeBox(string name, Transform parent, Vector2 topLeft, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), topLeft, size);
        var image = go.GetComponent<Image>();
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private Image MakeDisc(string name, Transform parent, Vector2 topLeft, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), topLeft, new Vector2(size, size));
        var image = go.GetComponent<Image>();
        image.sprite = MakePanSprite(96);
        return image;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Text MakeText(string name, Transform parent, string content, int fontSize, TextAnchor alignment,
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

    private Button MakeButton(string name, Transform parent, string label, int fontSize, Color color, Vector2 position, Vector2 size)
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
    private static Sprite MakeRoundedSprite(int size, int radius)
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

    // Brushed-steel weighing pan: a bright rim around a slightly darker dish.
    public static Sprite MakePanSprite(int size)
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
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float shade;
                if (r > 0.80f) shade = 0.62f + 0.30f * (1f - Mathf.Abs(r - 0.90f) / 0.10f) + 0.10f * (-dx - dy) * 0.5f; // rim
                else if (r > 0.74f) shade = 0.30f;                                                                         // groove
                else shade = 0.52f + 0.10f * (-dx - dy) * 0.5f;                                                            // dish
                shade = Mathf.Clamp01(shade);

                var edge = Mathf.Clamp01((1f - r) * center);
                texture.SetPixel(x, y, new Color(shade * 0.97f, shade, shade * 1.03f, edge));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
