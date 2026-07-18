using UnityEngine;
using UnityEngine.UI;

public class TunnelUI : MonoBehaviour
{
    public static TunnelUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text itemText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Text buyButtonText;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (TunnelState.Instance != null) TunnelState.Instance.OnTunnelChanged += RefreshIfOpen;
        if (buyButton != null) buyButton.onClick.AddListener(Purchase);
    }

    private void OnDestroy()
    {
        if (TunnelState.Instance != null) TunnelState.Instance.OnTunnelChanged -= RefreshIfOpen;
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        var offer = TunnelState.Instance != null ? TunnelState.Instance.CurrentOffer : null;

        if (titleText != null)
            titleText.text = "Le Marchand du Tunnel";

        if (offer == null)
        {
            if (itemText != null) itemText.text = "\"Reviens plus tard... je n'ai plus rien pour toi ce soir.\"";
            if (buyButton != null) buyButton.gameObject.SetActive(false);
            return;
        }

        var price = TunnelState.Instance.GetPrice(offer);
        if (itemText != null)
            itemText.text = $"{offer.Definition.displayName}\nOrigine : {offer.Origin.name}\nPrix : {price}€";

        if (buyButton != null) buyButton.gameObject.SetActive(true);
        if (buyButtonText != null) buyButtonText.text = $"Acheter ({price}€)";
    }

    private void Purchase()
    {
        if (TunnelState.Instance == null || PlayerInventory.Instance == null || PlayerWallet.Instance == null) return;

        var offer = TunnelState.Instance.CurrentOffer;
        if (offer == null) return;

        var price = TunnelState.Instance.GetPrice(offer);
        if (PlayerWallet.Instance.Money < price)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Pas assez d'argent pour cet achat.");
            return;
        }

        if (PlayerInventory.Instance.IsFull)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Inventaire plein : impossible d'acheter cette pièce.");
            return;
        }

        var name = offer.Definition.displayName;
        if (TunnelState.Instance.TryPurchase(PlayerInventory.Instance, PlayerWallet.Instance))
            ToolFeedbackUI.Instance?.ShowMessage($"Achat discret : {name} pour {price}€.");
    }
}
