using UnityEngine;

public class DirectorNPC : MonoBehaviour
{
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

        if (playerInventory.SelectedGem == null)
        {
            ToolFeedbackUI.Instance.ShowMessage("Le directeur : \"Revenez avec une gemme sélectionnée (Tab pour l'inventaire).\"");
            return;
        }

        ReportUIController.Instance.Open(playerInventory);
    }
}
