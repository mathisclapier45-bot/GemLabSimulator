using UnityEngine;

// The press of the treatments laboratory: press E in front of it to open the HPHT window.
public class HphtStation : MonoBehaviour
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
        HphtUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a few diamonds to treat.
        var state = HphtState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Ingénieur", "Voici quatre diamants taillés à la teinte jaune-brun, pas encore certifiés : ils sont dans votre sac. Vérifiez leur azote à l'infrarouge avant de les mettre sous presse : selon lui, ils deviendront incolores ou jaunes.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Ingénieur", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (HphtUI.Instance != null && HphtUI.Instance.IsOpen) HphtUI.Instance.Close();
        else HphtUI.TryOpen();
    }
}
