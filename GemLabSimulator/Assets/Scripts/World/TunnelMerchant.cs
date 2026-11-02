using UnityEngine;

public class TunnelMerchant : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D interactionCollider;

    private bool playerInRange;
    private bool wasPresent;

    private void Start()
    {
        Refresh();
    }

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
        Refresh();

        if (!wasPresent || !playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        Sfx.Play(Sfx.Id.Chat);
        TunnelUI.Instance?.Open();
    }

    private void Refresh()
    {
        var present = TunnelState.IsNight();
        if (present == wasPresent) return;
        wasPresent = present;

        if (spriteRenderer != null) spriteRenderer.enabled = present;
        if (interactionCollider != null) interactionCollider.enabled = present;

        if (!present)
        {
            playerInRange = false;
            TunnelUI.Instance?.Close();
        }
    }
}
