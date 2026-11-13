using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    private const int Columns = 4;
    private const int Rows = PlayerInventory.Capacity / Columns;
    private const float SlotSize = 56f;
    private const float SlotSpacing = 8f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private GemDropZone dropZone;
    [SerializeField] private Sprite mysteryIcon;
    [SerializeField] private Text clockText;

    public Sprite MysteryIcon => mysteryIcon;

    private void Awake()
    {
        Instance = this;
        FitPanelToSlots();
    }

    // The scenes were generated with three rows of slots: the window is made taller for the rows added since.
    private void FitPanelToSlots()
    {
        const int generatedRows = 3;
        if (panel == null || slotsContainer == null || Rows <= generatedRows) return;
        var extra = (Rows - generatedRows) * (SlotSize + SlotSpacing);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, panelRect.sizeDelta.y + extra);
        slotsContainer.sizeDelta = new Vector2(slotsContainer.sizeDelta.x, slotsContainer.sizeDelta.y + extra);
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
    }

    private void Start()
    {
        AddBinderButton();

        if (PlayerInventory.Instance == null) return;
        PlayerInventory.Instance.OnInventoryChanged += Refresh;
        if (dropZone != null) dropZone.TargetContainer = PlayerInventory.Instance;
        Refresh();
    }

    // "Classeur (B)": opens the card binder from the inventory.
    private void AddBinderButton()
    {
        if (panel == null) return;
        var button = RuntimeUIKit.MakeButton("BinderButton", panel.transform, "Classeur de cartes (B)", 13, new Color(0.36f, 0.26f, 0.18f, 1f),
            Vector2.zero, new Vector2(220f, 24f));
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 6f);
        button.onClick.AddListener(() => BinderUI.Instance?.Open());
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Update()
    {
        if (TextEntry.KeyDown(KeyCode.Tab))
            panel.SetActive(!panel.activeSelf);

        if (panel.activeSelf && clockText != null)
        {
            var day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
            clockText.text = $"{GameCalendar.WithWeekday(GameCalendar.DateOf(day))} — {DayNightOverlay.GetClockLabel()}";
        }
    }

    private void Refresh()
    {
        var inventory = PlayerInventory.Instance;
        if (inventory == null) return;

        for (var i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        var totalSlots = Columns * Rows;
        for (var i = 0; i < totalSlots; i++)
        {
            var gem = i < inventory.Gems.Count ? inventory.Gems[i] : null;
            CreateSlot(inventory, gem, i);
        }
    }

    private void CreateSlot(PlayerInventory inventory, GemInstance gem, int index)
    {
        var col = index % Columns;
        var row = index / Columns;
        var x = col * (SlotSize + SlotSpacing);
        var y = -row * (SlotSize + SlotSpacing);

        var go = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(slotsContainer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);

        var isSelected = gem != null && gem == inventory.SelectedGem;
        go.GetComponent<Image>().color = isSelected
            ? new Color(0.35f, 0.55f, 0.35f, 1f)
            : new Color(0.20f, 0.20f, 0.24f, 1f);

        if (gem == null) return;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        var iconSize = (SlotSize - 12f) * gem.SizeScale;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = Vector2.zero;

        var iconImage = iconGO.GetComponent<Image>();
        // Color and shape are visible at a glance, no instrument needed: even an unidentified stone shows them.
        iconImage.sprite = gem.DisplaySprite;
        iconImage.preserveAspect = true;

        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            inventory.Select(gem);
            GemTooltipUI.Instance?.Hide();
            GemDetailUI.Instance?.Open(gem);
        });

        // Like the shelves of the GemStore: the stone grows a little under the mouse.
        go.AddComponent<InventorySlotHover>().Icon = iconRect;

        RuntimeUIKit.AddEnvelopeBadge(go.transform, gem);

        var dragSource = go.AddComponent<GemDragSource>();
        dragSource.Gem = gem;
        dragSource.SourceContainer = inventory;
        dragSource.SetIcon(iconImage);
    }
}

// Enlarges the stone icon of an inventory slot while the mouse is over it.
public class InventorySlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float HoverScale = 1.2f;

    public RectTransform Icon;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Icon != null) Icon.localScale = Vector3.one * HoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Icon != null) Icon.localScale = Vector3.one;
    }
}
