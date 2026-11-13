using UnityEngine;

public class CabinetInteractable : MonoBehaviour
{
    [SerializeField] private CabinetKind kind = CabinetKind.Donations;

    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        CabinetUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.ChestOpen);
        CabinetUI.TryToggle(kind);
    }
}
