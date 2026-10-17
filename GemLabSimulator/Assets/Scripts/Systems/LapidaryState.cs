using System;
using System.Collections.Generic;
using UnityEngine;

public class LapidaryJob
{
    // The stone is already cut when it goes into the workshop; the owner just can't have it back until ReadyDay.
    public GemInstance Gem;
    public int ReadyDay;
}

public class LapidaryState : MonoBehaviour
{
    public static LapidaryState Instance { get; private set; }

    private readonly List<LapidaryJob> jobs = new List<LapidaryJob>();

    public IReadOnlyList<LapidaryJob> Jobs => jobs;
    public bool IsFull => jobs.Count >= LapidaryCatalog.MaxJobs;

    public event Action OnJobsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(int day)
    {
        OnJobsChanged?.Invoke();
    }

    public bool IsReady(LapidaryJob job)
    {
        var today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        return today >= job.ReadyDay;
    }

    public int DaysRemaining(LapidaryJob job)
    {
        var today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        return Mathf.Max(0, job.ReadyDay - today);
    }

    public bool TrySubmit(GemInstance gem, string shape, string style, LapidaryQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (gem == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (IsFull)
        {
            message = "Je ne peux travailler que sur une pierre à la fois : venez d'abord chercher la précédente.";
            return false;
        }

        if (wallet.Money < quote.Price)
        {
            message = "Vous n'avez pas assez d'argent pour cette taille.";
            return false;
        }

        wallet.TrySpend(quote.Price);
        LapidaryCatalog.RollResult(gem, shape, style, quote, out var newWeight, out var newValue);
        inventory.RemoveGem(gem);
        gem.ApplyCut(shape, style, newWeight, newValue);

        var today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        jobs.Add(new LapidaryJob { Gem = gem, ReadyDay = today + quote.Days });
        OnJobsChanged?.Invoke();
        return true;
    }

    public bool TryCollect(LapidaryJob job, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null || !jobs.Contains(job)) return false;

        if (!IsReady(job))
        {
            message = "La taille n'est pas encore terminée.";
            return false;
        }

        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de récupérer la pierre.";
            return false;
        }

        inventory.TryAddGem(job.Gem);
        jobs.Remove(job);
        OnJobsChanged?.Invoke();
        return true;
    }

    public List<LapidaryJobSaveData> ToSaveData()
    {
        var result = new List<LapidaryJobSaveData>();
        foreach (var job in jobs)
            result.Add(new LapidaryJobSaveData { gem = job.Gem.ToSaveData(), readyDay = job.ReadyDay });
        return result;
    }

    public void LoadJobs(List<LapidaryJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        jobs.Clear();
        if (saved != null)
        {
            foreach (var entry in saved)
            {
                var gem = restore(entry.gem);
                if (gem != null) jobs.Add(new LapidaryJob { Gem = gem, ReadyDay = entry.readyDay });
            }
        }
        OnJobsChanged?.Invoke();
    }
}
