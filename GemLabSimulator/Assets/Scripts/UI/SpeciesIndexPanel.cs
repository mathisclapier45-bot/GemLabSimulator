using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A small side panel for the instruments: a list of stones (or cases) with a few lines of words each, a search box above it, and a scrolling window.
// The instrument fills it with AddHeader / AddRow, then calls Finish.
public class SpeciesIndexPanel
{
    public struct Line
    {
        public string Label;        // "Polar.", "Cono."...
        public string Words;
        public Color? Chip;         // a small swatch before the words, or none
        public Color[] Swatches;    // or several swatches (the colours of a dichroic stone)
        public int TextRows;        // how many lines of words (0 or 1 = one)
    }

    private struct Row
    {
        public RectTransform Rect;
        public string Key;          // the name without accents or capitals, empty for a header
        public float Height;
    }

    private readonly List<Row> rows = new List<Row>();
    private readonly float width;
    private RectTransform content;
    private Text noResult;
    private InputField search;

    public static SpeciesIndexPanel Create(Transform parent, Vector2 topLeft, Vector2 size, string title, string hint, Color panelColor, Color gold)
    {
        var index = new SpeciesIndexPanel(size.x);
        index.Build(parent, topLeft, size, title, hint, panelColor, gold);
        return index;
    }

    private SpeciesIndexPanel(float width)
    {
        this.width = width;
    }

    private void Build(Transform parent, Vector2 topLeft, Vector2 size, string title, string hint, Color panelColor, Color gold)
    {
        var root = RuntimeUIKit.MakeBox("Index", parent, topLeft, size, panelColor);
        RuntimeUIKit.MakeText("IndexTitle", root.transform, title, 16, TextAnchor.UpperLeft, gold, new Vector2(12f, -10f), new Vector2(size.x - 20f, 22f));
        RuntimeUIKit.MakeText("IndexHint", root.transform, hint, 10, TextAnchor.UpperLeft, new Color(0.6f, 0.6f, 0.65f), new Vector2(12f, -32f), new Vector2(size.x - 20f, 26f));

        var boxGO = new GameObject("IndexSearch", typeof(RectTransform), typeof(Image), typeof(InputField));
        boxGO.transform.SetParent(root.transform, false);
        RuntimeUIKit.SetTopLeft(boxGO.GetComponent<RectTransform>(), new Vector2(8f, -60f), new Vector2(size.x - 16f, 24f));
        boxGO.GetComponent<Image>().color = new Color(0.20f, 0.21f, 0.26f, 1f);
        var text = RuntimeUIKit.MakeText("Text", boxGO.transform, "", 12, TextAnchor.MiddleLeft, Color.white, Vector2.zero, Vector2.zero);
        var placeholder = RuntimeUIKit.MakeText("Placeholder", boxGO.transform, "Rechercher…", 12, TextAnchor.MiddleLeft, new Color(0.58f, 0.60f, 0.66f), Vector2.zero, Vector2.zero);
        placeholder.fontStyle = FontStyle.Italic;
        foreach (var t in new[] { text, placeholder })
        {
            var r = t.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(8f, 1f);
            r.offsetMax = new Vector2(-8f, -1f);
        }
        search = boxGO.GetComponent<InputField>();
        search.textComponent = text;
        search.placeholder = placeholder;
        search.characterLimit = 30;
        search.onValueChanged.AddListener(ApplyFilter);

        var viewportGO = new GameObject("IndexView", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
        viewportGO.transform.SetParent(root.transform, false);
        RuntimeUIKit.SetTopLeft(viewportGO.GetComponent<RectTransform>(), new Vector2(6f, -90f), new Vector2(size.x - 12f, size.y - 98f));
        viewportGO.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 1f);
        var contentGO = new GameObject("IndexContent", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        var scroll = viewportGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportGO.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        noResult = RuntimeUIKit.MakeText("NoResult", content, "Aucune pierre ne correspond.", 12, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f), new Vector2(0f, -10f), new Vector2(size.x - 24f, 24f));
        noResult.gameObject.SetActive(false);
    }

    public void AddHeader(string title)
    {
        var header = RuntimeUIKit.MakeText("Header", content, title, 11, TextAnchor.MiddleLeft, new Color(0.95f, 0.85f, 0.65f), Vector2.zero, new Vector2(width - 24f, 20f));
        header.fontStyle = FontStyle.Bold;
        header.raycastTarget = false;
        rows.Add(new Row { Rect = header.rectTransform, Key = "", Height = 22f });
    }

    public void AddRow(string name, params Line[] lines) => AddRow(name, "", lines);

    // A row that the search also finds by other words (the number of a density, for instance).
    public void AddRow(string name, string keywords, Line[] lines)
    {
        var height = 18f + 3f;
        foreach (var line in lines) height += Mathf.Max(1, line.TextRows) * 11f + 1f;
        var row = RuntimeUIKit.MakeBox("Row_" + name, content, Vector2.zero, new Vector2(width - 24f, height - 3f), new Color(0.10f, 0.11f, 0.15f, 1f));
        row.raycastTarget = false;
        var title = RuntimeUIKit.MakeText("Name", row.transform, name, 10, TextAnchor.UpperLeft, new Color(0.88f, 0.90f, 0.96f), new Vector2(5f, -2f), new Vector2(width - 34f, 13f));
        title.raycastTarget = false;
        title.fontStyle = FontStyle.Bold;
        var y = -16f;
        for (var i = 0; i < lines.Length; i++)
        {
            var textRows = Mathf.Max(1, lines[i].TextRows);
            var label = RuntimeUIKit.MakeText("Label", row.transform, lines[i].Label, 9, TextAnchor.MiddleLeft, new Color(0.60f, 0.62f, 0.70f), new Vector2(5f, y), new Vector2(34f, 11f));
            label.raycastTarget = false;
            var wordsX = 40f;
            if (lines[i].Chip.HasValue)
            {
                var chip = RuntimeUIKit.MakeBox("Chip", row.transform, new Vector2(40f, y - 1f), new Vector2(14f, 9f), lines[i].Chip.Value, false);
                chip.raycastTarget = false;
                wordsX = 58f;
            }
            if (lines[i].Swatches != null)
                foreach (var color in lines[i].Swatches)
                {
                    var swatch = RuntimeUIKit.MakeBox("Swatch", row.transform, new Vector2(wordsX, y - 1f), new Vector2(11f, 9f), color, false);
                    swatch.raycastTarget = false;
                    wordsX += 13f;
                }
            var words = RuntimeUIKit.MakeText("Words", row.transform, lines[i].Words, 9, TextAnchor.UpperLeft, new Color(0.80f, 0.86f, 0.95f), new Vector2(wordsX + 1f, y), new Vector2(width - 24f - wordsX - 4f, textRows * 11f));
            words.raycastTarget = false;
            y -= textRows * 11f + 1f;
        }
        rows.Add(new Row { Rect = row.rectTransform, Key = TextEntry.Normalize(name + " " + keywords), Height = height });
    }

    public void Finish() => ApplyFilter("");

    public void ResetSearch()
    {
        if (search != null) search.text = "";
    }

    // Keeps the rows whose name holds every word typed (the headers show only when nothing is typed), in order.
    private void ApplyFilter(string query)
    {
        var words = TextEntry.Normalize(query).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        var filtering = words.Length > 0;
        var y = -4f;
        var shown = 0;
        foreach (var row in rows)
        {
            var isHeader = row.Key.Length == 0;
            var match = !isHeader;
            if (match)
                foreach (var word in words)
                    if (!row.Key.Contains(word)) { match = false; break; }
            var visible = isHeader ? !filtering : match;
            row.Rect.gameObject.SetActive(visible);
            if (!visible) continue;

            row.Rect.anchoredPosition = new Vector2(3f, y);
            y -= row.Height;
            if (!isHeader) shown++;
        }
        noResult.gameObject.SetActive(filtering && shown == 0);
        content.sizeDelta = new Vector2(0f, -y + 6f);
        content.anchoredPosition = Vector2.zero;
    }
}
