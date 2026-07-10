using UnityEngine;

public class Mailbox : MonoBehaviour
{
    [SerializeField] private GemDefinition[] gemPool;

    private bool playerInRange;
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
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        if (playerGemHolder.HasGem)
        {
            ToolFeedbackUI.Instance.ShowMessage("Boîte aux lettres : vous avez déjà une gemme à traiter.");
            return;
        }

        if (gemPool == null || gemPool.Length == 0) return;

        var gem = gemPool[Random.Range(0, gemPool.Length)];
        playerGemHolder.ReceiveGem(gem);
        ToolFeedbackUI.Instance.ShowMessage($"Boîte aux lettres : vous avez reçu \"{gem.displayName}\".");
    }
}
