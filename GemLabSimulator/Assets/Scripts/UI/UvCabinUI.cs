using UnityEngine;
using UnityEngine.UI;

// UV cabinet: drop a gem inside (it looks black), then switch on the long-wave (365 nm) or short-wave (254 nm)
// lamp. The stone glows in its luminescence color if its properties allow it.
public class UvCabinUI : MonoBehaviour
{
    public static UvCabinUI Instance { get; private set; }

    private const float PanelWidth = 600f;
    private const float PanelHeight = 372f;
    private const float FadeInSpeed = 3f;
    private const float FadeOutSpeed = 3.5f;
    private const float AfterglowSpeed = 0.3f;

    private enum Lamp { Off, Long, Short }

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.10f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonOff = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color LongTint = new Color(0.55f, 0.25f, 0.95f, 1f);
    private static readonly Color ShortTint = new Color(0.30f, 0.30f, 1f, 1f);

    private GameObject panel;
    private Image windowImage;
    private Image uvOverlay;
    private Image glowImage;
    private Image stoneBase;
    private Image stoneLit;
    private Text hintText;
    private Text lampText;
    private Text resultText;
    private Button longButton;
    private Button shortButton;

    private GemInstance stone;
    private Lamp lamp = Lamp.Off;
    private float lampLevel;       // 0..1, fades with the lamp
    private float glowLevel;       // 0..1 actual luminescence (can outlive the lamp for phosphorescent stones)
    private FluorescenceInfo currentInfo;
    private float stoneVariation = 1f;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<UvCabinUI>();
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
        lamp = Lamp.Off;
        lampLevel = 0f;
        glowLevel = 0f;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        lamp = Lamp.Off;
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        // Each specimen glows a little more or less than the species average.
        stoneVariation = 0.9f + (StableHash(gem.SpecimenId ?? "") & 0xFF) / 255f * 0.2f;
        lamp = Lamp.Off;
        lampLevel = 0f;
        glowLevel = 0f;
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        lamp = Lamp.Off;
        Refresh();
    }

    private void ToggleLamp(Lamp requested)
    {
        if (stone == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Cabine UV : déposez d'abord une gemme dans la cabine.");
            return;
        }

        lamp = lamp == requested ? Lamp.Off : requested;
        if (lamp != Lamp.Off) ObserveStone();
        Refresh();
    }

    private void ObserveStone()
    {
        var def = stone.Definition;
        var longWave = lamp == Lamp.Long;
        currentInfo = TreatmentSignatures.Fluorescence(stone, longWave);

        var key = longWave
            ? ExaminationTool.MeasurementType.FluorescenceLongWave.ToString()
            : ExaminationTool.MeasurementType.FluorescenceShortWave.ToString();
        stone.RecordObservation(key);

        var label = longWave ? "UV longs (365 nm)" : "UV courts (254 nm)";
        var text = longWave ? def.fluorescenceLongWave : def.fluorescenceShortWave;
        ToolFeedbackUI.Instance?.ShowMessage($"Cabine UV — {label} : {text}");
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        var dt = Time.deltaTime;
        lampLevel = Mathf.MoveTowards(lampLevel, lamp == Lamp.Off ? 0f : 1f, (lamp == Lamp.Off ? FadeOutSpeed : FadeInSpeed) * dt);

        // Luminescence follows the lamp, except phosphorescent stones keep glowing and fade slowly.
        var target = stone != null && lamp != Lamp.Off && currentInfo.Glows ? Mathf.Clamp01(currentInfo.Intensity * stoneVariation) : 0f;
        var speed = target > glowLevel ? FadeInSpeed : (currentInfo.Phosphorescent ? AfterglowSpeed : FadeOutSpeed);
        glowLevel = Mathf.MoveTowards(glowLevel, target, speed * dt);

        ApplyVisuals();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            lamp = Lamp.Off;
        }

        if (stone == null)
        {
            stoneBase.enabled = false;
            stoneLit.enabled = false;
            hintText.text = "Déposez une gemme dans la cabine";
            currentInfo = default;
        }
        else
        {
            var shape = stone.IsRough ? "Rond" : stone.ShapeName;
            var style = stone.IsRough ? "" : stone.CutStyleName;
            stoneBase.enabled = true;
            stoneBase.sprite = SolidOf(stone, shape, style, new Color(0.02f, 0.02f, 0.03f), new Color(0.07f, 0.07f, 0.09f));
            hintText.text = "";

            if (lamp != Lamp.Off) currentInfo = TreatmentSignatures.Fluorescence(stone, lamp == Lamp.Long);
            if (currentInfo.Glows)
            {
                stoneLit.sprite = SolidOf(stone, shape, style, currentInfo.Color, Color.Lerp(currentInfo.Color, Color.white, 0.45f));
                glowImage.color = new Color(currentInfo.Color.r, currentInfo.Color.g, currentInfo.Color.b, 0f);
            }
        }

        longButton.GetComponent<Image>().color = lamp == Lamp.Long ? LongTint : ButtonOff;
        shortButton.GetComponent<Image>().color = lamp == Lamp.Short ? ShortTint : ButtonOff;
        lampText.text = lamp == Lamp.Long ? "UV longs — 365 nm"
            : lamp == Lamp.Short ? "UV courts — 254 nm"
            : "Éteinte";

        resultText.text = BuildResultText();
        ApplyVisuals();
    }

    private string BuildResultText()
    {
        if (stone == null) return "Aucune gemme dans la cabine.";

        var def = stone.Definition;
        var text = "";
        if (stone.HasObserved(ExaminationTool.MeasurementType.FluorescenceLongWave.ToString()))
            text += $"UV longs : {def.fluorescenceLongWave}\n\n";
        if (stone.HasObserved(ExaminationTool.MeasurementType.FluorescenceShortWave.ToString()))
            text += $"UV courts : {def.fluorescenceShortWave}";

        return text.Length == 0 ? "La gemme est noire sans lampe. Allumez un UV pour l'observer." : text.TrimEnd('\n');
    }

    private void ApplyVisuals()
    {
        var uv = lamp == Lamp.Long ? LongTint : ShortTint;
        uvOverlay.color = new Color(uv.r, uv.g, uv.b, 0.30f * lampLevel);

        var glowing = stone != null && currentInfo.Glows;
        stoneLit.enabled = glowing;
        glowImage.enabled = glowing;
        if (!glowing) return;

        stoneLit.color = new Color(1f, 1f, 1f, glowLevel);
        glowImage.color = new Color(currentInfo.Color.r, currentInfo.Color.g, currentInfo.Color.b, glowLevel * 0.55f);
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

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("UvCabinPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Cabine UV", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildCabin();
        BuildSidePanel();
    }

    private void BuildCabin()
    {
        // Dark housing, black viewing chamber, metal tray.
        RuntimeUIKit.MakeBox("Housing", panel.transform, new Vector2(20f, -44f), new Vector2(340f, 310f), new Color(0.17f, 0.17f, 0.20f, 1f));
        windowImage = RuntimeUIKit.MakeBox("Window", panel.transform, new Vector2(36f, -60f), new Vector2(308f, 220f), new Color(0.01f, 0.01f, 0.015f, 1f));
        windowImage.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        windowImage.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        uvOverlay = RuntimeUIKit.MakeBox("UvOverlay", panel.transform, new Vector2(36f, -60f), new Vector2(308f, 220f), new Color(0.5f, 0.2f, 0.9f, 0f));
        uvOverlay.raycastTarget = false;

        var tray = RuntimeUIKit.MakeBox("Tray", panel.transform, new Vector2(120f, -216f), new Vector2(140f, 10f), new Color(0.30f, 0.31f, 0.35f, 1f));
        tray.raycastTarget = false;

        var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(glow.GetComponent<RectTransform>(), new Vector2(100f, -76f), new Vector2(180f, 180f));
        glowImage = glow.GetComponent<Image>();
        glowImage.sprite = RuntimeUIKit.Glow;
        glowImage.raycastTarget = false;
        glowImage.enabled = false;

        stoneBase = MakeStoneImage("StoneBase", new Vector2(154f, -138f));
        stoneLit = MakeStoneImage("StoneLit", new Vector2(154f, -138f));
        stoneLit.color = new Color(1f, 1f, 1f, 0f);
        stoneLit.enabled = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.45f, 0.45f, 0.55f),
            new Vector2(36f, -252f), new Vector2(308f, 18f));

        longButton = RuntimeUIKit.MakeButton("UvLong", panel.transform, "UV longs — 365 nm", 14, ButtonOff,
            new Vector2(36f, -296f), new Vector2(150f, 44f));
        longButton.onClick.AddListener(() => ToggleLamp(Lamp.Long));
        shortButton = RuntimeUIKit.MakeButton("UvShort", panel.transform, "UV courts — 254 nm", 14, ButtonOff,
            new Vector2(194f, -296f), new Vector2(150f, 44f));
        shortButton.onClick.AddListener(() => ToggleLamp(Lamp.Short));
    }

    private Image MakeStoneImage(string name, Vector2 topLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), topLeft, new Vector2(72f, 72f));
        var image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private void BuildSidePanel()
    {
        var muted = new Color(0.60f, 0.62f, 0.68f);
        RuntimeUIKit.MakeText("LampHeader", panel.transform, "Lampe", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(380f, -44f), new Vector2(200f, 20f));
        lampText = RuntimeUIKit.MakeText("Lamp", panel.transform, "Éteinte", 15, TextAnchor.UpperLeft, Color.white,
            new Vector2(380f, -66f), new Vector2(200f, 22f));

        RuntimeUIKit.MakeText("ResultHeader", panel.transform, "Observations", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(380f, -100f), new Vector2(200f, 20f));
        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(376f, -124f), new Vector2(208f, 130f), new Color(0.04f, 0.05f, 0.07f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(384f, -130f), new Vector2(192f, 118f));

        RuntimeUIKit.MakeText("Tip", panel.transform, "Un clic sur le bouton d'une lampe l'allume, un second clic l'éteint.", 11,
            TextAnchor.UpperLeft, muted, new Vector2(380f, -262f), new Vector2(200f, 32f));

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonOff,
            new Vector2(380f, -300f), new Vector2(200f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonOff,
            new Vector2(380f, -334f), new Vector2(200f, 28f));
        close.onClick.AddListener(Close);
    }

    // The flat sprite of a stone for the effects of the lamps: a rough crystal shows its habit, the other stones their shape and cut style.
    private static Sprite SolidOf(GemInstance stone, string shape, string style, Color body, Color facet)
    {
        var habit = stone != null && stone.IsRough ? stone.Habit : null;
        var crystal = habit != null ? GemIconFactory.GetHabitSolid(habit.Id, body, facet) : null;
        return crystal != null ? crystal : GemIconFactory.GetSolid(shape, style, body, facet);
    }
}
