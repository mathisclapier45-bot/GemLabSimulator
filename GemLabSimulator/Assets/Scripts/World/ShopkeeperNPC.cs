using UnityEngine;

// The GemStore clerk behind the counter. Placeholder dialogue until the shop's stock and purchase window exist.
public class ShopkeeperNPC : MonoBehaviour
{
    private static readonly string[] Lines =
    {
        "Bienvenue au GemStore ! Instruments, accessoires et améliorations pour gemmologues.",
        "Les étagères se remplissent au fil de votre progression. Repassez nous voir bientôt !",
    };

    private bool playerInRange;
    private int lineIndex;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        Sfx.Play(Sfx.Id.Chat);
        ToolFeedbackUI.Instance?.ShowMessage(Lines[lineIndex % Lines.Length]);
        lineIndex++;
    }
}
