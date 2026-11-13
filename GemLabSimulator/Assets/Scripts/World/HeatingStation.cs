using UnityEngine;

// The furnace of the treatments laboratory: press E in front of it to open the heating window.
public class HeatingStation : MonoBehaviour
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
        HeatingUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to heat.
        var state = HeatingState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Ingénieur", "Pour faire chauffer : un saphir incolore, un rubis, un héliodore, une améthyste, une topaze, un zircon, une agate et de l'ambre. Ils sont dans votre sac.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Ingénieur", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (HeatingUI.Instance != null && HeatingUI.Instance.IsOpen) HeatingUI.Instance.Close();
        else HeatingUI.TryOpen();
    }
}
