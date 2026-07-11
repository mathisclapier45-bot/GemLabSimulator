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

    public void Show(string text, Vector2 screenPosition)
    {
        label.text = text;
        rect.position = screenPosition + new Vector2(16f, 8f);
        panel.SetActive(true);
        rect.SetAsLastSibling();
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
