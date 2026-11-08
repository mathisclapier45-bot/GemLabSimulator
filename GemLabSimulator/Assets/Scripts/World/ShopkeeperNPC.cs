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

        // The visit of the story: Léa has something left by Vincent.
        var story = StoryState.Instance;
        if (story != null && !story.Legacy && story.Phase == StoryState.PhaseLeaVisit && StoryDirector.Instance != null)
        {
            if (!DialogueBoxUI.IsShowing && !PlayerController.InputLocked) StoryDirector.Instance.TalkToLea(gameObject);
            return;
        }

        Speak();
    }

    // A click on the dialogue window goes on to the second line.
    private void Speak()
    {
        var line = Lines[lineIndex % Lines.Length];
        lineIndex++;
        DialogueBoxUI.Say("Léa", line, gameObject, lineIndex % Lines.Length != 0 ? (System.Action)Speak : null);
    }
}
