using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The irradiator of the treatments laboratory: the stone is put in a shielded cell and bombarded. The player drops a stone in the slot, picks a
// source (gamma rays from cobalt-60, electrons from a linear accelerator, neutrons from a reactor) and a dose. The window lists what can be tried
// on the stone, but not the settings: they are in the course. The neutrons make the stone radioactive: it has to cool for days before it comes
// out. One stone at a time. Built at runtime.
public class IrradiationUI : MonoBehaviour
{
    public static IrradiationUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float CellX = 206f;
    private const float CellY = -92f;
    private const float CellW = 300f;
    private const float CellH = 300f;
    private const float ColumnX = 530f;
    private const float SliderWidth = 262f;
    private const int ParticleCount = 16;

    private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.06f, 0.98f);
    private static readonly Color Yellow = new Color(0.98f, 0.82f, 0.15f);
    private static readonly Color Magenta = new Color(0.90f, 0.28f, 0.68f);
    private static readonly Color Gold = new Color(0.98f, 0.86f, 0.50f);
    private static readonly Color TextSoft = new Color(0.90f, 0.92f, 0.86f);
    private static readonly Color ButtonIdle = new Color(0.14f, 0.16f, 0.12f, 1f);
    private static readonly Color ButtonOn = new Color(0.46f, 0.30f, 0.08f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text readoutText;
    private Text doseText;
    private Image barFill;
    private Image sourceGlow;
    private Image stoneInCell;
    private Image rodsImage;
    private Image tubeImage;
    private Image coreImage;
    private Image coreRightImage;
    private Slider slider;
    private Button runButton;
    private Button collectButton;
    private readonly Image[] sourceImages = new Image[3];
    private readonly Image[] particles = new Image[ParticleCount];
    private readonly float[] particlePhase = new float[ParticleCount];

    private GemInstance stone;
    private IrradiationSource source = IrradiationSource.Gamma;
    private int dose = 300;
    private IrradiationQuote quote;
    private static Sprite stripeSprite;
    private static Sprite trefoilSprite;
    private static Sprite dotSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        IrradiationState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<IrradiationUI>();
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
        var state = IrradiationState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (IrradiationState.Instance != null) IrradiationState.Instance.OnJobChanged -= RefreshIfOpen;
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
        source = IrradiationSource.Gamma;
        dose = 300;
        slider.SetValueWithoutNotify(dose / (float)IrradiationCatalog.DoseStep);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez une pierre. Je vous dis ce qu'on peut en tirer, mais pas comment : la source et la dose, c'est dans votre cours. Les neutrons, eux, rendent la pierre radioactive. »";
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
        AnimateCell();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = IrradiationCatalog.Refusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        technicianText.text = "« Voilà ce qu'on peut tenter sur cette pierre. À vous de choisir la source et la dose. Trop de dose, et la pierre noircit. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        Refresh();
    }

    private void SelectSource(int index)
    {
        source = (IrradiationSource)index;
        Sfx.Play(Sfx.Id.Click);
        Refresh();
    }

    private void OnDose(float value)
    {
        dose = Mathf.Clamp(Mathf.RoundToInt(value) * IrradiationCatalog.DoseStep, IrradiationCatalog.MinDose, IrradiationCatalog.MaxDose);
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
        RefreshCell();
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nune pierre\nici";
            stoneInfoText.text = "S'irradient : quartz, béryl,\ntopaze, tourmaline, perle,\nsaphir incolore ou jaune.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        var lines = new System.Text.StringBuilder();
        lines.Append($"{stone.Definition.displayName}  {stone.CaratWeight:F2} ct\n");
        lines.Append($"{stone.ColorName}  ·  {stone.FinalValue} €\n\n<color=#f5c06a>On peut tenter :</color>\n");
        foreach (var goal in IrradiationCatalog.GoalsFor(stone)) lines.Append($"· {goal}\n");
        stoneInfoText.text = lines.ToString();
    }

    private void RefreshControls()
    {
        for (var i = 0; i < sourceImages.Length; i++) sourceImages[i].color = i == (int)source ? ButtonOn : ButtonIdle;
        doseText.text = $"Dose : {dose} kGy";
        slider.SetValueWithoutNotify(dose / (float)IrradiationCatalog.DoseStep);
    }

    // The source of the cell is lit: the rods of cobalt for the gamma rays, the beam tube for the electrons, the core of the reactor for the neutrons.
    private void RefreshCell()
    {
        var job = IrradiationState.Instance != null ? IrradiationState.Instance.Job : null;
        var shownSource = job != null ? job.Source : source;
        var shown = job != null ? job.Stone : stone;
        stoneInCell.gameObject.SetActive(shown != null);
        if (shown != null) stoneInCell.sprite = shown.DisplaySprite;

        var glow = IrradiationCatalog.Sources[(int)shownSource].Glow;
        rodsImage.gameObject.SetActive(shownSource == IrradiationSource.Gamma);
        tubeImage.gameObject.SetActive(shownSource == IrradiationSource.Electrons);
        coreImage.gameObject.SetActive(shownSource == IrradiationSource.Neutrons);
        coreRightImage.gameObject.SetActive(shownSource == IrradiationSource.Neutrons);
        rodsImage.color = glow;
        tubeImage.color = glow;
        coreImage.color = glow;
        coreRightImage.color = glow;
        sourceGlow.color = new Color(glow.r, glow.g, glow.b, 0.35f);
        foreach (var particle in particles) particle.color = new Color(glow.r, glow.g, glow.b, 0f);
    }

    private void RefreshJob()
    {
        var state = IrradiationState.Instance;
        var job = state != null ? state.Job : null;
        collectButton.gameObject.SetActive(job != null);
        barFill.transform.parent.gameObject.SetActive(job != null);

        if (job == null)
        {
            jobText.text = "L'irradiateur est libre.";
            readoutText.text = "Cellule fermée  ·  0 mSv/h";
            return;
        }

        var ready = state.IsReady;
        var days = state.DaysRemaining;
        var info = IrradiationCatalog.Sources[(int)job.Source];
        jobText.text = ready
            ? $"{job.Stone.Definition.displayName} — {info.Name}, {job.Dose} kGy : la radioactivité est retombée, la pierre peut sortir."
            : $"{job.Stone.Definition.displayName} — {info.Name}, {job.Dose} kGy : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        readoutText.text = ready ? "Pierre refroidie" : $"Exposition ou refroidissement  ·  {Mathf.Max(1, days)} j";
        var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
        var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
        var bar = (RectTransform)barFill.transform;
        bar.sizeDelta = new Vector2((PanelWidth - CellX - 24f - 8f) * (ready ? 1f : done), bar.sizeDelta.y);
        barFill.color = ready ? Gold : Yellow;
        collectButton.interactable = ready;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void RefreshQuote()
    {
        var state = IrradiationState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "L'irradiateur est occupé : sortez d'abord la pierre en cours.";
            return;
        }
        if (stone == null)
        {
            quoteText.text = "Posez une pierre dans la case, puis réglez la source et la dose.";
            return;
        }

        quote = IrradiationCatalog.GetQuote(source, dose, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var info = IrradiationCatalog.Sources[(int)source];
        var reactionColor = quote.Reaction < 0.2f ? "#ff9a70" : quote.Reaction < 0.45f ? "#f5c629" : "#a8e0a0";
        var cooling = quote.CoolingDays > 0 ? $" + refroidissement {quote.CoolingDays} j" : "";
        quoteText.text =
            $"<b>{info.Name}</b>  <color=#f0c05a>{info.Detail}</color>  ·  {dose} kGy\n" +
            $"Durée : exposition {quote.ExposureDays} j{cooling} = {quote.Days} j  (sortie le {GameCalendar.Short(Today + quote.Days)})    Irradiation : {quote.Fee} €\n" +
            $"Réaction attendue : <color={reactionColor}>{quote.ReactionLabel}</color>\n" +
            $"« {quote.Advice} »";
        if (quote.Overdose) quoteText.text += "\n« Cette dose est bien trop forte pour cette pierre. »";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        runButton.gameObject.SetActive(true);
        runButton.interactable = money >= quote.Fee;
        if (money < quote.Fee) quoteText.text += "\nVous n'avez pas assez d'argent pour cette irradiation.";
    }

    private void Run()
    {
        if (stone == null || !quote.IsValid) return;
        var gem = stone;
        var current = quote;
        var chosen = source;
        var amount = dose;
        var info = IrradiationCatalog.Sources[(int)chosen];
        ConfirmDialog.Show("Lancer l'irradiation",
            $"Mettre {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans la cellule ?\n\n{info.Name} ({info.Detail}), {amount} kGy\nDurée : {current.Days} jour{(current.Days > 1 ? "s" : "")}\nPrix : {current.Fee} €\n\nUne pierre irradiée ne s'irradie pas deux fois : seul un chauffage peut inverser la couleur.",
            "Lancer", () => StartRun(chosen, amount, gem, current));
    }

    private void StartRun(IrradiationSource chosen, int amount, GemInstance gem, IrradiationQuote current)
    {
        var state = IrradiationState.Ensure();
        if (state == null) return;
        if (state.TryStart(chosen, amount, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.ComputerBoot);
            technicianText.text = $"« La cellule est fermée, le faisceau s'allume. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
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
        var state = IrradiationState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.IsRuined ? Sfx.Id.Error : Sfx.Id.Success);
            var fade = result.FadeDays > 0 ? $"\nCouleur instable : elle pâlira dans {result.FadeDays} jours environ." : "";
            technicianText.text = result.Outcome == IrradiationOutcome.NoEffect
                ? $"« {result.Message} »"
                : $"« {result.Message} »\nCouleur : {gem.ColorName.ToLowerInvariant()}{fade}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.IsRuined ? "Pierre trop irradiée." : "Pierre sortie de la cellule, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The source of the cell pulses, particles go from the source to the stone (and faster during a cycle), and the stone shimmers.
    private void AnimateCell()
    {
        var state = IrradiationState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var shownSource = state != null && state.Job != null ? state.Job.Source : source;
        var active = running || stone != null;
        var glow = IrradiationCatalog.Sources[(int)shownSource].Glow;
        var strength = running ? 1f : (stone != null ? 0.45f : 0.12f);

        var pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * (running ? 6f : 2f));
        sourceGlow.color = new Color(glow.r, glow.g, glow.b, 0.15f + 0.5f * strength * pulse);

        var origin = new Vector2(CellX, CellY);
        var target = origin + new Vector2(CellW / 2f, -148f);
        for (var i = 0; i < particles.Length; i++)
        {
            particlePhase[i] += Time.unscaledDeltaTime * (running ? 1.1f : 0.5f) * (0.7f + 0.06f * (i % 7));
            var t = particlePhase[i] % 1f;
            Vector2 from;
            switch (shownSource)
            {
                case IrradiationSource.Electrons:
                    from = origin + new Vector2(CellW / 2f + ((i % 3) - 1) * 4f, -48f);
                    break;
                case IrradiationSource.Neutrons:
                    from = origin + new Vector2(i % 2 == 0 ? 52f : CellW - 52f, -70f - (i % 4) * 22f);
                    break;
                default:
                    from = origin + new Vector2(70f + (i * 37) % 160, -214f);
                    break;
            }
            var position = Vector2.Lerp(from, target + new Vector2(((i * 13) % 30) - 15f, ((i * 7) % 20) - 10f), t);
            particles[i].rectTransform.anchoredPosition = position;
            var visible = active && (running || i < 7);
            particles[i].color = new Color(glow.r, glow.g, glow.b, visible ? Mathf.Sin(t * Mathf.PI) * 0.9f : 0f);
        }

        if (stoneInCell.gameObject.activeSelf)
        {
            var rect = stoneInCell.rectTransform;
            rect.anchoredPosition = new Vector2(CellX + CellW / 2f, CellY - 124f + Mathf.Sin(Time.unscaledTime * 2f) * 1.5f);
            var shimmer = 0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * (running ? 9f : 3f));
            stoneInCell.color = new Color(shimmer, shimmer, shimmer, 1f);
        }
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private static Sprite DotSprite()
    {
        if (dotSprite == null) dotSprite = RuntimeUIKit.MakeRoundedSprite(16, 8);
        return dotSprite;
    }

    // The radiation trefoil: a yellow disc, three black blades and a black centre.
    private static Sprite TrefoilSprite()
    {
        if (trefoilSprite != null) return trefoilSprite;
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var yellow = new Color32(250, 208, 38, 255);
        var black = new Color32(28, 28, 34, 255);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - (size - 1) / 2f;
                var dy = y - (size - 1) / 2f;
                var radius = Mathf.Sqrt(dx * dx + dy * dy);
                if (radius > 31f) { texture.SetPixel(x, y, Color.clear); continue; }
                var color = yellow;
                if (radius < 7f) color = black;
                else if (radius >= 11f && radius <= 27f)
                {
                    var angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 90f, 120f);
                    if (angle < 60f) color = black;
                }
                if (radius > 29f) color = black;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        trefoilSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return trefoilSprite;
    }

    // Yellow and magenta, the colours of the radiation signs, tiled along the top and the bottom of the window.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int w = 32;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var yellow = new Color32(250, 208, 38, 255);
        var magenta = new Color32(200, 50, 150, 255);
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                texture.SetPixel(x, y, ((x + y) % 16) < 8 ? yellow : magenta);
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("IrradiationPanel", typeof(RectTransform));
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

        var sign = new GameObject("Trefoil", typeof(RectTransform), typeof(Image));
        sign.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(sign.GetComponent<RectTransform>(), new Vector2(PanelWidth - 150f, -14f), new Vector2(34f, 34f));
        var signImage = sign.GetComponent<Image>();
        signImage.sprite = TrefoilSprite();
        signImage.raycastTarget = false;

        Txt("Title", "Irradiateur", 22, TextAnchor.UpperLeft, Yellow, new Vector2(24f, -20f), new Vector2(520f, 30f));
        Txt("Subtitle", "Des rayonnements de haute énergie créent des centres colorés : la source et la dose décident.", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 200f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Pierre à irradier", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.03f, 0.04f, 0.03f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.55f, 0.58f, 0.48f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 11, TextAnchor.UpperLeft, new Color(0.96f, 0.96f, 0.90f), new Vector2(24f, -258f), new Vector2(172f, 124f));
        stoneInfoText.supportRichText = true;

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -386f), new Vector2(170f, PanelHeight - 386f - 24f), new Color(0.04f, 0.05f, 0.04f, 1f));
        technicianText = Txt("Technician", "", 11, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 386f - 40f));

        BuildCell();
        BuildControls();

        // The cycle in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(CellX, -416f), new Vector2(PanelWidth - CellX - 24f, 74f), new Color(0.04f, 0.05f, 0.04f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(CellX + 10f, -424f), new Vector2(PanelWidth - CellX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(CellX + 4f, -476f), new Vector2(PanelWidth - CellX - 24f - 8f, 6f), new Color(0.10f, 0.11f, 0.08f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Yellow, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(CellX, -496f), new Vector2(PanelWidth - CellX - 24f, 92f), new Color(0.04f, 0.05f, 0.04f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(CellX + 10f, -502f), new Vector2(PanelWidth - CellX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer l'irradiation", 14, new Color(0.62f, 0.34f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The shielded cell seen from the front: thick concrete walls, a window onto the cell with the stone on a turntable, the source of the chosen
    // kind (rods of cobalt at the bottom, the tube of the accelerator at the top, the core of the reactor on the sides), the particles that cross
    // the cell, and under it the readout of the dosimeter.
    private void BuildCell()
    {
        var origin = new Vector2(CellX, CellY);
        RuntimeUIKit.MakeBox("CellFrame", panel.transform, origin, new Vector2(CellW, CellH), new Color(0.11f, 0.12f, 0.10f, 1f));
        RuntimeUIKit.MakeBox("Concrete", panel.transform, origin + new Vector2(12f, -12f), new Vector2(CellW - 24f, 232f), new Color(0.42f, 0.43f, 0.40f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Lead", panel.transform, origin + new Vector2(24f, -24f), new Vector2(CellW - 48f, 208f), new Color(0.30f, 0.32f, 0.34f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Inside", panel.transform, origin + new Vector2(34f, -34f), new Vector2(CellW - 68f, 188f), new Color(0.03f, 0.04f, 0.05f, 1f)).raycastTarget = false;

        var glow = RuntimeUIKit.MakeBox("SourceGlow", panel.transform, origin + new Vector2(40f, -40f), new Vector2(CellW - 80f, 176f), new Color(0.5f, 0.9f, 0.5f, 0.35f), false);
        glow.sprite = RuntimeUIKit.Glow;
        glow.raycastTarget = false;
        sourceGlow = glow;

        // The three kinds of source, only one of them shown at a time.
        rodsImage = MakeSourcePiece("Rods", origin + new Vector2(64f, -190f), new Vector2(CellW - 128f, 18f));
        tubeImage = MakeSourcePiece("Tube", origin + new Vector2(CellW / 2f - 7f, -34f), new Vector2(14f, 34f));
        coreImage = MakeSourcePiece("Core", origin + new Vector2(40f, -76f), new Vector2(22f, 82f));
        coreRightImage = MakeSourcePiece("CoreRight", origin + new Vector2(CellW - 62f, -76f), new Vector2(22f, 82f));

        // The turntable and the stone.
        RuntimeUIKit.MakeBox("Turntable", panel.transform, origin + new Vector2(CellW / 2f - 44f, -168f), new Vector2(88f, 12f), new Color(0.58f, 0.60f, 0.64f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("TurntableStem", panel.transform, origin + new Vector2(CellW / 2f - 6f, -178f), new Vector2(12f, 20f), new Color(0.40f, 0.42f, 0.46f, 1f), false).raycastTarget = false;
        var stoneGo = new GameObject("CellStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        var stoneRect = stoneGo.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(stoneRect, origin + new Vector2(CellW / 2f, -124f), new Vector2(60f, 50f));
        stoneRect.pivot = new Vector2(0.5f, 1f);
        stoneInCell = stoneGo.GetComponent<Image>();
        stoneInCell.preserveAspect = true;
        stoneInCell.raycastTarget = false;
        stoneGo.SetActive(false);

        // The particles that cross the cell.
        for (var i = 0; i < particles.Length; i++)
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            var particleRect = go.GetComponent<RectTransform>();
            RuntimeUIKit.SetTopLeft(particleRect, Vector2.zero, new Vector2(4f, 4f));
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = DotSprite();
            image.raycastTarget = false;
            particles[i] = image;
            particlePhase[i] = (i * 0.29f) % 1f;
        }

        // The radiation sign on the door, and the readout of the dosimeter.
        var sign = new GameObject("DoorSign", typeof(RectTransform), typeof(Image));
        sign.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(sign.GetComponent<RectTransform>(), origin + new Vector2(CellW - 58f, -4f), new Vector2(28f, 28f));
        var signImage = sign.GetComponent<Image>();
        signImage.sprite = TrefoilSprite();
        signImage.raycastTarget = false;

        RuntimeUIKit.MakeBox("Strip", panel.transform, origin + new Vector2(12f, -250f), new Vector2(CellW - 24f, 38f), new Color(0.20f, 0.22f, 0.20f, 1f)).raycastTarget = false;
        readoutText = Txt("Readout", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(12f, -250f), new Vector2(CellW - 24f, 38f));
    }

    private Image MakeSourcePiece(string name, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void BuildControls()
    {
        Txt("SourceTitle", "Source de rayonnement", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -80f), new Vector2(260f, 18f));
        for (var i = 0; i < IrradiationCatalog.Sources.Length; i++)
        {
            var captured = i;
            var info = IrradiationCatalog.Sources[i];
            var button = RuntimeUIKit.MakeButton("Source" + i, panel.transform, "", 12, ButtonIdle, new Vector2(ColumnX, -100f - i * 52f), new Vector2(262f, 46f));
            button.onClick.AddListener(() => SelectSource(captured));
            sourceImages[i] = button.GetComponent<Image>();

            RuntimeUIKit.MakeBox("Swatch", button.transform, new Vector2(6f, -6f), new Vector2(6f, 34f), info.Glow, false).raycastTarget = false;
            var name = RuntimeUIKit.MakeText("Name", button.transform, info.Name, 13, TextAnchor.UpperLeft, Color.white, new Vector2(20f, -3f), new Vector2(236f, 18f));
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            var detail = RuntimeUIKit.MakeText("Detail", button.transform, info.Detail, 11, TextAnchor.UpperLeft, new Color(0.80f, 0.84f, 0.74f), new Vector2(20f, -21f), new Vector2(236f, 14f));
            detail.raycastTarget = false;
            var label = button.GetComponentInChildren<Text>();
            if (label != null && label.text.Length == 0) label.gameObject.SetActive(false);
        }

        doseText = Txt("Dose", "", 16, TextAnchor.UpperLeft, Yellow, new Vector2(ColumnX, -266f), new Vector2(262f, 24f));
        doseText.fontStyle = FontStyle.Bold;
        slider = MakeSlider(new Vector2(ColumnX, -292f));
        Txt("DoseMin", $"{IrradiationCatalog.MinDose} kGy", 10, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -318f), new Vector2(80f, 14f));
        var max = Txt("DoseMax", $"{IrradiationCatalog.MaxDose} kGy", 10, TextAnchor.UpperRight, TextSoft, new Vector2(ColumnX + SliderWidth - 80f, -318f), new Vector2(80f, 14f));
        max.horizontalOverflow = HorizontalWrapMode.Overflow;
        Txt("Safety", "Les neutrons rendent la pierre radioactive : elle doit refroidir avant de sortir. Une pierre trop dosée noircit.", 11, TextAnchor.UpperLeft,
            new Color(0.80f, 0.84f, 0.70f), new Vector2(ColumnX, -344f), new Vector2(262f, 56f));
    }

    // A slider for the dose, in steps of 50 kGy.
    private Slider MakeSlider(Vector2 position)
    {
        var go = new GameObject("DoseSlider", typeof(RectTransform), typeof(Slider));
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
        backgroundImage.type = Image.Type.Tiled;
        backgroundImage.color = Color.white;

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
        result.minValue = IrradiationCatalog.MinDose / IrradiationCatalog.DoseStep;
        result.maxValue = IrradiationCatalog.MaxDose / IrradiationCatalog.DoseStep;
        result.value = dose / (float)IrradiationCatalog.DoseStep;
        result.onValueChanged.AddListener(OnDose);
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
