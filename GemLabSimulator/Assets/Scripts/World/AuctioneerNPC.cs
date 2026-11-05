using UnityEngine;

// The auctioneer of the bank, at the left counter: press E to open the auction room (the sale of the day, the stones left for the next one).
public class AuctioneerNPC : MonoBehaviour
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
        AuctionUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        if (AuctionUI.Instance != null && AuctionUI.Instance.IsOpen) return;
        Sfx.Play(Sfx.Id.Chat);
        AuctionUI.TryOpen();
    }
}
