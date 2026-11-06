using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The reader of the books of the library, in the books tab of the bag: first the closed book, its cover in leather with gold lines (a click opens it), then an
// open book with two cream pages side by side and a red bookmark. First spread: the title page and how to read the book. Then the index (every line a link),
// then one spread for each entry of the book: a species for the Guide, a treatment, a deposit, a chapter of the market. Built by the books tab.
public class BookReaderUI
{
    private const float BookX = 24f;
    private const float BookY = -52f;
    private const float BookW = 852f;
    private const float BookH = 590f;

    private const float PageW = 404f;
    private const float PageH = 560f;
    private const float PageY = -66f;
    private const float LeftX = 40f;
    private const float RightX = 456f;
    private const float Margin = 30f;

    private const int TocPerPage = 20;
    private const float TocRow = 22f;

    private static readonly Color Cream = new Color(0.96f, 0.92f, 0.80f, 1f);
    private static readonly Color Edge = new Color(0.78f, 0.70f, 0.52f, 1f);
    private static readonly Color Ink = new Color(0.22f, 0.15f, 0.10f, 1f);
    private static readonly Color Burgundy = new Color(0.48f, 0.16f, 0.14f, 1f);
    private static readonly Color Muted = new Color(0.45f, 0.38f, 0.30f, 1f);
    private static readonly Color Rule = new Color(0.62f, 0.50f, 0.30f, 0.75f);
    private static readonly Color Ribbon = new Color(0.62f, 0.16f, 0.18f, 1f);

    private readonly RectTransform layer;
    private readonly Dictionary<BookEntry, Sprite> spectra = new Dictionary<BookEntry, Sprite>();

    private BookInfo info;
    private BookContent book;
    private Color leather;
    private Color leatherDark;
    private Color foil;

    private readonly Action back;
    private RectTransform frame;        // the cover, the pages and the spine of the open book
    private RectTransform content;      // what changes from one spread to the next
    private int spread = -1;            // -1: the closed book, 0: the title page, then the index, then the entries
    private bool built;

    public GameObject Root { get; }

    public BookReaderUI(Transform parent, Action back)
    {
        this.back = back;
        Root = new GameObject("BookReaderContent", typeof(RectTransform));
        Root.transform.SetParent(parent, false);
        var rect = Root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(Root.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
    }

    private int TocSpreads => Mathf.Max(1, Mathf.CeilToInt(book.Entries.Count / (float)(2 * TocPerPage)));
    private int SpreadCount => 1 + TocSpreads + book.Entries.Count;
    private int FirstEntrySpread => 1 + TocSpreads;

    private static int PageNumber(int spreadIndex, int side) => spreadIndex * 2 + 1 + side;

    // Takes a book from the shelf: it shows its cover, closed.
    public void Open(string id)
    {
        info = BookCatalog.Find(id);
        if (info == null) return;
        book = BookCatalog.Build(id);
        leather = info.Leather;
        leatherDark = info.LeatherDark;
        foil = info.Foil;
        spectra.Clear();
        spread = -1;
        BuildBook();
        ShowSpread(-1);
    }

    // The arrow keys turn the pages while the book is open.
    public void HandleKeys()
    {
        if (!built) return;
        if (Input.GetKeyDown(KeyCode.RightArrow)) ShowSpread(spread + 1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) ShowSpread(spread - 1);
    }

    // ---- Small helpers ---------------------------------------------------------------------------------------

    private static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var box = RuntimeUIKit.MakeBox(name, parent, new Vector2(x, y), new Vector2(w, h), color, false);
        box.raycastTarget = false;
        return box;
    }

    private static Text Label(Transform parent, string name, string text, int size, TextAnchor anchor, Color color, float x, float y, float w, float h,
        bool bold = false, bool italic = false)
    {
        var label = RuntimeUIKit.MakeText(name, parent, text, size, anchor, color, new Vector2(x, y), new Vector2(w, h));
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.fontStyle = bold && italic ? FontStyle.BoldAndItalic : bold ? FontStyle.Bold : italic ? FontStyle.Italic : FontStyle.Normal;
        return label;
    }

    // A text that can be clicked, on the page.
    private static void Link(Transform parent, string name, string text, int size, TextAnchor anchor, Color color, float x, float y, float w, float h, Action onClick)
    {
        var hit = RuntimeUIKit.MakeBox(name, parent, new Vector2(x, y), new Vector2(w, h), new Color(1f, 1f, 1f, 0.004f), false);
        var button = hit.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());
        var label = RuntimeUIKit.MakeText("Label", hit.transform, text, size, anchor, color, Vector2.zero, new Vector2(w, h));
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // A square turned by 45 degrees, around its centre (given from the top left of the layer).
    private static void Lozenge(Transform parent, string name, float cx, float cy, float size, Color color)
    {
        var box = RuntimeUIKit.MakeBox(name, parent, Vector2.zero, new Vector2(size, size), color, false);
        box.raycastTarget = false;
        var rect = box.rectTransform;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(cx, cy);
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    // A column of text that scrolls with the mouse wheel when it is longer than its place. Returns true when it overflows.
    private static bool Body(Transform parent, string text, float x, float y, float w, float h, int size)
    {
        var go = new GameObject("Body", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        go.transform.SetParent(parent, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(w, h));
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.004f);

        var label = RuntimeUIKit.MakeText("Text", go.transform, Paper(BoldLabels(text)), size, TextAnchor.UpperLeft, Ink, Vector2.zero, new Vector2(w, 10f));
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = true;
        label.lineSpacing = 1.12f;
        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-6f, 0f);
        rect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = go.GetComponent<ScrollRect>();
        scroll.content = rect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        scroll.verticalNormalizedPosition = 1f;
        return rect.rect.height > h + 2f;
    }

    // The name of each piece of data goes in bold: what comes before the first " : " of a line (after the dot of a list), when it is short.
    private static string BoldLabels(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Contains("<")) continue;
            var start = line.StartsWith("· ") ? 2 : 0;
            var at = line.IndexOf(" : ", start, StringComparison.Ordinal);
            if (at < start + 2 || at - start > 38) continue;
            lines[i] = line.Substring(0, start) + "<b>" + line.Substring(start, at - start) + " :</b>" + line.Substring(at + 2);
        }
        return string.Join("\n", lines);
    }

    // The rich text of the grimoire is written for a dark window: on the cream paper the pale gold becomes burgundy and the grey-blue a brown.
    private static string Paper(string text)
    {
        return text.Replace("#F2D9A6", "#7A2E2A").Replace("#9AA3B5", "#7B6A55");
    }

    // ---- The book --------------------------------------------------------------------------------------------

    // The cover, the two pages, the spine and the bookmark: they stay while the spreads change.
    private void BuildBook()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(layer.GetChild(i).gameObject);

        frame = new GameObject("OpenBook", typeof(RectTransform)).GetComponent<RectTransform>();
        frame.SetParent(layer, false);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = frame.offsetMax = Vector2.zero;

        Box(frame, "CoverShadow", BookX + 5f, BookY - 5f, BookW, BookH, new Color(0f, 0f, 0f, 0.35f));
        Box(frame, "Cover", BookX, BookY, BookW, BookH, leather);
        // foil lines along the edge of the cover, and a stud at each corner.
        Box(frame, "GoldTop", BookX + 6f, BookY - 6f, BookW - 12f, 2f, foil);
        Box(frame, "GoldBottom", BookX + 6f, BookY - BookH + 8f, BookW - 12f, 2f, foil);
        Box(frame, "GoldLeft", BookX + 6f, BookY - 6f, 2f, BookH - 12f, foil);
        Box(frame, "GoldRight", BookX + BookW - 8f, BookY - 6f, 2f, BookH - 12f, foil);
        foreach (var cx in new[] { BookX + 7f, BookX + BookW - 7f })
            foreach (var cy in new[] { BookY - 7f, BookY - BookH + 9f })
                Lozenge(frame, "Stud", cx, cy, 8f, foil);

        DrawPage(LeftX, true);
        DrawPage(RightX, false);
        Box(frame, "Spine", LeftX + PageW, PageY, RightX - LeftX - PageW, PageH, leatherDark);

        content = new GameObject("Spread", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(layer, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = content.offsetMax = Vector2.zero;
        built = true;
    }

    // The bookmark hangs from the top of the right page: a click on it goes back to the index.
    private void DrawBookmark()
    {
        const float x = RightX + PageW - 26f;
        var hit = RuntimeUIKit.MakeBox("Bookmark", content, new Vector2(x, PageY + 10f), new Vector2(12f, 84f), Ribbon, false);
        var button = hit.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => ShowSpread(1));
        // The V cut in the end of the ribbon.
        Lozenge(content, "Notch", x + 6f, PageY + 10f - 84f, 9f, Cream);
    }

    private void DrawPage(float x, bool left)
    {
        // The stack of the other pages, seen on the outer side and below.
        Box(frame, "Stack", left ? x - 4f : x, PageY - 4f, PageW + 4f, PageH + 4f, Edge);
        Box(frame, "Page", x, PageY, PageW, PageH, Cream);

        // The shadow in the fold.
        for (var k = 0; k < 8; k++)
        {
            var width = 6f * (k + 1);
            Box(frame, "Fold", left ? x + PageW - width : x, PageY, width, PageH, new Color(0.35f, 0.25f, 0.12f, 0.05f));
        }

        // A frame inside the page, with a lozenge in each corner.
        const float inset = 12f;
        Box(frame, "FrameTop", x + inset, PageY - inset, PageW - 2f * inset, 1f, Rule);
        Box(frame, "FrameBottom", x + inset, PageY - PageH + inset, PageW - 2f * inset, 1f, Rule);
        Box(frame, "FrameLeft", x + inset, PageY - inset, 1f, PageH - 2f * inset, Rule);
        Box(frame, "FrameRight", x + PageW - inset, PageY - inset, 1f, PageH - 2f * inset, Rule);
        foreach (var cx in new[] { x + inset, x + PageW - inset })
            foreach (var cy in new[] { PageY - inset, PageY - PageH + inset })
                Lozenge(frame, "Corner", cx, cy, 7f, Rule);
    }

    // ---- The spreads -----------------------------------------------------------------------------------------

    private void ShowSpread(int index)
    {
        if (!built) return;
        spread = Mathf.Clamp(index, -1, SpreadCount - 1);
        frame.gameObject.SetActive(spread >= 0);

        for (var i = content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(content.GetChild(i).gameObject);

        if (spread < 0)
        {
            DrawClosedBook();
        }
        else
        {
            if (spread == 0) DrawTitleSpread();
            else if (spread < FirstEntrySpread) DrawIndexSpread(spread - 1);
            else DrawEntrySpread(book.Entries[spread - FirstEntrySpread]);

            DrawFooters();
            DrawBookmark();
        }

        // Back to the shelf, under the book.
        Link(content, "Shelf", "< Mes livres", 12, TextAnchor.MiddleLeft, new Color(0.95f, 0.85f, 0.65f), BookX + 4f, BookY - BookH - 6f, 160f, 20f, () => back?.Invoke());
    }

    // The closed book, as it lies on the shelf: the front cover in green leather, the gold frame, the diamond and the title. A click opens it.
    private void DrawClosedBook()
    {
        const float w = 430f;
        const float h = 590f;
        var x = BookX + (BookW - w) / 2f;
        var y = BookY;

        Box(content, "Shadow", x + 8f, y - 8f, w, h, new Color(0f, 0f, 0f, 0.35f));
        Box(content, "Pages", x + 7f, y - 5f, w, h, Edge);
        Box(content, "Cover", x, y, w, h, leather);
        Box(content, "SpineBand", x, y, 26f, h, leatherDark);
        foreach (var ridge in new[] { 60f, 70f, h - 70f, h - 60f })
            Box(content, "Ridge", x, y - ridge, 26f, 3f, foil);

        // The gold frame, double.
        const float left = 44f;
        const float top = 20f;
        const float right = 20f;
        const float bottom = 20f;
        foreach (var inset in new[] { 0f, 7f })
        {
            var t = inset > 0f ? 1f : 2f;
            Box(content, "FrameTop", x + left + inset, y - top - inset, w - left - right - 2f * inset, t, foil);
            Box(content, "FrameBottom", x + left + inset, y - h + bottom + inset, w - left - right - 2f * inset, t, foil);
            Box(content, "FrameLeft", x + left + inset, y - top - inset, t, h - top - bottom - 2f * inset, foil);
            Box(content, "FrameRight", x + w - right - inset, y - top - inset, t, h - top - bottom - 2f * inset, foil);
        }
        foreach (var cx in new[] { x + left, x + w - right })
            foreach (var cy in new[] { y - top, y - h + bottom })
                Lozenge(content, "Stud", cx, cy, 12f, foil);

        // The diamond, the title.
        var mid = x + left + (w - left - right) / 2f;
        Lozenge(content, "EmblemGold", mid, y - 190f, 124f, Color.Lerp(foil, Color.black, 0.15f));
        Lozenge(content, "EmblemLeather", mid, y - 190f, 102f, leather);
        Lozenge(content, "EmblemGem", mid, y - 190f, 66f, info.Emblem);
        Lozenge(content, "EmblemGlint", mid - 12f, y - 202f, 20f, Color.Lerp(info.Emblem, Color.white, 0.7f));

        Label(content, "TitleA", info.CoverTitle[0], TitleSize(info.CoverTitle[0], 42, 6), TextAnchor.MiddleCenter, foil, mid - 170f, y - 300f, 340f, 56f, true);
        Label(content, "TitleB", info.CoverTitle[1], 26, TextAnchor.MiddleCenter, foil, mid - 170f, y - 354f, 340f, 36f, false, true);
        Box(content, "TitleRule", mid - 70f, y - 406f, 140f, 2f, foil);
        Label(content, "TitleC", info.Subtitle, 13, TextAnchor.MiddleCenter, new Color(0.80f, 0.90f, 0.82f), mid - 150f, y - 420f, 300f, 20f, false, true);
        Label(content, "Open", "Ouvrir le livre >", 15, TextAnchor.MiddleCenter, foil, mid - 100f, y - h + 66f, 200f, 28f, true);

        Link(content, "OpenHit", "", 12, TextAnchor.MiddleCenter, foil, x, y, w, h, () => ShowSpread(0));
    }

    private void DrawFooters()
    {
        var y = PageY - PageH + 26f;
        if (spread >= 0)
            Link(content, "Prev", spread == 0 ? "< Couverture" : "< Page précédente", 11, TextAnchor.MiddleLeft, Burgundy, LeftX + Margin, y, 150f, 20f, () => ShowSpread(spread - 1));
        if (spread != 1 && spread > 0)
            Link(content, "Index", "Index", 11, TextAnchor.MiddleRight, Burgundy, LeftX + PageW - Margin - 60f, y, 60f, 20f, () => ShowSpread(1));
        if (spread < SpreadCount - 1)
            Link(content, "Next", "Page suivante >", 11, TextAnchor.MiddleRight, Burgundy, RightX + PageW - Margin - 150f, y, 150f, 20f, () => ShowSpread(spread + 1));

        if (spread > 0)
        {
            Label(content, "NumberLeft", PageNumber(spread, 0).ToString(), 11, TextAnchor.MiddleCenter, Muted, LeftX + PageW / 2f - 20f, y, 40f, 20f, false, true);
            Label(content, "NumberRight", PageNumber(spread, 1).ToString(), 11, TextAnchor.MiddleCenter, Muted, RightX + PageW / 2f - 20f, y, 40f, 20f, false, true);
        }
    }

    // The running head of a page: small, in italics, under the frame.
    private void RunningHead(float pageX, string text)
    {
        Label(content, "Head", text, 10, TextAnchor.MiddleCenter, Muted, pageX + Margin, PageY - 22f, PageW - 2f * Margin, 16f, false, true);
    }

    // A font size for the first word of a title, smaller when the word is long.
    private static int TitleSize(string word, int size, int fits)
    {
        return word.Length <= fits ? size : Mathf.Max(24, size - (word.Length - fits) * 5);
    }

    private void DrawTitleSpread()
    {
        // Left: the title page, with a cut stone in a lozenge.
        var cx = LeftX + PageW / 2f;
        Lozenge(content, "EmblemGold", cx, PageY - 170f, 86f, Color.Lerp(foil, Color.black, 0.15f));
        Lozenge(content, "EmblemLeather", cx, PageY - 170f, 70f, leather);
        Lozenge(content, "EmblemGem", cx, PageY - 170f, 46f, info.Emblem);
        Lozenge(content, "EmblemGlint", cx - 8f, PageY - 178f, 14f, Color.Lerp(info.Emblem, Color.white, 0.7f));

        Label(content, "TitleA", info.CoverTitle[0], TitleSize(info.CoverTitle[0], 34, 6), TextAnchor.MiddleCenter, Burgundy, LeftX + Margin, PageY - 262f, PageW - 2f * Margin, 44f, true);
        Label(content, "TitleB", info.CoverTitle[1], 22, TextAnchor.MiddleCenter, Burgundy, LeftX + Margin, PageY - 304f, PageW - 2f * Margin, 30f, false, true);
        Box(content, "TitleRule", cx - 60f, PageY - 348f, 120f, 2f, Rule);
        Label(content, "TitleC", info.Subtitle, 13, TextAnchor.UpperCenter, Muted, LeftX + Margin, PageY - 362f, PageW - 2f * Margin, 40f, false, true);
        if (!string.IsNullOrEmpty(book.TitleNote))
            Label(content, "TitleD", book.TitleNote, 12, TextAnchor.UpperCenter, Ink, LeftX + Margin, PageY - 420f, PageW - 2f * Margin, 40f);

        // Right: how to read it.
        RunningHead(RightX, "Avant de commencer");
        Label(content, "ReadTitle", book.IntroHeading, 20, TextAnchor.UpperLeft, Burgundy, RightX + Margin, PageY - 50f, PageW - 2f * Margin, 28f, true);
        Body(content, book.IntroText, RightX + Margin, PageY - 86f, PageW - 2f * Margin, 420f, 12);
    }

    // One spread of the index: the entries of the book in order, each with its page.
    private void DrawIndexSpread(int tocSpread)
    {
        for (var side = 0; side < 2; side++)
        {
            var pageX = side == 0 ? LeftX : RightX;
            var first = (tocSpread * 2 + side) * TocPerPage;
            RunningHead(pageX, book.IndexTitle);
            Label(content, "IndexTitle", side == 0 && tocSpread == 0 ? "Index" : "Index (suite)", 20, TextAnchor.UpperLeft, Burgundy, pageX + Margin, PageY - 46f, PageW - 2f * Margin, 28f, true);

            for (var i = 0; i < TocPerPage; i++)
            {
                var number = first + i;
                if (number >= book.Entries.Count) break;
                var y = PageY - 86f - i * TocRow;
                var target = FirstEntrySpread + number;
                var entry = book.Entries[number];
                var textX = pageX + Margin;
                if (entry.IndexIconIsFlag)
                {
                    DrawFlag(entry.IndexIcon, textX, y - 3f);
                    textX += 36f;
                }
                else if (entry.Icon != null)
                {
                    var small = new GameObject("EntryIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    small.transform.SetParent(content, false);
                    RuntimeUIKit.SetTopLeft(small.rectTransform, new Vector2(textX, y - 2f), new Vector2(26f, 18f));
                    small.sprite = entry.IndexIcon != null ? entry.IndexIcon : entry.Icon;
                    small.preserveAspect = true;
                    small.raycastTarget = false;
                    textX += 32f;
                }
                Link(content, "Entry", entry.Title, 12, TextAnchor.MiddleLeft, Ink, textX, y, pageX + PageW - Margin - 44f - textX, TocRow - 2f, () => ShowSpread(target));
                Label(content, "EntryPage", PageNumber(target, 0).ToString(), 12, TextAnchor.MiddleRight, Muted, pageX + PageW - Margin - 40f, y, 40f, TocRow - 2f, false, true);
                Box(content, "Dots", pageX + Margin, y + 1f - (TocRow - 2f), PageW - 2f * Margin, 1f, new Color(Rule.r, Rule.g, Rule.b, 0.25f));
            }
        }
    }

    // The spread of an entry. With a picture (a species) the left page opens with the picture, the name and the spectrum; without, with the name and a line.
    private void DrawEntrySpread(BookEntry entry)
    {
        RunningHead(LeftX, string.IsNullOrEmpty(entry.LeftHead) ? info.Title : entry.LeftHead);
        float bodyY;
        if (entry.Icon != null)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(content, false);
            RuntimeUIKit.SetTopLeft(icon.rectTransform, new Vector2(LeftX + Margin, PageY - 46f), new Vector2(56f, 56f));
            icon.sprite = entry.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Label(content, "Name", entry.Title, 22, TextAnchor.UpperLeft, Burgundy, LeftX + Margin + 68f, PageY - 44f, PageW - 2f * Margin - 68f, 28f, true);
            Label(content, "Sub", entry.Subtitle, 11, TextAnchor.UpperLeft, Muted, LeftX + Margin + 68f, PageY - 72f, PageW - 2f * Margin - 68f, 16f, false, true);

            if (entry.Spectrum != null)
            {
                var spectrum = new GameObject("Spectrum", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                spectrum.transform.SetParent(content, false);
                RuntimeUIKit.SetTopLeft(spectrum.rectTransform, new Vector2(LeftX + Margin + 68f, PageY - 92f), new Vector2(250f, 14f));
                if (!spectra.TryGetValue(entry, out var sprite) || sprite == null)
                {
                    sprite = SpectrumRenderer.BuildSpectrumSprite(entry.Spectrum);
                    spectra[entry] = sprite;
                }
                spectrum.sprite = sprite;
                spectrum.raycastTarget = false;
            }
            Box(content, "Rule", LeftX + Margin, PageY - 112f, PageW - 2f * Margin, 1f, Rule);
            bodyY = PageY - 120f;
        }
        else
        {
            Label(content, "Name", entry.Title, 22, TextAnchor.UpperLeft, Burgundy, LeftX + Margin, PageY - 44f, PageW - 2f * Margin, 28f, true);
            Label(content, "Sub", entry.Subtitle, 11, TextAnchor.UpperLeft, Muted, LeftX + Margin, PageY - 72f, PageW - 2f * Margin, 32f, false, true);
            Box(content, "Rule", LeftX + Margin, PageY - 108f, PageW - 2f * Margin, 1f, Rule);
            bodyY = PageY - 116f;
        }

        const float bottom = -570f;
        bodyY -= DrawStrip(entry.LeftStrip, LeftX + Margin, bodyY, PageW - 2f * Margin);
        var overflowLeft = Body(content, entry.Left, LeftX + Margin, bodyY, PageW - 2f * Margin, bodyY - bottom, 12);
        if (overflowLeft) More(LeftX, bottom);

        RunningHead(RightX, string.IsNullOrEmpty(entry.RightHead) ? info.Title : entry.RightHead);
        var rightY = PageY - 46f;
        rightY -= DrawStrip(entry.RightStrip, RightX + Margin, rightY, PageW - 2f * Margin);
        rightY -= DrawMap(entry.Map, RightX + Margin, rightY);
        var overflowRight = Body(content, entry.Right, RightX + Margin, rightY, PageW - 2f * Margin, rightY - bottom, 12);
        if (overflowRight) More(RightX, bottom);
    }

    // The map of a deposit, with a frame, the marks of the deposit (a red lozenge and its name) and a caption under it; returns its height.
    private float DrawMap(BookMap map, float x, float y)
    {
        if (map == null || map.Sprite == null) return 0f;

        Box(content, "MapFrame", x - 3f, y + 3f, BookMaps.Width + 6f, BookMaps.Height + 6f, new Color(0.30f, 0.22f, 0.15f, 1f));
        var image = new GameObject("Map", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(content, false);
        RuntimeUIKit.SetTopLeft(image.rectTransform, new Vector2(x, y), new Vector2(BookMaps.Width, BookMaps.Height));
        image.sprite = map.Sprite;
        image.raycastTarget = false;

        foreach (var marker in map.Markers)
        {
            var mx = x + marker.Pixel.x;
            var my = y - marker.Pixel.y;
            Lozenge(content, "PinOutline", mx, my, 13f, new Color(0.25f, 0.06f, 0.06f, 1f));
            Lozenge(content, "Pin", mx, my, 9f, new Color(0.85f, 0.14f, 0.16f, 1f));
            Lozenge(content, "PinCentre", mx, my, 3.5f, Color.white);

            const float labelW = 128f;
            var onRight = marker.Pixel.x + 10f + labelW <= BookMaps.Width;
            var label = Label(content, "PinLabel", marker.Label, 10, onRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, new Color(0.20f, 0.10f, 0.06f, 1f),
                onRight ? mx + 9f : mx - 9f - labelW, my + 7f, labelW, 14f, true);
            var halo = label.gameObject.AddComponent<Outline>();
            halo.effectColor = new Color(0.96f, 0.92f, 0.80f, 0.95f);
            halo.effectDistance = new Vector2(1f, -1f);
        }

        Label(content, "MapCaption", "Carte : " + map.Caption, 10, TextAnchor.UpperLeft, Muted, x, y - BookMaps.Height - 5f, BookMaps.Width, 14f, false, true);
        return BookMaps.Height + 26f;
    }

    // A small flag with a thin frame; a neutral grey one when the deposit has no flag (an ocean, an industrial process).
    private void DrawFlag(Sprite flag, float x, float y)
    {
        Box(content, "FlagFrame", x, y, 28f, 20f, new Color(0.30f, 0.22f, 0.15f, 1f));
        if (flag == null)
        {
            Box(content, "FlagNone", x + 1f, y - 1f, 26f, 18f, new Color(0.80f, 0.74f, 0.60f, 1f));
            Box(content, "FlagPole", x + 6f, y - 3f, 2f, 14f, new Color(0.45f, 0.38f, 0.30f, 1f));
            Box(content, "FlagCloth", x + 8f, y - 3f, 12f, 8f, new Color(0.62f, 0.56f, 0.46f, 1f));
            return;
        }
        var image = new GameObject("Flag", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(content, false);
        RuntimeUIKit.SetTopLeft(image.rectTransform, new Vector2(x + 1f, y - 1f), new Vector2(26f, 18f));
        image.sprite = flag;
        image.raycastTarget = false;
    }

    // A strip of small pictures on dark plates, in rows that wrap; returns its height. With captions, each picture has its name under it.
    private float DrawStrip(BookStrip strip, float x, float y, float width)
    {
        if (strip == null || strip.Icons.Count == 0) return 0f;

        var height = 0f;
        if (!string.IsNullOrEmpty(strip.Title))
        {
            Label(content, "StripTitle", strip.Title, 11, TextAnchor.UpperLeft, Muted, x, y, width, 16f, false, true);
            height += 18f;
        }

        var cellW = strip.Labeled ? 68f : 38f;
        var cellH = strip.Labeled ? 64f : 38f;
        var plateH = strip.Labeled ? 38f : 34f;
        var perRow = Mathf.Max(1, Mathf.FloorToInt(width / cellW));
        var plate = new Color(0.24f, 0.19f, 0.15f, 1f);

        for (var i = 0; i < strip.Icons.Count; i++)
        {
            var icon = strip.Icons[i];
            var cx = x + (i % perRow) * cellW;
            var cy = y - height - (i / perRow) * cellH;
            Box(content, "Plate", cx, cy, cellW - 4f, plateH, plate);

            var image = new GameObject("StripIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(content, false);
            RuntimeUIKit.SetTopLeft(image.rectTransform, new Vector2(cx + 3f, cy - 2f), new Vector2(cellW - 10f, plateH - 4f));
            image.sprite = icon.Sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = icon.Sprite != null;

            if (strip.Labeled && !string.IsNullOrEmpty(icon.Label))
                Label(content, "StripCaption", icon.Label, 9, TextAnchor.UpperCenter, Ink, cx - 2f, cy - plateH - 1f, cellW, 22f);
        }

        height += Mathf.CeilToInt(strip.Icons.Count / (float)perRow) * cellH + 4f;
        return height;
    }

    private void More(float pageX, float y)
    {
        Label(content, "More", "(suite : molette de la souris)", 10, TextAnchor.MiddleRight, Muted, pageX + Margin, y + 2f, PageW - 2f * Margin, 14f, false, true);
    }
}
