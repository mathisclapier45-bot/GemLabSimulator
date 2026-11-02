using UnityEngine;

// The grading desk of the second floor of the Diamond Quarter tower: E opens the grading interface (color, clarity, certificate).
public class GradingStation : MonoBehaviour
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
        GradingUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.ToolOpen);
        if (GradingUI.Instance != null && GradingUI.Instance.IsOpen) return;
        GradingUI.TryOpen();
    }
}
