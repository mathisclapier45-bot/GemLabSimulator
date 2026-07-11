using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform slotsContainer;

    private void OnEnable()
    {
        inventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        inventory.OnInventoryChanged -= Refresh;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            panel.SetActive(!panel.activeSelf);
    }

    private void Refresh()
    {
        for (var i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        if (inventory.Gems.Count == 0)
        {
            CreateLabel("Aucune gemme en inventaire.", 0);
            return;
        }

        for (var i = 0; i < inventory.Gems.Count; i++)
            CreateSlotButton(inventory.Gems[i], i);
    }

    private void CreateLabel(string text, int index)
    {
        var go = new GameObject("EmptyLabel", typeof(RectTransform));
        go.transform.SetParent(slotsContainer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(360, 34);
        rect.anchoredPosition = new Vector2(0, -index * 40);

        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.7f, 0.7f, 0.7f);
    }

    private void CreateSlotButton(GemInstance gem, int index)
    {
        var go = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(slotsContainer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(360, 34);
        rect.anchoredPosition = new Vector2(0, -index * 40);

        var isSelected = gem == inventory.SelectedGem;
        go.GetComponent<Image>().color = isSelected
            ? new Color(0.35f, 0.55f, 0.35f, 1f)
            : new Color(0.25f, 0.25f, 0.32f, 1f);

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var label = textGO.AddComponent<Text>();
        label.text = gem.Definition.displayName + (isSelected ? "  (sélectionnée)" : "");
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        go.GetComponent<Button>().onClick.AddListener(() => inventory.Select(gem));
    }
}
