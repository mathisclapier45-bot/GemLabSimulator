using UnityEngine;
using UnityEngine.UI;

// A collectible card drawn with uGUI: the name at the top, the artwork, a short description and its rarity. Rare cards are shiny:
// a bright band sweeps across them and a tint shimmers over the foil (rainbow for the ultra rare and secret ones, gold for the gold ones).
public class CardView : MonoBehaviour
{
    private static Sprite shineSprite;

    private CardDef card;
    private RectTransform shineRect;
    private Image shineImage;
    private Image foilImage;
    private float width;
    private float height;
    private float phase;

    public CardDef Card => card;

    // `compact`: the small version of the binder, without the description. `count` above 1 shows the counter at the top right of the artwork.
    public static CardView Create(Transform parent, CardDef card, Vector2 size, bool compact, int count)
    {
        var root = RuntimeUIKit.MakeBox("Card_" + card.Id, parent, Vector2.zero, size, CardCatalog.RarityColor(card.Rarity));
        var view = root.gameObject.AddComponent<CardView>();
        view.Build(root, card, size, compact, count);
        return view;
    }

    private void Build(Image root, CardDef definition, Vector2 size, bool compact, int count)
    {
        card = definition;
        width = size.x;
        height = size.y;
        phase = Random.value;
        root.gameObject.AddComponent<RectMask2D>();

        var rarityColor = CardCatalog.RarityColor(card.Rarity);
        var margin = Mathf.Max(3f, width * 0.03f);
        RuntimeUIKit.MakeBox("Inner", transform, new Vector2(margin, -margin), new Vector2(width - margin * 2f, height - margin * 2f),
            Color.Lerp(new Color(0.09f, 0.10f, 0.14f), rarityColor, 0.12f)).raycastTarget = false;

        // Name, at the top.
        var nameText = RuntimeUIKit.MakeText("Name", transform, card.Name, 12, TextAnchor.MiddleCenter, Color.white,
            new Vector2(margin + 2f, -margin - 1f), new Vector2(width - margin * 2f - 4f, height * 0.11f));
        FitText(nameText, 6, Mathf.RoundToInt(height * 0.075f));
        nameText.fontStyle = FontStyle.Bold;

        // Artwork.
        var artTop = margin + height * 0.115f;
        var artHeight = height * (compact ? 0.60f : 0.46f);
        var artWidth = width - margin * 2f - 8f;
        var artX = margin + 4f;
        RuntimeUIKit.MakeBox("ArtBack", transform, new Vector2(artX, -artTop), new Vector2(artWidth, artHeight), card.Tint).raycastTarget = false;
        BuildArt(artX, artTop, artWidth, artHeight);

        if (!compact)
        {
            var description = RuntimeUIKit.MakeText("Description", transform, card.Description, 9, TextAnchor.UpperCenter, new Color(0.88f, 0.88f, 0.92f),
                new Vector2(margin + 5f, -(artTop + artHeight + 4f)), new Vector2(width - margin * 2f - 10f, height - artTop - artHeight - height * 0.11f - margin - 4f));
            FitText(description, 6, Mathf.RoundToInt(height * 0.04f));
        }

        // Rarity and number of the card, at the bottom.
        var bottom = RuntimeUIKit.MakeText("Rarity", transform, $"{CardCatalog.RarityName(card.Rarity)}  ·  n° {card.Order}", 9, TextAnchor.MiddleCenter, rarityColor,
            new Vector2(margin + 2f, -(height - margin - height * 0.085f)), new Vector2(width - margin * 2f - 4f, height * 0.085f));
        FitText(bottom, 6, Mathf.RoundToInt(height * 0.05f));

        if (count > 1) BuildCountBadge(artX, artTop, artWidth, count);
        if (card.Rarity != CardRarity.Commune) BuildFoil();
    }

    private void BuildArt(float x, float top, float areaWidth, float areaHeight)
    {
        var center = new Vector2(x + areaWidth / 2f, -(top + areaHeight / 2f));

        if (card.Art != null)
        {
            var sprite = card.Art();
            if (sprite != null)
            {
                var iconSize = Mathf.Min(areaWidth, areaHeight) * (card.Badge != null ? 0.70f : 0.88f);
                var icon = new GameObject("Art", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(transform, false);
                RuntimeUIKit.SetTopLeft(icon.GetComponent<RectTransform>(), new Vector2(center.x - iconSize / 2f, center.y + iconSize / 2f), new Vector2(iconSize, iconSize));
                var image = icon.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        if (card.Badge == null) return;

        if (card.Art == null)
        {
            // An element: its symbol, large, with the atomic number and the family under it.
            var symbol = RuntimeUIKit.MakeText("Badge", transform, card.Badge, 40, TextAnchor.MiddleCenter, Color.white,
                new Vector2(x, -(top + areaHeight * 0.12f)), new Vector2(areaWidth, areaHeight * 0.62f));
            FitText(symbol, 8, Mathf.RoundToInt(areaHeight * 0.55f));
            symbol.fontStyle = FontStyle.Bold;
            symbol.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);

            if (!string.IsNullOrEmpty(card.SubBadge))
            {
                var sub = RuntimeUIKit.MakeText("SubBadge", transform, card.SubBadge, 10, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f),
                    new Vector2(x, -(top + areaHeight * 0.78f)), new Vector2(areaWidth, areaHeight * 0.18f));
                FitText(sub, 6, Mathf.RoundToInt(areaHeight * 0.12f));
            }
            return;
        }

        // A master stone: the grade written large over the stone.
        var grade = RuntimeUIKit.MakeText("Badge", transform, card.Badge, 24, TextAnchor.LowerCenter, Color.white,
            new Vector2(x, -(top + areaHeight * 0.58f)), new Vector2(areaWidth, areaHeight * 0.42f));
        FitText(grade, 8, Mathf.RoundToInt(areaHeight * 0.34f));
        grade.fontStyle = FontStyle.Bold;
        grade.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
    }

    private void BuildCountBadge(float artX, float artTop, float artWidth, int count)
    {
        var badgeWidth = Mathf.Clamp(width * 0.28f, 24f, 46f);
        var badgeHeight = Mathf.Clamp(height * 0.10f, 16f, 26f);
        var badge = RuntimeUIKit.MakeBox("Count", transform, new Vector2(artX + artWidth - badgeWidth - 2f, -(artTop + 2f)),
            new Vector2(badgeWidth, badgeHeight), new Color(0.78f, 0.16f, 0.18f, 1f));
        badge.raycastTarget = false;
        var text = RuntimeUIKit.MakeText("CountText", badge.transform, $"x{count}", 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(badgeWidth, badgeHeight));
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        FitText(text, 7, 16);
        text.fontStyle = FontStyle.Bold;
    }

    private void BuildFoil()
    {
        foilImage = RuntimeUIKit.MakeBox("Foil", transform, Vector2.zero, new Vector2(width, height), new Color(1f, 1f, 1f, 0.1f), false);
        foilImage.raycastTarget = false;

        var shine = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        shine.transform.SetParent(transform, false);
        shineRect = shine.GetComponent<RectTransform>();
        shineRect.anchorMin = shineRect.anchorMax = new Vector2(0f, 1f);
        shineRect.pivot = new Vector2(0.5f, 0.5f);
        shineRect.sizeDelta = new Vector2(width * 0.42f, height * 1.7f);
        shineRect.localRotation = Quaternion.Euler(0f, 0f, 22f);
        shineImage = shine.GetComponent<Image>();
        shineImage.sprite = ShineSprite;
        shineImage.raycastTarget = false;
    }

    private static Sprite ShineSprite
    {
        get
        {
            if (shineSprite != null) return shineSprite;

            const int w = 64;
            var texture = new Texture2D(w, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (var x = 0; x < w; x++)
            {
                var d = Mathf.Abs(x - (w - 1) / 2f) / ((w - 1) / 2f);
                var a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
                for (var y = 0; y < 2; y++) texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            shineSprite = Sprite.Create(texture, new Rect(0, 0, w, 2), new Vector2(0.5f, 0.5f), 100f);
            return shineSprite;
        }
    }

    private static void FitText(Text text, int minSize, int maxSize)
    {
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(5, minSize);
        text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize + 1, maxSize);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void Update()
    {
        if (foilImage == null) return;

        var t = Time.unscaledTime;
        var speed = card.Rarity == CardRarity.Secret ? 0.5f : card.Rarity == CardRarity.UltraRare ? 0.38f : 0.28f;
        var travel = Mathf.Repeat(t * speed + phase, 1f);
        shineRect.anchoredPosition = new Vector2(Mathf.Lerp(-width * 0.5f, width * 1.5f, travel), -height * 0.5f);

        switch (card.Rarity)
        {
            case CardRarity.Rare:
                shineImage.color = new Color(0.75f, 0.9f, 1f, 0.35f);
                foilImage.color = new Color(0.6f, 0.8f, 1f, 0.06f);
                break;
            case CardRarity.SuperRare:
                shineImage.color = new Color(0.9f, 0.8f, 1f, 0.45f);
                foilImage.color = Color.HSVToRGB(Mathf.Repeat(0.75f + 0.05f * Mathf.Sin(t * 1.5f), 1f), 0.5f, 1f).WithAlpha(0.10f);
                break;
            case CardRarity.UltraRare:
                shineImage.color = Color.HSVToRGB(Mathf.Repeat(t * 0.25f + phase, 1f), 0.45f, 1f).WithAlpha(0.6f);
                foilImage.color = Color.HSVToRGB(Mathf.Repeat(t * 0.2f + phase, 1f), 0.6f, 1f).WithAlpha(0.16f);
                break;
            case CardRarity.Gold:
                shineImage.color = new Color(1f, 0.92f, 0.55f, 0.75f);
                foilImage.color = new Color(1f, 0.82f, 0.3f, 0.16f + 0.05f * Mathf.Sin(t * 2.2f));
                break;
            default:
                shineImage.color = Color.HSVToRGB(Mathf.Repeat(t * 0.5f + phase, 1f), 0.5f, 1f).WithAlpha(0.7f);
                foilImage.color = Color.HSVToRGB(Mathf.Repeat(t * 0.35f + phase, 1f), 0.7f, 1f).WithAlpha(0.22f);
                break;
        }
    }
}

public static class ColorAlphaExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
