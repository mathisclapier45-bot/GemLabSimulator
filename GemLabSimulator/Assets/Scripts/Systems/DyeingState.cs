using System;
using System.Collections.Generic;
using UnityEngine;

// A stone in one of the pots of the dye room: it comes out on ReadyDay.
public class DyeJob
{
    public int PotIndex;
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public GemInstance Stone;
}

// The pots of the dye room: one stone at a time in each. The stone leaves the bag when it is dipped, and is dyed when the player takes it out.
// Lives on the player (added on demand, like the other persistent states).
public class DyeingState : MonoBehaviour
{
    public static DyeingState Instance { get; private set; }

    private readonly List<DyeJob> jobs = new List<DyeJob>();

    public IReadOnlyList<DyeJob> Jobs => jobs;

    // The technician gives one sample of each of these materials, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Agate", "Ambre", "Lapis_Lazuli", "Opale_Noble", "Turquoise", "Quartz" };

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

    public static DyeingState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<DyeingState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobsChanged?.Invoke();
    }

    public DyeJob JobOf(int potIndex)
    {
        foreach (var job in jobs)
            if (job.PotIndex == potIndex) return job;
        return null;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady(DyeJob job) => Today >= job.ReadyDay;

    public int DaysRemaining(DyeJob job) => Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(int potIndex, GemInstance stone, DyeQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (potIndex < 0 || potIndex >= DyeingCatalog.PotCount || stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (JobOf(potIndex) != null)
        {
            message = "Ce pot est déjà occupé.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour les colorants de ce bain.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        jobs.Add(new DyeJob { PotIndex = potIndex, StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Stone = stone });
        OnJobsChanged?.Invoke();
        return true;
    }

    // Takes the stone out of its pot: it is dyed now, and goes back into the bag.
    public bool TryCollect(DyeJob job, out DyeResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null || !jobs.Contains(job)) return false;

        if (!IsReady(job))
        {
            message = "La teinture n'est pas terminée : la pierre y reste encore.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre du bain.";
            return false;
        }

        result = DyeingCatalog.Resolve(job.PotIndex, job.Stone);
        result.Bath = DyeingCatalog.Baths[job.PotIndex].Name;
        job.Stone.ApplyDyeing(result);
        inventory.TryAddGem(job.Stone);
        GemCodex.Instance?.MarkDiscovered(job.Stone.Definition.name);
        jobs.Remove(job);
        OnJobsChanged?.Invoke();
        return true;
    }

    public bool TryGiveSamples(out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var database = GemDatabase.Instance;
        if (SamplesGiven || inventory == null || database == null) return false;

        if (inventory.FreeSlots < SampleAssets.Length)
        {
            message = $"J'ai des échantillons pour vous, mais il vous faut {SampleAssets.Length} places libres dans votre sac.";
            return false;
        }

        foreach (var asset in SampleAssets)
        {
            var definition = database.FindByAssetName(asset);
            if (definition == null) continue;
            var stone = new GemInstance(definition);
            stone.MarkIdentified();
            inventory.TryAddGem(stone);
            GemCodex.Instance?.MarkDiscovered(definition.name);
        }
        SamplesGiven = true;
        return true;
    }

    public void LoadSamplesGiven(bool given)
    {
        SamplesGiven = given;
    }

    public List<DyeJobSaveData> ToSaveData()
    {
        var result = new List<DyeJobSaveData>();
        foreach (var job in jobs)
            result.Add(new DyeJobSaveData { potIndex = job.PotIndex, startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, stone = job.Stone.ToSaveData() });
        return result;
    }

    public void LoadJobs(List<DyeJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        jobs.Clear();
        if (saved != null)
        {
            foreach (var entry in saved)
            {
                var stone = entry.stone != null ? restore(entry.stone) : null;
                if (stone != null)
                    jobs.Add(new DyeJob { PotIndex = entry.potIndex, StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Stone = stone });
            }
        }
        OnJobsChanged?.Invoke();
    }
}
