using System;
using System.Collections.Generic;
using UnityEngine;

// The animals of the player: the ones bought at the pet shop (they wait in the bag) and the one that is equipped, which follows
// the player through every scene. Lives on the player, like the owned tools.
public class PlayerPets : MonoBehaviour
{
    public static PlayerPets Instance { get; private set; }

    // How many animals the bag keeps besides the equipped one.
    public const int CollectionSlots = 4;

    private readonly List<string> owned = new List<string>();
    private string equipped;
    private PetFollower follower;

    public event Action OnPetsChanged;

    public IReadOnlyList<string> Owned => owned;
    public string Equipped => equipped;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    // Scenes generated before this component existed do not carry it: add it to the player on demand.
    public static PlayerPets Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<PlayerPets>();
        return Instance;
    }

    public bool Has(string petId) => owned.Contains(petId);

    // Animals waiting in the bag: every owned animal except the equipped one.
    public List<string> Collection
    {
        get
        {
            var list = new List<string>();
            foreach (var id in owned)
                if (id != equipped) list.Add(id);
            return list;
        }
    }

    public bool HasRoom => Collection.Count < CollectionSlots;

    // Buying: the animal goes into the bag, not into the world.
    public bool TryAdd(string petId)
    {
        if (!PetCatalog.IsPet(petId) || owned.Contains(petId) || !HasRoom) return false;
        owned.Add(petId);
        OnPetsChanged?.Invoke();
        return true;
    }

    // Equipping: the animal leaves the bag and follows the player; the one that was equipped goes back into the bag.
    public bool Equip(string petId)
    {
        if (!owned.Contains(petId) || equipped == petId) return false;
        equipped = petId;
        RefreshFollower();
        OnPetsChanged?.Invoke();
        return true;
    }

    public void Unequip()
    {
        if (string.IsNullOrEmpty(equipped)) return;
        equipped = null;
        RefreshFollower();
        OnPetsChanged?.Invoke();
    }

    public void LoadPets(IEnumerable<string> petIds, string equippedId)
    {
        owned.Clear();
        if (petIds != null)
            foreach (var id in petIds)
                if (PetCatalog.IsPet(id) && !owned.Contains(id)) owned.Add(id);

        equipped = !string.IsNullOrEmpty(equippedId) && owned.Contains(equippedId) ? equippedId : null;
        RefreshFollower();
        OnPetsChanged?.Invoke();
    }

    private void Start()
    {
        RefreshFollower();
    }

    private void OnDestroy()
    {
        if (Instance == this && follower != null) Destroy(follower.gameObject);
    }

    // One companion at a time: the follower is created when an animal is equipped and removed when it is put away.
    private void RefreshFollower()
    {
        if (string.IsNullOrEmpty(equipped))
        {
            if (follower != null) Destroy(follower.gameObject);
            follower = null;
            return;
        }

        if (follower == null)
        {
            var go = new GameObject("Compagnon");
            DontDestroyOnLoad(go);
            follower = go.AddComponent<PetFollower>();
        }
        follower.Setup(equipped);
    }
}
