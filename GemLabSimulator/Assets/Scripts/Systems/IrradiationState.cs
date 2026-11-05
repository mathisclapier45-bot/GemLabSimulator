using System;
using System.Collections.Generic;
using UnityEngine;

// The stone in the irradiator of the treatments laboratory: it comes out on ReadyDay, irradiated, once its radioactivity has gone.
public class IrradiationJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public IrradiationSource Source;
    public int Dose;
    public GemInstance Stone;
}

// The irradiator holds one stone at a time. The stone leaves the bag when the run starts, and is irradiated when the player takes it out. Also
// makes the unstable colours of the stones fade when their day comes. Lives on the player (added on demand, like the other persistent states).
public class IrradiationState : MonoBehaviour
{
    public static IrradiationState Instance { get; private set; }

    private IrradiationJob job;

    public IrradiationJob Job => job;

    // The technician gives one sample of each of these materials, once, to start with.
    public bool SamplesGiven { get; private set; }
    private static readonly string[] SampleAssets = { "Quartz", "Citrine", "Goshenite", "Morganite", "Topaze", "Tourmaline_Rose", "Perle", "Saphir_Incolore" };

    public event Action OnJobChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += HandleDayChanged;
        FadeAll(Today);
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= HandleDayChanged;
    }

    public static IrradiationState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<IrradiationState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        FadeAll(day);
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    // The unstable colours of every stone the player owns fade on their day.
    private static void FadeAll(int day)
    {
        var faded = 0;
        if (PlayerInventory.Instance != null)
            foreach (var gem in PlayerInventory.Instance.Gems) if (gem.FadeIrradiationIfDue(day)) faded++;
        if (PlayerChest.Instance != null)
            foreach (var gem in PlayerChest.Instance.Gems) if (gem.FadeIrradiationIfDue(day)) faded++;
        if (PlayerCabinet.Instance != null)
            foreach (var gem in PlayerCabinet.Instance.Gems) if (gem.FadeIrradiationIfDue(day)) faded++;
        if (PlayerOrderCabinet.Instance != null)
            foreach (var gem in PlayerOrderCabinet.Instance.Gems) if (gem.FadeIrradiationIfDue(day)) faded++;
        if (faded == 0) return;

        PlayerInventory.Instance?.NotifyChanged();
        ToolFeedbackUI.Instance?.ShowMessage(faded == 1 ? "La couleur d'une pierre irradiée a fané." : $"La couleur de {faded} pierres irradiées a fané.");
    }

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(IrradiationSource source, int dose, GemInstance stone, IrradiationQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (job != null)
        {
            message = "L'irradiateur est déjà occupé.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour cette irradiation.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        job = new IrradiationJob { StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Source = source, Dose = dose, Stone = stone };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the stone out of the irradiator: it is irradiated now, and goes back into the bag.
    public bool TryCollect(out IrradiationResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "La pierre est encore radioactive : elle doit finir de refroidir avant de sortir.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = IrradiationCatalog.Resolve(job.Source, job.Dose, job.Stone);
        job.Stone.ApplyIrradiation(result, Today);
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

    // The diamonds came later: two cut diamonds, pale and faintly yellow, handed over once, even to a player who already has the first samples.
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
            message = $"J'ai deux diamants à irradier pour vous, mais il vous faut {count} places libres dans votre sac.";
            return false;
        }

        var carats = new[] { 0.50f, 0.70f };
        var colors = new[] { 7, 11 };
        var clarities = new[] { 5, 6 };
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

    public List<IrradiationJobSaveData> ToSaveData()
    {
        var result = new List<IrradiationJobSaveData>();
        if (job != null)
            result.Add(new IrradiationJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, source = (int)job.Source, dose = job.Dose, stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<IrradiationJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
                job = new IrradiationJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee,
                    Source = (IrradiationSource)Mathf.Clamp(entry.source, 0, 2), Dose = Mathf.Max(IrradiationCatalog.MinDose, entry.dose), Stone = stone,
                };
        }
        OnJobChanged?.Invoke();
    }
}
