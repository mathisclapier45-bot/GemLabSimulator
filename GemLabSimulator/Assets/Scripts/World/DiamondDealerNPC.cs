using UnityEngine;

// A diamond dealer sitting at his desk on the 1st floor of the tower: press E near him to see his stock.
public class DiamondDealerNPC : MonoBehaviour
{
    [SerializeField] private int dealerIndex;

    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        DiamondDealerUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        DiamondDealerUI.TryOpen(dealerIndex);
    }
}
