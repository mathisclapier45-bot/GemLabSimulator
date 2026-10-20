using UnityEngine;
using UnityEngine.UI;

public enum CabinetKind { Donations, ClientOrders }

// Wardrobe window (donations, or client orders for the blue wardrobes): a grid of slots like the chest's. Drag a stone
// from the inventory into it, or drag one back out. Built at runtime, so no scene wiring is needed.
public class CabinetUI : MonoBehaviour
{
    public static CabinetUI Instance { get; private set; }

    private const int Columns = 6;
    private const int Rows = 4;
    private const float SlotSize = 56f;
    private const float SlotSpacing = 8f;

    private GameObject panel;
    private RectTransform slotsContainer;
    private Text titleText;
    private Text hintText;
    private Image panelImage;
    private CabinetKind kind = CabinetKind.Donations;
    private ICabinetStorage subscribedDonations;
    private ICabinetStorage subscribedOrders;

    private ICabinetStorage Storage
    {
        get
        {
            if (kind == CabinetKind.ClientOrders)
            {
                // Scenes generated before the order cabinet existed do not carry the component: add it to the player.
                if (PlayerOrderCabinet.Instance == null && PlayerController.Instance != null)
                    PlayerController.Instance.gameObject.AddComponent<PlayerOrderCabinet>();
                return PlayerOrderCabinet.Instance;
            }
            return PlayerCabinet.Instance;
        }
    }

    public static void TryToggle(CabinetKind requested = CabinetKind.Donations)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<CabinetUI>();
        }

        if (Instance == null) return;
        if (Instance.panel.activeSelf && Instance.kind == requested) Instance.Close();
        else Instance.Open(requested);
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (subscribedDonations != null) subscribedDonations.OnCabinetChanged -= RefreshIfOpen;
        if (subscribedOrders != null) subscribedOrders.OnCabinetChanged -= RefreshIfOpen;
    }

    private void EnsureSubscribed()
    {
        if (subscribedDonations == null && PlayerCabinet.Instance != null)
        {
            subscribedDonations = PlayerCabinet.Instance;
            subscribedDonations.OnCabinetChanged += RefreshIfOpen;
        }

        var orders = Storage;
        if (kind == CabinetKind.ClientOrders && subscribedOrders == null && orders != null)
        {
            subscribedOrders = orders;
            subscribedOrders.OnCabinetChanged += RefreshIfOpen;
        }
    }

    public void Open(CabinetKind requested)
    {
        kind = requested;
        EnsureSubscribed();
        ApplyKind();
        Open();
    }

    private void ApplyKind()
    {
        var orders = kind == CabinetKind.ClientOrders;
        titleText.text = orders ? "Armoire des commandes" : "Armoire des dons";
        titleText.color = orders ? new Color(0.65f, 0.82f, 0.98f) : new Color(0.95f, 0.85f, 0.65f);
        hintText.text = orders
            ? "Rangez ici les pierres confiées par les clients."
            : "Rangez ici les dons identifiés, à conserver.";
        panelImage.color = orders ? new Color(0.07f, 0.09f, 0.13f, 0.96f) : new Color(0.10f, 0.08f, 0.07f, 0.96f);

        var zone = panel.GetComponent<GemDropZone>();
        if (zone != null) zone.TargetContainer = Storage;
    }

    public void Open()
    {
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        var cabinet = Storage;
        if (cabinet == null) return;

        for (var i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        for (var i = 0; i < Columns * Rows; i++)
            CreateSlot(cabinet, i < cabinet.Gems.Count ? cabinet.Gems[i] : null, i);
    }

    private void CreateSlot(ICabinetStorage cabinet, GemInstance gem, int index)
    {
        var x = index % Columns * (SlotSize + SlotSpacing);
        var y = -(index / Columns) * (SlotSize + SlotSpacing);

        var go = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(slotsContainer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);
        go.GetComponent<Image>().color = new Color(0.20f, 0.18f, 0.16f, 1f);

        if (gem == null) return;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        var iconSize = (SlotSize - 12f) * gem.SizeScale;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = Vector2.zero;

        var iconImage = iconGO.GetComponent<Image>();
        iconImage.sprite = gem.DisplaySprite;
        iconImage.preserveAspect = true;

        RuntimeUIKit.AddEnvelopeBadge(go.transform, gem);

        var dragSource = go.AddComponent<GemDragSource>();
        dragSource.Gem = gem;
        dragSource.SourceContainer = cabinet;
        dragSource.SetIcon(iconImage);
    }

    private void BuildPanel()
    {
        panel = new GameObject("CabinetPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400f, 350f);
        rect.anchoredPosition = Vector2.zero;
        panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.10f, 0.08f, 0.07f, 0.96f);

        // Dropping anywhere on the panel stores the stone in the cabinet.
        var dropZone = panel.AddComponent<GemDropZone>();
        dropZone.TargetContainer = PlayerCabinet.Instance;

        titleText = RuntimeUIKit.MakeText("Title", panel.transform, "Armoire des dons", 18, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.65f),
            new Vector2(20f, -12f), new Vector2(360f, 26f));
        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "Rangez ici les dons identifiés, à conserver.", 11, TextAnchor.UpperCenter,
            new Color(0.7f, 0.65f, 0.6f), new Vector2(20f, -36f), new Vector2(360f, 16f));

        var slotsGO = new GameObject("Slots", typeof(RectTransform));
        slotsGO.transform.SetParent(panel.transform, false);
        slotsContainer = slotsGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(slotsContainer, new Vector2(12f, -58f), new Vector2(376f, 248f));

        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.30f, 0.25f, 0.22f, 1f),
            new Vector2(130f, -314f), new Vector2(140f, 28f));
        close.onClick.AddListener(Close);
    }

    // The drop zone needs the cabinet, which only exists once the player does: bind it late if it was missing.
    private void LateUpdate()
    {
        if (panel == null || !panel.activeSelf) return;
        var zone = panel.GetComponent<GemDropZone>();
        if (zone != null && zone.TargetContainer == null) zone.TargetContainer = Storage;
    }
}
