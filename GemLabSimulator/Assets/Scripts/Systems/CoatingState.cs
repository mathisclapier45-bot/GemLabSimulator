using System;
using System.Collections.Generic;
using UnityEngine;

// The stone in the vacuum chamber of the treatments laboratory: it comes out on ReadyDay, coated.
public class CoatJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public List<int> Targets = new List<int>();
    public int Thickness;
    public GemInstance Stone;
}

// The vacuum chamber holds one stone at a time. The stone leaves the bag when the run starts, and is coated when the player takes it out.
// Lives on the player (added on demand, like the other persistent states).
public class CoatingState : MonoBehaviour
{
    public static CoatingState Instance { get; private set; }

    private CoatJob job;

    public CoatJob Job => job;

    // The technician gives one sample of each of these materials, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Topaze", "Quartz", "Goshenite", "Tanzanite", "Opale_Noble", "Ambre" };

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

    public static CoatingState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<CoatingState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(IReadOnlyList<int> targets, int thickness, GemInstance stone, CoatQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (job != null)
        {
            message = "La chambre est déjà en cycle.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour ce cycle.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        job = new CoatJob { StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Targets = new List<int>(targets), Thickness = thickness, Stone = stone };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the stone out of the chamber: it is coated now, and goes back into the bag.
    public bool TryCollect(out CoatResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "Le cycle n'est pas terminé : la chambre doit refroidir.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = CoatingCatalog.Resolve(job.Targets, job.Thickness, job.Stone);
        job.Stone.ApplyCoating(result);
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

    // The diamonds came later: two cut diamonds with a yellow tint (K and M), handed over once, even to a player who already has the first samples.
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
            message = $"J'ai deux diamants à revêtir pour vous, mais il vous faut {count} places libres dans votre sac.";
            return false;
        }

        var carats = new[] { 0.60f, 0.50f };
        var colors = new[] { 9, 12 };
        var clarities = new[] { 5, 5 };
        for (var i = 0; i < count; i++)
        {
            var diameter = 6.5f * Mathf.Pow(carats[i], 1f / 3f);
            var value = DiamondGrading.Value(carats[i], colors[i], clarities[i]);
            var stone = GemInstance.CreateCutDiamond(diamond, carats[i], colors[i], clarities[i], "Rond", "Brillant", diameter, diameter, diameter * 0.61f, value,
                "Taillerie de démonstration", 1);
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

    public List<CoatJobSaveData> ToSaveData()
    {
        var result = new List<CoatJobSaveData>();
        if (job != null)
            result.Add(new CoatJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, recipe = CoatingCatalog.KeyOf(job.Targets), thickness = job.Thickness,
                stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<CoatJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
            {
                var targets = new List<int>();
                foreach (var part in (entry.recipe ?? "").Split(','))
                    if (int.TryParse(part, out var index)) targets.Add(index);
                job = new CoatJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee, Targets = targets,
                    Thickness = entry.thickness, Stone = stone,
                };
            }
        }
        OnJobChanged?.Invoke();
    }
}
