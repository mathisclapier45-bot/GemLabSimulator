using UnityEngine;
using UnityEngine.UI;

// The high-pressure high-temperature press of the treatments laboratory: a diamond is squeezed and heated at the same time. The player drops a
// diamond in the slot, reads its infrared spectrum (is there nitrogen in it?) and sets the temperature, the pressure and the length of the cycle.
// The window shows the cell of the press: four anvils that close in on the diamond as the pressure rises, and the glow of the temperature. One
// diamond at a time. Built at runtime.
public class HphtUI : MonoBehaviour
{
    public static HphtUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float PressX = 206f;
    private const float PressY = -92f;
    private const float PressW = 300f;
    private const float PressH = 300f;
    private const float ColumnX = 530f;
    private const float SliderWidth = 262f;

    private static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.11f, 0.98f);
    private static readonly Color Steel = new Color(0.62f, 0.76f, 0.92f);
    private static readonly Color Ember = new Color(1.00f, 0.62f, 0.30f);
    private static readonly Color Gold = new Color(0.98f, 0.86f, 0.50f);
    private static readonly Color TextSoft = new Color(0.88f, 0.92f, 0.98f);
    private static readonly Color ButtonIdle = new Color(0.13f, 0.17f, 0.24f, 1f);
    private static readonly Color ButtonOn = new Color(0.50f, 0.30f, 0.12f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text readoutText;
    private Text temperatureText;
    private Text pressureText;
    private Image barFill;
    private Image cellGlow;
    private Image stoneInCell;
    private Image[] anvils = new Image[4];
    private Slider temperatureSlider;
    private Slider pressureSlider;
    private Button runButton;
    private Button collectButton;
    private readonly Image[] durationImages = new Image[HphtCatalog.DurationDays.Length];

    private GemInstance stone;
    private int temperature = 2200;
    private int pressure = 55;
    private int durationIndex = 1;
    private HphtQuote quote;
    private static Sprite stripeSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        HphtState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<HphtUI>();
        }
        Instance?.Open();
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
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshIfOpen;
        var state = HphtState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (HphtState.Instance != null) HphtState.Instance.OnJobChanged -= RefreshIfOpen;
    }

    private void RefreshIfOpen(int _)
    {
        RefreshIfOpen();
    }

    private void RefreshIfOpen()
    {
        if (IsOpen) Refresh();
    }

    public void Open()
    {
        stone = null;
        temperature = 2200;
        pressure = 55;
        durationIndex = 1;
        temperatureSlider.SetValueWithoutNotify(temperature / (float)HphtCatalog.TemperatureStep);
        pressureSlider.SetValueWithoutNotify(pressure / (float)HphtCatalog.PressureStep);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez un diamant : la presse en lit l'azote à l'infrarouge. Chauffer sans assez de pression le transforme en graphite. »";
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }
        AnimatePress();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = HphtCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        technicianText.text = HphtCatalog.HasNitrogen(gem)
            ? "« Il y a de l'azote dans ce diamant : sous la presse, il jaunira. Si c'est ce que vous voulez... »"
            : "« Très peu d'azote : un diamant de ce type peut devenir incolore sous la presse. À vous de trouver la bonne température et la bonne pression. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        Refresh();
    }

    private void OnTemperature(float value)
    {
        temperature = Mathf.Clamp(Mathf.RoundToInt(value) * HphtCatalog.TemperatureStep, HphtCatalog.MinTemperature, HphtCatalog.MaxTemperature);
        Refresh();
    }

    private void OnPressure(float value)
    {
        pressure = Mathf.Clamp(Mathf.RoundToInt(value) * HphtCatalog.PressureStep, HphtCatalog.MinPressure, HphtCatalog.MaxPressure);
        Refresh();
    }

    private void SetDuration(int index)
    {
        durationIndex = index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
            if (ReferenceEquals(g, gem)) return true;
        return false;
    }

    private int Days => HphtCatalog.DurationDays[durationIndex];

    private void Refresh()
    {
        // The stone can leave the bag behind our back: forget it then.
        if (stone != null && !ContainsGem(stone)) stone = null;

        RefreshSlot();
        RefreshControls();
        RefreshJob();
        RefreshQuote();
        RefreshPress();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nun diamant\nici";
            stoneInfoText.text = "La presse ne traite que les\ndiamants taillés. Elle lit\nleur azote à l'infrarouge.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        var certified = stone.Certificate != null ? "\n<color=#f5c06a>Son certificat sera annulé.</color>" : "\nPas encore certifié.";
        stoneInfoText.text =
            $"{stone.CaratWeight:F2} ct — couleur {stone.ColorName.ToLowerInvariant()}\n{stone.FinalValue} €\n\n<color=#9fd0ff>Infrarouge :</color> {HphtCatalog.TypeLabel(stone)}{certified}";
    }

    private void RefreshControls()
    {
        for (var i = 0; i < durationImages.Length; i++) durationImages[i].color = i == durationIndex ? ButtonOn : ButtonIdle;
        temperatureText.text = $"Température : {temperature} °C";
        temperatureText.color = HeatColor(temperature);
        pressureText.text = $"Pression : {pressure / 10f:F1} GPa";
        temperatureSlider.SetValueWithoutNotify(temperature / (float)HphtCatalog.TemperatureStep);
        pressureSlider.SetValueWithoutNotify(pressure / (float)HphtCatalog.PressureStep);
    }

    // The colour of hot metal for a temperature: dark, red, orange, yellow, white.
    private static Color HeatColor(int celsius)
    {
        var t = Mathf.Clamp01((celsius - 1200f) / 1500f);
        if (t < 0.3f) return Color.Lerp(new Color(0.85f, 0.15f, 0.10f), new Color(1.00f, 0.55f, 0.12f), t / 0.3f);
        if (t < 0.7f) return Color.Lerp(new Color(1.00f, 0.55f, 0.12f), new Color(1.00f, 0.88f, 0.40f), (t - 0.3f) / 0.4f);
        return Color.Lerp(new Color(1.00f, 0.88f, 0.40f), new Color(1.00f, 0.98f, 0.90f), (t - 0.7f) / 0.3f);
    }

    // The cell glows at the colour of the temperature and the four anvils close in on it as the pressure rises.
    private void RefreshPress()
    {
        var job = HphtState.Instance != null ? HphtState.Instance.Job : null;
        var shown = job != null ? job.Stone : stone;
        stoneInCell.gameObject.SetActive(shown != null);
        if (shown != null) stoneInCell.sprite = shown.DisplaySprite;

        var shownTemperature = job != null ? job.Temperature : temperature;
        var color = HeatColor(shownTemperature);
        cellGlow.color = new Color(color.r, color.g, color.b, 0.8f);
        PlaceAnvils(job != null ? job.Pressure : pressure);
    }

    // Each anvil is a block that slides towards the centre of the press: the more pressure, the closer.
    private void PlaceAnvils(int pressureTenths)
    {
        var closing = (pressureTenths - HphtCatalog.MinPressure) / (float)(HphtCatalog.MaxPressure - HphtCatalog.MinPressure);
        var offset = 62f - closing * 26f;
        var centre = new Vector2(PressX + PressW / 2f, PressY - 124f);
        var directions = new[] { new Vector2(0f, 1f), new Vector2(0f, -1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
        for (var i = 0; i < anvils.Length; i++)
        {
            var rect = anvils[i].rectTransform;
            rect.sizeDelta = i < 2 ? new Vector2(70f, 52f) : new Vector2(52f, 70f);
            rect.anchoredPosition = centre + new Vector2(directions[i].x * offset, -directions[i].y * offset);
        }
    }

    private void RefreshJob()
    {
        var state = HphtState.Instance;
        var job = state != null ? state.Job : null;
        collectButton.gameObject.SetActive(job != null);
        barFill.transform.parent.gameObject.SetActive(job != null);

        if (job == null)
        {
            jobText.text = "La presse est libre.";
            readoutText.text = "Presse au repos  ·  prête";
            return;
        }

        var ready = state.IsReady;
        var days = state.DaysRemaining;
        jobText.text = ready
            ? $"Diamant de {job.Stone.CaratWeight:F2} ct — {job.Temperature} °C, {job.Pressure / 10f:F1} GPa : cycle terminé, la presse est retombée."
            : $"Diamant de {job.Stone.CaratWeight:F2} ct — {job.Temperature} °C, {job.Pressure / 10f:F1} GPa : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        readoutText.text = ready ? "Cycle terminé" : $"Sous presse  ·  {job.Temperature} °C  ·  {job.Pressure / 10f:F1} GPa";
        var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
        var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
        var bar = (RectTransform)barFill.transform;
        bar.sizeDelta = new Vector2((PanelWidth - PressX - 24f - 8f) * (ready ? 1f : done), bar.sizeDelta.y);
        barFill.color = ready ? Gold : Ember;
        collectButton.interactable = ready;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void RefreshQuote()
    {
        var state = HphtState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "La presse est occupée : sortez d'abord le diamant en cours.";
            return;
        }
        if (stone == null)
        {
            quoteText.text = "Posez un diamant dans la case, puis réglez la presse.";
            return;
        }

        quote = HphtCatalog.GetQuote(temperature, pressure, Days, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var reactionColor = quote.Reaction < 0.2f ? "#ff9a70" : quote.Reaction < 0.45f ? "#f5c629" : "#a8e0a0";
        var riskColor = quote.Graphitization < 0.1f ? "#a8e0a0" : quote.Graphitization < 0.3f ? "#f5c629" : "#ff7060";
        quoteText.text =
            $"<b>{temperature} °C  ·  {pressure / 10f:F1} GPa  ·  {Days} jour{(Days > 1 ? "s" : "")}</b>\n" +
            $"Durée : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}  (sortie le {GameCalendar.Short(Today + quote.Days)})    Cycle de presse : {quote.Fee} €\n" +
            $"Réaction attendue : <color={reactionColor}>{quote.ReactionLabel}</color>    Graphitisation : <color={riskColor}>{quote.RiskLabel}</color>\n" +
            $"« {quote.Advice} »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        runButton.gameObject.SetActive(true);
        runButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour ce cycle.";
    }

    private void Run()
    {
        if (stone == null || !quote.IsValid) return;
        var gem = stone;
        var current = quote;
        var degrees = temperature;
        var squeeze = pressure;
        var length = Days;
        var certificate = gem.Certificate != null ? "\nSon certificat sera annulé : il faudra la faire certifier à nouveau." : "";
        ConfirmDialog.Show("Lancer la presse",
            $"Mettre ce diamant ({gem.CaratWeight:F2} ct) sous presse ?\n\n{degrees} °C, {squeeze / 10f:F1} GPa, {length} jour{(length > 1 ? "s" : "")}\nPrix : {current.Fee} €\nGraphitisation : {current.RiskLabel}{certificate}\n\nUn diamant ne passe qu'une fois sous presse.",
            "Lancer", () => StartRun(degrees, squeeze, length, gem, current));
    }

    private void StartRun(int degrees, int squeeze, int length, GemInstance gem, HphtQuote current)
    {
        var state = HphtState.Ensure();
        if (state == null) return;
        if (state.TryStart(degrees, squeeze, length, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.ComputerBoot);
            technicianText.text = $"« Les enclumes se referment et la chauffe monte. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
            stone = null;
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    private void Collect()
    {
        var state = HphtState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.IsRuined ? Sfx.Id.Error : Sfx.Id.Success);
            var certificate = result.Outcome == HphtOutcome.NoEffect ? "" : "\nÀ faire certifier à nouveau.";
            technicianText.text = result.Outcome == HphtOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »\nCouleur : {gem.ColorName.ToLowerInvariant()}{certificate}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.IsRuined ? "Diamant graphitisé par la presse." : "Diamant sorti de la presse, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The glow of the cell pulses (faster during a cycle), the anvils tremble a little under load, and the diamond sparkles.
    private void AnimatePress()
    {
        var state = HphtState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var degrees = state != null && state.Job != null ? state.Job.Temperature : temperature;

        var pulse = 0.78f + 0.22f * Mathf.Sin(Time.unscaledTime * (running ? 6f : 2f));
        var glow = cellGlow.color;
        glow.a = Mathf.Lerp(0.3f, 0.95f, Mathf.Clamp01((degrees - 1800f) / 800f)) * (running ? pulse : 0.85f);
        cellGlow.color = glow;

        var shake = running ? Mathf.Sin(Time.unscaledTime * 40f) * 0.8f : 0f;
        var centre = new Vector2(PressX + PressW / 2f, PressY - 124f);
        stoneInCell.rectTransform.anchoredPosition = centre + new Vector2(shake, 0f);
        var shimmer = 0.92f + 0.08f * Mathf.Sin(Time.unscaledTime * (running ? 9f : 3f));
        stoneInCell.color = new Color(shimmer, shimmer, shimmer, 1f);
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    // A strip from orange to white-hot, tiled along the top and the bottom of the window.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int w = 64;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (var x = 0; x < w; x++)
        {
            var color = HeatColor(1600 + Mathf.RoundToInt(1000f * x / (w - 1)));
            for (var y = 0; y < h; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("HphtPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-150f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);

        foreach (var y in new[] { 0f, -(PanelHeight - 10f) })
        {
            var stripe = RuntimeUIKit.MakeBox("Stripe", panel.transform, new Vector2(0f, y), new Vector2(PanelWidth, 10f), Color.white, false);
            stripe.sprite = StripeSprite();
            stripe.type = Image.Type.Tiled;
            stripe.raycastTarget = false;
        }

        Txt("Title", "Presse haute pression, haute température", 22, TextAnchor.UpperLeft, Steel, new Vector2(24f, -20f), new Vector2(640f, 30f));
        Txt("Subtitle", "Changer la couleur d'un diamant : l'azote décide, la pression le garde de la graphitisation.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the diamond.
        Txt("SlotTitle", "Diamant à traiter", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.03f, 0.04f, 0.07f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.50f, 0.56f, 0.66f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 11, TextAnchor.UpperLeft, new Color(0.94f, 0.96f, 1f), new Vector2(24f, -258f), new Vector2(172f, 124f));
        stoneInfoText.supportRichText = true;

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -386f), new Vector2(170f, PanelHeight - 386f - 24f), new Color(0.04f, 0.05f, 0.08f, 1f));
        technicianText = Txt("Technician", "", 11, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 386f - 40f));

        BuildPress();
        BuildControls();

        // The cycle in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(PressX, -416f), new Vector2(PanelWidth - PressX - 24f, 74f), new Color(0.04f, 0.05f, 0.08f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(PressX + 10f, -424f), new Vector2(PanelWidth - PressX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir le diamant", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(PressX + 4f, -476f), new Vector2(PanelWidth - PressX - 24f - 8f, 6f), new Color(0.09f, 0.11f, 0.16f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Ember, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(PressX, -496f), new Vector2(PanelWidth - PressX - 24f, 92f), new Color(0.04f, 0.05f, 0.08f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(PressX + 10f, -502f), new Vector2(PanelWidth - PressX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer la presse", 14, new Color(0.62f, 0.34f, 0.12f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The cell of the press seen from the front: a steel frame, four anvils around a glowing cell, the diamond in the middle, and the readout.
    private void BuildPress()
    {
        var origin = new Vector2(PressX, PressY);
        RuntimeUIKit.MakeBox("PressFrame", panel.transform, origin, new Vector2(PressW, PressH), new Color(0.10f, 0.12f, 0.17f, 1f));
        RuntimeUIKit.MakeBox("Casing", panel.transform, origin + new Vector2(12f, -12f), new Vector2(PressW - 24f, 232f), new Color(0.40f, 0.45f, 0.52f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Plate", panel.transform, origin + new Vector2(22f, -22f), new Vector2(PressW - 44f, 212f), new Color(0.30f, 0.34f, 0.40f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Chamber", panel.transform, origin + new Vector2(34f, -34f), new Vector2(PressW - 68f, 188f), new Color(0.04f, 0.05f, 0.07f, 1f)).raycastTarget = false;

        var glow = RuntimeUIKit.MakeBox("CellGlow", panel.transform, origin + new Vector2(PressW / 2f - 70f, -54f), new Vector2(140f, 140f), new Color(1f, 0.6f, 0.2f, 0.8f), false);
        glow.sprite = RuntimeUIKit.Glow;
        glow.raycastTarget = false;
        cellGlow = glow;

        var stoneGo = new GameObject("CellStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        var stoneRect = stoneGo.GetComponent<RectTransform>();
        stoneRect.anchorMin = stoneRect.anchorMax = new Vector2(0f, 1f);
        stoneRect.pivot = new Vector2(0.5f, 0.5f);
        stoneRect.sizeDelta = new Vector2(48f, 48f);
        stoneInCell = stoneGo.GetComponent<Image>();
        stoneInCell.preserveAspect = true;
        stoneInCell.raycastTarget = false;
        stoneGo.SetActive(false);

        // Four anvils, drawn over the glow so they close in on the diamond.
        var anvilColors = new[] { new Color(0.62f, 0.66f, 0.74f), new Color(0.62f, 0.66f, 0.74f), new Color(0.54f, 0.58f, 0.66f), new Color(0.54f, 0.58f, 0.66f) };
        for (var i = 0; i < anvils.Length; i++)
        {
            var go = new GameObject("Anvil" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            var anvilRect = go.GetComponent<RectTransform>();
            anvilRect.anchorMin = anvilRect.anchorMax = new Vector2(0f, 1f);
            anvilRect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = RuntimeUIKit.Rounded;
            image.type = Image.Type.Sliced;
            image.color = anvilColors[i];
            image.raycastTarget = false;
            anvils[i] = image;
        }
        PlaceAnvils(HphtCatalog.MinPressure);

        RuntimeUIKit.MakeBox("Strip", panel.transform, origin + new Vector2(12f, -250f), new Vector2(PressW - 24f, 38f), new Color(0.16f, 0.18f, 0.24f, 1f)).raycastTarget = false;
        readoutText = Txt("Readout", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(12f, -250f), new Vector2(PressW - 24f, 38f));
    }

    private void BuildControls()
    {
        temperatureText = Txt("Temperature", "", 14, TextAnchor.UpperLeft, Ember, new Vector2(ColumnX, -80f), new Vector2(262f, 22f));
        temperatureText.fontStyle = FontStyle.Bold;
        temperatureSlider = MakeSlider("TemperatureSlider", new Vector2(ColumnX, -104f), HphtCatalog.MinTemperature / HphtCatalog.TemperatureStep,
            HphtCatalog.MaxTemperature / HphtCatalog.TemperatureStep, temperature / (float)HphtCatalog.TemperatureStep, OnTemperature);
        Txt("TemperatureMin", $"{HphtCatalog.MinTemperature} °C", 10, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -130f), new Vector2(80f, 14f));
        var tMax = Txt("TemperatureMax", $"{HphtCatalog.MaxTemperature} °C", 10, TextAnchor.UpperRight, TextSoft, new Vector2(ColumnX + SliderWidth - 80f, -130f), new Vector2(80f, 14f));
        tMax.horizontalOverflow = HorizontalWrapMode.Overflow;

        pressureText = Txt("Pressure", "", 14, TextAnchor.UpperLeft, Steel, new Vector2(ColumnX, -158f), new Vector2(262f, 22f));
        pressureText.fontStyle = FontStyle.Bold;
        pressureSlider = MakeSlider("PressureSlider", new Vector2(ColumnX, -182f), HphtCatalog.MinPressure / HphtCatalog.PressureStep,
            HphtCatalog.MaxPressure / HphtCatalog.PressureStep, pressure / (float)HphtCatalog.PressureStep, OnPressure);
        Txt("PressureMin", $"{HphtCatalog.MinPressure / 10f:F1} GPa", 10, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -208f), new Vector2(80f, 14f));
        var pMax = Txt("PressureMax", $"{HphtCatalog.MaxPressure / 10f:F1} GPa", 10, TextAnchor.UpperRight, TextSoft, new Vector2(ColumnX + SliderWidth - 80f, -208f), new Vector2(80f, 14f));
        pMax.horizontalOverflow = HorizontalWrapMode.Overflow;

        Txt("DurationTitle", "Durée du cycle", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -238f), new Vector2(260f, 18f));
        for (var i = 0; i < HphtCatalog.DurationDays.Length; i++)
        {
            var captured = i;
            var days = HphtCatalog.DurationDays[i];
            var button = RuntimeUIKit.MakeButton("Duration" + i, panel.transform, days == 1 ? "1 jour" : $"{days} jours", 12, ButtonIdle,
                new Vector2(ColumnX + i * 90f, -258f), new Vector2(84f, 28f));
            button.onClick.AddListener(() => SetDuration(captured));
            durationImages[i] = button.GetComponent<Image>();
        }

        Txt("Rules", "Trop peu de pression pour la température, et le diamant se transforme en graphite. Le HPHT est difficile à détecter sans appareils de laboratoire.", 11,
            TextAnchor.UpperLeft, new Color(0.72f, 0.78f, 0.88f), new Vector2(ColumnX, -304f), new Vector2(262f, 60f));
    }

    private Slider MakeSlider(string name, Vector2 position, int min, int max, float value, UnityEngine.Events.UnityAction<float> onChange)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, new Vector2(SliderWidth, 26f));

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(go.transform, false);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 8f);
        backgroundRect.anchoredPosition = Vector2.zero;
        var backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = StripeSprite();
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0f);
        handleRect.anchorMax = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(18f, 0f);
        var handleImage = handle.GetComponent<Image>();
        handleImage.sprite = RuntimeUIKit.Rounded;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0.98f, 0.92f, 0.80f, 1f);

        var result = go.GetComponent<Slider>();
        result.handleRect = handleRect;
        result.targetGraphic = handleImage;
        result.direction = Slider.Direction.LeftToRight;
        result.wholeNumbers = true;
        result.minValue = min;
        result.maxValue = max;
        result.value = value;
        result.onValueChanged.AddListener(onChange);
        return result;
    }

    private Text Txt(string name, string content, int size, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, panel.transform, content, size, alignment, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
