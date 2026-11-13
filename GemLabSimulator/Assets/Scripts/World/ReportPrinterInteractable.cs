using UnityEngine;

// The printer standing next to the professor: E opens the report printer interface.
public class ReportPrinterInteractable : MonoBehaviour
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
        ReportPrinterUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.ToolOpen);
        ReportPrinterUI.Instance?.Open();
    }
}
