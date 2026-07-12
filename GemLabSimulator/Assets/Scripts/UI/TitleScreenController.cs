using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TitleScreenController : MonoBehaviour
{
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject slotsPanel;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(ShowSlots);
    }

    private void ShowSlots()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (slotsPanel != null) slotsPanel.SetActive(true);
    }
}
