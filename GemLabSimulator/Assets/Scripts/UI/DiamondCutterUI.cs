using UnityEngine;
using UnityEngine.UI;

// Window of a diamond cutter of the cutting centre (5th floor of the Diamond Quarter): hand over a rough diamond, choose the cut, read the
// quote, order, and collect the finished stones when they are ready. Built at runtime.
public class DiamondCutterUI : MonoBehaviour
{
    public static DiamondCutterUI Instance { get; private set; }

    private const float PanelWidth = 720f;
    private const float PanelHeight = 570f;

    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.07f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color SelectedColor = new Color(0.30f, 0.55f, 0.35f, 1f);
    private static readonly Color DisabledColor = new Color(0.16f, 0.16f, 0.18f, 1f);

    private GameObject panel;
    private Text titleText;
    private Text specialtyText;
    private Text pitchText;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text quoteText;
    private Text greetingText;
    private Button confirmButton;
    private Text jobText;
    private Button collectButton;
    private Button[] optionButtons;
    private Text[] optionLabels;

    private int cutterIndex = -1;
    private GemInstance stone;
    private int selectedOption = -1;
    private DiamondCutQuote currentQuote;

    private DiamondCutter Cutter => cutterIndex >= 0 ? DiamondCutting.Cutters[cutterIndex] : null;

    public static void TryOpen(int index)
    {
        if (index < 0 || index >= DiamondCutting.Cutters.Length) return;
        DiamondCuttingState.Ensure();

        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<DiamondCutterUI>();
        }
        Instance?.Open(index);
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
        var state = DiamondCuttingState.Ensure();
        if (state != null) state.OnJobsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (DiamondCuttingState.Instance != null) DiamondCuttingState.Instance.OnJobsChanged -= RefreshIfOpen;
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void RefreshIfOpen(int _)
    {
        RefreshIfOpen();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    public void Open(int index)
    {
        cutterIndex = index;
        stone = null;
        selectedOption = -1;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();

        var cutter = Cutter;
        titleText.text = cutter.Name;
        specialtyText.text = cutter.Specialty;
        pitchText.text = cutter.Pitch;
        greetingText.text = "« Posez un diamant brut dans la case, puis choisissez la taille. »";
        for (var i = 0; i < optionButtons.Length; i++)
        {
            var exists = i < cutter.Options.Length;
            optionButtons[i].gameObject.SetActive(exists);
            if (exists) optionLabels[i].text = cutter.Options[i].Label;
        }
        Refresh();
    }

    public void Close()
    {
        cutterIndex = -1;
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        var refusal = DiamondCutting.Refusal(gem, Cutter);
        if (!string.IsNullOrEmpty(refusal))
        {
            greetingText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        selectedOption = -1;
        greetingText.text = "« Un beau cristal. Quelle taille voulez-vous en tirer ? »";
        Refresh();
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        selectedOption = -1;
        Refresh();
    }

    private void SelectOption(int index)
    {
        selectedOption = index;
        Refresh();
    }

    private void Refresh()
    {
        if (Cutter == null) return;

        // The stone can leave the bag behind our back: forget it then.
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
            selectedOption = -1;
        }

        RefreshSlot();
        for (var i = 0; i < optionButtons.Length && i < Cutter.Options.Length; i++)
        {
            optionButtons[i].interactable = stone != null;
            optionButtons[i].GetComponent<Image>().color = stone == null ? DisabledColor : i == selectedOption ? SelectedColor : ButtonColor;
        }
        RefreshQuote();
        RefreshJob();
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
            if (ReferenceEquals(g, gem)) return true;
        return false;
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nun diamant\nbrut ici";
            stoneInfoText.text = "";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        DiamondDealerState.YieldEstimate(stone, out var low, out var high);
        var form = string.IsNullOrEmpty(stone.RoughForm) ? "cristal" : stone.RoughForm.ToLowerInvariant();
        stoneInfoText.text =
            $"{stone.CaratWeight:F2} ct brut — {form}\n" +
            $"Teinte {stone.ColorName.ToLowerInvariant()}\n" +
            $"{Capital(DiamondDealerUI.ApparentPurity(stone))}\n" +
            $"Rendement estimé : {low}–{high} %\n" +
            "(clic sur la case pour retirer)";
    }

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

    private void RefreshQuote()
    {
        currentQuote = default;
        confirmButton.interactable = false;

        if (stone == null)
        {
            quoteText.text = "Posez un diamant brut dans la case pour obtenir un devis.";
            return;
        }
        if (selectedOption < 0)
        {
            quoteText.text = "Choisissez une taille.";
            return;
        }

        var cutter = Cutter;
        var option = cutter.Options[selectedOption];
        currentQuote = DiamondCutting.GetQuote(cutter, option, stone);
        if (!currentQuote.IsValid)
        {
            quoteText.text = $"« {currentQuote.Reason} »";
            return;
        }

        var q = currentQuote;
        var days = q.Days == 1 ? "1 jour" : $"{q.Days} jours";
        var good = (cutter.QualityWeights[0] + cutter.QualityWeights[1]) / Mathf.Max(0.01f, Sum(cutter.QualityWeights)) * 100f;
        var result = q.StoneCount > 1
            ? $"Environ {q.StoneCount} pierres, {q.WeightMin:F2} à {q.WeightMax:F2} ct au total"
            : $"Poids estimé : {q.WeightMin:F2} à {q.WeightMax:F2} ct";
        quoteText.text =
            $"{option.Label}\n\n" +
            $"Prix : {q.Price} €\n" +
            $"{result}\n" +
            $"Délai : {days}\n" +
            $"Chances d'une taille excellente ou très bonne : environ {Mathf.RoundToInt(good / 5f) * 5} %";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        var busy = DiamondCuttingState.Instance != null && DiamondCuttingState.Instance.JobOf(cutterIndex) != null;
        confirmButton.interactable = money >= q.Price && !busy;
        if (busy) quoteText.text += "\n\n« Je n'ai qu'un travail à la fois : récupérez d'abord le précédent. »";
        else if (money < q.Price) quoteText.text += "\n\n« Revenez quand vous aurez de quoi payer. »";
    }

    private static float Sum(float[] values)
    {
        var total = 0f;
        foreach (var v in values) total += v;
        return total;
    }

    private void RefreshJob()
    {
        var state = DiamondCuttingState.Instance;
        var job = state != null ? state.JobOf(cutterIndex) : null;
        collectButton.gameObject.SetActive(job != null);
        if (job == null)
        {
            jobText.text = "Aucune pierre en cours de taille chez ce tailleur.";
            return;
        }

        var ready = state.IsReady(job);
        var days = state.DaysRemaining(job);
        var count = job.Stones.Count > 1 ? $" ({job.Stones.Count} pierres)" : "";
        jobText.text = ready
            ? $"{job.Summary}{count} — prête !"
            : $"{job.Summary}{count} — encore {days} jour{(days > 1 ? "s" : "")}";
        collectButton.interactable = ready;
    }

    private void Confirm()
    {
        if (stone == null || selectedOption < 0 || !currentQuote.IsValid) return;
        var option = Cutter.Options[selectedOption];
        var quote = currentQuote;
        var rough = stone;
        ConfirmDialog.Show("Confirmer la commande",
            $"Confier ce diamant brut ({rough.CaratWeight:F2} ct) à {Cutter.Name} ?\n\n{option.Label}\nPrix : {quote.Price} €\nDélai : {quote.Days} jour{(quote.Days > 1 ? "s" : "")}",
            "Commander", () => Order(option, quote, rough));
    }

    private void Order(DiamondCutOption option, DiamondCutQuote quote, GemInstance rough)
    {
        var state = DiamondCuttingState.Ensure();
        if (state == null || Cutter == null) return;

        if (state.TrySubmit(cutterIndex, option, rough, quote, out var message))
        {
            var days = quote.Days == 1 ? "demain" : $"dans {quote.Days} jours";
            greetingText.text = $"« Marché conclu ! Repassez {days}. »";
            stone = null;
            selectedOption = -1;
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            greetingText.text = $"« {message} »";
        }
    }

    private void Collect()
    {
        var state = DiamondCuttingState.Instance;
        var job = state != null ? state.JobOf(cutterIndex) : null;
        if (job == null) return;

        var count = job.Stones.Count;
        var first = job.Stones.Count > 0 ? job.Stones[0] : null;
        if (state.TryCollect(job, out var message))
        {
            greetingText.text = count > 1
                ? $"« Voici vos {count} pierres. Faites-les classer au bureau de grading pour connaître leur couleur et leur pureté. »"
                : $"« Voici votre diamant{(first != null ? $" de {first.CaratWeight:F2} ct" : "")}. Le bureau de grading vous dira ce qu'il vaut. »";
        }
        else if (!string.IsNullOrEmpty(message))
        {
            greetingText.text = $"« {message} »";
        }
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("DiamondCutterPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-140f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        titleText = Txt("Title", "", 21, TextAnchor.UpperLeft, Gold, new Vector2(24f, -12f), new Vector2(PanelWidth - 120f, 28f));
        specialtyText = Txt("Specialty", "", 14, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(24f, -40f), new Vector2(PanelWidth - 48f, 20f));
        pitchText = Txt("Pitch", "", 12, TextAnchor.UpperLeft, new Color(0.78f, 0.78f, 0.82f), new Vector2(24f, -62f), new Vector2(PanelWidth - 48f, 40f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        // The slot for the rough.
        Txt("SlotTitle", "Cristal à tailler", 13, TextAnchor.UpperLeft, Gold, new Vector2(24f, -112f), new Vector2(200f, 18f));
        var slot = RuntimeUIKit.MakeBox("Slot", panel.transform, new Vector2(24f, -134f), new Vector2(150f, 150f), new Color(0.04f, 0.05f, 0.07f, 1f));
        slot.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        slot.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);
        var slotGo = new GameObject("SlotStone", typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(slotGo.GetComponent<RectTransform>(), new Vector2(39f, -149f), new Vector2(120f, 120f));
        slotImage = slotGo.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;
        slotLabel = Txt("SlotLabel", "", 13, TextAnchor.MiddleCenter, new Color(0.5f, 0.54f, 0.6f), new Vector2(24f, -134f), new Vector2(150f, 150f));
        stoneInfoText = Txt("StoneInfo", "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f), new Vector2(24f, -292f), new Vector2(210f, 110f));

        // The cuts offered.
        Txt("OptionsTitle", "Taille souhaitée", 13, TextAnchor.UpperLeft, Gold, new Vector2(256f, -112f), new Vector2(200f, 18f));
        optionButtons = new Button[8];
        optionLabels = new Text[8];
        for (var i = 0; i < optionButtons.Length; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Option" + i, panel.transform, "", 12, ButtonColor,
                new Vector2(256f + (i % 2) * 226f, -134f - (i / 2) * 34f), new Vector2(218f, 30f));
            button.onClick.AddListener(() => SelectOption(captured));
            optionButtons[i] = button;
            optionLabels[i] = button.GetComponentInChildren<Text>();
        }

        quoteText = Txt("Quote", "", 13, TextAnchor.UpperLeft, Color.white, new Vector2(256f, -284f), new Vector2(440f, 124f));
        confirmButton = RuntimeUIKit.MakeButton("Confirm", panel.transform, "Commander la taille", 15, new Color(0.25f, 0.50f, 0.30f, 1f),
            new Vector2(256f, -414f), new Vector2(230f, 38f));
        confirmButton.onClick.AddListener(Confirm);

        greetingText = Txt("Greeting", "", 13, TextAnchor.UpperLeft, new Color(0.95f, 0.9f, 0.75f), new Vector2(24f, -462f), new Vector2(PanelWidth - 48f, 36f));

        // The order in progress.
        RuntimeUIKit.MakeBox("JobBack", panel.transform, new Vector2(24f, -504f), new Vector2(PanelWidth - 48f, 50f), new Color(0.05f, 0.05f, 0.07f, 1f));
        jobText = Txt("Job", "", 13, TextAnchor.MiddleLeft, Color.white, new Vector2(36f, -504f), new Vector2(PanelWidth - 200f, 50f));
        collectButton = RuntimeUIKit.MakeButton("Collect", panel.transform, "Récupérer", 13, new Color(0.25f, 0.50f, 0.30f, 1f),
            new Vector2(PanelWidth - 140f, -514f), new Vector2(100f, 30f));
        collectButton.onClick.AddListener(Collect);
    }

    private Text Txt(string name, string content, int size, TextAnchor alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, panel.transform, content, size, alignment, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
