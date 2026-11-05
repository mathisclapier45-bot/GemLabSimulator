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
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;

        // The first time, the technician hands over a sample of each material to fill.
        var state = FillingState.Ensure();
        if (state != null && !state.SamplesGiven)
        {
            string message;
            if (state.TryGiveSamples(out message))
            {
                Sfx.Play(Sfx.Id.Success);
                DialogueBoxUI.Say("Technicien", "Pour essayer l'autoclave : une émeraude, un rubis, une jadéite, une turquoise, une opale et un lapis-lazuli. Ils sont dans votre sac.");
                return;
            }
            if (!string.IsNullOrEmpty(message))
            {
                DialogueBoxUI.Say("Technicien", message);
                return;
            }
        }

        Sfx.Play(Sfx.Id.ToolOpen);
        if (FillingUI.Instance != null && FillingUI.Instance.IsOpen) FillingUI.Instance.Close();
        else FillingUI.TryOpen();
    }
}
