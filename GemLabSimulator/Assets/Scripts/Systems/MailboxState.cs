using System;
using System.Collections.Generic;
using UnityEngine;

public class MailboxState : MonoBehaviour
{
    public static MailboxState Instance { get; private set; }

    private const int MaxPendingLetters = 4;
    private const int DailyLetterLimit = 5;
    private const float RoughLetterChance = 0.3f;

    private readonly List<GemInstance> pendingLetters = new List<GemInstance>();
    public IReadOnlyList<GemInstance> PendingLetters => pendingLetters;

    private int lettersReceivedToday;
    public int LettersRemainingToday => Mathf.Max(0, DailyLetterLimit - lettersReceivedToday);

    public event Action OnMailboxChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += OnDayChanged;
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= OnDayChanged;
    }

    private void OnDayChanged(int day)
    {
        lettersReceivedToday = 0;
        OnMailboxChanged?.Invoke();
    }

    // The first stone, sent by a client: an easy one, a cut amethyst, natural and untreated (the tutorial teaches how to look at it).
    private GemInstance tutorialLetter;

    public bool IsTutorialLetter(GemInstance gem) => gem != null && ReferenceEquals(gem, tutorialLetter);

    private GemInstance MakeTutorialStone(GemDatabase database)
    {
        var species = TutorialStones.Species(StoryState.Instance != null ? StoryState.Instance.TutorialRound : 1);
        GemDefinition amethyst = null;
        foreach (var definition in database.allGems)
            if (definition != null && definition.name == species) amethyst = definition;
        if (amethyst == null) return null;

        GemInstance best = null;
        for (var i = 0; i < 200; i++)
        {
            var candidate = new GemInstance(amethyst);
            best = candidate;
            if (candidate.IsDonation || candidate.IsSynthetic || candidate.TreatmentGrade != "Non traité") continue;
            if (candidate.ClarityGrade != "Eye-clean" && candidate.ClarityGrade != "Inclus") continue;
            if (candidate.CaratWeight < amethyst.weightCarats * 0.7f || candidate.CaratWeight > amethyst.weightCarats * 1.8f) continue;
            break;
        }
        best.MarkAsClientStone();
        return best;
    }

    // At the beginning of the game only one stone comes by post: the one of the tutorial. Returns true when it handled the stock.
    private bool StockTutorial(GemDatabase database)
    {
        var story = StoryState.Instance;
        if (story == null || !story.MailLimited) return false;

        var changed = false;
        if (story.TutorialStoneTaken)
        {
            if (pendingLetters.Count > 0)
            {
                pendingLetters.Clear();
                changed = true;
            }
        }
        else
        {
            if (tutorialLetter == null || !pendingLetters.Contains(tutorialLetter))
            {
                pendingLetters.Clear();
                tutorialLetter = MakeTutorialStone(database);
                if (tutorialLetter != null) pendingLetters.Add(tutorialLetter);
                changed = true;
            }
            else if (pendingLetters.Count > 1)
            {
                pendingLetters.RemoveAll(letter => !ReferenceEquals(letter, tutorialLetter));
                changed = true;
            }
        }

        if (changed) OnMailboxChanged?.Invoke();
        return true;
    }

    public void EnsureStocked()
    {
        var database = GemDatabase.Instance;
        if (database == null || database.allGems.Count == 0) return;
        if (StockTutorial(database)) return;

        var changed = false;
        while (pendingLetters.Count < MaxPendingLetters)
        {
            GemDefinition definition;
            do
            {
                definition = database.allGems[UnityEngine.Random.Range(0, database.allGems.Count)];
            } while (definition.isTunnelExclusive);

            var letter = new GemInstance(definition);
            if (!letter.IsDonation)
            {
                letter.MarkAsClientStone();
                // Some clients send a rough crystal: found in a river, a field, a family mine. A diamond is not sent rough this way.
                if (!(definition.name ?? "").StartsWith("Diamant") && UnityEngine.Random.value < RoughLetterChance) letter.MarkAsRough();
            }
            pendingLetters.Add(letter);
            changed = true;
        }

        if (changed) OnMailboxChanged?.Invoke();
    }

    public bool TryTakeLetter(GemInstance letter, PlayerInventory inventory)
    {
        if (lettersReceivedToday >= DailyLetterLimit) return false;
        if (!pendingLetters.Contains(letter)) return false;
        if (!inventory.TryAddGem(letter)) return false;

        pendingLetters.Remove(letter);
        lettersReceivedToday++;
        if (IsTutorialLetter(letter)) StoryState.Instance?.OnTutorialStoneTaken(letter);
        OnMailboxChanged?.Invoke();
        return true;
    }
}
