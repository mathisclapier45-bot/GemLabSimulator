using UnityEngine;

// The lab computer, standing next to the microscope: E turns it on (boot screen, then the desktop with the microphotographs).
public class ComputerInteractable : MonoBehaviour
{
    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        ComputerUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        if (ComputerUI.Instance == null || ComputerUI.Instance.IsOpen) return;
        ComputerUI.Instance.Open();
    }
}
