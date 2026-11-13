using UnityEngine;

// The irradiator of the treatments laboratory: press E in front of it to open the irradiation window.
public class IrradiationStation : MonoBehaviour
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
        IrradiationUI.Instance?.Close();
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to irradiate.
        var state = IrradiationState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Ingénieur", "Pour vos premières irradiations : un quartz, une citrine, une goshénite, une morganite, une topaze, une tourmaline rose, une perle et un saphir incolore. Ils sont dans votre sac. Revenez me voir pour les diamants. Les neutrons rendent la pierre radioactive : prévoyez du temps pour qu'elle refroidisse.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Ingénieur", message);
                return;
            }
        }

        // The diamonds: two cut ones, once.
        if (state != null && state.SamplesGiven && !state.DiamondSamplesGiven)
        {
            string diamondMessage;
            if (state.TryGiveDiamondSamples(out diamondMessage))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Ingénieur", "Nouveau : on irradie aussi les diamants. Deux diamants taillés, pâles, sont dans votre sac. Gamma pour un vert pâle, électrons pour un bleu-vert, neutrons pour un vert foncé. La couleur est stable, mais un diamant irradié perd son certificat et la photoluminescence le trahit.");
                return;
            }
            if (!string.IsNullOrEmpty(diamondMessage))
            {
                DialogueBoxUI.Say("Ingénieur", diamondMessage);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (IrradiationUI.Instance != null && IrradiationUI.Instance.IsOpen) IrradiationUI.Instance.Close();
        else IrradiationUI.TryOpen();
    }
}
