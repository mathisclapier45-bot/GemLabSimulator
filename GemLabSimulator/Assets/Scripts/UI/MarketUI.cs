using UnityEngine;
using UnityEngine.UI;

public class MarketUI : MonoBehaviour
{
    public static MarketUI Instance { get; private set; }

    private const float RowWidth = 400f;
    private const float RowHeight = 64f;
    private const float RowSpacing = 10f;

    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private RectTransform content;

    private MarketVendor currentVendor;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (MarketState.Instance != null) MarketState.Instance.OnMarketChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (MarketState.Instance != null) MarketState.Instance.OnMarketChanged -= RefreshIfOpen;
    }

    public void Open(MarketVendor vendor)
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
            titleText.text = currentVendor != null ? $"Marchand — {currentVendor.CountryName}" : "";

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
        var price = MarketState.Instance != null ? MarketState.Instance.GetPrice(item) : item.FinalValue;
        return $"{item.Definition.displayName}\nOrigine : {item.Origin.name}\nPrix : {price}€";
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
        textRect.sizeDelta = new Vector2(RowWidth - 130f, 0f);
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
    }

    private void Purchase(GemInstance item)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || MarketState.Instance == null)
            return;

        var price = MarketState.Instance.GetPrice(item);
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

        if (MarketState.Instance.TryPurchase(currentVendor, item, PlayerInventory.Instance, PlayerWallet.Instance))
            ToolFeedbackUI.Instance?.ShowMessage($"Achat : {item.Definition.displayName} pour {price}€.");
    }
}
