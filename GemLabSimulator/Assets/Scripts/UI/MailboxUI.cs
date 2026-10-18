using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailboxUI : MonoBehaviour
{
    public static MailboxUI Instance { get; private set; }

    private const float RowWidth = 400f;
    private const float RowHeight = 44f;
    private const float RowSpacing = 8f;

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform content;
    [SerializeField] private Text tabHintText;

    private bool showingReceive = true;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (MailboxState.Instance != null) MailboxState.Instance.OnMailboxChanged += RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (MailboxState.Instance != null) MailboxState.Instance.OnMailboxChanged -= RefreshIfOpen;
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Toggle()
    {
        var opening = panel != null && !panel.activeSelf;
        if (panel != null) panel.SetActive(opening);
        if (opening)
        {
            MailboxState.Instance?.EnsureStocked();
            ShowReceiveTab();
        }
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void ShowReceiveTab()
    {
        showingReceive = true;
        Refresh();
    }

    public void ShowSendTab()
    {
        showingReceive = false;
        Refresh();
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

        if (tabHintText != null)
        {
            var remaining = MailboxState.Instance != null ? MailboxState.Instance.LettersRemainingToday : 0;
            tabHintText.text = showingReceive
                ? $"Récupérer — {remaining} lettre(s) restante(s) aujourd'hui"
                : "Envoyer — gemmes de clients identifiées, prêtes à être renvoyées";
        }

        var rowCount = showingReceive ? BuildReceiveRows() : BuildSendRows();

        content.sizeDelta = new Vector2(content.sizeDelta.x,
            Mathf.Max(rowCount * (RowHeight + RowSpacing) + 10f, 40f));
    }

    private int BuildReceiveRows()
    {
        var letters = MailboxState.Instance != null ? MailboxState.Instance.PendingLetters : null;
        if (letters == null || letters.Count == 0)
        {
            CreateRow(0, "Aucune lettre en attente.");
            return 1;
        }

        var canTake = MailboxState.Instance != null && MailboxState.Instance.LettersRemainingToday > 0;

        for (var i = 0; i < letters.Count; i++)
        {
            var letter = letters[i];
            var label = letter.IsDonation
                ? "Don d'un généreux mécène — à conserver une fois identifié"
                : $"Lettre de {letter.ClientName}";
            var row = CreateRow(i, label);
            var button = CreateRowButton(row.transform, "Prendre");
            button.interactable = canTake;
            button.onClick.AddListener(() => TakeLetter(letter));
        }
        return letters.Count;
    }

    private int BuildSendRows()
    {
        var inventory = PlayerInventory.Instance;
        var sendable = new List<GemInstance>();
        if (inventory != null)
        {
            foreach (var gem in inventory.Gems)
                if (gem.IsIdentified && gem.IsClientStone) sendable.Add(gem);
        }

        if (sendable.Count == 0)
        {
            CreateRow(0, "Aucune gemme de client identifiée à renvoyer.");
            return 1;
        }

        for (var i = 0; i < sendable.Count; i++)
        {
            var gem = sendable[i];
            var row = CreateRow(i, $"{gem.Definition.displayName} (N° {gem.DisplayId}) — {gem.FinalValue}€");
            var button = CreateRowButton(row.transform, "Envoyer");
            button.onClick.AddListener(() => SendGem(gem));
        }
        return sendable.Count;
    }

    private void TakeLetter(GemInstance letter)
    {
        if (PlayerInventory.Instance == null || MailboxState.Instance == null) return;

        if (MailboxState.Instance.LettersRemainingToday <= 0)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Vous avez déjà reçu vos 5 lettres aujourd'hui. Revenez demain !");
            return;
        }

        if (!MailboxState.Instance.TryTakeLetter(letter, PlayerInventory.Instance))
        {
            ToolFeedbackUI.Instance?.ShowMessage("Inventaire plein : traitez d'abord une gemme.");
            return;
        }

        ToolFeedbackUI.Instance?.ShowMessage(letter.IsDonation
            ? "Vous avez récupéré un don anonyme, à conserver une fois identifié."
            : $"Vous avez récupéré une lettre de {letter.ClientName}.");
        MailboxState.Instance.EnsureStocked();
    }

    private void SendGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null || PlayerWallet.Instance == null) return;

        PlayerInventory.Instance.RemoveGem(gem);
        PlayerWallet.Instance.AddMoney(gem.FinalValue);
        QuestState.Instance?.ReportGemSent(gem.FinalValue);
        ToolFeedbackUI.Instance?.ShowMessage($"Colis envoyé à {gem.ClientName}. +{gem.FinalValue}€");
    }

    private GameObject CreateRow(int index, string label)
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
        textRect.sizeDelta = new Vector2(RowWidth - 110f, 0f);
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;

        return rowGO;
    }

    private Button CreateRowButton(Transform parent, string label)
    {
        var buttonGO = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);
        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-8f, 0f);
        rect.sizeDelta = new Vector2(90f, 32f);
        buttonGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f, 1f);

        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        var text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 13;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return buttonGO.GetComponent<Button>();
    }
}
