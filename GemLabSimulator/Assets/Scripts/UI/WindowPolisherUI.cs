using UnityEngine;
using UnityEngine.UI;

// Interface of the window-polishing machine, built at runtime like LapidaryUI.
public class WindowPolisherUI : MonoBehaviour
{
    public static WindowPolisherUI Instance { get; private set; }

    private const float PanelWidth = 420f;
    private const float PanelHeight = 400f;

    private static readonly Color PanelColor = new Color(0.10f, 0.09f, 0.09f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private GameObject panel;
    private Image slotImage;
    private Text slotLabel;
    private Text stoneInfoText;
    private Text resultText;
    private Button polishButton;
    private GemInstance stone;

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    public void Open()
    {
        stone = null;
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void SetStone(GemInstance gem)
    {
        if (!gem.IsRough)
        {
            ShowRefusal("Cette pierre est déjà taillée : il n'y a rien à fenêtrer.");
            return;
        }

        stone = gem;
        Refresh();
    }

    private void ShowRefusal(string message)
    {
        stone = null;
        Refresh();
        resultText.text = message;
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        polishButton.interactable = false;

        if (stone == null)
        {
            slotImage.enabled = false;
            slotImage.sprite = null;
            slotLabel.text = "Déposez\nune pierre\nbrute ici";
            stoneInfoText.text = "";
            resultText.text = "Glissez une pierre brute de votre inventaire dans la case.";
            return;
        }

        slotImage.enabled = true;
        slotImage.sprite = stone.DisplaySprite;
        slotLabel.text = "";
        stoneInfoText.text =
            $"{(stone.IsIdentified ? stone.Definition.displayName : "Pierre brute non identifiée")}\n" +
            $"Poids : {stone.CaratWeight:F2} ct\n" +
            "(clic sur la case pour retirer)";

        if (stone.HasWindow)
        {
            resultText.text = "Une fenêtre est déjà polie sur cette pierre.\n\n" + InclusionWindowReport.Build(stone);
        }
        else
        {
            resultText.text = "Cette pierre n'a pas encore de fenêtre.";
            polishButton.interactable = true;
        }
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, gem)) return true;
        }
        return false;
    }

    private void Polish()
    {
        if (stone == null || stone.HasWindow) return;

        stone.OpenWindow();
        Refresh();
        resultText.text = "Fenêtre polie. Vous pouvez maintenant observer l'intérieur au microscope.\n\n" +
                          InclusionWindowReport.Build(stone);
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("WindowPolisherPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-130f, 0f);
        panel.GetComponent<Image>().color = PanelColor;

        MakeText("Title", panel.transform, "Polisseuse à fenêtre", 20, TextAnchor.UpperCenter, Gold,
            new Vector2(20f, -12f), new Vector2(PanelWidth - 40f, 28f));
        MakeText("Hint", panel.transform,
            "Une petite facette plane polie sur la pierre brute permet d'en observer l'intérieur.", 12,
            TextAnchor.UpperCenter, new Color(0.8f, 0.8f, 0.8f), new Vector2(20f, -42f), new Vector2(PanelWidth - 40f, 34f));

        var slotGO = new GameObject("Slot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LapidarySlotDropZone));
        slotGO.transform.SetParent(panel.transform, false);
        SetTopLeft(slotGO.GetComponent<RectTransform>(), new Vector2(20f, -84f), new Vector2(88f, 88f));
        slotGO.GetComponent<Image>().color = new Color(0.20f, 0.18f, 0.16f, 1f);
        slotGO.GetComponent<Button>().onClick.AddListener(ClearStone);
        slotGO.GetComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotGO.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        slotImage = iconGO.GetComponent<Image>();
        slotImage.preserveAspect = true;
        slotImage.raycastTarget = false;
        slotImage.enabled = false;

        slotLabel = MakeText("SlotLabel", slotGO.transform, "", 11, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f),
            Vector2.zero, new Vector2(88f, 88f));
        slotLabel.raycastTarget = false;

        stoneInfoText = MakeText("StoneInfo", panel.transform, "", 12, TextAnchor.UpperLeft, Color.white,
            new Vector2(116f, -84f), new Vector2(PanelWidth - 136f, 88f));

        polishButton = MakeButton("Polish", panel.transform, "Polir la fenêtre", 14, new Vector2(20f, -184f),
            new Vector2(PanelWidth - 40f, 32f));
        polishButton.onClick.AddListener(Polish);

        MakeText("ResultHeader", panel.transform, "Observation", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(20f, -228f), new Vector2(PanelWidth - 40f, 20f));
        resultText = MakeText("Result", panel.transform, "", 13, TextAnchor.UpperLeft, Color.white,
            new Vector2(20f, -250f), new Vector2(PanelWidth - 40f, 100f));

        var close = MakeButton("Close", panel.transform, "Fermer", 14,
            new Vector2((PanelWidth - 160f) / 2f, -PanelHeight + 44f), new Vector2(160f, 32f));
        close.onClick.AddListener(Close);
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Text MakeText(string name, Transform parent, string content, int fontSize, TextAnchor alignment,
        Color color, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        var text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button MakeButton(string name, Transform parent, string label, int fontSize, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetTopLeft(go.GetComponent<RectTransform>(), position, size);
        go.GetComponent<Image>().color = ButtonColor;

        var text = MakeText("Label", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, size);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.raycastTarget = false;
        return go.GetComponent<Button>();
    }
}
