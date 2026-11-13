using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }

    private const float RowWidth = 400f;
    private const float RowHeight = 64f;
    private const float RowSpacing = 10f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform content;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (QuestState.Instance != null) QuestState.Instance.OnQuestsChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (QuestState.Instance != null) QuestState.Instance.OnQuestsChanged -= RefreshIfOpen;
    }

    private void Update()
    {
        if (TextEntry.KeyDown(KeyCode.J))
            Toggle();
    }

    public void Toggle()
    {
        var opening = panel != null && !panel.activeSelf;
        if (panel != null) panel.SetActive(opening);
        if (opening) Refresh();
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
        if (content == null) return;

        for (var i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        var quests = QuestState.Instance != null ? QuestState.Instance.ActiveQuests : null;
        if (quests == null || quests.Count == 0)
        {
            CreateRow(0, "Aucune quête disponible pour le moment.", null);
            content.sizeDelta = new Vector2(content.sizeDelta.x, RowHeight + RowSpacing);
            return;
        }

        for (var i = 0; i < quests.Count; i++)
            CreateRow(i, BuildQuestLabel(quests[i]), quests[i]);

        content.sizeDelta = new Vector2(content.sizeDelta.x, quests.Count * (RowHeight + RowSpacing) + 10f);
    }

    private static string BuildQuestLabel(Quest quest)
    {
        return $"{quest.Description}\nProgression : {Mathf.Min(quest.CurrentProgress, quest.TargetAmount)}/{quest.TargetAmount} — Récompense : {quest.Reward}€";
    }

    private void CreateRow(int index, string label, Quest quest)
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

        if (quest == null) return;

        var buttonGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(rowGO.transform, false);
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0.5f);
        buttonRect.anchorMax = new Vector2(1f, 0.5f);
        buttonRect.pivot = new Vector2(1f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-8f, 0f);
        buttonRect.sizeDelta = new Vector2(100f, 36f);
        buttonGO.GetComponent<Image>().color = quest.IsCompleted
            ? new Color(0.30f, 0.50f, 0.30f, 1f)
            : new Color(0.22f, 0.22f, 0.26f, 1f);

        var buttonTextGO = new GameObject("Label", typeof(RectTransform));
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        var buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        buttonTextRect.anchoredPosition = Vector2.zero;
        var buttonText = buttonTextGO.AddComponent<Text>();
        buttonText.text = quest.IsCompleted ? "Réclamer" : "En cours";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 13;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        var button = buttonGO.GetComponent<Button>();
        button.interactable = quest.IsCompleted;
        button.onClick.AddListener(() => Claim(quest));
    }

    private void Claim(Quest quest)
    {
        if (PlayerWallet.Instance == null || QuestState.Instance == null) return;

        if (QuestState.Instance.TryClaim(quest, PlayerWallet.Instance))
            ToolFeedbackUI.Instance?.ShowMessage($"Quête accomplie ! +{quest.Reward}€");
    }
}
