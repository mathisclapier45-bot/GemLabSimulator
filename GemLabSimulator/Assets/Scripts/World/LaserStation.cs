using UnityEngine;

// The laser station of the treatments laboratory: press E in front of it to open the laser window.
public class LaserStation : MonoBehaviour
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
        LaserUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a few diamonds to treat.
        var state = LaserState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Ingénieur", "Voici quatre diamants taillés, avec des inclusions foncées : ils sont dans votre sac, pas encore certifiés. Le laser les éclaircit, mais la pierre traitée devra repasser au bureau de grading.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Ingénieur", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (LaserUI.Instance != null && LaserUI.Instance.IsOpen) LaserUI.Instance.Close();
        else LaserUI.TryOpen();
    }
}
