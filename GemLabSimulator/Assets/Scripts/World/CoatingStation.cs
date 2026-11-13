using UnityEngine;

// The vacuum chamber of the treatments laboratory: press E in front of it to open the coating window.
public class CoatingStation : MonoBehaviour
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
        CoatingUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to coat.
        var state = CoatingState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Pour vos premiers dépôts : une topaze, un quartz, une goshénite, une tanzanite, une opale et de l'ambre. Ils sont dans votre sac. Revenez me voir pour les diamants.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Technicien", message);
                return;
            }
        }

        // The diamonds: two cut ones with a yellow tint, once.
        if (state != null && state.SamplesGiven && !state.DiamondSamplesGiven)
        {
            string diamondMessage;
            if (state.TryGiveDiamondSamples(out diamondMessage))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Nouveau : on revêt aussi les diamants. Deux diamants taillés un peu jaunes sont dans votre sac. Un voile très fin de cobalt seul masque le jaune : la pierre paraît plus blanche. Tout autre film en fait une pierre fancy qui n'est pas naturelle. Dans les deux cas, le film s'éraille et le certificat est perdu.");
                return;
            }
            if (!string.IsNullOrEmpty(diamondMessage))
            {
                DialogueBoxUI.Say("Technicien", diamondMessage);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (CoatingUI.Instance != null && CoatingUI.Instance.IsOpen) CoatingUI.Instance.Close();
        else CoatingUI.TryOpen();
    }
}
