using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The furnace of the treatments laboratory: the heating of a stone. The player drops a stone in the slot and sets the temperature, the atmosphere
// (oxidising, reducing or neutral), the cooling (fast or slow) and the length of the heating. The window lists what can be tried on the stone,
// but not the settings: they are in the course. A hint says whether the furnace is near a known process. One stone at a time; it comes out
// some days of the game later. Built at runtime.
public class HeatingUI : MonoBehaviour
{
    public static HeatingUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float FurnaceX = 206f;
    private const float FurnaceY = -92f;
    private const float FurnaceW = 300f;
    private const float FurnaceH = 300f;
    private const float ColumnX = 530f;
    private const float SliderWidth = 262f;

    private static readonly Color PanelColor = new Color(0.10f, 0.06f, 0.05f, 0.98f);
    private static readonly Color Ember = new Color(1.00f, 0.60f, 0.28f);
    private static readonly Color Gold = new Color(0.98f, 0.84f, 0.45f);
    private static readonly Color TextSoft = new Color(0.96f, 0.88f, 0.82f);
    private static readonly Color ButtonIdle = new Color(0.22f, 0.13f, 0.10f, 1f);
    private static readonly Color ButtonOn = new Color(0.62f, 0.28f, 0.10f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text readoutText;
    private Text temperatureText;
    private Image barFill;
    private Image chamberGlow;
    private Image thermometerFill;
    private Image stoneInFurnace;
    private Slider slider;
    private Button runButton;
    private Button collectButton;
    private readonly Image[] atmosphereImages = new Image[3];
    private readonly Image[] coolingImages = new Image[2];
    private readonly Image[] durationImages = new Image[HeatingCatalog.DurationDays.Length];
    private readonly Image[] cylinders = new Image[3];
    private readonly Image[] flames = new Image[7];

    private GemInstance stone;
    private int temperature = 1000;
    private HeatAtmosphere atmosphere = HeatAtmosphere.Oxidizing;
    private HeatCooling cooling = HeatCooling.Slow;
    private int durationIndex;
    private HeatQuote quote;
    private static Sprite stripeSprite;
    private static Sprite circleSprite;

    private static readonly Color[] GasColors = { new Color(0.35f, 0.62f, 0.95f), new Color(0.90f, 0.30f, 0.25f), new Color(0.35f, 0.78f, 0.45f) };
    private static readonly string[] AtmosphereNames = { "Oxydante (air, oxygène)", "Réductrice (hydrogène, CO)", "Neutre (argon)" };
    private static readonly string[] CoolingNames = { "Rapide (trempe)", "Lent (four éteint)" };

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        HeatingState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<HeatingUI>();
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
        var state = HeatingState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (HeatingState.Instance != null) HeatingState.Instance.OnJobChanged -= RefreshIfOpen;
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
        temperature = 1000;
        atmosphere = HeatAtmosphere.Oxidizing;
        cooling = HeatCooling.Slow;
        durationIndex = 0;
        slider.SetValueWithoutNotify(temperature / (float)HeatingCatalog.TemperatureStep);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez une pierre. Je vous dis ce qu'on peut tenter dessus, mais pas comment : la température, l'atmosphère et le refroidissement, c'est dans votre cours. »";
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
        AnimateFurnace();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = HeatingCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        technicianText.text = "« Voilà ce qu'on peut tenter sur cette pierre. À vous de régler le four. Un refroidissement trop rapide fissure la plupart des pierres. »";
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
        temperature = Mathf.Clamp(Mathf.RoundToInt(value) * HeatingCatalog.TemperatureStep, HeatingCatalog.MinTemperature, HeatingCatalog.MaxTemperature);
        Refresh();
    }

    private void SetAtmosphere(int index)
    {
        atmosphere = (HeatAtmosphere)index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private void SetCooling(int index)
    {
        cooling = (HeatCooling)index;
        Sfx.Play(Sfx.Id.Click);
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

    private int Days => HeatingCatalog.DurationDays[durationIndex];

    private void Refresh()
    {
        // The stone can leave the bag behind our back: forget it then.
        if (stone != null && !ContainsGem(stone)) stone = null;

        RefreshSlot();
        RefreshControls();
        RefreshJob();
        RefreshQuote();
        RefreshFurnace();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nune pierre\nici";
            stoneInfoText.text = "Se chauffent : corindon, béryl,\nquartz, topaze, tourmaline,\nzircon, tanzanite, ambre.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        var goals = HeatingCatalog.GoalsFor(stone);
        var lines = new System.Text.StringBuilder();
        lines.Append($"{stone.Definition.displayName}  {stone.CaratWeight:F2} ct\n");
        lines.Append($"{stone.ColorName}  ·  {stone.FinalValue} €\n\n<color=#f5c06a>On peut tenter :</color>\n");
        foreach (var goal in goals) lines.Append($"· {goal}\n");
        stoneInfoText.text = lines.ToString();
    }

    private void RefreshControls()
    {
        for (var i = 0; i < atmosphereImages.Length; i++) atmosphereImages[i].color = i == (int)atmosphere ? ButtonOn : ButtonIdle;
        for (var i = 0; i < coolingImages.Length; i++) coolingImages[i].color = i == (int)cooling ? ButtonOn : ButtonIdle;
        for (var i = 0; i < durationImages.Length; i++) durationImages[i].color = i == durationIndex ? ButtonOn : ButtonIdle;
        temperatureText.text = $"{temperature} °C";
        temperatureText.color = HeatColor(temperature);
        slider.SetValueWithoutNotify(temperature / (float)HeatingCatalog.TemperatureStep);
    }

    // The colour of hot metal for a temperature: dark, red, orange, yellow, white.
    private static Color HeatColor(int celsius)
    {
        var t = Mathf.Clamp01((celsius - 300f) / 1700f);
        if (t < 0.25f) return Color.Lerp(new Color(0.55f, 0.50f, 0.50f), new Color(0.85f, 0.15f, 0.10f), t / 0.25f);
        if (t < 0.55f) return Color.Lerp(new Color(0.85f, 0.15f, 0.10f), new Color(1.00f, 0.55f, 0.12f), (t - 0.25f) / 0.30f);
        if (t < 0.80f) return Color.Lerp(new Color(1.00f, 0.55f, 0.12f), new Color(1.00f, 0.88f, 0.40f), (t - 0.55f) / 0.25f);
        return Color.Lerp(new Color(1.00f, 0.88f, 0.40f), new Color(1.00f, 0.98f, 0.90f), (t - 0.80f) / 0.20f);
    }

    // The chamber glows at the colour of the temperature, the thermometer rises, the cylinder of the chosen gas is lit.
    private void RefreshFurnace()
    {
        var job = HeatingState.Instance != null ? HeatingState.Instance.Job : null;
        var shownTemperature = job != null ? job.Temperature : temperature;
        var shownAtmosphere = job != null ? job.Atmosphere : atmosphere;
        var shown = job != null ? job.Stone : stone;
        stoneInFurnace.gameObject.SetActive(shown != null);
        if (shown != null) stoneInFurnace.sprite = shown.DisplaySprite;

        var color = HeatColor(shownTemperature);
        thermometerFill.color = color;
        var fillRect = (RectTransform)thermometerFill.transform;
        fillRect.sizeDelta = new Vector2(fillRect.sizeDelta.x, 200f * shownTemperature / HeatingCatalog.MaxTemperature);
        for (var i = 0; i < cylinders.Length; i++)
            cylinders[i].color = i == (int)shownAtmosphere ? GasColors[i] : GasColors[i] * 0.35f;
        chamberGlow.color = new Color(color.r, color.g, color.b, 0.75f);
    }

    private void RefreshJob()
    {
        var state = HeatingState.Instance;
        var job = state != null ? state.Job : null;
        collectButton.gameObject.SetActive(job != null);
        barFill.transform.parent.gameObject.SetActive(job != null);

        if (job == null)
        {
            jobText.text = "Le four est libre.";
            readoutText.text = "Four éteint  ·  prêt";
            return;
        }

        var ready = state.IsReady;
        var days = state.DaysRemaining;
        jobText.text = ready
            ? $"{job.Stone.Definition.displayName} — {job.Temperature} °C : cycle terminé, le four a refroidi."
            : $"{job.Stone.Definition.displayName} — {job.Temperature} °C : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        readoutText.text = ready ? "Cycle terminé" : $"Chauffe en cours  ·  {job.Temperature} °C";
        var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
        var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
        var bar = (RectTransform)barFill.transform;
        bar.sizeDelta = new Vector2((PanelWidth - FurnaceX - 24f - 8f) * (ready ? 1f : done), bar.sizeDelta.y);
        barFill.color = ready ? Gold : Ember;
        collectButton.interactable = ready;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void RefreshQuote()
    {
        var state = HeatingState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "Le four est occupé : sortez d'abord la pierre en cours.";
            return;
        }
        if (stone == null)
        {
            quoteText.text = "Posez une pierre dans la case, puis réglez le four.";
            return;
        }

        quote = HeatingCatalog.GetQuote(temperature, atmosphere, cooling, Days, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var reactionColor = quote.Reaction < 0.2f ? "#ff9a70" : quote.Reaction < 0.45f ? "#f5c629" : "#a8e0a0";
        var riskColor = quote.TooHot || quote.CrackRisk >= 0.4f ? "#ff7060" : quote.CrackRisk >= 0.2f ? "#f5c629" : "#a8e0a0";
        quoteText.text =
            $"<b>{temperature} °C</b>  ·  {AtmosphereNames[(int)atmosphere].ToLowerInvariant()}  ·  refroidissement {CoolingNames[(int)cooling].ToLowerInvariant()}  ·  {Days} jour{(Days > 1 ? "s" : "")}" + "\n" +
            $"Durée totale : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}  (sortie le {GameCalendar.Short(Today + quote.Days)})    Four et gaz : {quote.Fee} €\n" +
            $"Réaction attendue : <color={reactionColor}>{quote.ReactionLabel}</color>    Risque : <color={riskColor}>{quote.RiskLabel}</color>";
        if (quote.TooHot) quoteText.text += "\n« Beaucoup trop chaud : la pierre fondrait. »";
        else if (quote.Reaction >= 0.2f && quote.Reaction < 0.45f) quoteText.text += "\n« On s'approche d'un procédé connu... mais ce n'est pas tout à fait ça. »";

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
        var gas = atmosphere;
        var cool = cooling;
        var length = Days;
        ConfirmDialog.Show("Lancer le chauffage",
            $"Mettre {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans le four ?\n\n{degrees} °C, atmosphère {AtmosphereNames[(int)gas].ToLowerInvariant()}\nRefroidissement {CoolingNames[(int)cool].ToLowerInvariant()}, {length} jour{(length > 1 ? "s" : "")}\nPrix : {current.Fee} €\nRisque : {current.RiskLabel}\n\nOn ne reprend pas une pierre chauffée.",
            "Lancer", () => StartRun(degrees, gas, cool, length, gem, current));
    }

    private void StartRun(int degrees, HeatAtmosphere gas, HeatCooling cool, int length, GemInstance gem, HeatQuote current)
    {
        var state = HeatingState.Ensure();
        if (state == null) return;
        if (state.TryStart(degrees, gas, cool, length, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.Flame);
            technicianText.text = $"« Le four monte en température. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
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
        var state = HeatingState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var beforeClarity = job.Stone.ClarityGrade;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.Outcome == HeatOutcome.Ruined ? Sfx.Id.Error : Sfx.Id.Success);
            var clarity = gem.ClarityGrade != beforeClarity ? $"\nPureté : {beforeClarity.ToLowerInvariant()} → {gem.ClarityGrade.ToLowerInvariant()}" : "";
            var star = result.Star ? "\nUn astérisme est apparu (visible en cabochon)." : "";
            technicianText.text = result.Outcome == HeatOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »\nCouleur : {gem.ColorName.ToLowerInvariant()}{clarity}{star}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.Outcome == HeatOutcome.Ruined ? "Pierre abîmée par la chaleur." : "Pierre sortie du four, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The flames flicker under the chamber while a cycle runs (and a little at rest when the temperature is set high); the stone shimmers.
    private void AnimateFurnace()
    {
        var state = HeatingState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var degrees = state != null && state.Job != null ? state.Job.Temperature : temperature;
        var strength = running ? 1f : Mathf.Clamp01((degrees - 500f) / 1500f) * 0.45f;

        for (var i = 0; i < flames.Length; i++)
        {
            var flicker = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (7f + i * 1.3f) + i * 2f);
            var rect = flames[i].rectTransform;
            rect.sizeDelta = new Vector2(10f, (8f + 18f * flicker) * strength + 2f);
            var color = HeatColor(Mathf.Max(800, degrees));
            flames[i].color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.15f, 0.9f, strength) * (0.6f + 0.4f * flicker));
        }

        if (stoneInFurnace.gameObject.activeSelf)
        {
            var rect = stoneInFurnace.rectTransform;
            rect.anchoredPosition = new Vector2(FurnaceX + FurnaceW / 2f - 30f, FurnaceY - 116f + Mathf.Sin(Time.unscaledTime * 2f) * 1.5f);
        }

        var pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
        var glow = chamberGlow.color;
        glow.a = Mathf.Lerp(0.35f, 0.9f, Mathf.Clamp01((degrees - 200f) / 1800f)) * (running ? pulse : 0.85f);
        chamberGlow.color = glow;
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private static Sprite CircleSprite()
    {
        if (circleSprite == null) circleSprite = RuntimeUIKit.MakeRoundedSprite(64, 32);
        return circleSprite;
    }

    // A strip from dark red to white-hot, tiled along the top and the bottom of the window.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int w = 64;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (var x = 0; x < w; x++)
        {
            var color = HeatColor(300 + Mathf.RoundToInt(1700f * x / (w - 1)));
            for (var y = 0; y < h; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("HeatingPanel", typeof(RectTransform));
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

        Txt("Title", "Four de traitement thermique", 22, TextAnchor.UpperLeft, Ember, new Vector2(24f, -20f), new Vector2(520f, 30f));
        Txt("Subtitle", "Produire, éliminer ou uniformiser la couleur : la température, l'atmosphère et le refroidissement décident.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Pierre à chauffer", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.05f, 0.03f, 0.03f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.62f, 0.50f, 0.46f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 11, TextAnchor.UpperLeft, new Color(0.98f, 0.94f, 0.90f), new Vector2(24f, -258f), new Vector2(172f, 124f));
        stoneInfoText.supportRichText = true;

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -386f), new Vector2(170f, PanelHeight - 386f - 24f), new Color(0.06f, 0.03f, 0.03f, 1f));
        technicianText = Txt("Technician", "", 11, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 386f - 40f));

        BuildFurnace();
        BuildControls();

        // The cycle in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(FurnaceX, -416f), new Vector2(PanelWidth - FurnaceX - 24f, 74f), new Color(0.06f, 0.03f, 0.03f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(FurnaceX + 10f, -424f), new Vector2(PanelWidth - FurnaceX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(FurnaceX + 4f, -476f), new Vector2(PanelWidth - FurnaceX - 24f - 8f, 6f), new Color(0.12f, 0.07f, 0.06f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Ember, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(FurnaceX, -496f), new Vector2(PanelWidth - FurnaceX - 24f, 92f), new Color(0.06f, 0.03f, 0.03f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(FurnaceX + 10f, -502f), new Vector2(PanelWidth - FurnaceX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer le chauffage", 14, new Color(0.70f, 0.30f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The electric furnace seen from the front: a thermometer on the left, the chamber with the stone in a crucible, the flames under it, and the
    // three gas cylinders at the foot of the furnace.
    private void BuildFurnace()
    {
        var origin = new Vector2(FurnaceX, FurnaceY);
        RuntimeUIKit.MakeBox("FurnaceFrame", panel.transform, origin, new Vector2(FurnaceW, FurnaceH), new Color(0.14f, 0.09f, 0.08f, 1f));
        RuntimeUIKit.MakeBox("Casing", panel.transform, origin + new Vector2(50f, -12f), new Vector2(FurnaceW - 64f, 214f), new Color(0.40f, 0.38f, 0.38f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Plate", panel.transform, origin + new Vector2(58f, -20f), new Vector2(FurnaceW - 80f, 198f), new Color(0.50f, 0.48f, 0.47f, 1f)).raycastTarget = false;

        // The chamber: a dark opening that glows at the temperature of the furnace.
        RuntimeUIKit.MakeBox("Chamber", panel.transform, origin + new Vector2(84f, -42f), new Vector2(FurnaceW - 132f, 118f), new Color(0.05f, 0.03f, 0.03f, 1f)).raycastTarget = false;
        var glow = RuntimeUIKit.MakeBox("ChamberGlow", panel.transform, origin + new Vector2(92f, -50f), new Vector2(FurnaceW - 148f, 102f), new Color(0.6f, 0.2f, 0.1f, 0.75f), false);
        glow.sprite = RuntimeUIKit.Glow;
        glow.raycastTarget = false;
        chamberGlow = glow;

        // The crucible, with the stone on it.
        RuntimeUIKit.MakeBox("Crucible", panel.transform, origin + new Vector2(FurnaceW / 2f + 8f - 38f, -128f), new Vector2(76f, 34f), new Color(0.82f, 0.74f, 0.64f, 1f)).raycastTarget = false;
        var stoneGo = new GameObject("FurnaceStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), origin + new Vector2(FurnaceW / 2f - 30f, -116f), new Vector2(60f, 50f));
        stoneInFurnace = stoneGo.GetComponent<Image>();
        stoneInFurnace.preserveAspect = true;
        stoneInFurnace.raycastTarget = false;
        stoneGo.SetActive(false);

        // The flames under the chamber.
        for (var i = 0; i < flames.Length; i++)
        {
            var go = new GameObject("Flame", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            var flameRect = go.GetComponent<RectTransform>();
            flameRect.anchorMin = flameRect.anchorMax = new Vector2(0f, 1f);
            flameRect.pivot = new Vector2(0.5f, 0f);
            flameRect.sizeDelta = new Vector2(10f, 12f);
            flameRect.anchoredPosition = origin + new Vector2(110f + i * 24f, -190f);
            var image = go.GetComponent<Image>();
            image.sprite = RuntimeUIKit.MakeRoundedSprite(16, 8);
            image.raycastTarget = false;
            flames[i] = image;
        }

        // The control strip under the chamber, with the readout.
        RuntimeUIKit.MakeBox("Strip", panel.transform, origin + new Vector2(58f, -196f), new Vector2(FurnaceW - 80f, 34f), new Color(0.24f, 0.22f, 0.22f, 1f)).raycastTarget = false;
        readoutText = Txt("Readout", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(58f, -196f), new Vector2(FurnaceW - 80f, 34f));

        // The thermometer on the left: a glass tube that fills with the colour of the temperature.
        RuntimeUIKit.MakeBox("TubeBack", panel.transform, origin + new Vector2(14f, -20f), new Vector2(22f, 206f), new Color(0.85f, 0.88f, 0.90f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("TubeInner", panel.transform, origin + new Vector2(18f, -24f), new Vector2(14f, 198f), new Color(0.10f, 0.08f, 0.08f, 1f), false).raycastTarget = false;
        var fillGo = new GameObject("Mercury", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(panel.transform, false);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0.5f, 0f);
        fillRect.sizeDelta = new Vector2(10f, 100f);
        fillRect.anchoredPosition = origin + new Vector2(25f, -222f);
        thermometerFill = fillGo.GetComponent<Image>();
        thermometerFill.raycastTarget = false;

        // The three gas cylinders at the foot of the furnace: air, hydrogen and carbon monoxide, argon.
        var gasNames = new[] { "O2 / air", "H2 / CO", "Argon" };
        for (var i = 0; i < 3; i++)
        {
            var x = 66f + i * 78f;
            RuntimeUIKit.MakeBox("CylinderCap", panel.transform, origin + new Vector2(x + 12f, -240f), new Vector2(22f, 8f), new Color(0.70f, 0.72f, 0.76f, 1f)).raycastTarget = false;
            cylinders[i] = RuntimeUIKit.MakeBox("Cylinder" + i, panel.transform, origin + new Vector2(x, -246f), new Vector2(46f, 46f), GasColors[i]);
            cylinders[i].raycastTarget = false;
            var label = Txt("GasName" + i, gasNames[i], 10, TextAnchor.MiddleCenter, Color.white, origin + new Vector2(x - 4f, -246f), new Vector2(54f, 46f));
            label.fontStyle = FontStyle.Bold;
        }
    }

    private void BuildControls()
    {
        Txt("TemperatureTitle", "Température du four", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -80f), new Vector2(260f, 18f));
        temperatureText = Txt("Temperature", "", 22, TextAnchor.UpperLeft, Ember, new Vector2(ColumnX, -98f), new Vector2(262f, 30f));
        temperatureText.fontStyle = FontStyle.Bold;
        slider = MakeSlider(new Vector2(ColumnX, -130f));
        Txt("TemperatureMin", $"{HeatingCatalog.MinTemperature} °C", 10, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -156f), new Vector2(80f, 14f));
        var maxLabel = Txt("TemperatureMax", $"{HeatingCatalog.MaxTemperature} °C", 10, TextAnchor.UpperRight, TextSoft, new Vector2(ColumnX + SliderWidth - 80f, -156f), new Vector2(80f, 14f));
        maxLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

        Txt("AtmosphereTitle", "Atmosphère", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -176f), new Vector2(260f, 18f));
        for (var i = 0; i < 3; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Atmosphere" + i, panel.transform, AtmosphereNames[i], 12, ButtonIdle, new Vector2(ColumnX, -196f - i * 32f), new Vector2(262f, 28f));
            button.onClick.AddListener(() => SetAtmosphere(captured));
            atmosphereImages[i] = button.GetComponent<Image>();
        }

        Txt("CoolingTitle", "Refroidissement", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -296f), new Vector2(260f, 18f));
        for (var i = 0; i < 2; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Cooling" + i, panel.transform, CoolingNames[i], 12, ButtonIdle, new Vector2(ColumnX + i * 134f, -316f), new Vector2(128f, 28f));
            button.onClick.AddListener(() => SetCooling(captured));
            coolingImages[i] = button.GetComponent<Image>();
        }

        Txt("DurationTitle", "Durée du chauffage", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -354f), new Vector2(260f, 18f));
        for (var i = 0; i < HeatingCatalog.DurationDays.Length; i++)
        {
            var captured = i;
            var days = HeatingCatalog.DurationDays[i];
            var button = RuntimeUIKit.MakeButton("Duration" + i, panel.transform, days == 1 ? "1 jour" : $"{days} jours", 12, ButtonIdle,
                new Vector2(ColumnX + i * 67f, -374f), new Vector2(62f, 26f));
            button.onClick.AddListener(() => SetDuration(captured));
            durationImages[i] = button.GetComponent<Image>();
        }
    }

    // A slider for the temperature, in steps of 50 degrees.
    private Slider MakeSlider(Vector2 position)
    {
        var go = new GameObject("TemperatureSlider", typeof(RectTransform), typeof(Slider));
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
        backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);

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
        result.minValue = HeatingCatalog.MinTemperature / HeatingCatalog.TemperatureStep;
        result.maxValue = HeatingCatalog.MaxTemperature / HeatingCatalog.TemperatureStep;
        result.value = temperature / (float)HeatingCatalog.TemperatureStep;
        result.onValueChanged.AddListener(OnTemperature);
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
