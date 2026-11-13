using UnityEngine;

// The autoclave of the treatments laboratory: press E in front of it to open the impregnation window.
public class FillingStation : MonoBehaviour
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
        FillingUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to fill.
        var state = FillingState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Pour essayer l'autoclave : une émeraude, un rubis, une jadéite, une turquoise, une opale et un lapis-lazuli. Ils sont dans votre sac. Revenez me voir pour les diamants.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Technicien", message);
                return;
            }
        }

        // The diamonds: two cut ones with feathers, once.
        if (state != null && state.SamplesGiven && !state.DiamondSamplesGiven)
        {
            string diamondMessage;
            if (state.TryGiveDiamondSamples(out diamondMessage))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Nouveau : on remplit aussi les diamants à plumes, au verre au plomb. Deux diamants taillés sont dans votre sac. La plume se voit moins, la pureté monte, mais le certificat est perdu et la chaleur d'un sertissage détruit le remplissage. À la loupe, l'effet flash orange et bleu le trahit.");
                return;
            }
            if (!string.IsNullOrEmpty(diamondMessage))
            {
                DialogueBoxUI.Say("Technicien", diamondMessage);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (FillingUI.Instance != null && FillingUI.Instance.IsOpen) FillingUI.Instance.Close();
        else FillingUI.TryOpen();
    }
}
