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

    // The message of a door that does not open: the one of the scene, or the story's (the house of Vincent Pardieu has lost its keys).
    private string LockedText()
    {
        if (StoryState.Instance != null && !StoryState.Instance.Legacy && name.Contains("Vincent Pardieu"))
            return "Maison de Vincent Pardieu : la porte est fermée à clef, et personne ne sait où sont les clefs.";
        return lockedMessage;
    }

    // Goes through the door, as the player does by walking into it.
    public void Use()
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!StoryGate.IsOpen(sceneName, out var closed))
        {
            Sfx.Play(Sfx.Id.DoorLocked);
            ToolFeedbackUI.Instance?.ShowMessage(closed);
            return;
        }
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
            ToolFeedbackUI.Instance?.ShowMessage(LockedText());
            return;
        }

        if (!StoryGate.IsOpen(sceneName, out var closed))
        {
            Sfx.Play(Sfx.Id.DoorLocked);
            ToolFeedbackUI.Instance?.ShowMessage(closed);
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
