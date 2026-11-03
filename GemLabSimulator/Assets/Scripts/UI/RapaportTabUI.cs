using System;
using UnityEngine;
using UnityEngine.UI;

// The Rapaport tab of the bag: the price list of the round brilliant diamonds, per carat, by weight (the range is chosen on the left), color
// (columns D to M) and clarity (rows FL/IF to I3), in hundreds of euros. An issue comes out each Thursday; the last twelve can be read.
public class RapaportTabUI
{
    private const float ListX = 24f;
    private const float GridX = 184f;
    private const float CellW = 52f;
    private const float CellH = 30f;
    private const int History = 12;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Red = new Color(0.90f, 0.32f, 0.28f);
    private static readonly Color ButtonColor = new Color(0.30f, 0.26f, 0.22f, 1f);
    private static readonly Color SelectedColor = new Color(0.55f, 0.42f, 0.18f, 1f);

    private readonly Transform root;
    private RectTransform layer;
    private Text headerText;
    private Text issueLabel;

    private int size = 5;
    private int issue;

    public GameObject Root { get; }

    public RapaportTabUI(Transform parent)
    {
        Root = new GameObject("RapaportContent", typeof(RectTransform));
        Root.transform.SetParent(parent, false);
        var rect = Root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        root = Root.transform;

        var title = RuntimeUIKit.MakeText("Title", root, "Rapaport Diamond Report", 20, TextAnchor.UpperLeft, Gold, new Vector2(ListX, -54f), new Vector2(380f, 26f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeBox("Accent", root, new Vector2(ListX, -82f), new Vector2(60f, 3f), Red, false).raycastTarget = false;
        headerText = RuntimeUIKit.MakeText("Header", root, "", 12, TextAnchor.UpperLeft, new Color(0.82f, 0.78f, 0.70f), new Vector2(ListX + 70f, -78f), new Vector2(520f, 16f));

        RuntimeUIKit.MakeButton("Older", root, "<", 14, ButtonColor, new Vector2(620f, -54f), new Vector2(30f, 26f)).onClick.AddListener(() => Move(-1));
        issueLabel = RuntimeUIKit.MakeText("Issue", root, "", 13, TextAnchor.MiddleCenter, Color.white, new Vector2(654f, -54f), new Vector2(84f, 26f));
        RuntimeUIKit.MakeButton("Newer", root, ">", 14, ButtonColor, new Vector2(742f, -54f), new Vector2(30f, 26f)).onClick.AddListener(() => Move(1));

        layer = new GameObject("Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(root, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
    }

    private static int LatestIssue => RapaportPrices.IssueNumber(RapaportPrices.IssueDate(GameCalendar.Today));

    public void Show()
    {
        issue = LatestIssue;
        Rebuild();
    }

    private void Move(int delta)
    {
        var latest = LatestIssue;
        issue = Mathf.Clamp(issue + delta, Mathf.Max(1, latest - History + 1), latest);
        Rebuild();
    }

    private void Rebuild()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(layer.GetChild(i).gameObject);

        var date = RapaportPrices.DateOfIssue(issue);
        issueLabel.text = $"N° {issue}";
        headerText.text = $"Publié le {GameCalendar.Long(date)}  ·  diamants ronds, taille brillant";

        BuildSizes();
        BuildGrid();
        BuildFooter();
    }

    private void BuildSizes()
    {
        RuntimeUIKit.MakeText("SizeTitle", layer, "Poids (ct)", 12, TextAnchor.UpperLeft, Gold, new Vector2(ListX, -104f), new Vector2(180f, 16f));
        for (var i = 0; i < RapaportPrices.SizeLabels.Length; i++)
        {
            var captured = i;
            var button = RuntimeUIKit.MakeButton("Size" + i, layer, RapaportPrices.SizeLabels[i], 12, i == size ? SelectedColor : ButtonColor,
                new Vector2(ListX, -124f - i * 30f), new Vector2(150f, 26f));
            button.onClick.AddListener(() =>
            {
                size = captured;
                Rebuild();
            });
        }
    }

    private void BuildGrid()
    {
        // The column heads, D to M.
        for (var c = 0; c < RapaportPrices.ColorCount; c++)
            RuntimeUIKit.MakeText("Color" + c, layer, DiamondGrading.Color(c), 14, TextAnchor.MiddleCenter, Gold,
                new Vector2(GridX + 56f + c * CellW, -104f), new Vector2(CellW - 2f, 20f)).fontStyle = FontStyle.Bold;

        // The prices of the page, and their range for the colour of the cells.
        var prices = new int[RapaportPrices.ClarityRows.Length, RapaportPrices.ColorCount];
        var min = int.MaxValue;
        var max = 0;
        for (var r = 0; r < prices.GetLength(0); r++)
        {
            for (var c = 0; c < prices.GetLength(1); c++)
            {
                prices[r, c] = RapaportPrices.PricePerCarat(issue, size, c, r);
                min = Mathf.Min(min, prices[r, c]);
                max = Mathf.Max(max, prices[r, c]);
            }
        }

        for (var r = 0; r < prices.GetLength(0); r++)
        {
            var y = -126f - r * (CellH + 2f);
            RuntimeUIKit.MakeText("Clarity" + r, layer, RapaportPrices.ClarityRows[r], 12, TextAnchor.MiddleLeft, Color.white,
                new Vector2(GridX, y), new Vector2(54f, CellH));
            for (var c = 0; c < prices.GetLength(1); c++)
            {
                var t = Mathf.Pow(Mathf.InverseLerp(min, max, prices[r, c]), 0.7f);
                var color = Color.Lerp(new Color(0.15f, 0.13f, 0.12f, 1f), new Color(0.50f, 0.37f, 0.15f, 1f), t);
                var cell = RuntimeUIKit.MakeBox("Cell", layer, new Vector2(GridX + 56f + c * CellW, y), new Vector2(CellW - 2f, CellH), color, false);
                cell.raycastTarget = false;
                RuntimeUIKit.MakeText("Price", cell.transform, Format(prices[r, c]), 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(CellW - 2f, CellH));
            }
        }
    }

    // In hundreds of euros: 5 200 € is written 52; below 1 000 € one decimal is kept.
    private static string Format(int euros)
    {
        var hundreds = euros / 100f;
        return hundreds >= 10f ? Mathf.RoundToInt(hundreds).ToString() : hundreds.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
    }

    private void BuildFooter()
    {
        var index = RapaportPrices.MarketIndex(issue);
        var change = RapaportPrices.WeeklyChange(issue);
        var arrow = change >= 0f ? "+" : "-";
        var color = change >= 0f ? new Color(0.50f, 0.90f, 0.52f) : new Color(0.96f, 0.45f, 0.42f);
        RuntimeUIKit.MakeText("Index", layer, $"Indice du marché : {index:0.0}   ({arrow}{Mathf.Abs(change) * 100f:0.0} % sur la semaine)".Replace('.', ','), 13,
            TextAnchor.UpperLeft, color, new Vector2(GridX, -454f), new Vector2(340f, 18f));
        RuntimeUIKit.MakeText("Legend", layer, "Prix en centaines d'euros par carat. Les prix réels se négocient en dessous de la liste.", 11,
            TextAnchor.UpperLeft, new Color(0.62f, 0.58f, 0.52f), new Vector2(GridX, -474f), new Vector2(340f, 28f)).horizontalOverflow = HorizontalWrapMode.Wrap;

        // The index of the last twelve issues.
        var first = Mathf.Max(1, issue - History + 1);
        var low = float.MaxValue;
        var high = 0f;
        for (var n = first; n <= issue; n++)
        {
            low = Mathf.Min(low, RapaportPrices.MarketIndex(n));
            high = Mathf.Max(high, RapaportPrices.MarketIndex(n));
        }
        RuntimeUIKit.MakeText("ChartTitle", layer, "Indice, 12 derniers numéros", 11, TextAnchor.UpperLeft, Gold, new Vector2(584f, -446f), new Vector2(190f, 14f));
        for (var n = first; n <= issue; n++)
        {
            var t = high - low < 0.01f ? 0.5f : Mathf.InverseLerp(low, high, RapaportPrices.MarketIndex(n));
            var height = 8f + t * 36f;
            var x = 584f + (n - first) * 15f;
            RuntimeUIKit.MakeBox("Bar", layer, new Vector2(x, -466f - (46f - height)), new Vector2(12f, height),
                n == issue ? new Color(0.95f, 0.75f, 0.30f, 1f) : new Color(0.45f, 0.36f, 0.20f, 1f), false).raycastTarget = false;
        }
    }
}
