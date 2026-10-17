using UnityEngine;

// Lapidary machine that polishes a small flat "window" on a rough stone so the inside can be examined.
// Interacting opens WindowPolisherUI.
public class WindowPolisher : MonoBehaviour
{
    public const string ToolName = "PolisseuseFenetre";

    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        ToolInfoUI.Instance?.ShowButtonFor(ToolName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        ToolInfoUI.Instance?.HideButtonIfCurrent(ToolName);
        WindowPolisherUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        WindowPolisherUI.Instance?.Open();
    }
}
