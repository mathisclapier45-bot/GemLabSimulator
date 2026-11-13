using UnityEngine;

// The row of vats of the treatments laboratory: press E in front of them to open the bleaching window.
public class BleachingStation : MonoBehaviour
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
        BleachingUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to bleach.
        var state = BleachingState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Tenez, quelques échantillons pour vos premiers bains : une perle de culture, une perle de Tahiti, un corail et un œil-de-tigre. Ils sont dans votre sac.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Technicien", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (BleachingUI.Instance != null && BleachingUI.Instance.IsOpen) BleachingUI.Instance.Close();
        else BleachingUI.TryOpen();
    }
}
