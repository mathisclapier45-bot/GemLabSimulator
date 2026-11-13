using UnityEngine;

public class ManoirOldLady : MonoBehaviour
{
    private static readonly string[] Lines =
    {
        "La vieille dame : \"Mon mari passe son temps à chercher des pierres introuvables... c'est sa seule distraction, à son âge.\"",
        "La vieille dame : \"Ce manoir a vu passer bien des collectionneurs. Peu avaient votre patience.\"",
        "La vieille dame : \"Ne faites pas attention à ses demandes étranges. Il paie toujours ce qu'il promet.\"",
        "La vieille dame : \"Par une nuit pareille, on entendrait presque le vent siffler dans le tunnel, plus loin en ville...\"",
    };

    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;
        Sfx.Play(Sfx.Id.Chat);
        DialogueBoxUI.Say(Lines[Random.Range(0, Lines.Length)], gameObject);
    }
}
