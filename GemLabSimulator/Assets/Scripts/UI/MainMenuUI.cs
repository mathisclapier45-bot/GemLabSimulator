using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    private const float SlotHeight = 130f;
    private const float SlotSpacing = 20f;
    private const float SlotWidth = 560f;

    [SerializeField] private RectTransform slotsContainer;

    private readonly string[] pendingNames = new string[SaveSystem.SlotCount];

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (slotsContainer == null) return;

        for (var i = slotsContainer.childCount - 1; i >= 0; i--)
            Destroy(slotsContainer.GetChild(i).gameObject);

        for (var i = 0; i < SaveSystem.SlotCount; i++)
            BuildSlot(i);
    }

    private void BuildSlot(int index)
    {
        var slotNumber = index + 1;
        var data = SaveSystem.PeekSlot(slotNumber);
        var y = -index * (SlotHeight + SlotSpacing);

        var panelGO = new GameObject($"Slot_{slotNumber}", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(slotsContainer, false);
        var rect = panelGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(SlotWidth, SlotHeight);
        panelGO.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.17f, 1f);

        if (data != null)
            BuildOccupiedSlot(panelGO.transform, slotNumber, data);
        else
            BuildEmptySlot(panelGO.transform, index, slotNumber);
    }

    private void BuildOccupiedSlot(Transform parent, int slotNumber, SaveData data)
    {
        CreateLabel(parent, $"Emplacement {slotNumber} — {data.saveName}",
            new Vector2(16, -14), new Vector2(SlotWidth - 220, 24), 16, TextAnchor.UpperLeft, Color.white);
        CreateLabel(parent, $"Jour {data.currentDay} — {data.money}€",
            new Vector2(16, -44), new Vector2(SlotWidth - 220, 22), 13, TextAnchor.UpperLeft, new Color(0.75f, 0.75f, 0.75f));

        var loadButton = CreateButton(parent, "Charger", new Vector2(-100, 0), new Vector2(140, 36));
        loadButton.onClick.AddListener(() => LoadSlot(slotNumber, data));

        var deleteButton = CreateButton(parent, "Supprimer", new Vector2(-100, -44), new Vector2(140, 30), 12);
        deleteButton.onClick.AddListener(() => DeleteSlot(slotNumber));
    }

    private void BuildEmptySlot(Transform parent, int index, int slotNumber)
    {
        CreateLabel(parent, $"Emplacement {slotNumber} — vide",
            new Vector2(16, -14), new Vector2(SlotWidth - 220, 24), 16, TextAnchor.UpperLeft, Color.white);

        var inputGO = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputGO.transform.SetParent(parent, false);
        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(0f, 0f);
        inputRect.pivot = new Vector2(0f, 0f);
        inputRect.anchoredPosition = new Vector2(16, 14);
        inputRect.sizeDelta = new Vector2(SlotWidth - 260, 34);
        inputGO.GetComponent<Image>().color = new Color(0.22f, 0.22f, 0.26f, 1f);

        var inputText = CreateLabel(inputGO.transform, "", new Vector2(8, 0), new Vector2(-16, 0), 14, TextAnchor.MiddleLeft, Color.white);
        var inputTextRect = inputText.GetComponent<RectTransform>();
        inputTextRect.anchorMin = new Vector2(0f, 0f);
        inputTextRect.anchorMax = new Vector2(1f, 1f);
        inputTextRect.pivot = new Vector2(0.5f, 0.5f);
        inputTextRect.anchoredPosition = Vector2.zero;
        inputTextRect.sizeDelta = new Vector2(-16, 0);

        var placeholderText = CreateLabel(inputGO.transform, "Nom de la sauvegarde...", new Vector2(8, 0),
            new Vector2(-16, 0), 14, TextAnchor.MiddleLeft, new Color(0.55f, 0.55f, 0.55f));
        placeholderText.fontStyle = FontStyle.Italic;
        var placeholderRect = placeholderText.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0f, 0f);
        placeholderRect.anchorMax = new Vector2(1f, 1f);
        placeholderRect.pivot = new Vector2(0.5f, 0.5f);
        placeholderRect.anchoredPosition = Vector2.zero;
        placeholderRect.sizeDelta = new Vector2(-16, 0);

        var inputField = inputGO.GetComponent<InputField>();
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        inputField.characterLimit = 24;
        inputField.onValueChanged.AddListener(value => pendingNames[index] = value);

        var startButton = CreateButton(parent, "Commencer", new Vector2(-100, 0), new Vector2(140, 36));
        startButton.onClick.AddListener(() => StartNewSlot(slotNumber));
    }

    private void StartNewSlot(int slotNumber)
    {
        var index = slotNumber - 1;
        var name = string.IsNullOrWhiteSpace(pendingNames[index]) ? $"Partie {slotNumber}" : pendingNames[index].Trim();

        SaveSystem.CreateNewSave(slotNumber, name);
        SaveSystem.ActiveSlot = slotNumber;
        SaveSystem.PendingLoad = true;
        SceneManager.LoadScene("Home");
    }

    private void LoadSlot(int slotNumber, SaveData data)
    {
        SaveSystem.ActiveSlot = slotNumber;
        SaveSystem.PendingLoad = true;
        SceneManager.LoadScene(string.IsNullOrEmpty(data.sceneName) ? "Home" : data.sceneName);
    }

    private void DeleteSlot(int slotNumber)
    {
        SaveSystem.DeleteSlot(slotNumber);
        Refresh();
    }

    private static Text CreateLabel(Transform parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta,
        int fontSize, TextAnchor alignment, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        return txt;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize = 14)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f, 1f);

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return go.GetComponent<Button>();
    }
}
