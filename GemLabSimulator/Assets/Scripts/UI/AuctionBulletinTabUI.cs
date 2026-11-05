using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The auction bulletin tab of the bag: the weekly catalogue of the bank, out every Sunday for the sale of the Saturday. It gives the theme, the
// lots with their picture, their weight, their origin, what the catalogue says about their treatment, the estimate and the starting price.
// Built by the bag, inside its panel.
public class AuctionBulletinTabUI
{
    private const float CardW = 372f;
    private const float CardH = 82f;
    private const float Gap = 8f;
    private const float StartX = 24f;
    private const float StartY = -150f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color Ink = new Color(0.82f, 0.78f, 0.70f);
    private static readonly Color Wine = new Color(0.72f, 0.28f, 0.30f);
    private static readonly Color CardColor = new Color(0.17f, 0.15f, 0.14f, 1f);

    private readonly RectTransform layer;

    public GameObject Root { get; }

    public AuctionBulletinTabUI(Transform parent)
    {
        Root = new GameObject("AuctionBulletinContent", typeof(RectTransform));
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

    public void Show()
    {
        for (var i = layer.childCount - 1; i >= 0; i--) Object.Destroy(layer.GetChild(i).gameObject);

        var state = AuctionState.Ensure();
        var lots = state != null ? state.EnsureLots() : new List<AuctionLot>();
        var date = AuctionCatalog.NextAuction(GameCalendar.Today);
        var theme = AuctionCatalog.ThemeFor(date);
        var issue = AuctionCatalog.IssueNumber(date);

        var title = Txt("Title", "LE BULLETIN DES VENTES", 22, TextAnchor.UpperLeft, Gold, new Vector2(24f, -50f), new Vector2(420f, 28f));
        title.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeBox("Accent", layer, new Vector2(24f, -80f), new Vector2(60f, 3f), Wine, false).raycastTarget = false;
        Txt("Issue", $"N° {issue}  ·  vente du {GameCalendar.Long(date)}", 12, TextAnchor.UpperLeft, Ink, new Vector2(94f, -76f), new Vector2(500f, 16f));
        Txt("Where", "Salle des ventes de la Banque de la Ville, de 9h à 20h  ·  frais acheteur 10 %", 11, TextAnchor.UpperRight, new Color(0.62f, 0.58f, 0.52f), new Vector2(430f, -54f), new Vector2(350f, 16f));

        RuntimeUIKit.MakeBox("ThemeBox", layer, new Vector2(24f, -98f), new Vector2(752f, 40f), new Color(0.25f, 0.13f, 0.14f, 1f), false).raycastTarget = false;
        var themeName = Txt("Theme", $"Thème : {theme.Name}", 14, TextAnchor.UpperLeft, new Color(1f, 0.86f, 0.62f), new Vector2(34f, -102f), new Vector2(730f, 18f));
        themeName.fontStyle = FontStyle.Bold;
        Txt("ThemeText", theme.Description, 11, TextAnchor.UpperLeft, Ink, new Vector2(34f, -120f), new Vector2(730f, 16f));

        if (lots.Count == 0)
        {
            Txt("None", "Le catalogue de cette semaine n'est pas encore imprimé.", 14, TextAnchor.UpperLeft, Color.white, new Vector2(30f, -170f), new Vector2(500f, 24f));
            return;
        }

        for (var i = 0; i < lots.Count && i < 8; i++)
        {
            var x = StartX + (i % 2) * (CardW + Gap);
            var y = StartY - (i / 2) * (CardH + 6f);
            BuildCard(lots[i], x, y);
        }

        Txt("Foot", "Les prix sont des estimations de la maison. Examinez les lots en salle avant d'enchérir : un catalogue n'est pas toujours exact sur les traitements.", 10,
            TextAnchor.UpperLeft, new Color(0.55f, 0.52f, 0.47f), new Vector2(24f, -496f), new Vector2(752f, 14f));
    }

    private void BuildCard(AuctionLot lot, float x, float y)
    {
        RuntimeUIKit.MakeBox("Card", layer, new Vector2(x, y), new Vector2(CardW, CardH), CardColor).raycastTarget = false;

        RuntimeUIKit.MakeBox("Frame", layer, new Vector2(x + 8f, y - 9f), new Vector2(64f, 64f), new Color(0.10f, 0.09f, 0.09f, 1f)).raycastTarget = false;
        var icon = new GameObject("Gem", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(layer, false);
        RuntimeUIKit.SetTopLeft(icon.GetComponent<RectTransform>(), new Vector2(x + 12f, y - 13f), new Vector2(56f, 56f));
        var image = icon.GetComponent<Image>();
        image.sprite = lot.Gem.DisplaySprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        RuntimeUIKit.MakeBox("Number", layer, new Vector2(x + 4f, y - 4f), new Vector2(22f, 16f), Wine).raycastTarget = false;
        var number = Txt("LotNumber", lot.Number.ToString(), 11, TextAnchor.MiddleCenter, Color.white, new Vector2(x + 4f, y - 4f), new Vector2(22f, 16f));
        number.fontStyle = FontStyle.Bold;

        var gem = lot.Gem;
        var state = gem.IsRough ? "brute" : $"{gem.ShapeName}, {gem.CutStyleName.ToLowerInvariant()}";
        var name = Txt("Name", lot.Name, 13, TextAnchor.UpperLeft, Color.white, new Vector2(x + 82f, y - 6f), new Vector2(CardW - 90f, 16f));
        name.fontStyle = FontStyle.Bold;
        Txt("Detail", $"{gem.CaratWeight:F2} ct, {state}  ·  {gem.Origin.name}", 10, TextAnchor.UpperLeft, Ink, new Vector2(x + 82f, y - 24f), new Vector2(CardW - 90f, 14f));

        var treatmentColor = lot.Honesty == CatalogHonesty.Declared ? new Color(1f, 0.78f, 0.55f) : new Color(0.70f, 0.82f, 0.70f);
        Txt("Treatment", AuctionCatalog.TreatmentLine(lot), 10, TextAnchor.UpperLeft, treatmentColor, new Vector2(x + 82f, y - 39f), new Vector2(CardW - 90f, 14f));

        var estimate = Txt("Estimate", $"Estimation {lot.EstimateLow:N0} – {lot.EstimateHigh:N0} €   ·   mise à prix {lot.Start:N0} €", 11, TextAnchor.UpperLeft, Gold, new Vector2(x + 82f, y - 58f), new Vector2(CardW - 90f, 16f));
        estimate.fontStyle = FontStyle.Bold;
    }

    private Text Txt(string name, string content, int size, TextAnchor anchor, Color color, Vector2 position, Vector2 dimensions)
    {
        var text = RuntimeUIKit.MakeText(name, layer, content, size, anchor, color, position, dimensions);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
