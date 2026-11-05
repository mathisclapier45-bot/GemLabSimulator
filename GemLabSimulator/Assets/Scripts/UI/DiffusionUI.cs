using UnityEngine;
using UnityEngine.UI;

// The diffusion furnace of the treatments laboratory: the stone, covered with the powder of an element, is heated at a very high temperature for
// days, and the element goes into it. The player drops a corundum in the slot, picks the element (iron and titanium, chromium, nickel, a titanium
// surplus, beryllium), the temperature and the number of days. The window shows a section of the stone with the layer the element would reach,
// so the difference between a surface diffusion and a core diffusion can be seen. One stone at a time. Built at runtime.
public class DiffusionUI : MonoBehaviour
{
    public static DiffusionUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float FurnaceX = 206f;
    private const float FurnaceY = -92f;
    private const float FurnaceW = 300f;
    private const float FurnaceH = 300f;
    private const float ColumnX = 530f;
    private const float SliderWidth = 262f;

    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.12f, 0.98f);
    private static readonly Color Ember = new Color(1.00f, 0.62f, 0.30f);
    private static readonly Color Gold = new Color(0.98f, 0.84f, 0.45f);
    private static readonly Color Steel = new Color(0.62f, 0.78f, 1.00f);
    private static readonly Color TextSoft = new Color(0.88f, 0.92f, 0.98f);
    private static readonly Color ButtonIdle = new Color(0.13f, 0.16f, 0.26f, 1f);
    private static readonly Color ButtonOn = new Color(0.52f, 0.30f, 0.14f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text readoutText;
    private Text temperatureText;
    private Text sectionText;
    private Image barFill;
    private Image tubeGlow;
    private Image powderImage;
    private Color powderBase = new Color(0.55f, 0.55f, 0.58f, 1f);
    private Image stoneInFurnace;
    private Image sectionOuter;
    private Image sectionInner;
    private Slider slider;
    private Button runButton;
    private Button collectButton;
    private readonly Image[] dopantImages = new Image[DiffusionCatalog.DopantCount];
    private readonly Image[] durationImages = new Image[DiffusionCatalog.DurationDays.Length];

    private GemInstance stone;
    private int dopant = -1;
    private int temperature = 1700;
    private int durationIndex = 1;
    private DiffusionQuote quote;
    private static Sprite stripeSprite;
    private static Sprite circleSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        DiffusionState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<DiffusionUI>();
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
        var state = DiffusionState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (DiffusionState.Instance != null) DiffusionState.Instance.OnJobChanged -= RefreshIfOpen;
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
        dopant = -1;
        temperature = 1700;
        durationIndex = 1;
        slider.SetValueWithoutNotify(temperature / (float)DiffusionCatalog.TemperatureStep);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez un corindon, choisissez l'élément, la température et la durée. Regardez la coupe : plus la couche est mince, plus un repolissage est dangereux. »";
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
        var refusal = DiffusionCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        technicianText.text = "« Un corindon prêt pour la poudre. Quel élément voulez-vous lui faire boire ? »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        Refresh();
    }

    private void SelectDopant(int index)
    {
        dopant = dopant == index ? -1 : index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private void OnTemperature(float value)
    {
        temperature = Mathf.Clamp(Mathf.RoundToInt(value) * DiffusionCatalog.TemperatureStep, DiffusionCatalog.MinTemperature, DiffusionCatalog.MaxTemperature);
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

    private int Days => DiffusionCatalog.DurationDays[durationIndex];

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
            slotLabel.text = "Déposez\nun corindon\nici";
            stoneInfoText.text = "La diffusion se fait sur les\ncorindons : rubis et saphirs.\nElle prend des jours.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{stone.Definition.displayName}\n{stone.CaratWeight:F2} ct — épaisseur {stone.DepthMm:F1} mm\nCouleur : {stone.ColorName.ToLowerInvariant()}\n" +
            $"Valeur estimée : {stone.FinalValue} €\n(clic sur la case pour retirer)";
    }

    private void RefreshControls()
    {
        for (var i = 0; i < dopantImages.Length; i++) dopantImages[i].color = i == dopant ? ButtonOn : ButtonIdle;
        for (var i = 0; i < durationImages.Length; i++) durationImages[i].color = i == durationIndex ? ButtonOn : ButtonIdle;
        temperatureText.text = $"Température : {temperature} °C";
        temperatureText.color = HeatColor(temperature);
        slider.SetValueWithoutNotify(temperature / (float)DiffusionCatalog.TemperatureStep);
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

    // The furnace glows at the colour of the temperature, the stone sits in the powder of the chosen element, and the section of the stone shows
    // the layer the element would reach.
    private void RefreshFurnace()
    {
        var job = DiffusionState.Instance != null ? DiffusionState.Instance.Job : null;
        var shown = job != null ? job.Stone : stone;
        var shownDopant = job != null ? job.Dopant : dopant;
        var shownTemperature = job != null ? job.Temperature : temperature;

        stoneInFurnace.gameObject.SetActive(shown != null);
        if (shown != null) stoneInFurnace.sprite = shown.DisplaySprite;
        var powder = shownDopant >= 0 ? DiffusionCatalog.Dopants[shownDopant].Powder : new Color(0.55f, 0.55f, 0.58f);
        powderBase = powder;
        powderImage.color = powder;
        var glow = HeatColor(shownTemperature);
        tubeGlow.color = new Color(glow.r, glow.g, glow.b, 0.8f);

        // The section: a coloured rim of the thickness of the layer around the original colour of the stone.
        if (shown == null)
        {
            sectionOuter.color = new Color(0.20f, 0.22f, 0.30f, 1f);
            sectionInner.gameObject.SetActive(false);
            sectionText.text = "Coupe de la pierre";
            return;
        }

        if (job == null && quote.IsValid && dopant >= 0)
        {
            var layer = DiffusionCatalog.Dopants[dopant].Powder;
            sectionOuter.color = Color.Lerp(new Color(0.20f, 0.22f, 0.30f, 1f), layer, Mathf.Clamp01(quote.Quality * 1.3f));
            var rest = Mathf.Clamp01(1f - quote.Coverage);
            sectionInner.gameObject.SetActive(rest > 0.02f);
            var inner = (RectTransform)sectionInner.transform;
            inner.sizeDelta = new Vector2(150f * rest, 84f * rest);
            inner.anchoredPosition = new Vector2(FurnaceX + FurnaceW / 2f - 75f * rest, FurnaceY - 250f + 42f * rest);
            sectionInner.color = shown.BodyColor;
            sectionText.text = $"Coupe : couche de {quote.DepthMm:F2} mm sur {shown.DepthMm * 0.5f:F1} mm";
        }
        else
        {
            sectionOuter.color = shown.BodyColor;
            sectionInner.gameObject.SetActive(false);
            sectionText.text = "Coupe de la pierre";
        }
    }

    private void RefreshJob()
    {
        var state = DiffusionState.Instance;
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
            ? $"{job.Stone.Definition.displayName} — {DiffusionCatalog.Dopants[job.Dopant].Name}, {job.Temperature} °C : cycle terminé, le four a refroidi."
            : $"{job.Stone.Definition.displayName} — {DiffusionCatalog.Dopants[job.Dopant].Name}, {job.Temperature} °C : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        readoutText.text = ready ? "Cycle terminé" : $"Diffusion en cours  ·  {job.Temperature} °C";
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
        var state = DiffusionState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "Le four est occupé : sortez d'abord la pierre en cours.";
            return;
        }
        if (dopant < 0)
        {
            quoteText.text = stone == null
                ? "Posez un corindon dans la case, puis choisissez un élément."
                : "Choisissez l'élément à diffuser, à droite.";
            return;
        }

        var item = DiffusionCatalog.Dopants[dopant];
        if (stone == null)
        {
            quoteText.text = $"<b>{item.Name}</b>  <color=#9fc4ff>{item.Detail}</color>\n\n{item.Description}\n\nPosez un corindon dans la case pour obtenir un devis.";
            return;
        }

        quote = DiffusionCatalog.GetQuote(dopant, temperature, Days, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var qualityColor = quote.Quality < 0.2f ? "#ff9a70" : quote.Quality < 0.45f ? "#f5c629" : "#a8e0a0";
        quoteText.text =
            $"<b>{item.Name}</b>  ·  {temperature} °C  ·  {Days} jours\n" +
            $"Pénétration : <b>{quote.DepthMm:F2} mm</b>  ({quote.DepthLabel})    Couleur : <color={qualityColor}>{quote.QualityLabel}</color>\n" +
            $"Durée : {quote.Days} jours  (sortie le {GameCalendar.Short(Today + quote.Days)})    Four et poudres : {quote.Fee} €\n" +
            $"« {quote.Advice} »";
        if (quote.TooHot) quoteText.text = "« Beaucoup trop chaud : le corindon fondrait en surface. »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        runButton.gameObject.SetActive(true);
        runButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour ce cycle.";
    }

    private void Run()
    {
        if (stone == null || dopant < 0 || !quote.IsValid) return;
        var gem = stone;
        var current = quote;
        var chosen = dopant;
        var degrees = temperature;
        var length = Days;
        ConfirmDialog.Show("Lancer la diffusion",
            $"Enterrer {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans la poudre ?\n\n{DiffusionCatalog.Dopants[chosen].Name}, {degrees} °C, {length} jours\nPénétration estimée : {current.DepthMm:F2} mm\nPrix : {current.Fee} €\n\nLa couleur ne s'enlève qu'au repolissage, et seulement si elle est restée en surface.",
            "Lancer", () => StartRun(chosen, degrees, length, gem, current));
    }

    private void StartRun(int chosen, int degrees, int length, GemInstance gem, DiffusionQuote current)
    {
        var state = DiffusionState.Ensure();
        if (state == null) return;
        if (state.TryStart(chosen, degrees, length, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.Flame);
            technicianText.text = $"« La pierre est dans la poudre et le four monte. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
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
        var state = DiffusionState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.Outcome == DiffusionOutcome.Ruined ? Sfx.Id.Error : Sfx.Id.Success);
            var star = result.Star ? "\nUn astérisme est apparu à la surface (visible en cabochon)." : "";
            technicianText.text = result.Outcome == DiffusionOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »\nCouleur : {gem.ColorName.ToLowerInvariant()}{star}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.Outcome == DiffusionOutcome.Ruined ? "Pierre brûlée par le four." : "Pierre sortie du four, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The tube of the furnace pulses at the colour of its temperature, faster during a cycle; the powder shimmers a little.
    private void AnimateFurnace()
    {
        var state = DiffusionState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var degrees = state != null && state.Job != null ? state.Job.Temperature : temperature;

        var pulse = 0.78f + 0.22f * Mathf.Sin(Time.unscaledTime * (running ? 4f : 1.6f));
        var glow = tubeGlow.color;
        glow.a = Mathf.Lerp(0.25f, 0.95f, Mathf.Clamp01((degrees - 1200f) / 800f)) * (running ? pulse : 0.9f);
        tubeGlow.color = glow;

        var shimmer = 0.92f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f);
        powderImage.color = new Color(powderBase.r * shimmer, powderBase.g * shimmer, powderBase.b * shimmer, 1f);
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
            var color = HeatColor(900 + Mathf.RoundToInt(1100f * x / (w - 1)));
            for (var y = 0; y < h; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("DiffusionPanel", typeof(RectTransform));
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

        Txt("Title", "Four de diffusion", 22, TextAnchor.UpperLeft, Steel, new Vector2(24f, -20f), new Vector2(520f, 30f));
        Txt("Subtitle", "Faire entrer une couleur dans un corindon : fer, titane, chrome, nickel, béryllium.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Corindon à diffuser", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.03f, 0.04f, 0.08f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.50f, 0.56f, 0.68f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 12, TextAnchor.UpperLeft, new Color(0.94f, 0.96f, 1f), new Vector2(24f, -260f), new Vector2(172f, 118f));

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -386f), new Vector2(170f, PanelHeight - 386f - 24f), new Color(0.03f, 0.04f, 0.08f, 1f));
        technicianText = Txt("Technician", "", 11, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 386f - 40f));

        BuildFurnace();
        BuildControls();

        // The cycle in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(FurnaceX, -416f), new Vector2(PanelWidth - FurnaceX - 24f, 74f), new Color(0.03f, 0.04f, 0.08f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(FurnaceX + 10f, -424f), new Vector2(PanelWidth - FurnaceX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(FurnaceX + 4f, -476f), new Vector2(PanelWidth - FurnaceX - 24f - 8f, 6f), new Color(0.08f, 0.09f, 0.14f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Ember, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(FurnaceX, -496f), new Vector2(PanelWidth - FurnaceX - 24f, 92f), new Color(0.03f, 0.04f, 0.08f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(FurnaceX + 10f, -502f), new Vector2(PanelWidth - FurnaceX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer la diffusion", 14, new Color(0.62f, 0.32f, 0.12f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The tube furnace seen from the front, with the crucible full of powder and the stone buried in it; under it, the section of the stone.
    private void BuildFurnace()
    {
        var origin = new Vector2(FurnaceX, FurnaceY);
        RuntimeUIKit.MakeBox("FurnaceFrame", panel.transform, origin, new Vector2(FurnaceW, FurnaceH), new Color(0.09f, 0.10f, 0.16f, 1f));

        // The steel tube with its glowing opening.
        RuntimeUIKit.MakeBox("Tube", panel.transform, origin + new Vector2(14f, -14f), new Vector2(FurnaceW - 28f, 150f), new Color(0.46f, 0.50f, 0.58f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("TubePlate", panel.transform, origin + new Vector2(22f, -22f), new Vector2(FurnaceW - 44f, 134f), new Color(0.58f, 0.62f, 0.70f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Opening", panel.transform, origin + new Vector2(36f, -34f), new Vector2(FurnaceW - 72f, 110f), new Color(0.04f, 0.03f, 0.04f, 1f)).raycastTarget = false;
        var glow = RuntimeUIKit.MakeBox("TubeGlow", panel.transform, origin + new Vector2(42f, -40f), new Vector2(FurnaceW - 84f, 98f), new Color(1f, 0.5f, 0.2f, 0.8f), false);
        glow.sprite = RuntimeUIKit.Glow;
        glow.raycastTarget = false;
        tubeGlow = glow;

        // The crucible: the powder fills it, the stone is half buried.
        RuntimeUIKit.MakeBox("Crucible", panel.transform, origin + new Vector2(FurnaceW / 2f - 54f, -102f), new Vector2(108f, 36f), new Color(0.84f, 0.76f, 0.66f, 1f)).raycastTarget = false;
        var powder = RuntimeUIKit.MakeBox("Powder", panel.transform, origin + new Vector2(FurnaceW / 2f - 48f, -106f), new Vector2(96f, 24f), new Color(0.55f, 0.55f, 0.58f, 1f));
        powder.raycastTarget = false;
        powderImage = powder;
        var stoneGo = new GameObject("FurnaceStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), origin + new Vector2(FurnaceW / 2f - 28f, -82f), new Vector2(56f, 40f));
        stoneInFurnace = stoneGo.GetComponent<Image>();
        stoneInFurnace.preserveAspect = true;
        stoneInFurnace.raycastTarget = false;
        stoneGo.SetActive(false);
        // The powder covers the lower half of the stone: drawn on top of it.
        var cover = RuntimeUIKit.MakeBox("PowderCover", panel.transform, origin + new Vector2(FurnaceW / 2f - 48f, -100f), new Vector2(96f, 14f), new Color(0.55f, 0.55f, 0.58f, 1f));
        cover.raycastTarget = false;
        cover.gameObject.AddComponent<PowderTint>().Source = () => powderImage.color;

        // The readout strip under the tube.
        RuntimeUIKit.MakeBox("Strip", panel.transform, origin + new Vector2(14f, -168f), new Vector2(FurnaceW - 28f, 24f), new Color(0.20f, 0.22f, 0.28f, 1f)).raycastTarget = false;
        readoutText = Txt("Readout", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(14f, -168f), new Vector2(FurnaceW - 28f, 24f));

        // The section of the stone: the layer the element reaches is the coloured rim around the colour of the stone.
        var outer = RuntimeUIKit.MakeBox("SectionOuter", panel.transform, origin + new Vector2(FurnaceW / 2f - 75f, -208f), new Vector2(150f, 84f), new Color(0.20f, 0.22f, 0.30f, 1f), false);
        outer.sprite = CircleSprite();
        outer.raycastTarget = false;
        sectionOuter = outer;
        var innerGo = new GameObject("SectionInner", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(innerGo.GetComponent<RectTransform>(), origin + new Vector2(FurnaceW / 2f, -250f), new Vector2(0f, 0f));
        sectionInner = innerGo.GetComponent<Image>();
        sectionInner.sprite = CircleSprite();
        sectionInner.raycastTarget = false;
        innerGo.SetActive(false);
        sectionText = Txt("SectionText", "Coupe de la pierre", 11, TextAnchor.UpperCenter, TextSoft, origin + new Vector2(8f, -278f), new Vector2(FurnaceW - 16f, 18f));
    }

    private void BuildControls()
    {
        Txt("DopantTitle", "Élément à diffuser", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -80f), new Vector2(260f, 18f));
        for (var i = 0; i < DiffusionCatalog.DopantCount; i++)
        {
            var captured = i;
            var item = DiffusionCatalog.Dopants[i];
            var button = RuntimeUIKit.MakeButton("Dopant" + i, panel.transform, "", 12, ButtonIdle, new Vector2(ColumnX, -100f - i * 33f), new Vector2(262f, 29f));
            button.onClick.AddListener(() => SelectDopant(captured));
            dopantImages[i] = button.GetComponent<Image>();

            RuntimeUIKit.MakeBox("Swatch", button.transform, new Vector2(6f, -4f), new Vector2(6f, 21f), item.Powder, false).raycastTarget = false;
            var name = RuntimeUIKit.MakeText("Name", button.transform, item.Name, 12, TextAnchor.UpperLeft, Color.white, new Vector2(20f, -1f), new Vector2(236f, 15f));
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            var detail = RuntimeUIKit.MakeText("Detail", button.transform, item.Detail, 10, TextAnchor.UpperLeft, new Color(0.72f, 0.80f, 0.92f), new Vector2(20f, -15f), new Vector2(236f, 13f));
            detail.raycastTarget = false;
            var label = button.GetComponentInChildren<Text>();
            if (label != null && label.text.Length == 0) label.gameObject.SetActive(false);
        }

        temperatureText = Txt("Temperature", "", 13, TextAnchor.UpperLeft, Ember, new Vector2(ColumnX, -274f), new Vector2(262f, 20f));
        temperatureText.fontStyle = FontStyle.Bold;
        slider = MakeSlider(new Vector2(ColumnX, -294f));
        Txt("DurationTitle", "Durée dans le four", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -326f), new Vector2(260f, 18f));
        for (var i = 0; i < DiffusionCatalog.DurationDays.Length; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Duration" + i, panel.transform, $"{DiffusionCatalog.DurationDays[i]} jours", 12, ButtonIdle,
                new Vector2(ColumnX + i * 67f, -346f), new Vector2(62f, 28f));
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
        result.minValue = DiffusionCatalog.MinTemperature / DiffusionCatalog.TemperatureStep;
        result.maxValue = DiffusionCatalog.MaxTemperature / DiffusionCatalog.TemperatureStep;
        result.value = temperature / (float)DiffusionCatalog.TemperatureStep;
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

    // The cover of the powder follows the colour of the powder of the crucible.
    private class PowderTint : MonoBehaviour
    {
        public System.Func<Color> Source;

        private void Update()
        {
            if (Source == null) return;
            var image = GetComponent<Image>();
            if (image != null) image.color = Source();
        }
    }
}
