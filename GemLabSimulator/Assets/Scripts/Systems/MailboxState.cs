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

    public void EnsureStocked()
    {
        var database = GemDatabase.Instance;
        if (database == null || database.allGems.Count == 0) return;

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
        OnMailboxChanged?.Invoke();
        return true;
    }
}
