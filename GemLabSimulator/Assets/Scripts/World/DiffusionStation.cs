using UnityEngine;

// The diffusion furnace of the treatments laboratory: press E in front of it to open the diffusion window.
public class DiffusionStation : MonoBehaviour
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
        DiffusionUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each corundum to diffuse.
        var state = DiffusionState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Ingénieur", "Pour la diffusion : un saphir incolore, un rose, un bleu, un jaune et un rubis. Ils sont dans votre sac. Prévoyez du temps : ça se compte en jours.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Ingénieur", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (DiffusionUI.Instance != null && DiffusionUI.Instance.IsOpen) DiffusionUI.Instance.Close();
        else DiffusionUI.TryOpen();
    }
}
