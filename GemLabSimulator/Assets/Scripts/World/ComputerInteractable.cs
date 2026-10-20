using UnityEngine;

// The lab computer, standing next to the microscope: it receives the microphotographs. Its interface comes later.
public class ComputerInteractable : MonoBehaviour
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

        var library = MicroPhotoLibrary.Ensure();
        var count = library != null ? library.Photos.Count : 0;
        ToolFeedbackUI.Instance?.ShowMessage(count == 0
            ? "Ordinateur du laboratoire : aucune microphotographie reçue. (Interface bientôt disponible.)"
            : $"Ordinateur du laboratoire : {count} microphotographie(s) reçue(s). (Interface bientôt disponible.)");
    }
}
