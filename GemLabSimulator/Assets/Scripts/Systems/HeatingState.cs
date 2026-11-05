using System;
using System.Collections.Generic;
using UnityEngine;

// The stone in the furnace of the treatments laboratory: it comes out on ReadyDay, heated.
public class HeatJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public int Temperature;
    public HeatAtmosphere Atmosphere;
    public HeatCooling Cooling;
    public int Days;
    public GemInstance Stone;
}

// The furnace holds one stone at a time. The stone leaves the bag when the run starts, and is heated when the player takes it out.
// Lives on the player (added on demand, like the other persistent states).
public class HeatingState : MonoBehaviour
{
    public static HeatingState Instance { get; private set; }

    private HeatJob job;

    public HeatJob Job => job;

    // The technician gives one sample of each of these materials, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Saphir_Incolore", "Corindon_Rubis", "Heliodore", "Amethyste", "Topaze", "Zircon", "Agate", "Ambre" };

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

    public static HeatingState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<HeatingState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(int temperature, HeatAtmosphere atmosphere, HeatCooling cooling, int days, GemInstance stone, HeatQuote quote, out string message)
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
        job = new HeatJob
        {
            StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Temperature = temperature, Atmosphere = atmosphere, Cooling = cooling, Days = days, Stone = stone,
        };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the stone out of the furnace: it is heated now, and goes back into the bag.
    public bool TryCollect(out HeatResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "Le cycle n'est pas terminé : le four est encore brûlant.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = HeatingCatalog.Resolve(job.Temperature, job.Atmosphere, job.Cooling, job.Days, job.Stone);
        job.Stone.ApplyHeating(result);
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

    public List<HeatJobSaveData> ToSaveData()
    {
        var result = new List<HeatJobSaveData>();
        if (job != null)
            result.Add(new HeatJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, temperature = job.Temperature, atmosphere = (int)job.Atmosphere,
                cooling = (int)job.Cooling, days = job.Days, stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<HeatJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
                job = new HeatJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Temperature = entry.temperature,
                    Atmosphere = (HeatAtmosphere)entry.atmosphere, Cooling = (HeatCooling)entry.cooling, Days = Mathf.Max(1, entry.days), Stone = stone,
                };
        }
        OnJobChanged?.Invoke();
    }
}
