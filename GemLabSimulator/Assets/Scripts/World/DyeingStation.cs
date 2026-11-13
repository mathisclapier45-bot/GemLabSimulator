using UnityEngine;

// The desk of the dye pots of the treatments laboratory: press E in front of it to open the dyeing window.
public class DyeingStation : MonoBehaviour
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
        DyeingUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to dye.
        var state = DyeingState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Voici de quoi essayer vos couleurs : une agate, de l'ambre, un lapis-lazuli, une opale, une turquoise et un quartz. Ils sont dans votre sac.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Technicien", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (DyeingUI.Instance != null && DyeingUI.Instance.IsOpen) DyeingUI.Instance.Close();
        else DyeingUI.TryOpen();
    }
}
