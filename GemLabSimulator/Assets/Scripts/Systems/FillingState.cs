using System;
using System.Collections.Generic;
using UnityEngine;

// The stone in the autoclave of the treatments laboratory: it comes out on ReadyDay, filled.
public class FillJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public int Product;
    public int Procedure;
    public GemInstance Stone;
}

// The autoclave holds one stone at a time. The stone leaves the bag when the cycle starts, and is filled when the player takes it out.
// Lives on the player (added on demand, like the other persistent states).
public class FillingState : MonoBehaviour
{
    public static FillingState Instance { get; private set; }

    private FillJob job;

    public FillJob Job => job;

    // The technician gives one sample of each of these materials, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Emeraude", "Corindon_Rubis", "Jade_Jadeite", "Turquoise", "Opale_Noble", "Lapis_Lazuli" };

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

    public static FillingState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<FillingState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(int product, int procedure, GemInstance stone, FillQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (job != null)
        {
            message = "L'autoclave est déjà en cycle.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour ce cycle.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        job = new FillJob { StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Product = product, Procedure = procedure, Stone = stone };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the stone out of the autoclave: it is filled now, and goes back into the bag.
    public bool TryCollect(out FillResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "Le cycle n'est pas terminé : l'autoclave est encore sous pression.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = FillingCatalog.Resolve(job.Product, job.Procedure, job.Stone);
        job.Stone.ApplyFilling(result);
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

    // The diamonds came later: two cut diamonds with feathers (SI1 and SI2), handed over once, even to a player who already has the first samples.
    public bool DiamondSamplesGiven { get; private set; }

    public bool TryGiveDiamondSamples(out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var database = GemDatabase.Instance;
        const int count = 2;
        if (DiamondSamplesGiven || inventory == null || database == null) return false;

        var diamond = database.FindByAssetName(DiamondDealerState.DiamondAssetName);
        if (diamond == null) return false;
        if (inventory.FreeSlots < count)
        {
            message = $"J'ai deux diamants à remplir pour vous, mais il vous faut {count} places libres dans votre sac.";
            return false;
        }

        var carats = new[] { 0.60f, 0.80f };
        var colors = new[] { 6, 8 };
        var clarities = new[] { 6, 7 };
        for (var i = 0; i < count; i++)
        {
            var diameter = 6.5f * Mathf.Pow(carats[i], 1f / 3f);
            var value = DiamondGrading.Value(carats[i], colors[i], clarities[i]);
            GemInstance stone = null;
            for (var attempt = 0; attempt < 300; attempt++)
            {
                stone = GemInstance.CreateCutDiamond(diamond, carats[i], colors[i], clarities[i], "Rond", "Brillant", diameter, diameter, diameter * 0.61f, value,
                    "Taillerie de démonstration", 1);
                if (FillingCatalog.DiamondFeathers(stone) > 0) break;
            }
            if (stone == null) continue;
            inventory.TryAddGem(stone);
            GemCodex.Instance?.MarkDiscovered(diamond.name);
        }
        DiamondSamplesGiven = true;
        return true;
    }

    public void LoadDiamondSamplesGiven(bool given)
    {
        DiamondSamplesGiven = given;
    }

    public List<FillJobSaveData> ToSaveData()
    {
        var result = new List<FillJobSaveData>();
        if (job != null)
            result.Add(new FillJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, product = job.Product, procedure = job.Procedure, stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<FillJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
                job = new FillJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Product = entry.product,
                    Procedure = entry.procedure, Stone = stone,
                };
        }
        OnJobChanged?.Invoke();
    }
}
