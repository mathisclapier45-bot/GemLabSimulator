using System;
using System.Collections.Generic;
using UnityEngine;

// A stone in the workshop of the jeweller: it comes out set in its jewel on ReadyDay.
public class JewelJob
{
    public GemInstance Stone;
    public int Kind;
    public int Metal;
    public int Setting;
    public int StartDay;
    public int ReadyDay;
    public int Fee;
}

// The workshop of the jeweller of the town: the stones left to be set, the jewels bought back, the stones taken out of their setting. Lives on the player
// (added on demand, like the other persistent states).
public class JewelerState : MonoBehaviour
{
    public static JewelerState Instance { get; private set; }

    private const int BuyerSeed = 4242;

    private readonly List<JewelJob> jobs = new List<JewelJob>();
    public IReadOnlyList<JewelJob> Jobs => jobs;

    public event Action OnChanged;

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

    public static JewelerState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<JewelerState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady(JewelJob job) => Today >= job.ReadyDay;

    public int DaysRemaining(JewelJob job) => Mathf.Max(0, job.ReadyDay - Today);

    // ---- Setting ---------------------------------------------------------------------------------------------------

    public bool TryStart(GemInstance stone, int kind, int metal, int setting, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null) return false;

        var quote = JewelCatalog.GetQuote(kind, metal, setting, stone);
        if (!quote.IsValid)
        {
            message = quote.Reason;
            return false;
        }
        if (jobs.Count >= 4)
        {
            message = "L'atelier est plein : récupérez d'abord un bijou.";
            return false;
        }
        if (!wallet.TrySpend(quote.Fee))
        {
            message = "Vous n'avez pas assez d'argent pour cette commande.";
            return false;
        }

        inventory.RemoveGem(stone);
        jobs.Add(new JewelJob { Stone = stone, Kind = kind, Metal = metal, Setting = setting, StartDay = Today, ReadyDay = Today + Mathf.Max(0, quote.Days), Fee = quote.Fee });
        OnChanged?.Invoke();
        return true;
    }

    // The jewel is ready: the stone goes back in the bag, set.
    public bool TryCollect(JewelJob job, out SetResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null || !jobs.Contains(job)) return false;

        if (!IsReady(job))
        {
            message = "Le bijou n'est pas encore terminé.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de reprendre le bijou.";
            return false;
        }

        result = JewelCatalog.Resolve(job.Kind, job.Metal, job.Setting, job.Stone);
        var metalCost = JewelCatalog.GetQuote(job.Kind, job.Metal, job.Setting, job.Stone).MetalCost;
        job.Stone.ApplySetting(JewelCatalog.Kinds[job.Kind].Name, JewelCatalog.Metals[job.Metal].Name, JewelCatalog.Settings[job.Setting].Name, metalCost);
        if (result.Outcome != SetOutcome.Fine) job.Stone.ApplyCraftDamage(result.ValueFactor);
        inventory.TryAddGem(job.Stone);
        jobs.Remove(job);
        OnChanged?.Invoke();
        return true;
    }

    // ---- Taking out ------------------------------------------------------------------------------------------------

    public bool TryUnset(GemInstance gem, out string message)
    {
        message = null;
        var wallet = PlayerWallet.Instance;
        if (gem == null || wallet == null || !gem.IsSet) return false;
        if (!wallet.TrySpend(JewelCatalog.UnsetFee))
        {
            message = $"Il faut {JewelCatalog.UnsetFee} € pour faire sortir la pierre de sa monture.";
            return false;
        }
        gem.RemoveSetting();
        OnChanged?.Invoke();
        return true;
    }

    // ---- Selling a jewel -----------------------------------------------------------------------------------------------

    // What the jeweller pays: the value of the jewel less his share. A treated stone is paid for what it is when the treatment is declared, and as an
    // untreated one when it is not.
    public int Offer(GemInstance gem, bool declared)
    {
        if (gem == null || !gem.IsSet) return 0;
        var stoneValue = !gem.IsTreated ? gem.FinalValue : declared ? TreatmentDisclosure.DeclaredValue(gem) : TreatmentDisclosure.HiddenValue(gem);
        return Mathf.Max(1, Mathf.RoundToInt(JewelCatalog.JewelValue(gem, stoneValue) * JewelCatalog.JewelerShare));
    }

    // The offer shown in the list: a treatment the player does not know about cannot be declared.
    public int ListedOffer(GemInstance gem) => Offer(gem, !gem.IsTreated || gem.IsTreatmentKnown);

    public float TestChance(GemInstance gem) => TreatmentDisclosure.ChanceFor(gem, JewelCatalog.JewelerExpertise * JewelCatalog.VisibilityOf(gem));

    public SaleOutcome SellDeclared(GemInstance gem)
    {
        var outcome = new SaleOutcome();
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (gem == null || !gem.IsSet || inventory == null || wallet == null) return outcome;

        outcome.Price = Offer(gem, true);
        inventory.RemoveGem(gem);
        wallet.AddMoney(outcome.Price);
        outcome.Sold = true;
        return outcome;
    }

    // Without a word about the treatment. The jeweller may test the stone, and the setting hides a part of it: if he finds the treatment, nothing is sold. A seller
    // who knew about it is fined and loses reputation; one who did not know gets a new offer, the declared price.
    public SaleOutcome SellUndisclosed(GemInstance gem)
    {
        var outcome = new SaleOutcome();
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (gem == null || !gem.IsSet || inventory == null || wallet == null) return outcome;

        var asked = Offer(gem, false);
        var knew = gem.IsTreatmentKnown;
        if (gem.IsTreated && TreatmentDisclosure.BuyerFinds(gem, BuyerSeed, JewelCatalog.JewelerExpertise * JewelCatalog.VisibilityOf(gem)))
        {
            gem.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            outcome.FoundTreatment = true;
            outcome.Deliberate = knew;
            if (knew)
            {
                outcome.Fine = Mathf.Min(wallet.Money, TreatmentDisclosure.FineOn(asked));
                if (outcome.Fine > 0) wallet.TrySpend(outcome.Fine);
                outcome.ReputationLost = TreatmentDisclosure.ReputationLoss;
                ReputationState.Instance?.RemovePoints(outcome.ReputationLost);
            }
            else outcome.Price = Offer(gem, true);
            return outcome;
        }

        inventory.RemoveGem(gem);
        wallet.AddMoney(asked);
        outcome.Sold = true;
        outcome.Price = asked;
        return outcome;
    }

    // ---- Save ------------------------------------------------------------------------------------------------------

    public List<JewelJobSaveData> ToSaveData()
    {
        var result = new List<JewelJobSaveData>();
        foreach (var job in jobs)
            result.Add(new JewelJobSaveData { stone = job.Stone.ToSaveData(), kind = job.Kind, metal = job.Metal, setting = job.Setting, startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee });
        return result;
    }

    public void LoadJobs(List<JewelJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        jobs.Clear();
        if (saved != null)
        {
            foreach (var entry in saved)
            {
                var stone = entry.stone != null ? restore(entry.stone) : null;
                if (stone != null)
                    jobs.Add(new JewelJob { Stone = stone, Kind = entry.kind, Metal = entry.metal, Setting = entry.setting, StartDay = entry.startDay, ReadyDay = entry.readyDay, Fee = entry.fee });
            }
        }
        OnChanged?.Invoke();
    }
}
