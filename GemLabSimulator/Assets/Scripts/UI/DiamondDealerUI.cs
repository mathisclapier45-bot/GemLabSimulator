using UnityEngine;
using UnityEngine.UI;

// Sales window of a diamond dealer (1st floor of the Diamond Quarter): the dealer's stock with each stone's weight, colour,
// clarity and certificate number; examine a stone with the loupe viewer, or buy it (with a confirmation). Built at runtime.
public class DiamondDealerUI : MonoBehaviour
{
    public static DiamondDealerUI Instance { get; private set; }

    private const float PanelWidth = 640f;
    private const float PanelHeight = 540f;
    private const float RowHeight = 66f;
    private const float RowSpacing = 6f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private GameObject panel;
    private Text titleText;
    private Text specialtyText;
    private Text pitchText;
    private RectTransform content;
    private DiamondDealer currentDealer;

    public static void TryOpen(int dealerIndex)
    {
        var state = DiamondDealerState.Ensure();
        if (state == null || dealerIndex < 0 || dealerIndex >= state.Dealers.Count) return;

        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<DiamondDealerUI>();
        }

        Instance?.Open(state.Dealers[dealerIndex]);
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        var state = DiamondDealerState.Ensure();
        if (state != null) state.OnStockChanged += RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (DiamondDealerState.Instance != null) DiamondDealerState.Instance.OnStockChanged -= RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open(DiamondDealer dealer)
    {
        currentDealer = dealer;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        currentDealer = null;
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        if (currentDealer == null) return;

        titleText.text = currentDealer.Name;
        specialtyText.text = currentDealer.Specialty;
        pitchText.text = currentDealer.Rough
            ? currentDealer.Pitch + "\nDiamants bruts naturels, vendus sans grade : la couleur et la pureté exactes ne se révèlent qu'à la taille."
            : currentDealer.Pitch + "\nDiamants naturels incolores (D à Z), livrés avec leur certificat.";

        for (var i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var state = DiamondDealerState.Instance;
        if (currentDealer.Stock.Count == 0)
        {
            MakeEmptyRow();
            content.sizeDelta = new Vector2(0f, RowHeight + 10f);
            return;
        }

        for (var i = 0; i < currentDealer.Stock.Count; i++)
        {
            var stone = currentDealer.Stock[i];
            MakeRow(i, stone, state != null ? state.GetPrice(currentDealer, stone) : stone.FinalValue);
        }
        content.sizeDelta = new Vector2(0f, currentDealer.Stock.Count * (RowHeight + RowSpacing) + 10f);
    }

    private void MakeEmptyRow()
    {
        var row = RuntimeUIKit.MakeBox("Empty", content, Vector2.zero, new Vector2(PanelWidth - 70f, RowHeight), new Color(0.16f, 0.16f, 0.19f, 1f));
        RuntimeUIKit.MakeText("Label", row.transform, "Cette maison n'a plus rien à vendre aujourd'hui. Revenez demain.", 13,
            TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(PanelWidth - 70f, RowHeight));
    }

    private void MakeRow(int index, GemInstance stone, int price)
    {
        var width = PanelWidth - 70f;
        var row = RuntimeUIKit.MakeBox($"Row_{index}", content, new Vector2(0f, -index * (RowHeight + RowSpacing)), new Vector2(width, RowHeight),
            new Color(0.16f, 0.16f, 0.19f, 1f));

        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(row.transform, false);
        RuntimeUIKit.SetTopLeft(icon.GetComponent<RectTransform>(), new Vector2(8f, -9f), new Vector2(48f, 48f));
        var iconImage = icon.GetComponent<Image>();
        iconImage.sprite = stone.DisplaySprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        string text;
        if (stone.IsRough)
        {
            DiamondDealerState.YieldEstimate(stone, out var low, out var high);
            text =
                $"{stone.CaratWeight:F2} ct brut — {stone.RoughForm}   ·   rendement estimé {low}–{high} %\n" +
                $"Teinte {stone.ColorName.ToLowerInvariant()}   ·   {ApparentPurity(stone)}\n" +
                $"Prix : {price:N0}€";
        }
        else
        {
            text =
                $"{stone.CaratWeight:F2} ct — {stone.ShapeName}, {stone.CutStyleName.ToLowerInvariant()}\n" +
                $"Couleur {stone.ColorGrade}   ·   Pureté {stone.DiamondClarity}   ·   Certificat {stone.Certificate.number}\n" +
                $"Prix : {price:N0}€";
        }
        RuntimeUIKit.MakeText("Label", row.transform, text, 13, TextAnchor.MiddleLeft, Color.white, new Vector2(64f, 0f), new Vector2(width - 64f - 190f, RowHeight));

        var inspect = RuntimeUIKit.MakeButton("Inspect", row.transform, "Examiner", 12, new Color(0.25f, 0.25f, 0.32f, 1f),
            new Vector2(width - 182f, -19f), new Vector2(84f, 28f));
        inspect.onClick.AddListener(() => Inspect(stone));
        var buy = RuntimeUIKit.MakeButton("Buy", row.transform, "Acheter", 12, new Color(0.25f, 0.50f, 0.30f, 1f),
            new Vector2(width - 92f, -19f), new Vector2(84f, 28f));
        buy.onClick.AddListener(() => Purchase(stone, price));
    }

    // Hides the window while the stone is examined, then brings it back.
    private void Inspect(GemInstance stone)
    {
        if (panel != null) panel.SetActive(false);
        LoupeUI.OpenForMarket(stone, ReopenAfterInspection);
    }

    private void ReopenAfterInspection()
    {
        if (panel != null && currentDealer != null) panel.SetActive(true);
        RefreshIfOpen();
    }

    private void Purchase(GemInstance stone, int price)
    {
        var wallet = PlayerWallet.Instance;
        var inventory = PlayerInventory.Instance;
        if (currentDealer == null || wallet == null || inventory == null) return;

        if (wallet.Money < price)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Pas assez d'argent pour cet achat.");
            return;
        }

        if (inventory.IsFull)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Inventaire plein : impossible d'acheter ce diamant.");
            return;
        }

        var description = stone.IsRough
            ? $"{stone.CaratWeight:F2} ct brut — {stone.RoughForm}, {ApparentPurity(stone)}"
            : $"{stone.CaratWeight:F2} ct — couleur {stone.ColorGrade}, pureté {stone.DiamondClarity}";
        ConfirmDialog.Show("Confirmer l'achat",
            $"Êtes-vous sûr de vouloir acheter ce diamant ?\n\n{description}\nPrix : {price:N0}€",
            "Acheter", () => ConfirmPurchase(stone, price));
    }

    private void ConfirmPurchase(GemInstance stone, int price)
    {
        var state = DiamondDealerState.Instance;
        if (currentDealer == null || state == null) return;

        if (!state.TryPurchase(currentDealer, stone, PlayerInventory.Instance, PlayerWallet.Instance)) return;
        ToolFeedbackUI.Instance?.ShowMessage(stone.IsRough
            ? $"Achat : diamant brut de {stone.CaratWeight:F2} ct ({stone.RoughForm}) pour {price:N0}€."
            : $"Achat : diamant de {stone.CaratWeight:F2} ct ({stone.ColorGrade} / {stone.DiamondClarity}) pour {price:N0}€. Certificat {stone.Certificate.number} remis.");
    }

    // What can be seen of the clarity of a rough crystal from outside.
    public static string ApparentPurity(GemInstance stone)
    {
        switch (stone.ClarityGrade)
        {
            case "Loupe clean": return "très propre";
            case "Eye-clean": return "propre à l'œil nu";
            case "Inclus": return "inclusions visibles";
            default: return "très inclus";
        }
    }

    private void BuildPanel()
    {
        panel = new GameObject("DiamondDealerPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.98f);

        titleText = RuntimeUIKit.MakeText("Title", panel.transform, "", 21, TextAnchor.UpperLeft, Gold, new Vector2(24f, -12f), new Vector2(PanelWidth - 120f, 28f));
        specialtyText = RuntimeUIKit.MakeText("Specialty", panel.transform, "", 14, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f),
            new Vector2(24f, -40f), new Vector2(PanelWidth - 48f, 20f));
        pitchText = RuntimeUIKit.MakeText("Pitch", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.78f, 0.78f, 0.82f),
            new Vector2(24f, -62f), new Vector2(PanelWidth - 48f, 48f));

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f));
        close.onClick.AddListener(Close);

        // Scrolling list of the stock.
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(viewport.GetComponent<RectTransform>(), new Vector2(24f, -118f), new Vector2(PanelWidth - 48f, PanelHeight - 138f));
        viewport.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 1f);

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewport.transform, false);
        content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = new Vector2(6f, -6f);
        content.sizeDelta = new Vector2(-12f, 100f);

        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
    }
}
