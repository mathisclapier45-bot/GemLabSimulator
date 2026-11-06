using System;
using UnityEngine;

// The Guide of the gemmologist, sold by the librarian: a book with a page for every species of the grimoire, with all that is known about it (the
// optical and physical properties, the treatments that can be done, what the instruments of the laboratory show). Once bought, it is kept for good and
// has its own tab in the bag. Lives on the player (added on demand, like the other persistent states).
public class GuideBookState : MonoBehaviour
{
    public static GuideBookState Instance { get; private set; }

    public const int Price = 1800;

    public bool Owned { get; private set; }

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static GuideBookState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<GuideBookState>();
        return Instance;
    }

    public static bool HasBook => Instance != null && Instance.Owned;

    public bool TryBuy(out string message)
    {
        message = null;
        if (Owned)
        {
            message = "Vous possédez déjà le Guide du gemmologue.";
            return false;
        }
        var wallet = PlayerWallet.Instance;
        if (wallet == null) return false;
        if (!wallet.TrySpend(Price))
        {
            message = $"Il vous faut {Price:N0} € pour acheter le Guide du gemmologue.";
            return false;
        }

        Owned = true;
        OnChanged?.Invoke();
        return true;
    }

    public void Load(bool owned)
    {
        Owned = owned;
        OnChanged?.Invoke();
    }
}
