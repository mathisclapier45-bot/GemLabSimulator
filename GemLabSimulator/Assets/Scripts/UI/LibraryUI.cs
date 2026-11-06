using UnityEngine;
using UnityEngine.UI;

// The desk of the library: the books for sale, each with its cover, what it holds and its price. A bought book goes to the shelf of the bag (the books tab),
// where it is read. Built at runtime.
public class LibraryUI : MonoBehaviour
{
    public static LibraryUI Instance { get; private set; }

    private const float PanelWidth = 760f;
    private const float PanelHeight = 610f;
    private const float CardHeight = 112f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Ink = new Color(0.82f, 0.78f, 0.70f);
    private static readonly Color CardColor = new Color(0.17f, 0.15f, 0.14f, 1f);

    private GameObject panel;
    private RectTransform layer;
    private Text subtitle;

    public static void TryOpen()
    {
        BookShelfState.Ensure();
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<LibraryUI>();
        }
        Instance?.Open();
    }

    private void Awake()
    {
        Instance = this;
        Build();
        panel.SetActive(false);
    }

    private void Start()
    {
        var state = BookShelfState.Ensure();
        if (state != null) state.OnChanged += RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged += RefreshMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (BookShelfState.Instance != null) BookShelfState.Instance.OnChanged -= RefreshIfOpen;
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyChanged -= RefreshMoney;
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Open()
    {
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    private void RefreshMoney(int _) => RefreshIfOpen();

    private static int Money => PlayerWallet.Instance != null ? PlayerWallet.Instance.Money : 0;

    private Text Txt(string name, string content, int size, Vector2 position, Vector2 dimensions, Color color, bool bold = false, TextAnchor anchor = TextAnchor.UpperLeft)
    {
        var text = RuntimeUIKit.MakeText(name, layer, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) text.fontStyle = FontStyle.Bold;
        return text;
    }

    private void Refresh()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Destroy(layer.GetChild(i).gameObject);

        subtitle.text = $"Sur vous : {Money:N0} €   ·   les livres achetés vont dans le sac (H), onglet « Livres »";

        var row = 0;
        foreach (var info in BookCatalog.Infos) BookCard(row++, info);
        StardewKit.Skin(layer);
    }

    // One book for sale: its small cover, its title, what it holds, its price and the button to buy it.
    private void BookCard(int row, BookInfo info)
    {
        var y = -88f - row * (CardHeight + 4f);
        var owned = BookShelfState.Owns(info.Id);
        RuntimeUIKit.MakeBox("Card", layer, new Vector2(24f, y), new Vector2(PanelWidth - 48f, CardHeight), CardColor, false).raycastTarget = false;
        BookCovers.DrawSmall(layer, info, new Vector2(40f, y - 14f));

        Txt("BookTitle", info.Title, 17, new Vector2(120f, y - 12f), new Vector2(430f, 24f), Gold, true);
        Txt("BookText", info.Blurb, 12, new Vector2(120f, y - 40f), new Vector2(430f, 68f), Ink);

        if (owned)
        {
            Txt("Owned", "Dans votre sac", 14, new Vector2(PanelWidth - 200f, y - 40f), new Vector2(160f, 24f), new Color(0.55f, 0.85f, 0.55f), true, TextAnchor.UpperCenter);
            return;
        }

        Txt("Price", $"{info.Price:N0} €", 20, new Vector2(PanelWidth - 200f, y - 14f), new Vector2(160f, 28f), Gold, true, TextAnchor.UpperCenter);
        var can = Money >= info.Price;
        var buy = RuntimeUIKit.MakeButton("Buy", layer, can ? "Acheter" : "Pas assez d'argent", 13, can ? new Color(0.25f, 0.50f, 0.30f, 1f) : new Color(0.30f, 0.28f, 0.28f, 1f),
            new Vector2(PanelWidth - 200f, y - 52f), new Vector2(160f, 36f));
        buy.interactable = can;
        var id = info.Id;
        buy.onClick.AddListener(() => Buy(id));
    }

    private void Buy(string id)
    {
        var state = BookShelfState.Ensure();
        if (state == null) return;
        if (state.TryBuy(id, out var message))
        {
            Sfx.Play(Sfx.Id.Chat);
            var info = BookCatalog.Find(id);
            ToolFeedbackUI.Instance?.ShowMessage(info != null ? $"{info.Title} est dans votre sac (H), onglet « Livres »." : "Le livre est dans votre sac.");
        }
        else if (!string.IsNullOrEmpty(message))
        {
            ToolFeedbackUI.Instance?.ShowMessage(message);
        }
        Refresh();
    }

    private void Build()
    {
        panel = new GameObject("LibraryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        StardewKit.FramePanel(panel.GetComponent<Image>());

        var title = RuntimeUIKit.MakeText("Title", panel.transform, "Bibliothèque de la Ville", 22, TextAnchor.UpperLeft, Gold, new Vector2(30f, -16f), new Vector2(PanelWidth - 160f, 30f));
        title.fontStyle = FontStyle.Bold;
        subtitle = RuntimeUIKit.MakeText("Subtitle", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.62f, 0.82f, 1f), new Vector2(30f, -50f), new Vector2(PanelWidth - 60f, 18f));
        RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(PanelWidth - 108f, -14f), new Vector2(84f, 28f)).onClick.AddListener(Close);

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(panel.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        StardewKit.Skin(panel.transform);
    }
}
