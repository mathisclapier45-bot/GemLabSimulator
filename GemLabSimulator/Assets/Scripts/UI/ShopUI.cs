using UnityEngine;
using UnityEngine.UI;

// Purchase window of the GemStore. Built at runtime, so no scene wiring is needed.
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    private GameObject panel;
    private Text nameText;
    private Text descriptionText;
    private Text priceText;
    private Text statusText;
    private Button buyButton;
    private Text buyLabel;
    private ShopItem item;

    public static void TryOpen(ShopItem requested)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<ShopUI>();
        }

        if (Instance == null) return;
        if (Instance.panel.activeSelf && Instance.item == requested) Instance.Close();
        else Instance.Open(requested);
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshIfOpen;
        var tools = PlayerOwnedTools.Ensure();
        if (tools != null) tools.OnToolsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshIfOpen;
        if (PlayerOwnedTools.Instance != null) PlayerOwnedTools.Instance.OnToolsChanged -= RefreshIfOpen;
    }

    private void Open(ShopItem requested)
    {
        item = requested;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        item = null;
    }

    public void CloseIfShowing(ShopItem leaving)
    {
        if (item == leaving) Close();
    }

    private void RefreshIfOpen(int _) { RefreshIfOpen(); }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Refresh()
    {
        if (item == null) return;

        nameText.text = item.DisplayName;
        descriptionText.text = item.Description;
        priceText.text = $"Prix : {item.Price}€";

        var tools = PlayerOwnedTools.Ensure();
        var wallet = PlayerWallet.Instance;
        var alreadyOwned = tools != null && tools.Has(item.ToolId);
        var money = wallet != null ? wallet.Money : 0;

        if (alreadyOwned)
        {
            statusText.text = "Déjà en votre possession.";
            statusText.color = new Color(0.55f, 0.85f, 0.55f);
            buyLabel.text = "Acheté";
            buyButton.interactable = false;
        }
        else if (item.AddsToBag && tools != null && !tools.BagHasRoom)
        {
            statusText.text = $"Votre sac est plein ({PlayerOwnedTools.BagSlotCount} emplacements).";
            statusText.color = new Color(0.95f, 0.45f, 0.40f);
            buyLabel.text = "Sac plein";
            buyButton.interactable = false;
        }
        else
        {
            statusText.text = $"Votre argent : {money}€";
            var canAfford = money >= item.Price;
            statusText.color = canAfford ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.95f, 0.45f, 0.40f);
            buyLabel.text = canAfford ? "Acheter" : "Fonds insuffisants";
            buyButton.interactable = canAfford;
        }
    }

    private void Buy()
    {
        var tools = PlayerOwnedTools.Ensure();
        var wallet = PlayerWallet.Instance;
        if (item == null || tools == null || wallet == null || tools.Has(item.ToolId)) return;
        if (item.AddsToBag && !tools.BagHasRoom) return;
        if (!wallet.TrySpend(item.Price)) return;

        if (item.AddsToBag) tools.TryAddToBag(item.ToolId);
        else tools.Add(item.ToolId);
        ToolFeedbackUI.Instance?.ShowMessage(!string.IsNullOrEmpty(item.PurchaseMessage)
            ? item.PurchaseMessage
            : item.AddsToBag
                ? $"{item.DisplayName} acheté ! Il a été rangé dans votre sac (H)."
                : $"{item.DisplayName} acheté ! Il est installé dans le laboratoire de la maison.");
        Refresh();
    }

    private void BuildPanel()
    {
        panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(460f, 300f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        panel.GetComponent<Image>().color = new Color(0.09f, 0.08f, 0.07f, 0.97f);

        nameText = RuntimeUIKit.MakeText("Name", panel.transform, "", 20, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.60f),
            new Vector2(20f, -14f), new Vector2(420f, 30f));
        descriptionText = RuntimeUIKit.MakeText("Description", panel.transform, "", 14, TextAnchor.UpperLeft, new Color(0.88f, 0.88f, 0.88f),
            new Vector2(24f, -54f), new Vector2(412f, 130f));
        priceText = RuntimeUIKit.MakeText("Price", panel.transform, "", 18, TextAnchor.UpperLeft, new Color(0.98f, 0.82f, 0.35f),
            new Vector2(24f, -192f), new Vector2(200f, 26f));
        statusText = RuntimeUIKit.MakeText("Status", panel.transform, "", 14, TextAnchor.UpperRight, Color.white,
            new Vector2(226f, -196f), new Vector2(210f, 22f));

        buyButton = RuntimeUIKit.MakeButton("Buy", panel.transform, "Acheter", 16, new Color(0.25f, 0.50f, 0.30f, 1f),
            new Vector2(60f, -240f), new Vector2(170f, 38f));
        buyButton.onClick.AddListener(Buy);
        buyLabel = buyButton.GetComponentInChildren<Text>();

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 16, new Color(0.32f, 0.26f, 0.22f, 1f),
            new Vector2(250f, -240f), new Vector2(150f, 38f));
        close.onClick.AddListener(Close);
    }
}
