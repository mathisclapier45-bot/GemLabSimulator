using UnityEngine;

// The bed of the player's house: E puts the light out and goes to sleep (see SleepSequence): night in the room, a real sleep, "La nuit porte conseil.", the day changes,
// the game is saved, and the day rises with a rooster.
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
        if (SleepSequence.Running || PlayerController.InputLocked) return;

        SleepSequence.Begin(transform);
    }
}
