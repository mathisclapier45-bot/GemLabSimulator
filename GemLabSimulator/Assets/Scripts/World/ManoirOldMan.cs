using UnityEngine;

public class ManoirOldMan : MonoBehaviour
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
        if (ManoirRequestState.Instance == null) return;

        if (ManoirRequestState.Instance.CurrentRequest == null)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Le vieil homme : \"Rien à demander pour l'instant... repassez un autre jour.\"");
            return;
        }

        var selected = playerInventory.SelectedGem;
        if (selected != null && ManoirRequestState.Instance.Matches(selected))
        {
            if (ManoirRequestState.Instance.TryFulfill(selected, playerInventory, PlayerWallet.Instance, out var reward))
                ToolFeedbackUI.Instance?.ShowMessage($"Le vieil homme sourit et vous tend {reward}€. \"Voilà qui fera l'affaire.\"");
            return;
        }

        ToolFeedbackUI.Instance?.ShowMessage(
            $"{ManoirRequestState.Instance.DescribeRequest()}\n(Sélectionnez la gemme correspondante dans l'inventaire avec Tab, puis revenez me voir.)");
    }
}
