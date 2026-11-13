using UnityEngine;

// Professor Thouvenot, waiting in the laboratory during the first steps (the tutorial): E in front of her talks to her. What she says depends on where the player is
// in the tutorial (see StoryDirector). Built by the StoryDirector, not in the scene.
public class ThouvenotNPC : MonoBehaviour
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
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        if (PlayerController.InputLocked || DialogueBoxUI.IsShowing || StoryChoiceUI.IsOpen) return;
        StoryDirector.Instance?.TalkToThouvenot(gameObject);
    }
}
