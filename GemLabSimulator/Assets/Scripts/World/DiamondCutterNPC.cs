using UnityEngine;

// A diamond cutter at his bench on the 5th floor of the tower: press E near him to hand him a rough diamond.
public class DiamondCutterNPC : MonoBehaviour
{
    [SerializeField] private int cutterIndex;

    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        DiamondCutterUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.Chat);
        DiamondCutterUI.TryOpen(cutterIndex);
    }
}
