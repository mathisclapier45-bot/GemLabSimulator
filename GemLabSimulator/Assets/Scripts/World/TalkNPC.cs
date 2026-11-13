using UnityEngine;

// A character you can talk to with E: says its lines one after the other. Placeholder dialogue until the real one exists.
public class TalkNPC : MonoBehaviour
{
    [SerializeField] private string speaker;
    [SerializeField] private string[] lines;

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
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E) || lines == null || lines.Length == 0) return;

        Sfx.Play(Sfx.Id.Chat);
        Speak();
    }

    // A click on the dialogue window goes on to the following line as long as there is one left to read.
    private void Speak()
    {
        var line = lines[lineIndex % lines.Length];
        lineIndex++;
        DialogueBoxUI.Say(speaker, line, gameObject, lineIndex % lines.Length != 0 ? (System.Action)Speak : null);
    }
}
