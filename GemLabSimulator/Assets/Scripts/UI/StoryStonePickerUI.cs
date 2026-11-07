using System;
using UnityEngine;
using UnityEngine.UI;

// The question of identification of the tutorial: a few stones are suggested, each one with its data beside it (colour, transparency, luster, density), and the
// player picks the one that matches what was measured. The stones that were wrong are crossed out when the question is asked again.
public static class StoryStonePickerUI
{
    private static GameObject current;

    public static bool IsOpen => current != null;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    // `subtitle`: what the player found out about the stone. `chosen` receives the index of the stone picked, or -1 for "later".
    public static void Show(string subtitle, GemDefinition[] stones, bool[] eliminated, Action<int> chosen)
    {
        Close();
        var canvas = GameObject.Find("Canvas");
        if (canvas == null || stones == null || stones.Length == 0)
        {
            chosen?.Invoke(-1);
            return;
        }

        const float cardWidth = 250f;
        const float cardHeight = 300f;
        const float gap = 14f;
        var width = stones.Length * cardWidth + (stones.Length - 1) * gap + 40f;
        const float height = 470f;

        current = new GameObject("StoryStonePickerPanel", typeof(RectTransform));
        current.transform.SetParent(canvas.transform, false);
        current.transform.SetAsLastSibling();
        var rect = current.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta = new Vector2(width, height);

        RuntimeUIKit.MakeBox("Body", rect, Vector2.zero, new Vector2(width, height), new Color(0.08f, 0.08f, 0.10f, 0.97f));
        RuntimeUIKit.MakeText("Title", rect, "De quelle pierre s'agit-il ?", 22, TextAnchor.UpperCenter, Gold, new Vector2(20f, -14f), new Vector2(width - 40f, 30f)).fontStyle = FontStyle.Bold;
        var sub = RuntimeUIKit.MakeText("Subtitle", rect, subtitle, 14, TextAnchor.UpperCenter, new Color(0.80f, 0.88f, 1f), new Vector2(30f, -50f), new Vector2(width - 60f, 40f));
        sub.horizontalOverflow = HorizontalWrapMode.Wrap;

        var done = false;
        for (var i = 0; i < stones.Length; i++)
        {
            var index = i;
            var def = stones[i];
            var crossed = eliminated != null && i < eliminated.Length && eliminated[i];
            var x = 20f + i * (cardWidth + gap);
            var top = -100f;

            RuntimeUIKit.MakeBox("Card", rect, new Vector2(x, top), new Vector2(cardWidth, cardHeight), crossed ? new Color(0.14f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.18f, 0.24f, 1f)).raycastTarget = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(rect, false);
            RuntimeUIKit.SetTopLeft(icon.GetComponent<RectTransform>(), new Vector2(x + cardWidth / 2f - 32f, top - 10f), new Vector2(64f, 64f));
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = def.GetIcon();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.color = crossed ? new Color(1f, 1f, 1f, 0.35f) : Color.white;

            var name = RuntimeUIKit.MakeText("Name", rect, def.displayName, 19, TextAnchor.UpperCenter, crossed ? new Color(0.6f, 0.55f, 0.55f) : Color.white, new Vector2(x + 8f, top - 80f), new Vector2(cardWidth - 16f, 26f));
            name.fontStyle = FontStyle.Bold;

            var lines = $"Couleur : {def.color}\\nTransparence : {def.TransparencyLabel}\\nÉclat : {def.luster}\\nDensité : {def.density.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}".Replace("\\n", "\n");
            var data = RuntimeUIKit.MakeText("Data", rect, lines, 14, TextAnchor.UpperLeft, crossed ? new Color(0.55f, 0.55f, 0.58f) : new Color(0.85f, 0.90f, 1f), new Vector2(x + 16f, top - 116f), new Vector2(cardWidth - 32f, 110f));
            data.horizontalOverflow = HorizontalWrapMode.Wrap;
            data.lineSpacing = 1.15f;

            var button = RuntimeUIKit.MakeButton("Pick", rect, crossed ? "Écartée" : "C'est celle-ci", 16, crossed ? new Color(0.25f, 0.25f, 0.28f, 1f) : new Color(0.25f, 0.50f, 0.30f, 1f),
                new Vector2(x + 25f, top - cardHeight + 52f), new Vector2(cardWidth - 50f, 38f));
            button.interactable = !crossed;
            button.onClick.AddListener(() =>
            {
                if (done) return;
                done = true;
                Sfx.Play(Sfx.Id.Click, 0.6f);
                Close();
                chosen?.Invoke(index);
            });
        }

        var later = RuntimeUIKit.MakeButton("Later", rect, "Plus tard", 14, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(width / 2f - 70f, -height + 52f), new Vector2(140f, 34f));
        later.onClick.AddListener(() =>
        {
            if (done) return;
            done = true;
            Close();
            chosen?.Invoke(-1);
        });
        StardewKit.Skin(rect);
    }

    public static void Close()
    {
        if (current != null) UnityEngine.Object.Destroy(current);
        current = null;
    }
}
