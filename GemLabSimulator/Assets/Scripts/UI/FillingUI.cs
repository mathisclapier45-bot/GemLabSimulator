using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The autoclave of the treatments laboratory: the impregnation and the filling of the fissures and the pores of a stone. The player drops a stone
// in the slot, picks a product (oil, wax, resin, glass, flux) and a procedure (simple bath, vacuum, vacuum and pressure), reads the quote
// (duration, price, how well the product fills this material, risk, durability) and starts the cycle. One stone at a time; it comes out some
// days of the game later. Built at runtime.
public class FillingUI : MonoBehaviour
{
    public static FillingUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float ChamberX = 206f;
    private const float ChamberY = -92f;
    private const float ChamberW = 300f;
    private const float ChamberH = 300f;
    private const float ColumnX = 530f;
    private const int BubbleCount = 9;

    private static readonly Color PanelColor = new Color(0.04f, 0.09f, 0.10f, 0.98f);
    private static readonly Color Copper = new Color(0.96f, 0.68f, 0.40f);
    private static readonly Color Teal = new Color(0.50f, 0.88f, 0.84f);
    private static readonly Color Gold = new Color(0.98f, 0.84f, 0.45f);
    private static readonly Color TextSoft = new Color(0.84f, 0.92f, 0.92f);
    private static readonly Color ButtonIdle = new Color(0.12f, 0.20f, 0.22f, 1f);
    private static readonly Color ButtonOn = new Color(0.45f, 0.30f, 0.16f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text gaugeText;
    private Text procedureHint;
    private Image barFill;
    private Image liquidImage;
    private Image stoneInChamber;
    private RectTransform needle;
    private Button runButton;
    private Button collectButton;
    private readonly Image[] productImages = new Image[FillingCatalog.ProductCount];
    private readonly Image[] procedureImages = new Image[FillingCatalog.ProcedureCount];
    private readonly Image[] bubbles = new Image[BubbleCount];
    private readonly float[] bubblePhase = new float[BubbleCount];

    private GemInstance stone;
    private int product = -1;
    private int procedure = 1;
    private FillQuote quote;
    private static Sprite stripeSprite;
    private static Sprite circleSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        FillingState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<FillingUI>();
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
        var state = FillingState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (FillingState.Instance != null) FillingState.Instance.OnJobChanged -= RefreshIfOpen;
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
        product = -1;
        procedure = 1;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez une pierre fissurée ou poreuse, choisissez le produit, puis le procédé. Chaque matière a ses produits : je vous le dirai. »";
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
        AnimateAutoclave();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = FillingCatalog.StoneRefusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        technicianText.text = "« Une pierre prête pour l'autoclave. Quel produit voulez-vous ? Regardez les conseils pour chaque matière. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        Refresh();
    }

    private void SelectProduct(int index)
    {
        product = product == index ? -1 : index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private void SelectProcedure(int index)
    {
        procedure = index;
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

    private void Refresh()
    {
        // The stone can leave the bag behind our back: forget it then.
        if (stone != null && !ContainsGem(stone)) stone = null;

        RefreshSlot();
        RefreshControls();
        RefreshJob();
        RefreshQuote();
        RefreshChamber();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nune pierre\nici";
            stoneInfoText.text = "S'imprègnent : émeraude, rubis,\nsaphir, topaze, tourmaline,\nturquoise, jade, lapis, corail,\nopale, feldspath...";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{stone.Definition.displayName}\n{stone.CaratWeight:F2} ct — pureté : {stone.ClarityGrade.ToLowerInvariant()}\nTraitement : {stone.TreatmentGrade.ToLowerInvariant()}\n" +
            $"Valeur estimée : {stone.FinalValue} €\n(clic sur la case pour retirer)";
    }

    private void RefreshControls()
    {
        for (var i = 0; i < productImages.Length; i++)
            productImages[i].color = i == product ? ButtonOn : ButtonIdle;
        for (var i = 0; i < procedureImages.Length; i++)
            procedureImages[i].color = i == procedure ? ButtonOn : ButtonIdle;
        procedureHint.text = FillingCatalog.ProcedureHint(procedure);
    }

    // The liquid of the porthole takes the colour of the chosen product; the stone in it is the one in the slot, or the one in the cycle.
    private void RefreshChamber()
    {
        var job = FillingState.Instance != null ? FillingState.Instance.Job : null;
        var shown = job != null ? job.Stone : stone;
        stoneInChamber.gameObject.SetActive(shown != null);
        if (shown != null) stoneInChamber.sprite = shown.DisplaySprite;

        var shownProduct = job != null ? job.Product : product;
        var color = shownProduct >= 0 ? FillingCatalog.Products[shownProduct].Liquid : new Color(0.55f, 0.65f, 0.70f);
        color.a = 0.85f;
        liquidImage.color = color;
        var tint = color;
        tint = Color.Lerp(tint, Color.white, 0.6f);
        foreach (var bubble in bubbles) bubble.color = new Color(tint.r, tint.g, tint.b, 0.7f);
    }

    private void RefreshJob()
    {
        var state = FillingState.Instance;
        var job = state != null ? state.Job : null;
        collectButton.gameObject.SetActive(job != null);
        barFill.transform.parent.gameObject.SetActive(job != null);

        if (job == null)
        {
            jobText.text = "L'autoclave est libre.";
            gaugeText.text = "Pression : 1,0 bar  ·  prête";
            return;
        }

        var ready = state.IsReady;
        var days = state.DaysRemaining;
        jobText.text = ready
            ? $"{job.Stone.Definition.displayName} — {FillingCatalog.Products[job.Product].Name} : cycle terminé, la pression est retombée."
            : $"{job.Stone.Definition.displayName} — {FillingCatalog.Products[job.Product].Name} : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        gaugeText.text = ready ? "Cycle terminé" : "Cycle en cours...";
        var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
        var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
        var bar = (RectTransform)barFill.transform;
        bar.sizeDelta = new Vector2((PanelWidth - ChamberX - 24f - 8f) * (ready ? 1f : done), bar.sizeDelta.y);
        barFill.color = ready ? Gold : Copper;
        collectButton.interactable = ready;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void RefreshQuote()
    {
        var state = FillingState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "L'autoclave est occupé : sortez d'abord la pierre en cours.";
            return;
        }
        if (product < 0)
        {
            quoteText.text = stone == null
                ? "Posez une pierre dans la case, puis choisissez un produit."
                : "Choisissez un produit à droite.";
            return;
        }

        var item = FillingCatalog.Products[product];
        if (stone == null)
        {
            quoteText.text = $"<b>{item.Name}</b>  <color=#80e0d6>{item.Detail}</color>\n\n{item.Description}\n\nPosez une pierre dans la case pour obtenir un devis.";
            return;
        }

        quote = FillingCatalog.GetQuote(product, procedure, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var effColor = quote.Effectiveness < 0.3f ? "#ff9a70" : quote.Effectiveness < 0.55f ? "#f5c629" : "#a8e0a0";
        var riskColor = quote.Risk < 0.2f ? "#a8e0a0" : quote.Risk < 0.4f ? "#f5c629" : "#ff7060";
        quoteText.text =
            $"<b>{item.Name}</b>  <color=#80e0d6>{FillingCatalog.ProcedureNames[procedure].ToLowerInvariant()}</color>\n" +
            $"Durée : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}  (sortie le {GameCalendar.Short(Today + quote.Days)})    Produit et cycle : {quote.Fee} €\n" +
            $"Remplissage : <color={effColor}>{quote.EffectivenessLabel}</color>   Risque : <color={riskColor}>{quote.RiskLabel}</color>   Tenue : {(quote.Durable ? "durable" : "temporaire")}\n" +
            $"« {quote.Advice} »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        runButton.gameObject.SetActive(true);
        runButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour ce cycle.";
    }

    private void Run()
    {
        if (stone == null || product < 0 || !quote.IsValid) return;
        var gem = stone;
        var current = quote;
        var chosen = product;
        var level = procedure;
        var item = FillingCatalog.Products[chosen];
        ConfirmDialog.Show("Lancer le cycle",
            $"Mettre {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans l'autoclave ?\n\n{item.Name}, {FillingCatalog.ProcedureNames[level].ToLowerInvariant()}\nDurée : {current.Days} jour{(current.Days > 1 ? "s" : "")}\nPrix : {current.Fee} €\nRisque : {current.RiskLabel}\n\nUne pierre remplie ne se traite pas une seconde fois.",
            "Lancer", () => StartRun(chosen, level, gem, current));
    }

    private void StartRun(int chosen, int level, GemInstance gem, FillQuote current)
    {
        var state = FillingState.Ensure();
        if (state == null) return;
        if (state.TryStart(chosen, level, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.ComputerBoot);
            technicianText.text = $"« L'autoclave monte en pression. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
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
        var state = FillingState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var beforeClarity = job.Stone.ClarityGrade;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.Outcome == FillOutcome.Ruined ? Sfx.Id.Error : Sfx.Id.Success);
            var clarity = gem.ClarityGrade != beforeClarity ? $"\nPureté : {beforeClarity.ToLowerInvariant()} → {gem.ClarityGrade.ToLowerInvariant()}" : "";
            technicianText.text = result.Outcome == FillOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »{clarity}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.Outcome == FillOutcome.Ruined ? "Pierre abîmée par le remplissage." : "Pierre sortie de l'autoclave, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // Bubbles rise in the porthole, faster during a cycle; the stone drifts in the liquid; the needle of the gauge shakes, and climbs during a cycle.
    private void AnimateAutoclave()
    {
        var state = FillingState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var origin = new Vector2(ChamberX + 62f, ChamberY - 76f);
        var speed = running ? 0.55f : 0.18f;

        for (var i = 0; i < bubbles.Length; i++)
        {
            bubblePhase[i] += Time.unscaledDeltaTime * speed * (0.6f + 0.1f * (i % 5));
            var t = bubblePhase[i] % 1f;
            var rect = bubbles[i].rectTransform;
            rect.anchoredPosition = origin + new Vector2(14f + (i * 23) % 100 + Mathf.Sin(t * 8f + i) * 3f, -(104f - t * 92f));
            var c = bubbles[i].color;
            c.a = Mathf.Sin(t * Mathf.PI) * 0.8f;
            bubbles[i].color = c;
            bubbles[i].enabled = running || i < 4;
        }

        if (stoneInChamber.gameObject.activeSelf)
        {
            var rect = stoneInChamber.rectTransform;
            rect.anchoredPosition = new Vector2(ChamberX + ChamberW / 2f - 30f + Mathf.Sin(Time.unscaledTime * 1.2f) * 5f, ChamberY - 118f + Mathf.Sin(Time.unscaledTime * 1.7f) * 4f);
        }

        var target = running ? 0.78f : 0.12f;
        var shake = Mathf.Sin(Time.unscaledTime * (running ? 9f : 2f)) * (running ? 0.03f : 0.015f);
        var pressure = Mathf.Clamp01(target + shake);
        needle.localRotation = Quaternion.Euler(0f, 0f, 120f - pressure * 240f);
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private static Sprite CircleSprite()
    {
        if (circleSprite == null) circleSprite = RuntimeUIKit.MakeRoundedSprite(64, 32);
        return circleSprite;
    }

    // A strip of the colours of the products, tiled along the top and the bottom of the window.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int segment = 6;
        var w = segment * FillingCatalog.ProductCount;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (var x = 0; x < w; x++)
        {
            var color = FillingCatalog.Products[x / segment].Liquid;
            for (var y = 0; y < h; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("FillingPanel", typeof(RectTransform));
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

        Txt("Title", "Autoclave d'imprégnation", 22, TextAnchor.UpperLeft, Copper, new Vector2(24f, -20f), new Vector2(520f, 30f));
        Txt("Subtitle", "Remplir les fissures et les pores d'une pierre : huile, cire, résine, verre ou fondant.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Pierre à imprégner", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.02f, 0.05f, 0.06f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.45f, 0.58f, 0.60f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 12, TextAnchor.UpperLeft, new Color(0.92f, 0.98f, 0.96f), new Vector2(24f, -260f), new Vector2(172f, 118f));

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -384f), new Vector2(170f, PanelHeight - 384f - 24f), new Color(0.02f, 0.05f, 0.06f, 1f));
        technicianText = Txt("Technician", "", 12, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 384f - 40f));

        BuildAutoclave();
        BuildControls();

        // The cycle in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(ChamberX, -416f), new Vector2(PanelWidth - ChamberX - 24f, 74f), new Color(0.02f, 0.05f, 0.06f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(ChamberX + 10f, -424f), new Vector2(PanelWidth - ChamberX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(ChamberX + 4f, -476f), new Vector2(PanelWidth - ChamberX - 24f - 8f, 6f), new Color(0.05f, 0.09f, 0.10f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Copper, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(ChamberX, -496f), new Vector2(PanelWidth - ChamberX - 24f, 92f), new Color(0.02f, 0.05f, 0.06f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(ChamberX + 10f, -502f), new Vector2(PanelWidth - ChamberX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer le cycle", 14, new Color(0.60f, 0.38f, 0.14f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The steel cylinder of the autoclave seen from the front: a round porthole with the liquid, the bubbles and the stone, the bolts around it,
    // a pressure gauge with its needle, and the base with the readout.
    private void BuildAutoclave()
    {
        var origin = new Vector2(ChamberX, ChamberY);
        RuntimeUIKit.MakeBox("AutoclaveFrame", panel.transform, origin, new Vector2(ChamberW, ChamberH), new Color(0.07f, 0.12f, 0.14f, 1f));
        RuntimeUIKit.MakeBox("Body", panel.transform, origin + new Vector2(14f, -14f), new Vector2(ChamberW - 28f, 232f), new Color(0.46f, 0.52f, 0.58f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Plate", panel.transform, origin + new Vector2(22f, -22f), new Vector2(ChamberW - 44f, 216f), new Color(0.58f, 0.64f, 0.70f, 1f)).raycastTarget = false;

        // The porthole.
        var ring = RuntimeUIKit.MakeBox("PortholeRing", panel.transform, origin + new Vector2(58f, -52f), new Vector2(164f, 164f), new Color(0.30f, 0.34f, 0.40f, 1f), false);
        ring.sprite = CircleSprite();
        ring.raycastTarget = false;
        var glass = RuntimeUIKit.MakeBox("Porthole", panel.transform, origin + new Vector2(66f, -60f), new Vector2(148f, 148f), new Color(0.02f, 0.05f, 0.07f, 1f), false);
        glass.sprite = CircleSprite();
        glass.raycastTarget = false;
        var liquid = RuntimeUIKit.MakeBox("Liquid", panel.transform, origin + new Vector2(72f, -76f), new Vector2(136f, 126f), new Color(0.55f, 0.65f, 0.70f, 0.85f), false);
        liquid.sprite = CircleSprite();
        liquid.raycastTarget = false;
        liquidImage = liquid;

        for (var i = 0; i < bubbles.Length; i++)
        {
            var go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), Vector2.zero, new Vector2(3f + i % 3, 3f + i % 3));
            var image = go.GetComponent<Image>();
            image.sprite = RuntimeUIKit.MakeRoundedSprite(16, 8);
            image.raycastTarget = false;
            bubbles[i] = image;
            bubblePhase[i] = (i * 0.37f) % 1f;
        }

        var stoneGo = new GameObject("ChamberStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(stoneGo.GetComponent<RectTransform>(), origin + new Vector2(ChamberW / 2f - 30f, -118f), new Vector2(60f, 50f));
        stoneInChamber = stoneGo.GetComponent<Image>();
        stoneInChamber.preserveAspect = true;
        stoneInChamber.raycastTarget = false;
        stoneGo.SetActive(false);

        // The bolts around the porthole.
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Mathf.PI / 4f;
            var bolt = RuntimeUIKit.MakeBox("Bolt", panel.transform,
                origin + new Vector2(140f + Mathf.Cos(angle) * 88f - 4f, -134f - Mathf.Sin(angle) * 88f + 4f), new Vector2(8f, 8f), new Color(0.78f, 0.82f, 0.88f, 1f), false);
            bolt.sprite = RuntimeUIKit.MakeRoundedSprite(16, 8);
            bolt.raycastTarget = false;
        }

        // The pressure gauge in the corner, with its needle.
        var dialRing = RuntimeUIKit.MakeBox("DialRing", panel.transform, origin + new Vector2(ChamberW - 84f, -26f), new Vector2(60f, 60f), new Color(0.22f, 0.26f, 0.30f, 1f), false);
        dialRing.sprite = CircleSprite();
        dialRing.raycastTarget = false;
        var dial = RuntimeUIKit.MakeBox("Dial", panel.transform, origin + new Vector2(ChamberW - 80f, -30f), new Vector2(52f, 52f), new Color(0.94f, 0.94f, 0.90f, 1f), false);
        dial.sprite = CircleSprite();
        dial.raycastTarget = false;
        var needleGo = new GameObject("Needle", typeof(RectTransform), typeof(Image));
        needleGo.transform.SetParent(panel.transform, false);
        needle = needleGo.GetComponent<RectTransform>();
        needle.anchorMin = needle.anchorMax = new Vector2(0f, 1f);
        needle.pivot = new Vector2(0.5f, 0f);
        needle.sizeDelta = new Vector2(3f, 21f);
        needle.anchoredPosition = origin + new Vector2(ChamberW - 54f, -56f);
        var needleImage = needleGo.GetComponent<Image>();
        needleImage.color = new Color(0.80f, 0.15f, 0.12f, 1f);
        needleImage.raycastTarget = false;

        // The base with the readout.
        RuntimeUIKit.MakeBox("Base", panel.transform, origin + new Vector2(8f, -254f), new Vector2(ChamberW - 16f, 38f), new Color(0.26f, 0.30f, 0.34f, 1f));
        gaugeText = Txt("Gauge", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(8f, -254f), new Vector2(ChamberW - 16f, 38f));
    }

    private void BuildControls()
    {
        Txt("ProductsTitle", "Produit de remplissage", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -80f), new Vector2(260f, 18f));
        for (var i = 0; i < FillingCatalog.ProductCount; i++)
        {
            var captured = i;
            var item = FillingCatalog.Products[i];
            var button = RuntimeUIKit.MakeButton("Product" + i, panel.transform, "", 12, ButtonIdle,
                new Vector2(ColumnX, -100f - i * 34f), new Vector2(266f, 30f));
            button.onClick.AddListener(() => SelectProduct(captured));
            productImages[i] = button.GetComponent<Image>();

            RuntimeUIKit.MakeBox("Swatch", button.transform, new Vector2(6f, -4f), new Vector2(6f, 22f), item.Liquid, false).raycastTarget = false;
            var name = RuntimeUIKit.MakeText("Name", button.transform, item.Name, 12, TextAnchor.UpperLeft, Color.white, new Vector2(20f, -1f), new Vector2(240f, 16f));
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            var detail = RuntimeUIKit.MakeText("Detail", button.transform, item.Detail, 10, TextAnchor.UpperLeft, new Color(0.74f, 0.86f, 0.86f), new Vector2(20f, -15f), new Vector2(240f, 14f));
            detail.raycastTarget = false;
            var label = button.GetComponentInChildren<Text>();
            if (label != null && label.text.Length == 0) label.gameObject.SetActive(false);
        }

        Txt("ProcedureTitle", "Procédé", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -342f), new Vector2(260f, 18f));
        for (var i = 0; i < FillingCatalog.ProcedureCount; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Procedure" + i, panel.transform, FillingCatalog.ProcedureNames[i], 11, ButtonIdle,
                new Vector2(ColumnX + i * 90f, -360f), new Vector2(86f, 24f));
            button.onClick.AddListener(() => SelectProcedure(captured));
            procedureImages[i] = button.GetComponent<Image>();
        }
        procedureHint = Txt("ProcedureHint", "", 11, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -388f), new Vector2(266f, 14f));
        procedureHint.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private Text Txt(string name, string content, int size, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, panel.transform, content, size, alignment, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
