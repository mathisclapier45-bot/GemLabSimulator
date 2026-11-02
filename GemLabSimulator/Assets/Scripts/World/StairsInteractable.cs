using UnityEngine;

// A staircase leading to another scene (here: the roof of the Diamond Quarter tower and back). Press E next to it.
public class StairsInteractable : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private string targetSpawnPoint = "SpawnPoint_Stairs";

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
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E) || string.IsNullOrEmpty(targetScene)) return;

        Sfx.Play(Sfx.Id.Stairs);
        PlayerController.PendingSpawnPointName = targetSpawnPoint;
        SceneFader.FadeToScene(targetScene);
    }
}
