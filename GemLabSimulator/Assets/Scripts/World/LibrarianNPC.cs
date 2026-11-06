using UnityEngine;

// The librarian at her desk: press E to see the Guide of the gemmologist, and buy it.
public class LibrarianNPC : MonoBehaviour
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
        LibraryUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        if (LibraryUI.Instance != null && LibraryUI.Instance.IsOpen) return;
        Sfx.Play(Sfx.Id.Chat);
        LibraryUI.TryOpen();
    }
}
