using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private bool implemented;
    [SerializeField] private string lockedMessage = "Ce lieu n'est pas encore accessible.";
    [SerializeField] private string spawnPointName;

    // False for decorative buildings whose door stays locked.
    public bool IsAccessible => implemented && !string.IsNullOrEmpty(sceneName);

    public string TargetScene => sceneName;

    // Goes through the door, as the player does by walking into it.
    public void Use()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        Sfx.Play(Sfx.Id.DoorOpen);
        PlayerController.PendingSpawnPointName = spawnPointName;
        SceneFader.FadeToScene(sceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!implemented || string.IsNullOrEmpty(sceneName))
        {
            Sfx.Play(Sfx.Id.DoorLocked);
            ToolFeedbackUI.Instance?.ShowMessage(lockedMessage);
            return;
        }

        if (!OpeningHours.IsOpen(sceneName))
        {
            Sfx.Play(Sfx.Id.DoorLocked);
            ToolFeedbackUI.Instance?.ShowMessage($"C'est fermé pour le moment. Ouvert de {OpeningHours.Describe(sceneName)}.");
            return;
        }

        Use();
    }
}
