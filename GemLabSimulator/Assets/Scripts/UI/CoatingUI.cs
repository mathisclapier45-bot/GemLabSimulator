using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The vacuum chamber of the treatments laboratory: a thin layer of metal is deposited on the pavilion of a stone. The player drops a stone in
// the slot, picks one or two targets (gold, titanium, cobalt) or a special coating, the thickness of the layer, and reads the quote. The stone
// shown in the chamber already wears the colour and the rainbow sheen of the chosen coating. One stone at a time; it comes out some days of
// the game later. Built at runtime.
public class CoatingUI : MonoBehaviour
{
    public static CoatingUI Instance { get; private set; }

    private const float PanelWidth = 820f;
    private const float PanelHeight = 612f;
    private const float ChamberX = 206f;
    private const float ChamberY = -92f;
    private const float ChamberW = 300f;
    private const float ChamberH = 300f;
    private const float ColumnX = 530f;

    private static readonly Color PanelColor = new Color(0.04f, 0.06f, 0.11f, 0.98f);
    private static readonly Color Cyan = new Color(0.50f, 0.85f, 0.98f);
    private static readonly Color Violet = new Color(0.78f, 0.62f, 1.00f);
    private static readonly Color Gold = new Color(0.98f, 0.84f, 0.45f);
    private static readonly Color TextSoft = new Color(0.82f, 0.88f, 0.96f);
    private static readonly Color ButtonIdle = new Color(0.14f, 0.18f, 0.28f, 1f);
    private static readonly Color ButtonOn = new Color(0.30f, 0.26f, 0.56f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text technicianText;
    private Text quoteText;
    private Text jobText;
    private Text gaugeText;
    private Image barFill;
    private Image glowImage;
    private Image chamberStone;
    private RectTransform platter;
    private Button runButton;
    private Button collectButton;
    private readonly Button[] targetButtons = new Button[CoatingCatalog.TargetCount];
    private readonly Image[] targetImages = new Image[CoatingCatalog.TargetCount];
    private readonly Button[] thicknessButtons = new Button[CoatingCatalog.ThicknessCount];
    private readonly Image[] thicknessImages = new Image[CoatingCatalog.ThicknessCount];
    private Text thicknessHint;

    private GemInstance stone;
    private readonly List<int> selected = new List<int>();
    private int thickness = 1;
    private CoatQuote quote;
    private readonly Dictionary<string, Sprite> previews = new Dictionary<string, Sprite>();
    private static Sprite stripeSprite;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void TryOpen()
    {
        CoatingState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<CoatingUI>();
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
        var state = CoatingState.Ensure();
        if (state != null) state.OnJobChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (CoatingState.Instance != null) CoatingState.Instance.OnJobChanged -= RefreshIfOpen;
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
        selected.Clear();
        thickness = 1;
        previews.Clear();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        InventoryUI.Instance?.Open();
        technicianText.text = "« Posez une pierre à facettes, choisissez une ou deux cibles et l'épaisseur : la chambre vous montre ce que donnerait la couche. »";
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
        AnimateChamber();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = CoatingCatalog.StoneRefusal(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            technicianText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        previews.Clear();
        technicianText.text = "« Une pierre prête pour le vide. Quelle couche voulez-vous ? Le titane donne l'effet « mystique » arc-en-ciel. »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        previews.Clear();
        Refresh();
    }

    private void ToggleTarget(int index)
    {
        Sfx.Play(Sfx.Id.Click);
        if (selected.Contains(index))
        {
            selected.Remove(index);
        }
        else if (!CoatingCatalog.Targets[index].IsMetal)
        {
            // A special coating is done alone.
            selected.Clear();
            selected.Add(index);
        }
        else
        {
            selected.RemoveAll(t => !CoatingCatalog.Targets[t].IsMetal);
            selected.Add(index);
            if (selected.Count > 2) selected.RemoveAt(0);
        }
        previews.Clear();
        Refresh();
    }

    private void SetThickness(int index)
    {
        thickness = index;
        Sfx.Play(Sfx.Id.Click);
        previews.Clear();
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
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            previews.Clear();
        }

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
            stoneInfoText.text = "Se revêtent : topaze, quartz,\nzircone, béryl, tanzanite,\nkunzite, tourmaline ; avec\nune laque : opale, ambre.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{stone.Definition.displayName}\n{stone.CaratWeight:F2} ct — {stone.CutStyleName.ToLowerInvariant()}\nCouleur : {stone.ColorName.ToLowerInvariant()}\n" +
            $"Valeur estimée : {stone.FinalValue} €\n(clic sur la case pour retirer)";
    }

    private void RefreshControls()
    {
        for (var i = 0; i < targetImages.Length; i++)
            targetImages[i].color = selected.Contains(i) ? ButtonOn : ButtonIdle;
        for (var i = 0; i < thicknessImages.Length; i++)
            thicknessImages[i].color = i == thickness ? ButtonOn : ButtonIdle;
        thicknessHint.text = CoatingCatalog.ThicknessHint(thickness);
    }

    // The colour of the plasma follows the targets; the stone in the chamber shows what the coating would give it.
    private void RefreshChamber()
    {
        var job = CoatingState.Instance != null ? CoatingState.Instance.Job : null;
        if (job != null)
        {
            chamberStone.gameObject.SetActive(true);
            chamberStone.sprite = job.Stone.DisplaySprite;
            return;
        }

        var preview = PreviewSprite();
        chamberStone.gameObject.SetActive(preview != null);
        chamberStone.sprite = preview;
    }

    private Sprite PreviewSprite()
    {
        if (stone == null) return null;
        if (!quote.IsValid || selected.Count == 0) return stone.DisplaySprite;

        var key = $"{CoatingCatalog.KeyOf(selected)}|{thickness}";
        if (previews.TryGetValue(key, out var cached) && cached != null) return cached;

        var value = new GemColorValue(Mathf.Repeat(quote.Hue, 360f), Mathf.Clamp01(quote.Saturation), Mathf.Clamp01(quote.Brightness));
        var body = GemColorSystem.BodyColor(value);
        var facet = GemColorSystem.FacetColor(value);
        var sprite = quote.Iridescence >= 0.25f
            ? GemIconFactory.GetIridescent(stone.Definition, stone.ShapeName, stone.CutStyleName, body, facet, quote.Iridescence)
            : GemIconFactory.Get(stone.Definition, stone.ShapeName, stone.CutStyleName, body, facet);
        previews[key] = sprite;
        return sprite;
    }

    private void RefreshJob()
    {
        var state = CoatingState.Instance;
        var job = state != null ? state.Job : null;
        collectButton.gameObject.SetActive(job != null);
        barFill.transform.parent.gameObject.SetActive(job != null);

        if (job == null)
        {
            jobText.text = "La chambre est libre.";
            gaugeText.text = "Vide : 10^-5 mbar  ·  prête";
            return;
        }

        var ready = state.IsReady;
        var days = state.DaysRemaining;
        var names = new List<string>();
        foreach (var t in job.Targets) names.Add(CoatingCatalog.Targets[t].Name);
        jobText.text = ready
            ? $"{job.Stone.Definition.displayName} — {string.Join(" + ", names)} : cycle terminé, la chambre a refroidi."
            : $"{job.Stone.Definition.displayName} — {string.Join(" + ", names)} : encore {days} jour{(days > 1 ? "s" : "")} (sortie le {GameCalendar.Short(job.ReadyDay)}).";
        gaugeText.text = ready ? "Cycle terminé" : "Dépôt en cours...";
        var span = Mathf.Max(1, job.ReadyDay - job.StartDay);
        var done = Mathf.Clamp01((float)(job.ReadyDay - job.StartDay - days) / span);
        var bar = (RectTransform)barFill.transform;
        bar.sizeDelta = new Vector2((PanelWidth - ChamberX - 24f - 8f) * (ready ? 1f : done), bar.sizeDelta.y);
        barFill.color = ready ? Gold : Violet;
        collectButton.interactable = ready;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    private void RefreshQuote()
    {
        var state = CoatingState.Instance;
        var busy = state != null && state.Job != null;
        runButton.gameObject.SetActive(false);
        quote = default;

        if (busy)
        {
            quoteText.text = "La chambre est occupée : sortez d'abord la pierre en cours.";
            return;
        }
        if (stone == null)
        {
            quoteText.text = selected.Count == 0
                ? "Posez une pierre dans la case, puis choisissez les cibles."
                : $"{CoatingCatalog.Targets[selected[0]].Name} : {CoatingCatalog.Targets[selected[0]].Description}\n\nPosez une pierre dans la case pour obtenir un devis.";
            return;
        }

        quote = CoatingCatalog.GetQuote(selected, thickness, stone);
        if (!quote.IsValid)
        {
            quoteText.text = $"« {quote.Reason} »";
            return;
        }

        var adhesionColor = quote.Adhesion < 0.4f ? "#ff9a70" : quote.Adhesion < 0.6f ? "#f5c629" : "#a8e0a0";
        quoteText.text =
            $"<b>{quote.CoatingName}</b>  <color=#c8b0ff>{CoatingCatalog.ThicknessNames[thickness].ToLowerInvariant()}</color>\n" +
            $"Durée : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}  (sortie le {GameCalendar.Short(Today + quote.Days)})    Cibles et vide : {quote.Fee} €\n" +
            $"Adhérence : <color={adhesionColor}>{quote.AdhesionLabel}</color>    Irisation : <color=#c8b0ff>{quote.IridescenceLabel}</color>\n" +
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
        var targets = new List<int>(selected);
        var level = thickness;
        ConfirmDialog.Show("Lancer le dépôt",
            $"Mettre {gem.Definition.displayName} ({gem.CaratWeight:F2} ct) dans la chambre ?\n\n{current.CoatingName}, couche {CoatingCatalog.ThicknessNames[level].ToLowerInvariant()}\nDurée : {current.Days} jour{(current.Days > 1 ? "s" : "")}\nPrix : {current.Fee} €\nAdhérence : {current.AdhesionLabel}\n\nLe revêtement ne s'enlève pas.",
            "Lancer", () => StartRun(targets, level, gem, current));
    }

    private void StartRun(List<int> targets, int level, GemInstance gem, CoatQuote current)
    {
        var state = CoatingState.Ensure();
        if (state == null) return;
        if (state.TryStart(targets, level, gem, current, out var message))
        {
            Sfx.Play(Sfx.Id.ComputerBoot);
            technicianText.text = $"« La chambre pompe, puis le plasma s'allume. Revenez le {GameCalendar.Short(Today + current.Days)}. »";
            stone = null;
            previews.Clear();
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    private void Collect()
    {
        var state = CoatingState.Instance;
        var job = state != null ? state.Job : null;
        if (job == null) return;

        var before = job.Stone.FinalValue;
        var gem = job.Stone;
        if (state.TryCollect(out var result, out var message))
        {
            Sfx.Play(result.Outcome == CoatOutcome.Ruined ? Sfx.Id.Error : Sfx.Id.Success);
            technicianText.text = $"« {result.Message} »\nCouleur : {gem.ColorName.ToLowerInvariant()}\nValeur : {before} € → {gem.FinalValue} €";
            ToolFeedbackUI.Instance?.ShowMessage(result.Outcome == CoatOutcome.Ruined ? "Revêtement écaillé." : "Pierre sortie de la chambre, de retour dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            technicianText.text = $"« {message} »";
        }
    }

    // ---- Animation -------------------------------------------------------------------------------------------

    // The plasma pulses in the colour of the targets; the stone turns on the platter, seen from the side (it narrows as it goes round).
    private void AnimateChamber()
    {
        var state = CoatingState.Instance;
        var running = state != null && state.Job != null && !state.IsReady;
        var color = new Color(0.5f, 0.5f, 0.8f);
        if (state != null && state.Job != null)
        {
            color = AverageGlow(state.Job.Targets);
        }
        else if (selected.Count > 0)
        {
            color = AverageGlow(selected);
        }

        var active = running || (state != null && state.Job == null && selected.Count > 0);
        var pulse = 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * (running ? 5f : 2.2f));
        color.a = active ? pulse * (running ? 1f : 0.55f) : 0.12f;
        glowImage.color = color;

        if (chamberStone.gameObject.activeSelf)
        {
            var spin = running || selected.Count > 0 ? Time.unscaledTime * 1.4f : 0f;
            chamberStone.rectTransform.localScale = new Vector3(0.25f + 0.75f * Mathf.Abs(Mathf.Cos(spin)), 1f, 1f);
            chamberStone.rectTransform.anchoredPosition = new Vector2(ChamberX + ChamberW / 2f, ChamberY - 112f + Mathf.Sin(Time.unscaledTime * 1.7f) * 3f);
        }
        platter.localScale = new Vector3(1f, 1f, 1f);
    }

    private static Color AverageGlow(IReadOnlyList<int> targets)
    {
        if (targets.Count == 0) return new Color(0.5f, 0.5f, 0.8f);
        var sum = Color.black;
        foreach (var t in targets) sum += CoatingCatalog.Targets[t].Glow;
        return sum / targets.Count;
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    // A strip of the colours of the rainbow, tiled along the top and the bottom of the window.
    private static Sprite StripeSprite()
    {
        if (stripeSprite != null) return stripeSprite;
        const int w = 96;
        const int h = 10;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (var x = 0; x < w; x++)
        {
            var color = Color.HSVToRGB(x / (float)w, 0.55f, 0.95f);
            for (var y = 0; y < h; y++) texture.SetPixel(x, y, color);
        }
        texture.Apply();
        stripeSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return stripeSprite;
    }

    private void BuildPanel()
    {
        panel = new GameObject("CoatingPanel", typeof(RectTransform));
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

        Txt("Title", "Chambre de dépôt sous vide", 22, TextAnchor.UpperLeft, Cyan, new Vector2(24f, -20f), new Vector2(520f, 30f));
        Txt("Subtitle", "Une couche de métal très fine sur la culasse : la topaze ou le quartz « mystique ».", 12, TextAnchor.UpperLeft, TextSoft,
            new Vector2(24f, -50f), new Vector2(PanelWidth - 160f, 20f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -22f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the stone.
        Txt("SlotTitle", "Pierre à revêtir", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -80f), new Vector2(170f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -102f), new Vector2(150f, 150f), new Color(0.02f, 0.03f, 0.06f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -117f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.45f, 0.52f, 0.62f), new Vector2(24f, -102f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 12, TextAnchor.UpperLeft, new Color(0.90f, 0.94f, 1f), new Vector2(24f, -260f), new Vector2(172f, 110f));

        // The technician's words.
        RuntimeUIKit.MakeBox("TechnicianBack", panel.transform, new Vector2(24f, -384f), new Vector2(170f, PanelHeight - 384f - 24f), new Color(0.02f, 0.04f, 0.08f, 1f));
        technicianText = Txt("Technician", "", 12, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.80f), new Vector2(32f, -392f), new Vector2(154f, PanelHeight - 384f - 40f));

        BuildChamber();
        BuildControls();

        // The run in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(ChamberX, -416f), new Vector2(PanelWidth - ChamberX - 24f, 74f), new Color(0.02f, 0.04f, 0.08f, 1f));
        jobText = Txt("Job", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(ChamberX + 10f, -424f), new Vector2(PanelWidth - ChamberX - 200f, 34f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Sortir la pierre", 13, new Color(0.65f, 0.48f, 0.10f, 1f),
            new Vector2(PanelWidth - 24f - 172f, -424f), new Vector2(160f, 30f));
        collectButton.onClick.AddListener(Collect);
        var barBack = RuntimeUIKit.MakeBox("BarBack", panel.transform, new Vector2(ChamberX + 4f, -476f), new Vector2(PanelWidth - ChamberX - 24f - 8f, 6f), new Color(0.05f, 0.07f, 0.12f, 1f), false);
        barBack.raycastTarget = false;
        barFill = RuntimeUIKit.MakeBox("BarFill", barBack.transform, Vector2.zero, new Vector2(0f, 6f), Violet, false);
        barFill.raycastTarget = false;

        // The quote and the button.
        RuntimeUIKit.MakeBox("QuoteBack", panel.transform, new Vector2(ChamberX, -496f), new Vector2(PanelWidth - ChamberX - 24f, 92f), new Color(0.02f, 0.04f, 0.08f, 1f));
        quoteText = Txt("Quote", "", 12, TextAnchor.UpperLeft, Color.white, new Vector2(ChamberX + 10f, -502f), new Vector2(PanelWidth - ChamberX - 220f, 84f));
        quoteText.supportRichText = true;
        runButton = RuntimeUIKit.MakeButton("Run", panel.transform, "Lancer le dépôt", 14, new Color(0.36f, 0.30f, 0.70f, 1f),
            new Vector2(PanelWidth - 24f - 184f, -508f), new Vector2(172f, 34f));
        runButton.onClick.AddListener(Run);
    }

    // The glass bell of the chamber: a steel base, the glow of the plasma, the turning platter and the stone on it.
    private void BuildChamber()
    {
        var origin = new Vector2(ChamberX, ChamberY);
        RuntimeUIKit.MakeBox("ChamberFrame", panel.transform, origin, new Vector2(ChamberW, ChamberH), new Color(0.08f, 0.11f, 0.18f, 1f));
        RuntimeUIKit.MakeBox("Glass", panel.transform, origin + new Vector2(16f, -14f), new Vector2(ChamberW - 32f, 236f), new Color(0.55f, 0.72f, 0.90f, 1f)).raycastTarget = false;
        RuntimeUIKit.MakeBox("Vacuum", panel.transform, origin + new Vector2(21f, -19f), new Vector2(ChamberW - 42f, 226f), new Color(0.015f, 0.025f, 0.05f, 1f)).raycastTarget = false;

        var glowGo = new GameObject("Plasma", typeof(RectTransform), typeof(Image));
        glowGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(glowGo.GetComponent<RectTransform>(), origin + new Vector2(30f, -30f), new Vector2(ChamberW - 60f, 200f));
        glowImage = glowGo.GetComponent<Image>();
        glowImage.sprite = RuntimeUIKit.Glow;
        glowImage.raycastTarget = false;
        glowImage.color = new Color(0.5f, 0.5f, 0.8f, 0.12f);

        // The two target holders on the sides, and the platter at the bottom.
        RuntimeUIKit.MakeBox("HolderLeft", panel.transform, origin + new Vector2(24f, -70f), new Vector2(14f, 90f), new Color(0.50f, 0.54f, 0.62f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("HolderRight", panel.transform, origin + new Vector2(ChamberW - 38f, -70f), new Vector2(14f, 90f), new Color(0.50f, 0.54f, 0.62f, 1f), false).raycastTarget = false;
        var platterBox = RuntimeUIKit.MakeBox("Platter", panel.transform, origin + new Vector2(ChamberW / 2f - 70f, -196f), new Vector2(140f, 14f), new Color(0.62f, 0.66f, 0.74f, 1f));
        platterBox.raycastTarget = false;
        platter = platterBox.rectTransform;
        RuntimeUIKit.MakeBox("Stem", panel.transform, origin + new Vector2(ChamberW / 2f - 8f, -210f), new Vector2(16f, 20f), new Color(0.42f, 0.46f, 0.54f, 1f), false).raycastTarget = false;

        var stoneGo = new GameObject("ChamberStone", typeof(RectTransform), typeof(Image));
        stoneGo.transform.SetParent(panel.transform, false);
        var stoneRect = stoneGo.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(stoneRect, origin + new Vector2(ChamberW / 2f - 48f, -112f), new Vector2(96f, 84f));
        chamberStone = stoneGo.GetComponent<Image>();
        chamberStone.preserveAspect = true;
        chamberStone.raycastTarget = false;
        stoneGo.SetActive(false);
        // The stone turns around its middle: the animation puts it back at the centre of the chamber every frame.
        stoneRect.pivot = new Vector2(0.5f, 1f);

        // The steel base, with the gauge.
        RuntimeUIKit.MakeBox("Base", panel.transform, origin + new Vector2(8f, -254f), new Vector2(ChamberW - 16f, 38f), new Color(0.30f, 0.34f, 0.42f, 1f));
        gaugeText = Txt("Gauge", "", 12, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.75f), origin + new Vector2(8f, -254f), new Vector2(ChamberW - 16f, 38f));
        gaugeText.supportRichText = true;
    }

    private void BuildControls()
    {
        Txt("TargetsTitle", "Cibles dans la chambre", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -80f), new Vector2(260f, 18f));
        for (var i = 0; i < CoatingCatalog.TargetCount; i++)
        {
            var captured = i;
            var target = CoatingCatalog.Targets[i];
            var column = i % 2;
            var row = i / 2;
            var button = RuntimeUIKit.MakeButton("Target" + i, panel.transform, "", 12, ButtonIdle,
                new Vector2(ColumnX + column * 134f, -102f - row * 54f), new Vector2(128f, 48f));
            button.onClick.AddListener(() => ToggleTarget(captured));
            targetButtons[i] = button;
            targetImages[i] = button.GetComponent<Image>();

            // The colour of the target as a small bar at the left of its button.
            RuntimeUIKit.MakeBox("Swatch", button.transform, new Vector2(6f, -8f), new Vector2(5f, 32f), target.Glow, false).raycastTarget = false;
            var name = RuntimeUIKit.MakeText("Name", button.transform, target.Name, 12, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -6f), new Vector2(106f, 18f));
            name.fontStyle = FontStyle.Bold;
            name.raycastTarget = false;
            var detail = RuntimeUIKit.MakeText("Detail", button.transform, target.Detail, 10, TextAnchor.UpperLeft, new Color(0.72f, 0.78f, 0.90f), new Vector2(18f, -25f), new Vector2(106f, 18f));
            detail.raycastTarget = false;
            // The default label of the button is empty: hide it behind the others.
            var label = button.GetComponentInChildren<Text>();
            if (label != null && label.text.Length == 0) label.gameObject.SetActive(false);
        }

        Txt("ThicknessTitle", "Épaisseur de la couche", 13, TextAnchor.UpperLeft, Gold, new Vector2(ColumnX, -270f), new Vector2(260f, 18f));
        for (var i = 0; i < CoatingCatalog.ThicknessCount; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Thickness" + i, panel.transform, CoatingCatalog.ThicknessNames[i], 12, ButtonIdle,
                new Vector2(ColumnX + i * 90f, -292f), new Vector2(84f, 30f));
            button.onClick.AddListener(() => SetThickness(captured));
            thicknessButtons[i] = button;
            thicknessImages[i] = button.GetComponent<Image>();
        }
        thicknessHint = Txt("ThicknessHint", "", 11, TextAnchor.UpperLeft, TextSoft, new Vector2(ColumnX, -328f), new Vector2(266f, 40f));
        Txt("Rules", "Deux métaux au plus, ou une laque seule. Les métaux se déposent sur les facettes de la culasse.", 11, TextAnchor.UpperLeft,
            new Color(0.62f, 0.70f, 0.84f), new Vector2(ColumnX, -360f), new Vector2(266f, 44f));
    }

    private Text Txt(string name, string content, int size, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, panel.transform, content, size, alignment, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
