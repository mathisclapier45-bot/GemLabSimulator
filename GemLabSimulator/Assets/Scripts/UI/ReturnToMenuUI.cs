using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnToMenuUI : MonoBehaviour
{
    public static ReturnToMenuUI Instance { get; private set; }

    [SerializeField] private GameObject panel;

    private Text musicButtonLabel;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Toggle()
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) RefreshMusicLabel();
    }

    // Built at runtime so existing scenes need no regeneration: the pause panel just grows by one row.
    private void BuildMusicButton()
    {
        if (panel == null) return;

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 215f);

        var go = new GameObject("MusicButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(panel.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -160f);
        rect.sizeDelta = new Vector2(250f, 36f);
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.32f, 1f);

        var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        musicButtonLabel = textGO.GetComponent<Text>();
        musicButtonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        musicButtonLabel.fontSize = 16;
        musicButtonLabel.alignment = TextAnchor.MiddleCenter;
        musicButtonLabel.color = Color.white;
        musicButtonLabel.raycastTarget = false;

        go.GetComponent<Button>().onClick.AddListener(ToggleMusic);
        RefreshMusicLabel();
    }

    private void ToggleMusic()
    {
        var music = MusicManager.Instance;
        if (music == null) return;
        music.SetMuted(!music.IsMuted);
        RefreshMusicLabel();
    }

    private void RefreshMusicLabel()
    {
        if (musicButtonLabel == null) return;
        var muted = MusicManager.Instance != null && MusicManager.Instance.IsMuted;
        musicButtonLabel.text = muted ? "Activer la musique" : "Couper la musique";
    }

    public void Confirm()
    {
        SaveSystem.Save();
        SceneManager.LoadScene("MainMenu");
    }

    public void Cancel()
    {
        if (panel != null) panel.SetActive(false);
    }
}
