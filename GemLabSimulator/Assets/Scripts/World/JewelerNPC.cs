using UnityEngine;

// The jeweller at his counter: press E to open the workshop (the stones to set, the jewels in the making, the jewels to sell).
public class JewelerNPC : MonoBehaviour
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
        JewelerUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        if (JewelerUI.Instance != null && JewelerUI.Instance.IsOpen) return;
        Sfx.Play(Sfx.Id.Chat);
        JewelerUI.TryOpen();
    }
}
