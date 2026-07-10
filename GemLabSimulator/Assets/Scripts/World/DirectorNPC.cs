using UnityEngine;

public class DirectorNPC : MonoBehaviour
{
    private bool playerInRange;
    private PlayerGemHolder playerGemHolder;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var holder = other.GetComponent<PlayerGemHolder>();
        if (holder == null) return;
        playerGemHolder = holder;
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerGemHolder>() != playerGemHolder) return;
        playerInRange = false;
        playerGemHolder = null;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        if (!playerGemHolder.HasGem)
        {
            Debug.Log("Le directeur : \"Revenez me voir avec une gemme à faire valider.\"");
            return;
        }

        ReportUIController.Instance.Open(playerGemHolder);
    }
}
