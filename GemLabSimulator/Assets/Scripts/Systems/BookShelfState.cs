using System;
using System.Collections.Generic;
using UnityEngine;

// The books the player has bought at the library, read from the books tab of the bag. Lives on the player (added on demand, like the other persistent
// states).
public class BookShelfState : MonoBehaviour
{
    public static BookShelfState Instance { get; private set; }

    private readonly HashSet<string> owned = new HashSet<string>();

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static BookShelfState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<BookShelfState>();
        return Instance;
    }

    public static bool HasAny => Instance != null && Instance.owned.Count > 0;

    public static bool Owns(string id) => Instance != null && Instance.owned.Contains(id);

    public bool TryBuy(string id, out string message)
    {
        message = null;
        var info = BookCatalog.Find(id);
        if (info == null) return false;
        if (owned.Contains(id))
        {
            message = "Vous possédez déjà ce livre.";
            return false;
        }
        var wallet = PlayerWallet.Instance;
        if (wallet == null) return false;
        if (!wallet.TrySpend(info.Price))
        {
            message = $"Il vous faut {info.Price:N0} € pour acheter ce livre.";
            return false;
        }

        owned.Add(id);
        OnChanged?.Invoke();
        return true;
    }

    public static List<string> ToSaveData()
    {
        return Instance != null ? new List<string>(Instance.owned) : new List<string>();
    }

    // The books of a save; an old save only knew the guide.
    public void Load(List<string> ids, bool guideOnly)
    {
        owned.Clear();
        if (ids != null) foreach (var id in ids) if (BookCatalog.Find(id) != null) owned.Add(id);
        if (guideOnly) owned.Add(BookCatalog.Guide);
        OnChanged?.Invoke();
    }
}
