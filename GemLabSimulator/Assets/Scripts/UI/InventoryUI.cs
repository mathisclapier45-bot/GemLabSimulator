using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private const int Columns = 4;
    private const int Rows = 3;
    private const float SlotSize = 56f;
    private const float SlotSpacing = 8f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private GemDropZone dropZone;
    [SerializeField] private Sprite mysteryIcon;

    private void Start()
    {
        if (PlayerInventory.Instance == null) return;
        PlayerInventory.Instance.OnInventoryChanged += Refresh;
        if (dropZone != null) dropZone.TargetContainer = PlayerInventory.Instance;
        Refresh();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= Refresh;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            panel.SetActive(!panel.activeSelf);
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
        iconImage.sprite = gem.IsIdentified ? gem.DisplaySprite : mysteryIcon;
        iconImage.preserveAspect = true;

        var button = go.AddComponent<Button>();
        button.onClick.AddListener(() => inventory.Select(gem));

        var dragSource = go.AddComponent<GemDragSource>();
        dragSource.Gem = gem;
        dragSource.SourceContainer = inventory;
        dragSource.SetIcon(iconImage);
    }
}
