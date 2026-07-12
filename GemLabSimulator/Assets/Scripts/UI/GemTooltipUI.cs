using UnityEngine;
using UnityEngine.UI;

public class GemTooltipUI : MonoBehaviour
{
    public static GemTooltipUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text label;
    [SerializeField] private RectTransform rect;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private const float LineHeight = 18f;
    private const float VerticalPadding = 14f;

    public void Show(string text, Vector2 screenPosition)
    {
        label.text = text;
        var lineCount = text.Split('\n').Length;
        var height = Mathf.Max(30f, lineCount * LineHeight + VerticalPadding);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        rect.position = screenPosition + new Vector2(16f, 8f);
        panel.SetActive(true);
        rect.SetAsLastSibling();
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
