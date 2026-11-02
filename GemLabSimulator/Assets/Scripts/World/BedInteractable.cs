using UnityEngine;

public class BedInteractable : MonoBehaviour
{
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

        Sfx.Play(Sfx.Id.Sleep);
        GameClock.Instance?.AdvanceDay();
        DayNightOverlay.ResetToDay();
        SaveSystem.Save();
        ToolFeedbackUI.Instance?.ShowMessage("Partie sauvegardée. Bonne nuit ! Vous vous réveillez à 7h.");
    }
}
