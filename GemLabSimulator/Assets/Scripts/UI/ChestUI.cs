using UnityEngine;
using UnityEngine.UI;

public class ChestUI : MonoBehaviour
{
    public static ChestUI Instance { get; private set; }

    private const int Columns = 6;
    private const int Rows = 4;
    private const float SlotSize = 56f;
    private const float SlotSpacing = 8f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private GemDropZone dropZone;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerChest.Instance == null) return;
        PlayerChest.Instance.OnChestChanged += Refresh;
        if (dropZone != null) dropZone.TargetContainer = PlayerChest.Instance;
        Refresh();
    }

    private void OnDestroy()
    {
        if (PlayerChest.Instance != null)
            PlayerChest.Instance.OnChestChanged -= Refresh;
    }

    public void Toggle()
    {
        panel.SetActive(!panel.activeSelf);
    }

    private void Refresh()
    {
        var chest = PlayerChest.Instance;
        if (chest == null) return;

        for (var i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        var totalSlots = Columns * Rows;
        for (var i = 0; i < totalSlots; i++)
        {
            var gem = i < chest.Gems.Count ? chest.Gems[i] : null;
            CreateSlot(chest, gem, i);
        }
    }

    private void CreateSlot(PlayerChest chest, GemInstance gem, int index)
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
        go.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.24f, 1f);

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
        iconImage.sprite = gem.DisplaySprite;
        iconImage.preserveAspect = true;

        var dragSource = go.AddComponent<GemDragSource>();
        dragSource.Gem = gem;
        dragSource.SourceContainer = chest;
        dragSource.SetIcon(iconImage);
    }
}
