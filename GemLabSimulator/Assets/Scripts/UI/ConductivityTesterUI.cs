using UnityEngine;
using UnityEngine.UI;

// The two diamond testers of the laboratory: the thermal tester (black, a bar of lights that fills up when the stone conducts heat like a
// diamond, with a beep) and the electrical tester (white, which lights up and beeps for a conductive stone, the moissanite). Drop a stone
// in the box at the centre, grab the tester with the mouse and put its tip on the stone. Built at runtime.
public class ConductivityTesterUI : MonoBehaviour
{
    public static ConductivityTesterUI Instance { get; private set; }

    public enum Mode { Thermal, Electrical }

    private enum DeviceState { Rest, Held, Placed }

    private const float PanelWidth = 660f;
    private const float PanelHeight = 560f;
    private const float DeviceScale = 1.15f;
    private const float StoneRadius = 64f;

    private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.11f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color LedOff = new Color(0.17f, 0.18f, 0.21f, 1f);

    // The bar of the Diamond Selector: four green lights, four yellow ones, four red ones, from the probe upwards.
    private const int ThermalSegmentCount = 12;
    private const float ThermalStep = 0.2f;
    private const int LcdSegmentCount = 8;
    private const float ElectricStep = 0.35f;

    private static Sprite circle;

    private GameObject panel;
    private RectTransform panelRect;
    private Text titleText;
    private Text statusText;
    private Image slotImage;
    private Text slotHint;
    private RectTransform thermalDevice;
    private RectTransform electricDevice;
    private Image[] thermalSegments;
    private Image[] thermalLamps;
    private Image[] lcdSegments;
    private Image[] electricLamps;
    private Image stand;
    private AudioSource audioSource;
    private AudioClip tickClip;
    private AudioClip beepClip;
    private AudioClip lowClip;

    private Mode mode;
    private GemInstance stone;
    private DeviceState state = DeviceState.Rest;
    private Vector2 tip;
    private Vector2 grabOffset;
    private float progress;
    private int litCount;
    private bool completed;
    private float alarmTimer;
    private float lastDeviceTouch = -10f;

    private static readonly Vector2 SlotCenter = new Vector2(PanelWidth * 0.5f - 20f, 270f);

    // The vertical tester rests on the right, the horizontal one at the bottom.
    private Vector2 RestTip => mode == Mode.Thermal ? new Vector2(PanelWidth - 60f, PanelHeight - 26f) : new Vector2(300f, PanelHeight - 66f);

    public bool IsOpen => panel != null && panel.activeSelf;

    public static bool TryOpen(Mode wanted)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<ConductivityTesterUI>();
        }
        if (Instance == null) return false;
        Instance.Open(wanted);
        return true;
    }

    private void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.45f;
        tickClip = MakeTone("Tick", 1250f, 0.05f);
        beepClip = MakeTone("Beep", 2600f, 0.22f);
        lowClip = MakeTone("Low", 330f, 0.28f);
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Open(Mode wanted)
    {
        mode = wanted;
        stone = null;
        ResetMeasurement();
        state = DeviceState.Rest;
        tip = RestTip;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();

        titleText.text = mode == Mode.Thermal ? "Testeur thermique (Diamond Selector)" : "Testeur de conductivité électrique (moissanite)";
        thermalDevice.gameObject.SetActive(mode == Mode.Thermal);
        electricDevice.gameObject.SetActive(mode == Mode.Electrical);
        var standPosition = RestTip;
        RuntimeUIKit.SetTopLeft(stand.rectTransform, new Vector2(standPosition.x - 32f, -(standPosition.y + 2f)), new Vector2(64f, 10f));
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (IsOpen) Refresh();
    }

    // ---- Stone ---------------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        ResetMeasurement();
        Refresh();
    }

    private void ClearStone()
    {
        // Letting go of the tester above the slot must not take the stone back.
        if (stone == null || state != DeviceState.Rest || Time.unscaledTime - lastDeviceTouch < 0.3f) return;
        stone = null;
        ResetMeasurement();
        Refresh();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            ResetMeasurement();
        }

        slotImage.enabled = stone != null;
        slotImage.sprite = stone != null ? stone.DisplaySprite : null;
        slotHint.text = stone == null ? "Déposez une pierre\nici" : "";
        if (!completed) statusText.text = IdleText();
    }

    private string IdleText()
    {
        if (stone == null) return "Glissez une pierre de votre sac dans l'encart central.";
        return "Saisissez le testeur avec la souris et posez sa pointe sur la pierre. Attendez le résultat. (Clic sur l'encart pour reprendre la pierre.)";
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
            if (ReferenceEquals(g, gem)) return true;
        return false;
    }

    private void ResetMeasurement()
    {
        progress = 0f;
        litCount = 0;
        completed = false;
        alarmTimer = 0f;
        if (statusText != null) statusText.text = IdleText();
        UpdateLights(false, false);
    }

    // ---- The device in the hand ------------------------------------------------------------------------------------

    private RectTransform ActiveDevice => mode == Mode.Thermal ? thermalDevice : electricDevice;

    // Mouse position in the panel, in pixels from its top-left corner (y downward).
    private Vector2 MouseInPanel()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRect, Input.mousePosition, null, out var local);
        return new Vector2(local.x + PanelWidth * 0.5f, PanelHeight * 0.5f - local.y);
    }

    private bool TipOnStone()
    {
        return stone != null && Vector2.Distance(tip, SlotCenter) <= StoneRadius;
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        HandleDevice();
        UpdateMeasurement();
    }

    private void HandleDevice()
    {
        var device = ActiveDevice;
        if (state != DeviceState.Held && Input.GetMouseButtonDown(0)
            && RectTransformUtility.RectangleContainsScreenPoint(device, Input.mousePosition, null))
        {
            state = DeviceState.Held;
            lastDeviceTouch = Time.unscaledTime;
            grabOffset = tip - MouseInPanel();
        }

        if (state == DeviceState.Held)
        {
            var wanted = MouseInPanel() + grabOffset;
            tip = new Vector2(Mathf.Clamp(wanted.x, 20f, PanelWidth - 20f), Mathf.Clamp(wanted.y, 30f, PanelHeight - 10f));
            if (Input.GetMouseButtonUp(0))
            {
                lastDeviceTouch = Time.unscaledTime;
                state = TipOnStone() ? DeviceState.Placed : DeviceState.Rest;
                if (state == DeviceState.Rest) tip = RestTip;
            }
        }

        device.anchoredPosition = new Vector2(tip.x, -tip.y);
        // The tester leans a little when it is in the hand or on the stone.
        device.localEulerAngles = new Vector3(0f, 0f, state == DeviceState.Rest ? 0f : (mode == Mode.Thermal ? -12f : 14f));
    }

    // ---- Measurement -----------------------------------------------------------------------------------------------

    private void UpdateMeasurement()
    {
        var contact = stone != null && state != DeviceState.Rest && TipOnStone();
        if (contact)
        {
            if (progress <= 0f && !completed) statusText.text = "Mesure en cours...";
            progress += Time.unscaledDeltaTime;
        }
        else
        {
            progress = Mathf.Max(0f, progress - Time.unscaledDeltaTime * 3f);
            if (progress <= 0f) completed = false;
        }

        if (mode == Mode.Thermal) UpdateThermal(contact);
        else UpdateElectrical(contact);
    }

    private bool Conductive => mode == Mode.Thermal
        ? stone != null && stone.Definition.isThermallyConductiveLikeDiamond
        : stone != null && stone.Definition.isElectricallyConductive;

    // The bar fills up one light at a time. A stone that conducts heat like a diamond lights all twelve, up into the red, and the tester
    // beeps; any other stone stops lower down, in the green and yellow.
    private void UpdateThermal(bool contact)
    {
        var level = ThermalSegmentCount;
        if (stone != null && !Conductive)
            level = 2 + (int)(StoneOptics.StableHash(stone.Definition.name) % 5);

        var wanted = Mathf.Min(level, Mathf.FloorToInt(progress / ThermalStep));
        if (contact && wanted > litCount) Play(tickClip);
        litCount = wanted;

        var done = contact && progress >= level * ThermalStep + 0.5f;
        if (done && !completed) Complete();
        var alarm = completed && contact && level == ThermalSegmentCount;
        UpdateLights(alarm, contact);
        RunAlarm(alarm, 0.4f);
    }

    // The display slides towards MOISSANITE (a conductive stone, with flashing and beeps) or towards DIA (an insulator, quietly).
    private void UpdateElectrical(bool contact)
    {
        var wanted = Mathf.Min(4, Mathf.FloorToInt(progress / ElectricStep));
        if (contact && wanted > litCount) Play(tickClip);
        litCount = wanted;

        var done = contact && progress >= 4 * ElectricStep + 0.4f;
        if (done && !completed) Complete();
        var alarm = completed && contact && Conductive;
        UpdateLights(alarm, contact);
        RunAlarm(alarm, 0.32f);
    }

    private void Complete()
    {
        completed = true;
        var type = mode == Mode.Thermal ? ExaminationTool.MeasurementType.ThermalConductivity : ExaminationTool.MeasurementType.ElectricalConductivity;
        stone.RecordObservation(type.ToString());
        PlayerInventory.Instance?.NotifyChanged();

        if (mode == Mode.Thermal)
        {
            statusText.text = Conductive
                ? "Bip ! Conduction thermique élevée : diamant ou moissanite. Un test électrique permet de les départager."
                : "Conduction thermique faible : ce n'est pas un diamant.";
        }
        else
        {
            statusText.text = Conductive
                ? "Bip ! La pierre conduit l'électricité : moissanite (carbure de silicium)."
                : "Pas de signal : la pierre ne conduit pas l'électricité, cohérent avec un diamant (ou une autre gemme).";
        }
        if (!Conductive) Play(lowClip);
        ToolFeedbackUI.Instance?.ShowMessage(statusText.text);
    }

    private void RunAlarm(bool alarm, float interval)
    {
        if (!alarm)
        {
            alarmTimer = 0f;
            return;
        }
        alarmTimer -= Time.unscaledDeltaTime;
        if (alarmTimer > 0f) return;
        alarmTimer = interval;
        Play(beepClip);
    }

    private void UpdateLights(bool alarm, bool contact)
    {
        if (thermalSegments == null) return;
        var powered = state != DeviceState.Rest;

        // Thermal tester: the bar, and the three lamps (the first one shows a diamond).
        var pulse = alarm && Mathf.Repeat(Time.unscaledTime * 5f, 1f) < 0.5f;
        for (var i = 0; i < thermalSegments.Length; i++)
        {
            var color = i < litCount ? SegmentColor(i) : LedOff;
            thermalSegments[i].color = pulse ? Color.Lerp(color, Color.white, 0.45f) : color;
        }
        thermalLamps[0].color = alarm ? new Color(1f, 0.9f, 0.5f) : LedOff;
        thermalLamps[1].color = powered ? new Color(1f, 0.15f, 0.12f) : new Color(0.28f, 0.06f, 0.06f);
        thermalLamps[2].color = powered ? new Color(1f, 0.15f, 0.12f) : new Color(0.28f, 0.06f, 0.06f);

        // Electrical tester: the liquid crystal display and its three lamps.
        var flash = alarm && Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.5f;
        var lcdLight = new Color(0.67f, 0.70f, 0.66f, 1f);
        var lcdDark = new Color(0.10f, 0.12f, 0.10f, 1f);
        for (var i = 0; i < lcdSegments.Length; i++)
        {
            bool dark;
            if (alarm) dark = i < 4 && (i % 2 == 0) == flash;
            else if (Conductive) dark = i < 4 && 3 - i < litCount;
            else dark = i >= 4 && i - 4 < litCount;
            lcdSegments[i].color = dark && (contact || completed) ? lcdDark : lcdLight;
        }
        electricLamps[0].color = contact ? new Color(1f, 0.75f, 0.2f) : LedOff;
        electricLamps[1].color = new Color(0.30f, 0.07f, 0.07f);
        electricLamps[2].color = powered ? new Color(0.30f, 1f, 0.40f) : new Color(0.08f, 0.25f, 0.10f);
    }

    private static Color SegmentColor(int index)
    {
        if (index < 4) return new Color(0.20f, 0.78f, 0.22f);
        return index < 8 ? new Color(1f, 0.76f, 0.10f) : new Color(0.96f, 0.16f, 0.12f);
    }

    // ---- Sound -------------------------------------------------------------------------------------------------------

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private static AudioClip MakeTone(string name, float frequency, float seconds)
    {
        const int rate = 44100;
        var count = Mathf.RoundToInt(rate * seconds);
        var samples = new float[count];
        for (var i = 0; i < count; i++)
        {
            var t = i / (float)rate;
            var envelope = Mathf.Min(1f, i / 200f) * Mathf.Min(1f, (count - i) / 400f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.5f * envelope;
        }
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // ---- Construction ------------------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("ConductivityTesterPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        panelRect.anchoredPosition = new Vector2(-140f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        titleText = RuntimeUIKit.MakeText("Title", panel.transform, "", 20, TextAnchor.UpperLeft, Gold, new Vector2(24f, -14f), new Vector2(PanelWidth - 120f, 28f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The box for the stone: a dark metal plate. A click on it takes the stone back.
        var plate = RuntimeUIKit.MakeBox("Plate", panel.transform, new Vector2(SlotCenter.x - 110f, -(SlotCenter.y - 110f)), new Vector2(220f, 220f), new Color(0.15f, 0.16f, 0.19f, 1f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(SlotCenter.x - 100f, -(SlotCenter.y - 100f)), new Vector2(200f, 200f), new Color(0.05f, 0.05f, 0.07f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        plate.raycastTarget = false;

        var stoneGo = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), new Vector2(SlotCenter.x - 64f, -(SlotCenter.y - 64f)), new Vector2(128f, 128f));
        slotImage = stoneGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotHint = RuntimeUIKit.MakeText("Hint", panel.transform, "", 14, TextAnchor.MiddleCenter, new Color(0.5f, 0.54f, 0.6f),
            new Vector2(SlotCenter.x - 100f, -(SlotCenter.y - 100f)), new Vector2(200f, 200f));

        // The stand where the tester rests.
        stand = RuntimeUIKit.MakeBox("Stand", panel.transform, Vector2.zero, new Vector2(64f, 10f), new Color(0.2f, 0.21f, 0.25f, 1f));
        stand.raycastTarget = false;

        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "", 14, TextAnchor.UpperLeft, new Color(0.88f, 0.93f, 1f),
            new Vector2(24f, -50f), new Vector2(PanelWidth - 48f, 60f));
        statusText.horizontalOverflow = HorizontalWrapMode.Wrap;

        thermalDevice = BuildThermalDevice();
        electricDevice = BuildElectricDevice();
    }

    private RectTransform MakeDevice(string name, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one * DeviceScale;
        return rect;
    }

    private static Image Box(Transform parent, float x, float y, float w, float h, Color color)
    {
        var image = RuntimeUIKit.MakeBox("Part", parent, new Vector2(x, y), new Vector2(w, h), color, false);
        image.raycastTarget = false;
        return image;
    }

    private static Image Disc(Transform parent, float centerX, float centerY, float diameter, Color color)
    {
        var go = new GameObject("Lamp", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), new Vector2(centerX - diameter / 2f, centerY + diameter / 2f), new Vector2(diameter, diameter));
        var image = go.GetComponent<Image>();
        image.sprite = CircleSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(Transform parent, string content, float x, float y, float w, float h, int size, Color color,
        TextAnchor anchor = TextAnchor.MiddleLeft, float rotation = 0f, bool bold = false)
    {
        var text = RuntimeUIKit.MakeText("Label", parent, content, size, anchor, color, new Vector2(x, y), new Vector2(w, h));
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) text.fontStyle = FontStyle.BoldAndItalic;
        // Rotated about its centre.
        var rect = text.rectTransform;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x + w / 2f, y - h / 2f);
        rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        return text;
    }

    private static Sprite CircleSprite()
    {
        if (circle != null) return circle;
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(size / 2f - d) * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }

    // The Diamond Selector II: a black body, the volume slider, three lamps, the bar of twelve lights with its scale from 1 to 8 and the
    // black nozzle with its steel probe. The tip is at the bottom centre.
    private RectTransform BuildThermalDevice()
    {
        var rect = MakeDevice("ThermalTester", new Vector2(72f, 286f), new Vector2(0.5f, 0f));
        var t = rect.transform;
        var white = new Color(0.92f, 0.93f, 0.95f);

        Box(t, 0f, 0f, 72f, 225f, new Color(0.03f, 0.03f, 0.035f, 1f));
        Box(t, 2f, -2f, 68f, 221f, new Color(0.11f, 0.11f, 0.12f, 1f));
        Box(t, 64f, 0f, 8f, 52f, new Color(0.20f, 0.20f, 0.22f, 1f));

        // Volume slider.
        Label(t, "ON", 8f, -8f, 18f, 9f, 6, white);
        Box(t, 8f, -19f, 20f, 46f, new Color(0.02f, 0.02f, 0.025f, 1f));
        Box(t, 12f, -25f, 12f, 26f, new Color(0.30f, 0.30f, 0.33f, 1f));
        for (var i = 0; i < 5; i++) Box(t, 12f, -27f - i * 5f, 12f, 1.5f, new Color(0.10f, 0.10f, 0.11f, 1f));
        Label(t, "OFF", 8f, -69f, 18f, 9f, 6, white);
        Label(t, "< VOLUME >", 18f, -40f, 56f, 9f, 6, white, TextAnchor.MiddleCenter, -90f, true);

        // The three lamps: the first one shows a diamond, the two red ones are the battery and the ready lamps.
        thermalLamps = new Image[3];
        for (var i = 0; i < 3; i++)
        {
            Disc(t, 16f + i * 20f, -100f, 16f, new Color(0.02f, 0.02f, 0.025f, 1f));
            thermalLamps[i] = Disc(t, 16f + i * 20f, -100f, 9f, LedOff);
        }

        // The bar of twelve lights, in a recess, and its scale.
        Box(t, 6f, -118f, 26f, 100f, new Color(0.02f, 0.02f, 0.025f, 1f));
        thermalSegments = new Image[ThermalSegmentCount];
        for (var i = 0; i < ThermalSegmentCount; i++)
            thermalSegments[i] = Box(t, 9f, -122f - (ThermalSegmentCount - 1 - i) * 8f, 20f, 6f, LedOff);
        Box(t, 36f, -124f, 1f, 90f, white);
        for (var n = 1; n <= 8; n++)
        {
            var y = -208f + (n - 1) * (78f / 7f);
            Box(t, 36f, y, 4f, 1f, white);
            Label(t, n.ToString(), 41f, y + 5f, 8f, 9f, 6, white);
        }
        Label(t, "DIA >", 36f, -118f, 20f, 8f, 5, white);
        Label(t, "DIAMOND SELECTOR II", 12f, -166f, 140f, 12f, 8, white, TextAnchor.MiddleCenter, 90f, true).rectTransform.anchoredPosition = new Vector2(60f, -170f);

        // Nozzle and probe.
        var black = new Color(0.02f, 0.02f, 0.025f, 1f);
        Box(t, 20f, -225f, 32f, 8f, black);
        Box(t, 23f, -233f, 26f, 8f, new Color(0.05f, 0.05f, 0.06f, 1f));
        Box(t, 26f, -241f, 20f, 8f, black);
        Box(t, 29f, -249f, 14f, 8f, new Color(0.05f, 0.05f, 0.06f, 1f));
        Box(t, 32f, -257f, 8f, 8f, black);
        Box(t, 35f, -265f, 2f, 21f, new Color(0.80f, 0.82f, 0.86f, 1f));
        return rect;
    }

    // The Moissan/Dia tester: a silver case with its liquid crystal display (MOISSANITE on the left, DIA on the right), the three lamps,
    // the off / on slider and the dark blue probe. Horizontal, with the tip at the left.
    private RectTransform BuildElectricDevice()
    {
        var rect = MakeDevice("ElectricTester", new Vector2(280f, 90f), new Vector2(0f, 0.5f));
        var t = rect.transform;
        var ink = new Color(0.10f, 0.11f, 0.13f, 1f);

        // Probe: a dark blue cone ending in a steel point.
        Box(t, 0f, -44f, 12f, 2f, new Color(0.80f, 0.82f, 0.86f, 1f));
        Box(t, 10f, -38f, 14f, 14f, new Color(0.05f, 0.07f, 0.20f, 1f));
        Box(t, 22f, -33f, 16f, 24f, new Color(0.07f, 0.09f, 0.26f, 1f));
        Box(t, 36f, -29f, 24f, 32f, new Color(0.08f, 0.10f, 0.30f, 1f));
        Box(t, 24f, -36f, 30f, 3f, new Color(0.55f, 0.62f, 0.95f, 0.45f));

        // Silver case.
        Box(t, 58f, -3f, 222f, 84f, new Color(0.58f, 0.59f, 0.61f, 1f));
        Box(t, 60f, -5f, 218f, 80f, new Color(0.86f, 0.87f, 0.88f, 1f));
        Box(t, 60f, -5f, 218f, 3f, new Color(1f, 1f, 1f, 0.6f));

        // Display.
        Box(t, 72f, -12f, 130f, 18f, new Color(0.30f, 0.31f, 0.33f, 1f));
        Box(t, 74f, -14f, 126f, 14f, new Color(0.67f, 0.70f, 0.66f, 1f));
        lcdSegments = new Image[LcdSegmentCount];
        for (var i = 0; i < LcdSegmentCount; i++)
            lcdSegments[i] = Box(t, 76f + i * 15.2f, -16f, 12.5f, 10f, new Color(0.67f, 0.70f, 0.66f, 1f));
        Label(t, "MOISSANITE", 76f, -32f, 60f, 9f, 6, ink);
        Label(t, "DIA", 174f, -32f, 28f, 9f, 6, ink);

        // Slider.
        Label(t, "OFF", 206f, -12f, 14f, 9f, 6, ink);
        Box(t, 220f, -10f, 38f, 20f, new Color(0.75f, 0.76f, 0.78f, 1f));
        Box(t, 222f, -12f, 20f, 16f, new Color(0.16f, 0.17f, 0.19f, 1f));
        for (var i = 0; i < 5; i++) Box(t, 224f + i * 3.5f, -13f, 1.2f, 14f, new Color(0.35f, 0.36f, 0.38f, 1f));
        Label(t, "ON", 260f, -12f, 14f, 9f, 6, ink);

        // Lamps: test, low battery, ready.
        electricLamps = new Image[3];
        var lampY = new[] { -46f, -60f, -74f };
        for (var i = 0; i < 3; i++)
        {
            Disc(t, 150f, lampY[i], 13f, new Color(0.22f, 0.22f, 0.24f, 1f));
            electricLamps[i] = Disc(t, 150f, lampY[i], 8f, LedOff);
        }
        Label(t, "LOW BATTERY", 164f, lampY[1] - 5f, 70f, 9f, 6, ink);
        Label(t, "READY", 164f, lampY[2] - 5f, 70f, 9f, 6, ink);
        Label(t, "MOISSAN/DIA TESTER", 66f, -56f, 80f, 14f, 7, ink, TextAnchor.MiddleLeft, 0f, true);
        return rect;
    }
}
