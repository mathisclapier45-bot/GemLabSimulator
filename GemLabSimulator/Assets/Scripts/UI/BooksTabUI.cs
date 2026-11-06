using System;
using UnityEngine;
using UnityEngine.UI;

// The books tab of the bag: the shelf, with a line for each book the player owns. A click on a book takes it from the shelf: the window grows, the closed book
// shows its cover, and a click opens it (GuideBookTabUI). Built by the bag, inside its panel.
public class BooksTabUI
{
    public const float ListWidth = 800f;
    public const float ListHeight = 520f;
    public const float BookWidth = 900f;
    public const float BookHeight = 690f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Ink = new Color(0.82f, 0.78f, 0.70f);
    private static readonly Color Leather = new Color(0.13f, 0.34f, 0.30f, 1f);
    private static readonly Color CardColor = new Color(0.17f, 0.15f, 0.14f, 1f);

    private readonly Action<float, float, float> resize;      // width, height and offset of the panel of the bag
    private readonly RectTransform listLayer;
    private readonly GuideBookTabUI guide;
    private bool reading;

    public GameObject Root { get; }

    public BooksTabUI(Transform parent, Action<float, float, float> resize)
    {
        this.resize = resize;
        Root = new GameObject("BooksContent", typeof(RectTransform));
        Root.transform.SetParent(parent, false);
        var rect = Root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        listLayer = new GameObject("Shelf", typeof(RectTransform)).GetComponent<RectTransform>();
        listLayer.SetParent(Root.transform, false);
        listLayer.anchorMin = Vector2.zero;
        listLayer.anchorMax = Vector2.one;
        listLayer.offsetMin = listLayer.offsetMax = Vector2.zero;

        guide = new GuideBookTabUI(Root.transform, ShowShelf);
        guide.Root.SetActive(false);
    }

    // The tab is shown: always on the shelf.
    public void Show() => ShowShelf();

    public void HandleKeys()
    {
        if (reading) guide.HandleKeys();
    }

    private void ShowShelf()
    {
        reading = false;
        guide.Root.SetActive(false);
        listLayer.gameObject.SetActive(true);
        resize?.Invoke(ListWidth, ListHeight, -190f);
        BuildShelf();
    }

    private void OpenGuide()
    {
        reading = true;
        listLayer.gameObject.SetActive(false);
        guide.Root.SetActive(true);
        resize?.Invoke(BookWidth, BookHeight, -150f);
        guide.Open();
    }

    private Text Txt(string name, string content, int size, TextAnchor anchor, Color color, Vector2 position, Vector2 dimensions, bool bold = false)
    {
        var text = RuntimeUIKit.MakeText(name, listLayer, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        if (bold) text.fontStyle = FontStyle.Bold;
        return text;
    }

    private void BuildShelf()
    {
        for (var i = listLayer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(listLayer.GetChild(i).gameObject);

        Txt("Title", "MES LIVRES", 22, TextAnchor.UpperLeft, Gold, new Vector2(24f, -50f), new Vector2(420f, 28f), true);
        RuntimeUIKit.MakeBox("Accent", listLayer, new Vector2(24f, -80f), new Vector2(60f, 3f), Leather, false).raycastTarget = false;
        Txt("Hint", "Cliquez sur un livre pour le prendre et l'ouvrir.", 12, TextAnchor.UpperLeft, Ink, new Vector2(94f, -76f), new Vector2(500f, 16f));

        var row = 0;
        if (GuideBookState.HasBook)
        {
            var count = GemDatabase.Instance != null ? GemDatabase.Instance.allGems.Count : 0;
            BookCard(row++, "Le Guide du gemmologue", $"Toutes les pages du grimoire, en un seul volume : {count} espèces avec leurs propriétés, leurs traitements possibles et ce que montrent les instruments du laboratoire.", OpenGuide);
        }

        if (row == 0)
            Txt("None", "Vous n'avez pas encore de livre. La bibliothèque de la ville en vend.", 14, TextAnchor.UpperLeft, Color.white, new Vector2(30f, -110f), new Vector2(600f, 24f));
    }

    // One line of the shelf: the small closed book, its title, what it holds.
    private void BookCard(int row, string title, string description, Action open)
    {
        var y = -104f - row * 112f;
        RuntimeUIKit.MakeBox("Card", listLayer, new Vector2(24f, y), new Vector2(752f, 100f), CardColor, false).raycastTarget = false;

        // The small book: leather, a gold frame, a diamond.
        RuntimeUIKit.MakeBox("BookBack", listLayer, new Vector2(40f, y - 10f), new Vector2(58f, 80f), new Color(0.80f, 0.72f, 0.55f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("Book", listLayer, new Vector2(38f, y - 8f), new Vector2(58f, 80f), Leather, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("BookSpine", listLayer, new Vector2(38f, y - 8f), new Vector2(7f, 80f), new Color(0.07f, 0.17f, 0.15f, 1f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("BookFrame", listLayer, new Vector2(50f, y - 14f), new Vector2(40f, 1f), new Color(0.86f, 0.70f, 0.30f), false).raycastTarget = false;
        RuntimeUIKit.MakeBox("BookFrame", listLayer, new Vector2(50f, y - 74f), new Vector2(40f, 1f), new Color(0.86f, 0.70f, 0.30f), false).raycastTarget = false;
        var gem = RuntimeUIKit.MakeBox("BookGem", listLayer, Vector2.zero, new Vector2(18f, 18f), new Color(0.62f, 0.86f, 0.98f), false);
        gem.raycastTarget = false;
        gem.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        gem.rectTransform.anchoredPosition = new Vector2(70f, y - 44f);
        gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Txt("BookTitle", title, 17, TextAnchor.UpperLeft, Gold, new Vector2(116f, y - 12f), new Vector2(480f, 24f), true);
        Txt("BookText", description, 12, TextAnchor.UpperLeft, Ink, new Vector2(116f, y - 40f), new Vector2(480f, 52f));

        var button = RuntimeUIKit.MakeButton("Open", listLayer, "Ouvrir", 14, new Color(0.25f, 0.50f, 0.30f, 1f), new Vector2(632f, y - 32f), new Vector2(120f, 36f));
        button.onClick.AddListener(() => open());

        // The whole line can be clicked.
        var hit = RuntimeUIKit.MakeBox("Hit", listLayer, new Vector2(24f, y), new Vector2(600f, 100f), new Color(1f, 1f, 1f, 0.004f), false);
        hit.gameObject.AddComponent<Button>().onClick.AddListener(() => open());
    }
}
