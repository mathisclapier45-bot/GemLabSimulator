using UnityEngine;

public class Mailbox : MonoBehaviour
{
    public enum MailboxKind { Lab, Personal }

    // Lab: the letters addressed to the gemology lab (client stones). Personal: the player's own mail, not developed yet.
    [SerializeField] private MailboxKind kind = MailboxKind.Lab;

    private bool playerInRange;

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

        Sfx.Play(Sfx.Id.Paper);
        if (kind == MailboxKind.Personal)
        {
            if (StoryLetterUI.HasMail) StoryLetterUI.TryOpen();
            else ToolFeedbackUI.Instance?.ShowMessage("Courrier personnel : rien pour le moment.");
            return;
        }

        MailboxUI.Instance?.Toggle();
    }
}
