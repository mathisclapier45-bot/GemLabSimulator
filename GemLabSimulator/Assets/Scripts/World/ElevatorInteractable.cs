using UnityEngine;

// The elevator doors of the Diamond Quarter tower: press E in front of them to pick a floor.
public class ElevatorInteractable : MonoBehaviour
{
    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        ElevatorUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.Ding);
        ElevatorUI.TryToggle();
    }
}
