using UnityEngine;
using UnityEngine.UI;

public class RoughMarketUI : MonoBehaviour
{
    public static RoughMarketUI Instance { get; private set; }

    private const float RowWidth = 400f;
    private const float RowHeight = 64f;
    private const float RowSpacing = 10f;

    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private RectTransform content;

    private RoughMarketVendor currentVendor;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (RoughMarketState.Instance != null) RoughMarketState.Instance.OnMarketChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (RoughMarketState.Instance != null) RoughMarketState.Instance.OnMarketChanged -= RefreshIfOpen;
    }

    public void Open(RoughMarketVendor vendor)
    {
        if (vendor == null) return;
        currentVendor = vendor;
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        currentVendor = null;
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        if (titleText != null)
            titleText.text = currentVendor != null ? $"Marchand de pierres brutes — {currentVendor.CountryName}" : "";

        if (content == null) return;

        for (var i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (currentVendor == null) return;

        if (currentVendor.Stock.Count == 0)
        {
            CreateRow(0, "Ce marchand n'a plus rien à vendre aujourd'hui.", null);
            content.sizeDelta = new Vector2(content.sizeDelta.x, RowHeight + RowSpacing);
            return;
        }

        for (var i = 0; i < currentVendor.Stock.Count; i++)
            CreateRow(i, BuildItemLabel(currentVendor.Stock[i]), currentVendor.Stock[i]);

        content.sizeDelta = new Vector2(content.sizeDelta.x,
            currentVendor.Stock.Count * (RowHeight + RowSpacing) + 10f);
    }

    private string BuildItemLabel(GemInstance item)
    {
        var price = RoughMarketState.Instance != null ? RoughMarketState.Instance.GetPrice(item) : item.FinalValue;
        return $"{item.Definition.displayName} (brute) — {item.CaratWeight:F2} ct\nOrigine : {item.Origin.name}\nPrix : {price}€";
    }

    private void CreateRow(int index, string label, GemInstance item)
    {
        var y = -index * (RowHeight + RowSpacing);

        var rowGO = new GameObject($"Row_{index}", typeof(RectTransform), typeof(Image));
        rowGO.transform.SetParent(content, false);
        var rect = rowGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(RowWidth, RowHeight);
        rowGO.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.19f, 1f);

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(rowGO.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = new Vector2(10f, 0f);
        textRect.sizeDelta = new Vector2(RowWidth - (item != null ? 232f : 130f), 0f);
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;

        if (item == null) return;

        var buttonGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(rowGO.transform, false);
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-8f, 0f);
        buttonRect.sizeDelta = new Vector2(100f, 36f);
        buttonGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f, 1f);

        var buttonTextGO = new GameObject("Label", typeof(RectTransform));
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        buttonTextRect.anchoredPosition = Vector2.zero;
        var buttonText = buttonTextGO.AddComponent<Text>();
        buttonText.text = "Acheter";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 13;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        var button = buttonGO.GetComponent<Button>();
        button.onClick.AddListener(() => Purchase(item));

        // "Examiner": look at the stone from every side, with a loupe, a torch and a UV lamp.
        var inspectGO = new GameObject("InspectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        inspectGO.transform.SetParent(rowGO.transform, false);
        var inspectRect = inspectGO.GetComponent<RectTransform>();
        inspectRect.anchorMin = new Vector2(1f, 0.5f);
        inspectRect.anchorMax = new Vector2(1f, 0.5f);
        inspectRect.pivot = new Vector2(1f, 0.5f);
        inspectRect.anchoredPosition = new Vector2(-114f, 0f);
        inspectRect.sizeDelta = new Vector2(84f, 36f);
        inspectGO.GetComponent<Image>().color = new Color(0.22f, 0.32f, 0.42f, 1f);

        var inspectTextGO = new GameObject("Label", typeof(RectTransform));
        inspectTextGO.transform.SetParent(inspectGO.transform, false);
        var inspectTextRect = inspectTextGO.GetComponent<RectTransform>();
        inspectTextRect.anchorMin = Vector2.zero;
        inspectTextRect.anchorMax = Vector2.one;
        inspectTextRect.sizeDelta = Vector2.zero;
        inspectTextRect.anchoredPosition = Vector2.zero;
        var inspectText = inspectTextGO.AddComponent<Text>();
        inspectText.text = "Examiner";
        inspectText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inspectText.fontSize = 13;
        inspectText.alignment = TextAnchor.MiddleCenter;
        inspectText.color = Color.white;
        inspectGO.GetComponent<Button>().onClick.AddListener(() => Inspect(item));
    }

    // Hides the vendor while the stone is examined, then brings it back.
    private void Inspect(GemInstance item)
    {
        if (panel != null) panel.SetActive(false);
        LoupeUI.OpenForMarket(item, ReopenAfterInspection);
    }

    private void ReopenAfterInspection()
    {
        if (panel != null && currentVendor != null) panel.SetActive(true);
        RefreshIfOpen();
    }

    private void Purchase(GemInstance item)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || RoughMarketState.Instance == null)
            return;

        var price = RoughMarketState.Instance.GetPrice(item);
        if (PlayerWallet.Instance.Money < price)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Pas assez d'argent pour cet achat.");
            return;
        }

        if (PlayerInventory.Instance.IsFull)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Inventaire plein : impossible d'acheter cette gemme.");
            return;
        }

        ConfirmDialog.Show("Confirmer l'achat",
            $"Êtes-vous sûr de vouloir acheter cette pierre brute ?\n\n{item.Definition.displayName} (brute) — {item.CaratWeight:F2} ct\nPrix : {price}€",
            "Acheter", () => ConfirmPurchase(item, price));
    }

    private void ConfirmPurchase(GemInstance item, int price)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || RoughMarketState.Instance == null)
            return;

        if (RoughMarketState.Instance.TryPurchase(currentVendor, item, PlayerInventory.Instance, PlayerWallet.Instance))
            ToolFeedbackUI.Instance?.ShowMessage($"Achat : {item.Definition.displayName} (brute) pour {price}€.");
    }
}
