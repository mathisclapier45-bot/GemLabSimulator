using System;
using System.Collections.Generic;
using UnityEngine;

// A rough diamond handed to one of the cutters: the finished stones are already decided, the player gets them back on ReadyDay.
public class DiamondCutJob
{
    public int CutterIndex;
    public int ReadyDay;
    public string Summary;                       // "Octaèdre de 2.30 ct — Rond brillant"
    public List<GemInstance> Stones = new List<GemInstance>();
}

// Orders in progress at the cutting centre of the Diamond Quarter: one at a time for each of the four cutters. Lives on the player (added
// on demand, like the other persistent states).
public class DiamondCuttingState : MonoBehaviour
{
    public static DiamondCuttingState Instance { get; private set; }

    private readonly List<DiamondCutJob> jobs = new List<DiamondCutJob>();

    public IReadOnlyList<DiamondCutJob> Jobs => jobs;

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

    public static DiamondCuttingState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<DiamondCuttingState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobsChanged?.Invoke();
    }

    public DiamondCutJob JobOf(int cutterIndex)
    {
        foreach (var job in jobs)
            if (job.CutterIndex == cutterIndex) return job;
        return null;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady(DiamondCutJob job) => Today >= job.ReadyDay;

    public int DaysRemaining(DiamondCutJob job) => Mathf.Max(0, job.ReadyDay - Today);

    public bool TrySubmit(int cutterIndex, DiamondCutOption option, GemInstance rough, DiamondCutQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (cutterIndex < 0 || cutterIndex >= DiamondCutting.Cutters.Length || rough == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (JobOf(cutterIndex) != null)
        {
            message = "Je n'ai qu'un travail à la fois : venez d'abord chercher la pierre précédente.";
            return false;
        }

        if (wallet.Money < quote.Price)
        {
            message = "Vous n'avez pas assez d'argent pour cette taille.";
            return false;
        }

        var database = GemDatabase.Instance;
        GemDefinition diamond = rough.Definition;
        if (diamond == null && database != null) diamond = database.FindByAssetName(DiamondDealerState.DiamondAssetName);
        if (diamond == null) return false;

        var cutter = DiamondCutting.Cutters[cutterIndex];
        wallet.TrySpend(quote.Price);
        var stones = DiamondCutting.Cut(cutter, option, rough, diamond);
        inventory.RemoveGem(rough);

        jobs.Add(new DiamondCutJob
        {
            CutterIndex = cutterIndex,
            ReadyDay = Today + quote.Days,
            Summary = $"{(string.IsNullOrEmpty(rough.RoughForm) ? "Cristal" : rough.RoughForm)} de {rough.CaratWeight:F2} ct — {option.Label}",
            Stones = stones,
        });
        OnJobsChanged?.Invoke();
        return true;
    }

    public bool TryCollect(DiamondCutJob job, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null || !jobs.Contains(job)) return false;

        if (!IsReady(job))
        {
            message = "La taille n'est pas encore terminée.";
            return false;
        }

        if (inventory.FreeSlots < job.Stones.Count)
        {
            message = job.Stones.Count == 1
                ? "Inventaire plein : faites de la place avant de récupérer la pierre."
                : $"Il vous faut {job.Stones.Count} places libres dans votre sac pour récupérer les pierres.";
            return false;
        }

        foreach (var stone in job.Stones)
        {
            inventory.TryAddGem(stone);
            GemCodex.Instance?.MarkDiscovered(stone.Definition.name);
        }
        jobs.Remove(job);
        OnJobsChanged?.Invoke();
        return true;
    }

    public List<DiamondCutJobSaveData> ToSaveData()
    {
        var result = new List<DiamondCutJobSaveData>();
        foreach (var job in jobs)
        {
            var saved = new DiamondCutJobSaveData { cutterIndex = job.CutterIndex, readyDay = job.ReadyDay, summary = job.Summary };
            foreach (var stone in job.Stones) saved.stones.Add(stone.ToSaveData());
            result.Add(saved);
        }
        return result;
    }

    public void LoadJobs(List<DiamondCutJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        jobs.Clear();
        if (saved != null)
        {
            foreach (var entry in saved)
            {
                var job = new DiamondCutJob { CutterIndex = entry.cutterIndex, ReadyDay = entry.readyDay, Summary = entry.summary };
                foreach (var data in entry.stones)
                {
                    var stone = restore(data);
                    if (stone != null) job.Stones.Add(stone);
                }
                if (job.Stones.Count > 0) jobs.Add(job);
            }
        }
        OnJobsChanged?.Invoke();
    }
}
