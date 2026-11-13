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
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.Chat);
        if (ManoirRequestState.Instance == null) return;

        if (ManoirRequestState.Instance.CurrentRequest == null)
        {
            DialogueBoxUI.Say("Le vieil homme", "Rien à demander pour l'instant... repassez un autre jour.", gameObject);
            return;
        }

        var selected = playerInventory.SelectedGem;
        if (selected != null && ManoirRequestState.Instance.Matches(selected))
        {
            if (ManoirRequestState.Instance.TryFulfill(selected, playerInventory, PlayerWallet.Instance, out var reward))
                DialogueBoxUI.Say("Le vieil homme", $"Il sourit et vous tend {reward}€. \"Voilà qui fera l'affaire.\"", gameObject);
            return;
        }

        DialogueBoxUI.Say("Le vieil homme",
            $"{ManoirRequestState.Instance.DescribeRequest()}\n(Sélectionnez la gemme correspondante dans l'inventaire avec Tab, puis revenez me voir.)", gameObject);
    }
}
