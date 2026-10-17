using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The workshop interface is built at runtime so it needs no editor wiring beyond adding this component to the Canvas.
public class LapidaryUI : MonoBehaviour
{
    public static LapidaryUI Instance { get; private set; }

    private const float PanelWidth = 640f;
    private const float PanelHeight = 560f;

    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.07f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color SelectedColor = new Color(0.30f, 0.55f, 0.35f, 1f);
    private static readonly Color DisabledColor = new Color(0.16f, 0.16f, 0.18f, 1f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text quoteText;
    private Text greetingText;
    private Button confirmButton;
    private RectTransform jobsContainer;

    private readonly Dictionary<string, Button> shapeButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, Button> styleButtons = new Dictionary<string, Button>();

    private GemInstance stone;
    private string selectedShape;
    private string selectedStyle;
    private LapidaryQuote currentQuote;

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
        if (LapidaryState.Instance != null) LapidaryState.Instance.OnJobsChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
        if (LapidaryState.Instance != null) LapidaryState.Instance.OnJobsChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
    }

    private void RefreshIfOpen(int _)
    {
        RefreshIfOpen();
    }

    public void Open()
    {
        stone = null;
        selectedShape = null;
        selectedStyle = null;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    public void SetStone(GemInstance gem)
    {
        var refusal = LapidaryCatalog.GetRefusalReason(gem);
        if (!string.IsNullOrEmpty(refusal))
        {
            greetingText.text = $"« {refusal} »";
            return;
        }

        stone = gem;
        selectedShape = null;
        selectedStyle = null;
        greetingText.text = "« Belle pièce. Choisissez la forme, puis le style de taille. »";
        Refresh();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        // The stone can leave the inventory behind our back (mailbox, chest...): forget it then.
        if (stone != null && (PlayerInventory.Instance == null || !ContainsGem(stone)))
        {
            stone = null;
            selectedShape = null;
            selectedStyle = null;
        }

        RefreshSlot();
        RefreshChoiceButtons();
        RefreshQuote();
        RefreshJobs();
    }

    private static bool ContainsGem(GemInstance gem)
    {
        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, gem)) return true;
        }
        return false;
    }

    private void RefreshSlot()
    {
        if (stone == null)
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
            slotLabel.text = "Déposez\nune pierre\nbrute ici";
            stoneInfoText.text = "";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{stone.Definition.displayName}\n" +
            $"Poids brut : {stone.CaratWeight:F2} ct\n" +
            $"Pureté : {stone.ClarityGrade}\n" +
            "(clic sur la case pour retirer)";
    }

    private void RefreshChoiceButtons()
    {
        foreach (var pair in shapeButtons)
        {
            pair.Value.interactable = stone != null;
            pair.Value.GetComponent<Image>().color = !pair.Value.interactable ? DisabledColor
                : pair.Key == selectedShape ? SelectedColor : ButtonColor;
        }

        foreach (var pair in styleButtons)
        {
            var allowed = stone != null && selectedShape != null &&
                          LapidaryCatalog.IsStyleAllowed(stone.Definition, selectedShape, pair.Key);
            pair.Value.interactable = stone != null && selectedShape != null;
            pair.Value.GetComponent<Image>().color = !pair.Value.interactable || !allowed ? DisabledColor
                : pair.Key == selectedStyle ? SelectedColor : ButtonColor;
        }
    }

    private void RefreshQuote()
    {
        currentQuote = default;
        confirmButton.interactable = false;

        if (stone == null)
        {
            quoteText.text = "Posez une pierre brute dans la case pour obtenir un devis.";
            return;
        }

        if (selectedShape == null)
        {
            quoteText.text = "Choisissez une forme.";
            return;
        }

        if (selectedStyle == null)
        {
            quoteText.text = $"Forme : {selectedShape}\nChoisissez maintenant un style de taille.";
            return;
        }

        currentQuote = LapidaryCatalog.GetQuote(stone, selectedShape, selectedStyle);
        if (!currentQuote.IsValid)
        {
            quoteText.text = $"« {currentQuote.Reason} »";
            return;
        }

        var q = currentQuote;
        var days = q.Days == 1 ? "1 jour" : $"{q.Days} jours";
        quoteText.text =
            $"Taille {selectedStyle.ToLowerInvariant()} — {selectedShape.ToLowerInvariant()}\n\n" +
            $"Prix : {q.Price} €\n" +
            $"Perte de poids : {q.LossMin * 100f:F0} à {q.LossMax * 100f:F0} %\n" +
            $"Poids final estimé : {q.WeightMin:F2} à {q.WeightMax:F2} ct\n" +
            $"Délai : {days}";

        var money = PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;
        var busy = LapidaryState.Instance != null && LapidaryState.Instance.IsFull;
        confirmButton.interactable = money >= q.Price && !busy;
        if (busy) quoteText.text += "\n\n« Je n'ai qu'une pierre à la fois : récupérez d'abord la précédente. »";
        else if (money < q.Price) quoteText.text += "\n\n« Revenez quand vous aurez de quoi payer. »";
    }

    private void RefreshJobs()
    {
        for (var i = jobsContainer.childCount - 1; i >= 0; i--)
            Destroy(jobsContainer.GetChild(i).gameObject);

        var state = LapidaryState.Instance;
        if (state == null || state.Jobs.Count == 0)
        {
            MakeText("Empty", jobsContainer, "Aucune pierre en cours de taille.", 12, TextAnchor.UpperLeft,
                new Color(0.7f, 0.7f, 0.7f), new Vector2(0, 0), new Vector2(PanelWidth / 2f - 40f, 20f));
            return;
        }

        var y = 0f;
        foreach (var job in new List<LapidaryJob>(state.Jobs))
        {
            var ready = state.IsReady(job);
            var days = state.DaysRemaining(job);
            var label = ready
                ? $"{job.Gem.Definition.displayName} {job.Gem.CaratWeight:F2} ct — prête"
                : $"{job.Gem.Definition.displayName} — encore {days} jour{(days > 1 ? "s" : "")}";

            MakeText("JobLabel", jobsContainer, label, 12, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, y), new Vector2(190f, 32f));

            var capturedJob = job;
            var button = MakeButton("Collect", jobsContainer, "Récupérer", 12, new Vector2(196f, y), new Vector2(84f, 30f));
            button.interactable = ready;
            button.onClick.AddListener(() => Collect(capturedJob));
            y -= 36f;
        }
    }

    private void Collect(LapidaryJob job)
    {
        if (LapidaryState.Instance == null) return;
        if (LapidaryState.Instance.TryCollect(job, out var message))
            greetingText.text = $"« Voici votre {job.Gem.Definition.displayName.ToLowerInvariant()} taillé. »";
        else if (!string.IsNullOrEmpty(message))
            greetingText.text = $"« {message} »";
    }

    private void SelectShape(string shape)
    {
        selectedShape = shape;
        // Keep the chosen style only if it still makes sense for the new shape.
        if (selectedStyle != null && !LapidaryCatalog.IsStyleAllowed(stone.Definition, shape, selectedStyle))
            selectedStyle = null;
        Refresh();
    }

    private void SelectStyle(string style)
    {
        selectedStyle = style;
        if (stone != null && selectedShape != null && !LapidaryCatalog.IsStyleAllowed(stone.Definition, selectedShape, style))
        {
            var quote = LapidaryCatalog.GetQuote(stone, selectedShape, style);
            greetingText.text = $"« {quote.Reason} »";
        }
        Refresh();
    }

    private void Confirm()
    {
        if (stone == null || !currentQuote.IsValid || LapidaryState.Instance == null) return;

        var quote = currentQuote;
        var shape = selectedShape;
        var style = selectedStyle;
        var name = stone.Definition.displayName.ToLowerInvariant();

        if (LapidaryState.Instance.TrySubmit(stone, shape, style, quote, out var message))
        {
            var days = quote.Days == 1 ? "demain" : $"dans {quote.Days} jours";
            greetingText.text = $"« Marché conclu ! Repassez {days} chercher votre {name}. »";
            stone = null;
            selectedShape = null;
            selectedStyle = null;
            Refresh();
        }
        else if (!string.IsNullOrEmpty(message))
        {
            greetingText.text = $"« {message} »";
        }
    }

    private void ClearStone()
    {
        if (stone == null) return;
        stone = null;
        selectedShape = null;
        selectedStyle = null;
        greetingText.text = "« Que puis-je tailler pour vous ? »";
        Refresh();
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("LapidaryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-130f, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        MakeText("Title", panel.transform, "Atelier lapidaire", 20, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.65f),
            new Vector2(20f, -12f), new Vector2(PanelWidth - 40f, 28f));
        greetingText = MakeText("Greeting", panel.transform, "« Que puis-je tailler pour vous ? »", 13, TextAnchor.UpperCenter,
            new Color(0.85f, 0.85f, 0.85f), new Vector2(20f, -42f), new Vector2(PanelWidth - 40f, 36f));

        BuildSlot();
        BuildChoiceGrid("Forme", 150f, LapidaryCatalog.Shapes, shapeButtons, SelectShape);
        BuildChoiceGrid("Taille", 340f, LapidaryCatalog.Styles, styleButtons, SelectStyle);

        MakeText("QuoteHeader", panel.transform, "Devis", 14, TextAnchor.UpperLeft, new Color(0.95f, 0.85f, 0.65f),
            new Vector2(320f, -84f), new Vector2(300f, 20f));
        quoteText = MakeText("Quote", panel.transform, "", 13, TextAnchor.UpperLeft, Color.white,
            new Vector2(320f, -106f), new Vector2(300f, 150f));

        confirmButton = MakeButton("Confirm", panel.transform, "Confier la pierre", 14, new Vector2(320f, -262f), new Vector2(300f, 32f));
        confirmButton.onClick.AddListener(Confirm);

        MakeText("JobsHeader", panel.transform, "En cours chez le lapidaire", 14, TextAnchor.UpperLeft,
            new Color(0.95f, 0.85f, 0.65f), new Vector2(320f, -310f), new Vector2(300f, 20f));
        var jobsGO = new GameObject("Jobs", typeof(RectTransform));
        jobsGO.transform.SetParent(panel.transform, false);
        jobsContainer = jobsGO.GetComponent<RectTransform>();
        SetTopLeft(jobsContainer, new Vector2(320f, -334f), new Vector2(300f, 150f));

        var close = MakeButton("Close", panel.transform, "Fermer", 14,
            new Vector2((PanelWidth - 160f) / 2f, -PanelHeight + 44f), new Vector2(160f, 32f));
        close.onClick.AddListener(Close);
    }

    private void BuildSlot()
    {
        var slotGO = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LapidarySlotDropZone));
        slotGO.transform.SetParent(panel.transform, false);
        SetTopLeft(slotGO.GetComponent<RectTransform>(), new Vector2(20f, -84f), new Vector2(88f, 88f));
        slotGO.GetComponent<Image>().color = new Color(0.20f, 0.18f, 0.16f, 1f);
        slotGO.GetComponent<Button>().onClick.AddListener(ClearStone);
        slotGO.GetComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        slotImage = iconGO.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;

        slotLabel = MakeText("SlotLabel", slotGO.transform, "", 11, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f),
            Vector2.zero, new Vector2(88f, 88f));
        slotLabel.raycastTarget = false;

        stoneInfoText = MakeText("StoneInfo", panel.transform, "", 12, TextAnchor.UpperLeft, Color.white,
            new Vector2(116f, -84f), new Vector2(190f, 88f));
    }

    private void BuildChoiceGrid(string header, float top, string[] labels, Dictionary<string, Button> store,
        System.Action<string> onSelect)
    {
        MakeText(header + "Header", panel.transform, header, 14, TextAnchor.UpperLeft, new Color(0.95f, 0.85f, 0.65f),
            new Vector2(20f, -top - 20f), new Vector2(280f, 20f));

        const int columns = 3;
        const float buttonWidth = 90f;
        const float buttonHeight = 28f;
        const float gap = 5f;

        for (var i = 0; i < labels.Length; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var label = labels[i];
            var shown = label == "Rectangle à pans coupés" ? "Rect. pans coupés" : label;
            var button = MakeButton("Choice_" + label, panel.transform, shown, 11,
                new Vector2(20f + col * (buttonWidth + gap), -top - 44f - row * (buttonHeight + gap)),
                new Vector2(buttonWidth, buttonHeight));
            button.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            button.GetComponentInChildren<Text>().resizeTextMinSize = 8;
            button.GetComponentInChildren<Text>().resizeTextMaxSize = 12;
            button.onClick.AddListener(() => onSelect(label));
            store[label] = button;
        }
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
        return text;
    }

    private static Button MakeButton(string name, Transform parent, string label, int fontSize, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        go.GetComponent<Image>().color = ButtonColor;

        var text = MakeText("Label", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, size);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.raycastTarget = false;
        return go.GetComponent<Button>();
    }
}
