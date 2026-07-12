using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenuUI : MonoBehaviour
{
    public static ReturnToMenuUI Instance { get; private set; }

    [SerializeField] private GameObject panel;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Toggle()
    {
        if (panel != null) panel.SetActive(!panel.activeSelf);
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
