using UnityEngine;

public class Mailbox : MonoBehaviour
{
    [SerializeField] private GemDefinition[] gemPool;

    private bool playerInRange;
    private PlayerInventory playerInventory;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null) return;
        playerInventory = inventory;
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerInventory>() != playerInventory) return;
        playerInRange = false;
        playerInventory = null;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        if (playerInventory.IsFull)
        {
            ToolFeedbackUI.Instance.ShowMessage("Boîte aux lettres : inventaire plein, traitez d'abord une gemme.");
            return;
        }

        if (gemPool == null || gemPool.Length == 0) return;

        var gem = gemPool[Random.Range(0, gemPool.Length)];
        playerInventory.AddGem(gem);
        ToolFeedbackUI.Instance.ShowMessage($"Boîte aux lettres : vous avez reçu \"{gem.displayName}\".");
    }
}
