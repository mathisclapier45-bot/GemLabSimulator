using UnityEngine;

// The receptionist of the Diamond Quarter: sells the diamond dealer licence, then welcomes the player to the upper floors.
public class ReceptionistNPC : MonoBehaviour
{
    [SerializeField] private ShopItem licenseItem;

    private bool playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        ShopUI.Instance?.CloseIfShowing(licenseItem);
    }

    private void Update()
    {
        if (!playerInRange || !TextEntry.KeyDown(KeyCode.E)) return;

        Sfx.Play(Sfx.Id.Chat);
        if (DiamondBuilding.HasLicense)
        {
            DialogueBoxUI.Say("Accueil", "Bonne visite ! L'ascenseur vous mène à tous les étages.", gameObject);
            return;
        }

        DialogueBoxUI.Say("Accueil", "Les étages sont réservés aux diamantaires licenciés. Voici les conditions de la licence.", gameObject);
        if (licenseItem != null) ShopUI.TryOpen(licenseItem);
    }
}
