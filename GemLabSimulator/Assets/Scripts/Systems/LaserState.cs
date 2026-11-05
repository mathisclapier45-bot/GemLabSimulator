using System;
using System.Collections.Generic;
using UnityEngine;

// The diamond under the laser of the treatments laboratory: it comes out on ReadyDay, treated (the laser, then the acid bath).
public class LaserJob
{
    public int StartDay;
    public int ReadyDay;
    public int Fee;
    public LaserMethod Method;
    public int Power;
    public int Treated;
    public GemInstance Stone;
}

// The laser station holds one diamond at a time. The stone leaves the bag when the run starts, and is treated when the player takes it out.
// Lives on the player (added on demand, like the other persistent states).
public class LaserState : MonoBehaviour
{
    public static LaserState Instance { get; private set; }

    private LaserJob job;

    public LaserJob Job => job;

    // The technician gives a few diamonds with dark inclusions, once, to start with.
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

    public static LaserState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<LaserState>();
        return Instance;
    }

    private void HandleDayChanged(int day)
    {
        OnJobChanged?.Invoke();
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public bool IsReady => job != null && Today >= job.ReadyDay;

    public int DaysRemaining => job == null ? 0 : Mathf.Max(0, job.ReadyDay - Today);

    public bool TryStart(LaserMethod method, int power, int treated, GemInstance stone, LaserQuote quote, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (stone == null || inventory == null || wallet == null || !quote.IsValid) return false;

        if (job != null)
        {
            message = "Le poste laser est déjà occupé.";
            return false;
        }
        if (wallet.Money < quote.Fee)
        {
            message = "Vous n'avez pas assez d'argent pour ce traitement.";
            return false;
        }

        wallet.TrySpend(quote.Fee);
        inventory.RemoveGem(stone);
        job = new LaserJob { StartDay = Today, ReadyDay = Today + quote.Days, Fee = quote.Fee, Method = method, Power = power, Treated = quote.Treated, Stone = stone };
        OnJobChanged?.Invoke();
        return true;
    }

    // Takes the diamond out of the acid bath: it is treated now, and goes back into the bag.
    public bool TryCollect(out LaserResult result, out string message)
    {
        result = default;
        message = null;
        var inventory = PlayerInventory.Instance;
        if (job == null || inventory == null) return false;

        if (!IsReady)
        {
            message = "Le traitement n'est pas terminé : le diamant est encore dans le bain d'acide.";
            return false;
        }
        if (inventory.IsFull)
        {
            message = "Inventaire plein : faites de la place avant de sortir la pierre.";
            return false;
        }

        result = LaserCatalog.Resolve(job.Method, job.Power, job.Treated, job.Stone);
        job.Stone.ApplyLaser(result);
        inventory.TryAddGem(job.Stone);
        GemCodex.Instance?.MarkDiscovered(job.Stone.Definition.name);
        job = null;
        OnJobChanged?.Invoke();
        return true;
    }

    // Four cut diamonds of a modest clarity, each with at least one dark inclusion, not certified yet.
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

        var carats = new[] { 0.55f, 0.72f, 0.90f, 1.10f };
        var colors = new[] { 3, 4, 2, 5 };
        var clarities = new[] { 7, 8, 6, 7 };
        for (var i = 0; i < count; i++)
        {
            GemInstance stone = null;
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var diameter = 6.5f * Mathf.Pow(carats[i], 1f / 3f);
                var value = DiamondGrading.Value(carats[i], colors[i], clarities[i]);
                stone = GemInstance.CreateCutDiamond(diamond, carats[i], colors[i], clarities[i], "Rond", "Brillant", diameter, diameter, diameter * 0.61f, value,
                    "Taillerie de démonstration", 1);
                if (LaserCatalog.DarkInclusions(stone).Count >= 2) break;
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

    public List<LaserJobSaveData> ToSaveData()
    {
        var result = new List<LaserJobSaveData>();
        if (job != null)
            result.Add(new LaserJobSaveData
            {
                startDay = job.StartDay, readyDay = job.ReadyDay, fee = job.Fee, method = (int)job.Method, power = job.Power, treated = job.Treated,
                stone = job.Stone.ToSaveData(),
            });
        return result;
    }

    public void LoadJobs(List<LaserJobSaveData> saved, Func<GemInstanceSaveData, GemInstance> restore)
    {
        job = null;
        if (saved != null && saved.Count > 0 && saved[0].stone != null)
        {
            var entry = saved[0];
            var stone = restore(entry.stone);
            if (stone != null)
                job = new LaserJob
                {
                    StartDay = entry.startDay > 0 ? entry.startDay : entry.readyDay - 1, ReadyDay = entry.readyDay, Fee = entry.fee,
                    Method = (LaserMethod)Mathf.Clamp(entry.method, 0, 1), Power = Mathf.Max(LaserCatalog.MinPower, entry.power), Treated = Mathf.Max(1, entry.treated), Stone = stone,
                };
        }
        OnJobChanged?.Invoke();
    }
}
