using UnityEngine;

// Martin Rapaport at his desk, on the 6th floor of the Diamond Quarter: press E to talk about the subscription to his price list.
public class RapaportNPC : MonoBehaviour
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
        RapaportOfficeUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.Chat);
        RapaportOfficeUI.TryOpen();
    }
}
