using System;
using System.Collections.Generic;
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
    private bool selling;
    private Button buyTabButton;
    private Button sellTabButton;

    private void Awake()
    {
        Instance = this;
        BuildTabs();
        if (panel != null)
        {
            StardewKit.FramePanel(panel.GetComponent<Image>());
            StardewKit.Skin(panel.transform);
            panel.SetActive(false);
        }
    }

    private void Start()
    {
        if (MarketState.Instance != null) MarketState.Instance.OnMarketChanged += RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (MarketState.Instance != null) MarketState.Instance.OnMarketChanged -= RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    // Tabs and the shifted scroll area are created here rather than in the editor script, so existing scenes
    // only need to be regenerated, not re-wired.
    private void BuildTabs()
    {
        if (panel == null) return;

        var scrollView = panel.transform.Find("MarketScrollView") as RectTransform;
        if (scrollView != null)
        {
            scrollView.anchoredPosition = new Vector2(0f, -78f);
            scrollView.sizeDelta = new Vector2(420f, 250f);
        }

        buyTabButton = MakeTabButton("BuyTab", "Acheter", -105f);
        sellTabButton = MakeTabButton("SellTab", "Vendre", 105f);
        buyTabButton.onClick.AddListener(() => SetMode(false));
        sellTabButton.onClick.AddListener(() => SetMode(true));
    }

    private Button MakeTabButton(string name, string label, float x)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panel.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -44f);
        rect.sizeDelta = new Vector2(190f, 28f);

        var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textGO.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    private void SetMode(bool sell)
    {
        selling = sell;
        Refresh();
    }

    public void Open(MarketVendor vendor)
    {
        if (vendor == null) return;
        currentVendor = vendor;
        selling = false;
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

        if (buyTabButton != null)
        {
            buyTabButton.GetComponent<Image>().color = selling ? new Color(0.22f, 0.22f, 0.27f) : new Color(0.30f, 0.55f, 0.35f);
            sellTabButton.GetComponent<Image>().color = selling ? new Color(0.30f, 0.55f, 0.35f) : new Color(0.22f, 0.22f, 0.27f);
        }

        if (content == null) return;

        for (var i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (currentVendor == null) return;

        var rows = selling ? BuildSellRows() : BuildBuyRows();
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(rows * (RowHeight + RowSpacing) + 10f, 40f));
        StardewKit.Skin(content);
    }

    private int BuildBuyRows()
    {
        if (currentVendor.Stock.Count == 0)
        {
            CreateRow(0, "Ce marchand n'a plus rien à vendre aujourd'hui.", null, null);
            return 1;
        }

        for (var i = 0; i < currentVendor.Stock.Count; i++)
        {
            var item = currentVendor.Stock[i];
            CreateRow(i, BuildItemLabel(item), "Acheter", () => Purchase(item), () => Inspect(item));
        }
        return currentVendor.Stock.Count;
    }

    private int BuildSellRows()
    {
        var sellable = new List<GemInstance>();
        if (PlayerInventory.Instance != null)
        {
            foreach (var gem in PlayerInventory.Instance.Gems)
            {
                if (MarketState.CanSell(gem)) sellable.Add(gem);
            }
        }

        if (sellable.Count == 0)
        {
            CreateRow(0, "Aucun spécimen identifié à vendre.\n(Les pierres de clients repartent par courrier.)", null, null);
            return 1;
        }

        for (var i = 0; i < sellable.Count; i++)
        {
            var gem = sellable[i];
            var offer = MarketState.Instance.GetListedOffer(currentVendor, gem);
            var state = gem.IsRough ? gem.RoughLabel : $"{gem.ShapeName}, {gem.CutStyleName.ToLowerInvariant()}";
            var note = gem.IsTreated && gem.IsTreatmentKnown ? " (traitement déclaré)" : "";
            var label = $"{gem.Definition.displayName} — {gem.CaratWeight:F2} ct ({state})\nOffre : {offer}€{note}";
            CreateRow(i, label, "Vendre", () => Sell(gem));
        }
        return sellable.Count;
    }

    private string BuildItemLabel(GemInstance item)
    {
        var price = MarketState.Instance != null ? MarketState.Instance.GetPrice(item) : item.FinalValue;
        return $"{item.Definition.displayName} — {item.CaratWeight:F2} ct\nOrigine : {item.Origin.name}\nPrix : {price}€";
    }

    private void CreateRow(int index, string label, string buttonLabel, Action onClick, Action onInspect = null)
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
        textRect.sizeDelta = new Vector2(RowWidth - (onInspect != null ? 232f : 130f), 0f);
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;

        if (onClick == null) return;

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
        buttonText.text = buttonLabel;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 13;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        buttonGO.GetComponent<Button>().onClick.AddListener(() => onClick());

        if (onInspect == null) return;

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
        inspectGO.GetComponent<Button>().onClick.AddListener(() => onInspect());
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

        ConfirmDialog.Show("Confirmer l'achat",
            $"Êtes-vous sûr de vouloir acheter cette pierre ?\n\n{item.Definition.displayName} — {item.CaratWeight:F2} ct\nPrix : {price}€",
            "Acheter", () => ConfirmPurchase(item, price));
    }

    private void ConfirmPurchase(GemInstance item, int price)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || MarketState.Instance == null)
            return;

        if (MarketState.Instance.TryPurchase(currentVendor, item, PlayerInventory.Instance, PlayerWallet.Instance))
            ToolFeedbackUI.Instance?.ShowMessage($"Achat : {item.Definition.displayName} pour {price}€.");
    }

    // An untreated stone is simply sold. A treated one the player knows about is sold declared or not, as they choose. A treated one the player does
    // not know about goes without a word: the buyer may find the treatment.
    private void Sell(GemInstance gem)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || MarketState.Instance == null)
            return;

        if (!gem.IsTreated)
        {
            SellDeclared(gem);
            return;
        }

        if (!gem.IsTreatmentKnown)
        {
            SellUndisclosed(gem);
            return;
        }

        var declared = MarketState.Instance.GetBuybackPrice(currentVendor, gem, true);
        var hidden = MarketState.Instance.GetBuybackPrice(currentVendor, gem, false);
        var risk = TreatmentDisclosure.RiskLabel(TreatmentDisclosure.ChanceFor(gem, currentVendor.Expertise));
        ConfirmDialog.ShowChoice("Pierre traitée",
            $"{gem.TreatmentGrade}\nDéclarer le traitement : {declared}€\nNe rien dire : {hidden}€ (risque de contrôle {risk})",
            "Déclarer", () => SellDeclared(gem),
            "Ne rien dire", () => SellUndisclosed(gem));
    }

    private void SellDeclared(GemInstance gem)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || MarketState.Instance == null)
            return;

        var name = gem.Definition.displayName;
        var treated = gem.IsTreated;
        if (MarketState.Instance.TrySell(currentVendor, gem, PlayerInventory.Instance, PlayerWallet.Instance, out var price))
            ToolFeedbackUI.Instance?.ShowMessage($"Vente : {name} pour {price}€ ({currentVendor.CountryName}){(treated ? ", traitement déclaré" : "")}.");
    }

    private void SellUndisclosed(GemInstance gem)
    {
        if (currentVendor == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null || MarketState.Instance == null)
            return;

        var name = gem.Definition.displayName;
        var asked = MarketState.Instance.GetBuybackPrice(currentVendor, gem, false);
        var outcome = MarketState.Instance.TrySellUndisclosed(currentVendor, gem, PlayerInventory.Instance, PlayerWallet.Instance);

        if (outcome.Sold)
        {
            ToolFeedbackUI.Instance?.ShowMessage($"Vente : {name} pour {outcome.Price}€ ({currentVendor.CountryName}).");
            return;
        }

        if (!outcome.FoundTreatment) return;

        if (outcome.Deliberate)
        {
            ToolFeedbackUI.Instance?.ShowMessage($"Le marchand a testé la pierre : {gem.TreatmentGrade} non déclaré ! Vente annulée, amende de {outcome.Fine}€ et {outcome.ReputationLost * ReputationState.XpPerStone} XP de réputation perdus.");
            Refresh();
            return;
        }

        var newOffer = outcome.Price;
        ConfirmDialog.Show("Traitement trouvé",
            $"Le marchand a trouvé un traitement dont vous ignoriez l'existence : {gem.TreatmentGrade}.\nNouvelle offre : {newOffer}€ (au lieu de {asked}€).",
            "Accepter", () => SellDeclared(gem));
        Refresh();
    }
}
