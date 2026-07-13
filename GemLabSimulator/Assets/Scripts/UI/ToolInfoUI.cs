using UnityEngine;
using UnityEngine.UI;

public class ToolInfoUI : MonoBehaviour
{
    public static ToolInfoUI Instance { get; private set; }

    [SerializeField] private GameObject infoButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;

    private string currentToolName;

    private void Awake()
    {
        Instance = this;
        if (infoButton != null) infoButton.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    public void ShowButtonFor(string toolName)
    {
        currentToolName = toolName;
        if (infoButton != null) infoButton.SetActive(true);
    }

    public void HideButtonIfCurrent(string toolName)
    {
        if (currentToolName != toolName) return;
        currentToolName = null;
        if (infoButton != null) infoButton.SetActive(false);
        Close();
    }

    public void OpenInfo()
    {
        if (string.IsNullOrEmpty(currentToolName)) return;

        if (titleText != null) titleText.text = currentToolName;
        if (bodyText != null) bodyText.text = ToolInfo.GetDescription(currentToolName);
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
}
