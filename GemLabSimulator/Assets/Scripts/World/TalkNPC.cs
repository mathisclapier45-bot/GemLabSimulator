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
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E) || lines == null || lines.Length == 0) return;

        Sfx.Play(Sfx.Id.Chat);
        ToolFeedbackUI.Instance?.ShowMessage($"{speaker} : {lines[lineIndex % lines.Length]}");
        lineIndex++;
    }
}
