using System;
using System.Collections.Generic;
using UnityEngine;

// A stone in one of the vats of the treatments laboratory: it comes out on ReadyDay.
public class BleachJob
{
    public int VatIndex;
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public GemInstance Stone;
}

// The vats of the bleaching room: one stone at a time in each. The stone leaves the bag when it is dipped, and is bleached when the player
// takes it out. Lives on the player (added on demand, like the other persistent states).
public class BleachingState : MonoBehaviour
{
    public static BleachingState Instance { get; private set; }

    private readonly List<BleachJob> jobs = new List<BleachJob>();

    public IReadOnlyList<BleachJob> Jobs => jobs;

    // The technician gives one sample of each bleachable material, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Perle", "Perle_Tahiti", "Corail", "Oeil_de_Tigre" };

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

    public static BleachingState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<BleachingState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobsChanged?.Invoke();
    }

    public BleachJob JobOf(int vatIndex)
    {
        foreach (var job in jobs)
            if (job.VatIndex == vatIndex) return job;
        return null;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady(BleachJob job) => Today >= job.ReadyDay;

    public int DaysRemaining(BleachJob job) => Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(int vatIndex, GemInstance stone, BleachQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (vatIndex < 0 || vatIndex >= BleachingCatalog.VatCount || stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (JobOf(vatIndex) != null)
        {
            message = "Cette cuve est déjà occupée.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour les produits de ce bain.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        jobs.Add(new BleachJob { VatIndex = vatIndex, StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Stone = stone });
        OnJobsChanged?.Invoke();
        return true;
    }

    // Takes the stone out of its vat: it is bleached now, and goes back into the bag.
    public bool TryCollect(BleachJob job, out BleachResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null || !jobs.Contains(job)) return false;

        if (!IsReady(job))
        {
            message = "Le bain n'est pas terminé : la pierre y reste encore.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre du bain.";
            return false;
        }

        result = BleachingCatalog.Resolve(job.VatIndex, job.Stone);
        job.Stone.ApplyBleaching(result);
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

    public List<BleachJobSaveData> ToSaveData()
    {
        var result = new List<BleachJobSaveData>();
        foreach (var job in jobs)
            result.Add(new BleachJobSaveData { vatIndex = job.VatIndex, startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, stone = job.Stone.ToSaveData() });
        return result;
    }

    public void LoadJobs(List<BleachJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        jobs.Clear();
        if (saved != null)
        {
            foreach (var entry in saved)
            {
                var stone = entry.stone != null ? restore(entry.stone) : null;
                if (stone != null)
                    jobs.Add(new BleachJob { VatIndex = entry.vatIndex, StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Stone = stone });
            }
        }
        OnJobsChanged?.Invoke();
    }
}
