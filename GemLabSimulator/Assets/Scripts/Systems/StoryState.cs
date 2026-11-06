using System;
using System.Collections.Generic;
using UnityEngine;

// Where the player is in the story: the prologue (the arrival at Crystal Grove, the walk with Professor Thouvenot, the letter of Vincent in the mailbox of
// the house), then the free game. It is saved with the game. An old save, made before the story existed, is "settled": nothing is locked and nothing is told.
// Lives on the player (added on demand, like the other persistent states).
public class StoryState : MonoBehaviour
{
    public static StoryState Instance { get; private set; }

    // The stages of the prologue.
    public const int Arrival = 0;        // the player has just landed: Professor Thouvenot waits at the airport
    public const int Tour = 1;           // the walk through the town is going on (TourIndex says where)
    public const int GoHome = 2;         // the walk is over: the letter of Vincent is waiting in the mailbox of the house
    public const int Settled = 3;        // the letter has been read: the game is free

    public int Stage { get; private set; } = Settled;
    public int TourIndex { get; private set; }
    public bool Legacy { get; private set; } = true;       // a save from before the story: everything is open

    // The letters of the story the player has received (ids of StoryLetters), and the ones that were read.
    private readonly List<string> received = new List<string>();
    private readonly HashSet<string> read = new HashSet<string>();

    public IReadOnlyList<string> Received => received;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static StoryState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<StoryState>();
        return Instance;
    }

    // A game that starts with the story: the cinematic, then the arrival.
    public void BeginStory()
    {
        Legacy = false;
        Stage = Arrival;
        TourIndex = 0;
        received.Clear();
        read.Clear();
        OnChanged?.Invoke();
    }

    public void SetStage(int stage, int tourIndex = 0)
    {
        Stage = stage;
        TourIndex = tourIndex;
        OnChanged?.Invoke();
    }

    public bool PrologueOver => Stage >= Settled;

    // ---- Letters ---------------------------------------------------------------------------------------------------------

    public void Receive(string id)
    {
        if (received.Contains(id)) return;
        received.Add(id);
        OnChanged?.Invoke();
    }

    public bool HasReceived(string id) => received.Contains(id);

    public bool IsRead(string id) => read.Contains(id);

    public int UnreadCount
    {
        get
        {
            var count = 0;
            foreach (var id in received) if (!read.Contains(id)) count++;
            return count;
        }
    }

    public void MarkRead(string id)
    {
        if (!received.Contains(id) || !read.Add(id)) return;
        OnChanged?.Invoke();
    }

    // ---- Save ------------------------------------------------------------------------------------------------------------

    public void Save(SaveData data)
    {
        data.storyStarted = !Legacy;
        data.storyStage = Stage;
        data.storyTourIndex = TourIndex;
        data.storyLettersReceived = new List<string>(received);
        data.storyLettersRead = new List<string>(read);
    }

    public void Load(SaveData data)
    {
        received.Clear();
        read.Clear();
        if (!data.storyStarted)
        {
            // A save from before the story: the game is free and open.
            Legacy = true;
            Stage = Settled;
            TourIndex = 0;
        }
        else
        {
            Legacy = false;
            Stage = Mathf.Clamp(data.storyStage, Arrival, Settled);
            TourIndex = Mathf.Max(0, data.storyTourIndex);
            if (data.storyLettersReceived != null) received.AddRange(data.storyLettersReceived);
            if (data.storyLettersRead != null) foreach (var id in data.storyLettersRead) read.Add(id);
        }
        OnChanged?.Invoke();
    }
}

// What is open and what is not yet. The places of the second half of the game open with the reputation of the player (a good report to the director gives
// a point, and the level doubles each time): this is a first rule, until the chapters of the story decide it. A save from before the story has everything open.
public static class StoryGate
{
    private struct Rule
    {
        public string Scene;            // the name of the scene, or its beginning
        public int Level;               // the reputation level needed
        public string Message;          // what the closed door says, or null for the usual sentence
    }

    private static readonly Rule[] Rules =
    {
        new Rule { Scene = "Musee", Level = 2 },
        new Rule { Scene = "MarcheBrutes", Level = 2 },
        new Rule { Scene = "AtelierLapidaire", Level = 2 },
        new Rule { Scene = "Manoir", Level = 3 },
        // The big laboratory, with the complex instruments (spectrometers, FTIR, Raman, EDXRF, MEB, LA-ICP-MS...): the office of gemology has the classic ones.
        new Rule { Scene = "Laboratoire", Level = 3, Message = "Le grand laboratoire de gemmologie est fermé pour le moment : ses instruments sont trop complexes pour débuter. Il ouvrira avec la réputation de niveau 3." },
        new Rule { Scene = "LaboTraitements", Level = 3 },
        new Rule { Scene = "Diamantaires", Level = 4 },       // the Rapaport is on the 6th floor
        new Rule { Scene = "Bijouterie", Level = 5 },
        new Rule { Scene = "Banque", Level = 5 },              // the bank, and its auctions
        new Rule { Scene = "Tunnel", Level = 6 },
    };

    private static bool Everything => StoryState.Instance == null || StoryState.Instance.Legacy;

    private static int Level => ReputationState.Instance != null ? ReputationState.Instance.Level : 1;

    public static int RequiredLevel(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return 0;
        foreach (var rule in Rules)
            if (scene.StartsWith(rule.Scene, StringComparison.Ordinal)) return rule.Level;
        return 0;
    }

    public static bool IsOpen(string scene, out string message)
    {
        message = null;
        if (Everything) return true;
        foreach (var rule in Rules)
        {
            if (!scene.StartsWith(rule.Scene, StringComparison.Ordinal)) continue;
            if (Level >= rule.Level) return true;
            message = rule.Message ?? $"Ce lieu ouvrira plus tard : il faut la réputation de niveau {rule.Level} (un rapport réussi au directeur du laboratoire fait gagner un point).";
            return false;
        }
        return true;
    }

    public static bool IsOpen(string scene) => IsOpen(scene, out _);

    // The auctions of the bank, and everything that talks about them (the bulletin, the calendar, the catalogue in the mailbox).
    public static bool AuctionsOpen => IsOpen("Banque");

    // The Rapaport price list, sold in the Diamond Quarter.
    public static bool RapaportOpen => IsOpen("Diamantaires");
}
