using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool implemented;
    [SerializeField] private string lockedMessage = "Ce lieu n'est pas encore accessible.";
    [SerializeField] private string spawnPointName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!implemented || string.IsNullOrEmpty(sceneName))
        {
            ToolFeedbackUI.Instance?.ShowMessage(lockedMessage);
            return;
        }

        PlayerController.PendingSpawnPointName = spawnPointName;
        SceneFader.FadeToScene(sceneName);
    }
}
