using System;
using System.Collections.Generic;
using UnityEngine;

// A stone the player left with the auction house for the next sale, with the lowest price they accept.
public class AuctionConsignment
{
    public GemInstance Gem;
    public int Reserve;
    public CatalogHonesty Honesty;
    public bool Knew;               // the player knew about the treatment when they left it
    public int AuctionDay;          // the day of the sale where it goes
    public int Seed;
}

// What came out of a sale for a stone of the player, waiting at the auction house: the money, or the stone itself when it was not sold.
public class AuctionResult
{
    public string Name;
    public bool Sold;
    public int Proceeds;
    public int Fine;
    public int ReputationLost;
    public string Message;
    public GemInstance Returned;
}

// The weekly sale of the bank: the lots of the day, the stones the player left to be sold, and what the sales brought. Lives on the player (added
// on demand, like the other persistent states).
public class AuctionState : MonoBehaviour
{
    public static AuctionState Instance { get; private set; }

    public const int MaxConsignments = 3;

    public int LotsDay { get; private set; }
    public List<AuctionLot> Lots { get; private set; } = new List<AuctionLot>();
    public readonly List<AuctionConsignment> Consignments = new List<AuctionConsignment>();
    public readonly List<AuctionResult> Results = new List<AuctionResult>();

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += HandleDayChanged;
        ResolveDue();
        EnsureLots();
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= HandleDayChanged;
    }

    public static AuctionState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<AuctionState>();
        return Instance;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public static bool IsAuctionToday => AuctionCatalog.IsAuctionDay(GameCalendar.DateOf(Today));

    private void HandleDayChanged(int day)
    {
        ResolveDue();
        EnsureLots();
        OnChanged?.Invoke();
    }

    // The catalogue of the coming sale (today's, on a Saturday): made once, the day after the previous sale, so that the bulletin can show the lots
    // all week long and the room offers the very same ones on the day.
    public List<AuctionLot> EnsureLots()
    {
        var date = AuctionCatalog.NextAuction(GameCalendar.Today);
        var day = GameCalendar.DayOf(date);
        if (LotsDay != day || Lots.Count == 0)
        {
            Lots = AuctionCatalog.GenerateLots(date, GemDatabase.Instance);
            LotsDay = day;
        }
        return Lots;
    }

    // The date of the sale the catalogue is for, and the number of the issue.
    public DateTime CatalogueDate => AuctionCatalog.NextAuction(GameCalendar.Today);
    public int CatalogueIssue => AuctionCatalog.IssueNumber(CatalogueDate);

    // ---- Buying ------------------------------------------------------------------------------------------------------

    // The player wins a lot at a hammer price: they pay it with the premium of the house.
    public bool TryBuy(AuctionLot lot, int hammer, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (lot == null || lot.Sold || inventory == null || wallet == null) return false;

        var cost = AuctionCatalog.WithPremium(hammer);
        if (!wallet.TrySpend(cost))
        {
            message = "Vous n'avez pas assez d'argent pour régler cette pierre.";
            return false;
        }
        if (!inventory.TryAddGem(lot.Gem))
        {
            wallet.AddMoney(cost);
            message = "Inventaire plein : la pierre ne peut pas être remise.";
            return false;
        }

        lot.Sold = true;
        lot.ToPlayer = true;
        lot.Hammer = hammer;
        lot.Gem.RecordPurchase(cost);
        GemCodex.Instance?.MarkDiscovered(lot.Gem.Definition.name);
        OnChanged?.Invoke();
        return true;
    }

    // The room wins the lot.
    public void SellToRoom(AuctionLot lot, int hammer)
    {
        if (lot == null || lot.Sold) return;
        lot.Sold = true;
        lot.ToPlayer = false;
        lot.Hammer = hammer;
        OnChanged?.Invoke();
    }

    // ---- Selling -----------------------------------------------------------------------------------------------------

    // Leaves a stone with the house for the next sale: it goes out of the bag.
    public bool TryConsign(GemInstance gem, int reserve, bool declare, out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        if (gem == null || inventory == null || !MarketState.CanSell(gem))
        {
            message = "Cette pierre ne peut pas être vendue aux enchères.";
            return false;
        }
        if (IsAuctionToday)
        {
            message = "La vente a lieu aujourd'hui : les inscriptions pour la prochaine vente se prennent à partir de demain.";
            return false;
        }
        if (Consignments.Count >= MaxConsignments)
        {
            message = $"La maison ne prend que {MaxConsignments} pierres par vente.";
            return false;
        }

        var honesty = !gem.IsTreated ? CatalogHonesty.Untreated : declare ? CatalogHonesty.Declared : CatalogHonesty.Silent;
        inventory.RemoveGem(gem);
        Consignments.Add(new AuctionConsignment
        {
            Gem = gem,
            Reserve = Mathf.Max(1, reserve),
            Honesty = honesty,
            Knew = gem.IsTreated && gem.IsTreatmentKnown,
            AuctionDay = GameCalendar.DayOf(AuctionCatalog.AuctionAfter(GameCalendar.Today)),
            Seed = (int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "|consign|" + Today) & 0x7FFFFFFF),
        });
        OnChanged?.Invoke();
        return true;
    }

    // The stone of the player was left on the day of its sale: it gets sold to the room, and the buyers may test it.
    private void ResolveDue()
    {
        var any = false;
        for (var i = Consignments.Count - 1; i >= 0; i--)
        {
            var c = Consignments[i];
            if (Today < c.AuctionDay) continue;
            Results.Add(Resolve(c));
            Consignments.RemoveAt(i);
            any = true;
        }
        if (any) OnChanged?.Invoke();
    }

    private static AuctionResult Resolve(AuctionConsignment c)
    {
        var gem = c.Gem;
        var result = new AuctionResult { Name = gem.Definition.displayName };
        var lot = AuctionCatalog.MakeLot(1, gem, c.Seed, c.Honesty);
        var hammer = AuctionCatalog.HammerWithoutPlayer(lot, out var sold);

        if (!sold || hammer < c.Reserve)
        {
            result.Message = $"{result.Name} : invendu (offre la plus haute {hammer} €, réserve {c.Reserve} €). La pierre est à reprendre.";
            result.Returned = gem;
            return result;
        }

        var treatedUndisclosed = gem.IsTreated && c.Honesty != CatalogHonesty.Declared;
        if (treatedUndisclosed && TreatmentDisclosure.BuyerFinds(gem, c.Seed, 1f))
        {
            gem.RecordObservation(ExaminationTool.MeasurementType.Treatment.ToString());
            if (c.Knew)
            {
                result.Fine = TreatmentDisclosure.FineOn(hammer);
                result.ReputationLost = TreatmentDisclosure.ReputationLoss;
                result.Message = $"{result.Name} : l'acheteur a testé la pierre et trouvé un traitement non déclaré ({gem.TreatmentGrade}). Vente annulée, amende de {result.Fine} €, {result.ReputationLost * ReputationState.XpPerStone} XP de réputation en moins.";
                result.Returned = gem;
                return result;
            }

            // The seller did not know: the sale goes through at the price of a declared treatment.
            var ratio = TreatmentDisclosure.DeclaredValue(gem) / Mathf.Max(1f, TreatmentDisclosure.HiddenValue(gem));
            hammer = Mathf.Max(1, Mathf.RoundToInt(hammer * ratio));
            result.Sold = true;
            result.Proceeds = AuctionCatalog.AfterCommission(hammer);
            result.Message = $"{result.Name} : adjugé {hammer} €, mais l'acheteur a trouvé un traitement ({gem.TreatmentGrade}) que vous ignoriez : prix ramené à la valeur d'une pierre traitée. Vous touchez {result.Proceeds} € après commission.";
            return result;
        }

        result.Sold = true;
        result.Proceeds = AuctionCatalog.AfterCommission(hammer);
        result.Message = $"{result.Name} : adjugé {hammer} €. Vous touchez {result.Proceeds} € après la commission de {Mathf.RoundToInt(AuctionCatalog.SellerCommission * 100)} %.";
        return result;
    }

    // Takes the money and the unsold stones back. A fine is taken from what the player has, a stone that does not fit in the bag stays at the house.
    public int Collect(out string message)
    {
        message = null;
        var inventory = PlayerInventory.Instance;
        var wallet = PlayerWallet.Instance;
        if (inventory == null || wallet == null || Results.Count == 0) return 0;

        var total = 0;
        var kept = 0;
        for (var i = Results.Count - 1; i >= 0; i--)
        {
            var r = Results[i];
            if (r.Returned != null && !inventory.TryAddGem(r.Returned))
            {
                kept++;
                continue;
            }

            total += r.Proceeds;
            if (r.Proceeds > 0) wallet.AddMoney(r.Proceeds);
            if (r.Fine > 0) wallet.TrySpend(Mathf.Min(wallet.Money, r.Fine));
            if (r.ReputationLost > 0) ReputationState.Instance?.RemovePoints(r.ReputationLost);
            Results.RemoveAt(i);
        }

        if (kept > 0) message = $"{kept} pierre(s) n'ont pas pu être remises dans le sac : faites de la place.";
        OnChanged?.Invoke();
        return total;
    }

    // ---- Save --------------------------------------------------------------------------------------------------------

    public void WriteSave(SaveData data)
    {
        data.auctionLotsDay = LotsDay;
        data.auctionLots = new List<AuctionLotSaveData>();
        foreach (var lot in Lots)
            data.auctionLots.Add(new AuctionLotSaveData { stone = lot.Gem.ToSaveData(), number = lot.Number, honesty = (int)lot.Honesty, seed = lot.Seed, hammer = lot.Hammer, sold = lot.Sold, toPlayer = lot.ToPlayer });

        data.auctionConsignments = new List<AuctionConsignmentSaveData>();
        foreach (var c in Consignments)
            data.auctionConsignments.Add(new AuctionConsignmentSaveData { stone = c.Gem.ToSaveData(), reserve = c.Reserve, honesty = (int)c.Honesty, auctionDay = c.AuctionDay, seed = c.Seed, knew = c.Knew });

        data.auctionResults = new List<AuctionResultSaveData>();
        foreach (var r in Results)
            data.auctionResults.Add(new AuctionResultSaveData { stone = r.Returned != null ? r.Returned.ToSaveData() : null, name = r.Name, sold = r.Sold, proceeds = r.Proceeds, fine = r.Fine, reputationLost = r.ReputationLost, message = r.Message });
    }

    public void ReadSave(SaveData data, Func<GemInstanceSaveData, GemInstance> restore)
    {
        Lots = new List<AuctionLot>();
        Consignments.Clear();
        Results.Clear();
        LotsDay = data.auctionLotsDay;

        if (data.auctionLots != null)
        {
            foreach (var saved in data.auctionLots)
            {
                var gem = saved.stone != null ? restore(saved.stone) : null;
                if (gem == null) continue;
                var lot = AuctionCatalog.MakeLot(saved.number, gem, saved.seed, (CatalogHonesty)saved.honesty);
                lot.Sold = saved.sold;
                lot.ToPlayer = saved.toPlayer;
                lot.Hammer = saved.hammer;
                Lots.Add(lot);
            }
        }

        if (data.auctionConsignments != null)
        {
            foreach (var saved in data.auctionConsignments)
            {
                var gem = saved.stone != null ? restore(saved.stone) : null;
                if (gem == null) continue;
                Consignments.Add(new AuctionConsignment { Gem = gem, Reserve = saved.reserve, Honesty = (CatalogHonesty)saved.honesty, AuctionDay = saved.auctionDay, Seed = saved.seed, Knew = saved.knew });
            }
        }

        if (data.auctionResults != null)
        {
            foreach (var saved in data.auctionResults)
            {
                var gem = saved.stone != null ? restore(saved.stone) : null;
                Results.Add(new AuctionResult { Name = saved.name, Sold = saved.sold, Proceeds = saved.proceeds, Fine = saved.fine, ReputationLost = saved.reputationLost, Message = saved.message, Returned = gem });
            }
        }

        ResolveDue();
        EnsureLots();
        OnChanged?.Invoke();
    }
}
