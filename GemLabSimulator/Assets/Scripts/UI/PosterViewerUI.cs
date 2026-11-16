using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The window of a poster: the landscape picture (PosterArt) with its labels. Opened from the shelf of the bag; the same window will open when a poster
// hung on a wall is clicked. Built at runtime on the canvas of the scene.
public class PosterViewerUI : MonoBehaviour
{
    public static PosterViewerUI Instance { get; private set; }

    private const float Border = 20f;
    private const float TitleHeight = 46f;
    private const float CaptionHeight = 64f;

    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);

    private GameObject panel;
    private RectTransform poster;
    private RawImage picture;
    private Text title;
    private Text caption;
    private RectTransform panelRect;
    private RectTransform closeRect;
    private RectTransform titleRect;
    private RectTransform captionRect;

    public bool IsOpen => panel != null && panel.activeSelf;

    public static void Open(string id)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<PosterViewerUI>();
        }
        Instance?.Show(id);
    }

    private void Awake()
    {
        Instance = this;
        Build();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) { EscapeGate.Consume(); Close(); }
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Show(string id)
    {
        var info = PosterCatalog.Find(id);
        if (info == null) return;

        title.text = info.Title;
        caption.text = info.Caption ?? "<b>Du bord au centre</b> : " + RowCaption() + "\n<color=#9AA3B5>À gauche, le nom que le jeu donne à chaque zone ; à droite, l'angle de la teinte, de 0° (rouge) à 360°, dans le sens des aiguilles d'une montre.</color>";
        Layout(PosterArt.SizeOf(id));

        for (var i = poster.childCount - 1; i >= 0; i--) Destroy(poster.GetChild(i).gameObject);

        var texture = PosterArt.Poster(id, out var labels);
        picture.texture = texture;
        foreach (var label in labels) MakeLabel(label);

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    // Sizes the window to the picture, then shrinks it if it does not fit the canvas.
    private void Layout(Vector2Int size)
    {
        var width = size.x + 2f * Border;
        var height = TitleHeight + size.y + CaptionHeight + Border;
        panelRect.sizeDelta = new Vector2(width, height);
        RuntimeUIKit.SetTopLeft(poster, new Vector2(Border, -TitleHeight), new Vector2(size.x, size.y));
        RuntimeUIKit.SetTopLeft(titleRect, new Vector2(Border + 10f, -14f), new Vector2(width - 160f, 30f));
        RuntimeUIKit.SetTopLeft(closeRect, new Vector2(width - 108f, -12f), new Vector2(84f, 28f));
        RuntimeUIKit.SetTopLeft(captionRect, new Vector2(Border + 10f, -(TitleHeight + size.y + 6f)), new Vector2(width - 2f * Border - 20f, CaptionHeight - 6f));

        var scale = 1f;
        if (transform is RectTransform canvasRect && canvasRect.rect.width > 1f)
            scale = Mathf.Min(1f, canvasRect.rect.width * 0.98f / width, canvasRect.rect.height * 0.97f / height);
        panelRect.localScale = new Vector3(scale, scale, 1f);
    }

    private static string RowCaption()
    {
        var words = new List<string>();
        for (var row = 0; row < GemColorSystem.RowCount; row++) words.Add(GemColorSystem.RowLabel(row));
        return string.Join(", ", words) + ".";
    }

    private void MakeLabel(PosterLabel label)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(poster, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(label.Left ? 0f : 0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(label.Position.x, -label.Position.y);
        rect.sizeDelta = new Vector2(Mathf.Max(label.Width, 24f), Mathf.Max(14f, label.Size + 4f));
        rect.localRotation = Quaternion.Euler(0f, 0f, label.Rotation);

        var text = go.GetComponent<Text>();
        text.text = label.Text;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = label.Size;
        text.alignment = label.Left ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        text.fontStyle = label.Bold ? (label.Italic ? FontStyle.BoldAndItalic : FontStyle.Bold) : (label.Italic ? FontStyle.Italic : FontStyle.Normal);
        text.color = label.Color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
    }

    private void Build()
    {
        var width = PosterArt.PosterWidth + 2f * Border;
        var height = TitleHeight + PosterArt.PosterHeight + CaptionHeight + Border;

        panel = new GameObject("PosterPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        StardewKit.FramePanel(panel.GetComponent<Image>());

        title = RuntimeUIKit.MakeText("Title", panel.transform, "", 22, TextAnchor.UpperLeft, Gold, new Vector2(Border + 10f, -14f), new Vector2(width - 160f, 30f));
        title.fontStyle = FontStyle.Bold;
        var closeButton = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, new Color(0.32f, 0.26f, 0.22f, 1f), new Vector2(width - 108f, -12f), new Vector2(84f, 28f));
        closeButton.onClick.AddListener(Close);
        closeRect = closeButton.GetComponent<RectTransform>();
        panelRect = rect;
        titleRect = title.GetComponent<RectTransform>();

        var posterGO = new GameObject("Poster", typeof(RectTransform), typeof(RawImage));
        posterGO.transform.SetParent(panel.transform, false);
        poster = posterGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(poster, new Vector2(Border, -TitleHeight), new Vector2(PosterArt.PosterWidth, PosterArt.PosterHeight));
        picture = posterGO.GetComponent<RawImage>();
        picture.raycastTarget = false;

        caption = RuntimeUIKit.MakeText("Caption", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.82f, 0.78f, 0.70f),
            new Vector2(Border + 10f, -(TitleHeight + PosterArt.PosterHeight + 6f)), new Vector2(width - 2f * Border - 20f, CaptionHeight - 6f));
        caption.supportRichText = true;
        caption.horizontalOverflow = HorizontalWrapMode.Wrap;
        captionRect = caption.GetComponent<RectTransform>();
        StardewKit.Skin(panel.transform);
    }
}
