using UnityEngine;

public class Mailbox : MonoBehaviour
{
    [SerializeField] private GemDefinition gemToDeliver;

    private bool playerInRange;
    private bool delivered;
    private PlayerGemHolder playerGemHolder;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var holder = other.GetComponent<PlayerGemHolder>();
        if (holder == null) return;
        playerGemHolder = holder;
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerGemHolder>() != playerGemHolder) return;
        playerInRange = false;
        playerGemHolder = null;
    }

    private void Update()
    {
        if (!playerInRange || delivered || gemToDeliver == null) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        playerGemHolder.ReceiveGem(gemToDeliver);
        delivered = true;
        Debug.Log("Boîte aux lettres : courrier récupéré.");
    }
}
