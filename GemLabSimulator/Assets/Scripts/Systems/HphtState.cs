using System;
using System.Collections.Generic;
using UnityEngine;

// The diamond in the press of the treatments laboratory: it comes out on ReadyDay, treated.
public class HphtJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public int Temperature;
    public int Pressure;             // in tenths of a GPa
    public int Days;
    public GemInstance Stone;
}

// The press holds one diamond at a time. The stone leaves the bag when the run starts, and is treated when the player takes it out.
// Lives on the player (added on demand, like the other persistent states).
public class HphtState : MonoBehaviour
{
    public static HphtState Instance { get; private set; }

    private HphtJob job;

    public HphtJob Job => job;

    // The technician gives a few brownish diamonds, once, to start with.
    public bool SamplesGiven { get; private set; }

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

    public static HphtState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<HphtState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(int temperature, int pressure, int days, GemInstance stone, HphtQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (job != null)
        {
            message = "La presse est déjà en cycle.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour ce cycle.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        job = new HphtJob { StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Temperature = temperature, Pressure = pressure, Days = days, Stone = stone };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the diamond out of the press: it is treated now, and goes back into the bag.
    public bool TryCollect(out HphtResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "Le cycle n'est pas terminé : la presse est encore sous pression.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = HphtCatalog.Resolve(job.Temperature, job.Pressure, job.Days, job.Stone);
        job.Stone.ApplyHpht(result);
        inventory.TryAddGem(job.Stone);
        GemCodex.Instance?.MarkDiscovered(job.Stone.Definition.name);
        job = null;
        OnJobChanged?.Invoke();
        return true;
    }

    // Four cut diamonds with a yellow-brown tint, not certified yet: two with very little nitrogen, two with nitrogen.
    public bool TryGiveSamples(out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var database = GemDatabase.Instance;
        const int count = 4;
        if (SamplesGiven || inventory == null || database == null) return false;

        var diamond = database.FindByAssetName(DiamondDealerState.DiamondAssetName);
        if (diamond == null) return false;
        if (inventory.FreeSlots < count)
        {
            message = $"J'ai des diamants pour vous, mais il vous faut {count} places libres dans votre sac.";
            return false;
        }

        var carats = new[] { 0.60f, 0.80f, 0.70f, 1.00f };
        var colors = new[] { 12, 10, 14, 9 };
        var clarities = new[] { 5, 4, 6, 5 };
        var nitrogen = new[] { false, false, true, true };
        for (var i = 0; i < count; i++)
        {
            GemInstance stone = null;
            for (var attempt = 0; attempt < 600; attempt++)
            {
                var diameter = 6.5f * Mathf.Pow(carats[i], 1f / 3f);
                var value = DiamondGrading.Value(carats[i], colors[i], clarities[i]);
                stone = GemInstance.CreateCutDiamond(diamond, carats[i], colors[i], clarities[i], "Rond", "Brillant", diameter, diameter, diameter * 0.61f, value,
                    "Taillerie de démonstration", 1);
                if (HphtCatalog.HasNitrogen(stone) == nitrogen[i]) break;
            }
            if (stone == null) continue;
            inventory.TryAddGem(stone);
            GemCodex.Instance?.MarkDiscovered(diamond.name);
        }
        SamplesGiven = true;
        return true;
    }

    public void LoadSamplesGiven(bool given)
    {
        SamplesGiven = given;
    }

    public List<HphtJobSaveData> ToSaveData()
    {
        var result = new List<HphtJobSaveData>();
        if (job != null)
            result.Add(new HphtJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, temperature = job.Temperature, pressure = job.Pressure, days = job.Days,
                stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<HphtJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
                job = new HphtJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Temperature = entry.temperature,
                    Pressure = Mathf.Max(HphtCatalog.MinPressure, entry.pressure), Days = Mathf.Max(1, entry.days), Stone = stone,
                };
        }
        OnJobChanged?.Invoke();
    }
}
