using System;
using System.Collections.Generic;
using UnityEngine;

// The stone in the diffusion furnace of the treatments laboratory: it comes out on ReadyDay, diffused.
public class DiffusionJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public int Dopant;
    public int Temperature;
    public int Days;
    public GemInstance Stone;
}

// The diffusion furnace holds one stone at a time, for several days. The stone leaves the bag when the run starts, and is diffused when the
// player takes it out. Lives on the player (added on demand, like the other persistent states).
public class DiffusionState : MonoBehaviour
{
    public static DiffusionState Instance { get; private set; }

    private DiffusionJob job;

    public DiffusionJob Job => job;

    // The technician gives one sample of each of these materials, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Saphir_Incolore", "Saphir_Rose", "Saphir_Bleu", "Saphir_Jaune", "Corindon_Rubis" };

    public event Action OnJobChanged;

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

    public static DiffusionState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<DiffusionState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(int dopant, int temperature, int days, GemInstance stone, DiffusionQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (job != null)
        {
            message = "Le four est déjà en cycle.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour ce cycle.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        job = new DiffusionJob { StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Dopant = dopant, Temperature = temperature, Days = days, Stone = stone };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the stone out of the furnace: it is diffused now, and goes back into the bag.
    public bool TryCollect(out DiffusionResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "Le cycle n'est pas terminé : la pierre est encore dans la poudre, en plein four.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = DiffusionCatalog.Resolve(job.Dopant, job.Temperature, job.Days, job.Stone);
        result.Element = DiffusionCatalog.Dopants[job.Dopant].Name;
        job.Stone.ApplyDiffusion(result);
        inventory.TryAddGem(job.Stone);
        GemCodex.Instance?.MarkDiscovered(job.Stone.Definition.name);
        job = null;
        OnJobChanged?.Invoke();
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
            stone.ClearTreatment();
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

    public List<DiffusionJobSaveData> ToSaveData()
    {
        var result = new List<DiffusionJobSaveData>();
        if (job != null)
            result.Add(new DiffusionJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, dopant = job.Dopant, temperature = job.Temperature, days = job.Days,
                stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<DiffusionJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
                job = new DiffusionJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Dopant = entry.dopant,
                    Temperature = entry.temperature, Days = Mathf.Max(1, entry.days), Stone = stone,
                };
        }
        OnJobChanged?.Invoke();
    }
}
