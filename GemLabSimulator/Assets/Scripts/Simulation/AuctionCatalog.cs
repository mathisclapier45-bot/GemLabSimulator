using System;
using System.Collections.Generic;
using UnityEngine;

public class AuctionTheme
{
    public string Name;
    public string Description;
    public Func<GemDefinition, bool> Matches;
    public string OriginKeyword;           // for the themes of a country: the lots come from this origin
    public bool TreatedOnly;               // the lots are all treated stones
}

// What the catalogue of the house says about the treatment of a stone. The sellers are not all honest.
public enum CatalogHonesty { Untreated, Declared, Silent, Lied }

// A lot of the sale: the stone, what the catalogue says about it, and the buyers in the room.
public class AuctionLot
{
    public int Number;
    public GemInstance Gem;
    public CatalogHonesty Honesty;
    public int Seed;
    public bool Sold;
    public bool ToPlayer;
    public int Hammer;
    public List<int> Limits = new List<int>();      // what each buyer in the room is ready to pay, hammer price
    public int Start;
    public int Step;
    public int EstimateLow;
    public int EstimateHigh;

    public string Name => Gem.Definition.displayName;
}

// The weekly sale of the bank: every Saturday, with a theme. The theme sets the stones of the sale, the catalogue describes them (not always
// honestly about their treatment), and buyers simulated in the room raise the price. Everything that depends on the day is deterministic, so the
// calendar can announce the theme weeks ahead.
public static class AuctionCatalog
{
    public const float BuyerPremium = 0.10f;        // paid by the buyer on top of the hammer price
    public const float SellerCommission = 0.10f;    // kept by the house on the hammer price

    private static bool Starts(GemDefinition d, params string[] prefixes)
    {
        var n = d.name ?? "";
        foreach (var p in prefixes) if (n.StartsWith(p)) return true;
        return false;
    }

    private static bool Natural(GemDefinition d) => !d.forcedSynthetic && !(d.name ?? "").Contains("Synthetique");

    public static readonly AuctionTheme[] Themes =
    {
        new AuctionTheme { Name = "Rubis et saphirs", Description = "Le corindon dans toutes ses couleurs : rouge, bleu, rose, jaune, orange.",
            Matches = d => Natural(d) && Starts(d, "Corindon_Rubis", "Saphir", "Padparadscha") },
        new AuctionTheme { Name = "Diamants", Description = "Une vente de diamants naturels, taillés par les meilleurs ateliers.",
            Matches = d => Natural(d) && d.name == "Diamant" },
        new AuctionTheme { Name = "Émeraudes et béryls", Description = "Le béryl sous toutes ses formes : émeraude, héliodore, morganite et les plus rares.",
            Matches = d => Natural(d) && (d.name == "Emeraude" || d.name == "Beryl_Rouge" || d.name == "Goshenite" || d.name == "Heliodore" || d.name == "Morganite" || d.name == "Pezzottaite") },
        new AuctionTheme { Name = "Grenats", Description = "Almandin, pyrope, tsavorite, démantoïde : la grande famille des grenats.",
            Matches = d => Natural(d) && Starts(d, "Grenat_") },
        new AuctionTheme { Name = "Quartz et améthystes", Description = "Les quartz colorés : améthyste, citrine, ametrine, quartz fumé et rose.",
            Matches = d => Natural(d) && (Starts(d, "Quartz") || d.name == "Amethyste" || d.name == "Ametrine" || d.name == "Citrine" || d.name == "Prasiolite") },
        new AuctionTheme { Name = "Tourmalines, topazes et kunzites", Description = "Des pierres de couleur vive, des pastels aux verts les plus intenses.",
            Matches = d => Natural(d) && (Starts(d, "Tourmaline") || d.name == "Topaze" || d.name == "Kunzite" || d.name == "Zircon" || d.name == "Apatite" || d.name == "Iolite") },
        new AuctionTheme { Name = "Opales et pierres organiques", Description = "Opales, perles, corail, ambre : des matières nées du vivant et de l'eau.",
            Matches = d => Natural(d) && (Starts(d, "Opale", "Perle") || d.name == "Corail" || d.name == "Ambre" || d.name == "Ecaille_Tortue") },
        new AuctionTheme { Name = "Pierres rares et de collection", Description = "Des pierres que l'on voit une fois dans une carrière : les pièces de collectionneur.",
            Matches = d => Natural(d) && d.baseValue >= 300 && !d.isTunnelExclusive },
        new AuctionTheme { Name = "Pierres ornementales", Description = "Jades, turquoises, lapis-lazuli, agates : des pierres de caractère, plus que d'éclat.",
            Matches = d => Natural(d) && (Starts(d, "Jade", "Turquoise") || d.name == "Lapis_Lazuli" || d.name == "Agate" || d.name == "Oeil_de_Tigre" || d.name == "Rhodonite" || d.name == "Rhodocrosite" || d.name == "Serpentine_Bowenite" || d.name == "Obsidienne") },
        new AuctionTheme { Name = "Birmanie", Description = "Toutes les pierres des mines de Birmanie, de Mogok aux vallées voisines.",
            OriginKeyword = "Birmanie", Matches = d => Natural(d) },
        new AuctionTheme { Name = "Sri Lanka", Description = "Les gisements de l'île de Ceylan : saphirs, spinelles, tourmalines et leurs voisins.",
            OriginKeyword = "Sri Lanka", Matches = d => Natural(d) },
        new AuctionTheme { Name = "Colombie", Description = "Les mines de Muzo et de Chivor : les émeraudes d'abord, et bien d'autres pierres.",
            OriginKeyword = "Colombie", Matches = d => Natural(d) },
        new AuctionTheme { Name = "Le grand déballage : pierres traitées", Description = "Toutes les pierres de cette vente ont subi un traitement. Les catalogues disent-ils toute la vérité ?",
            TreatedOnly = true, Matches = d => Natural(d) },
        new AuctionTheme { Name = "Synthèses et imitations", Description = "Cubic zirconia, YAG, verres, corindons et diamants de laboratoire : l'envers du décor.",
            Matches = d => d.forcedSynthetic || (d.name ?? "").Contains("Synthetique") || d.name == "CZ" || d.name == "YAG" || d.name == "GGG" || d.name == "Verre" },
    };

    // ---- The calendar ----------------------------------------------------------------------------------------------

    public static bool IsAuctionDay(DateTime date) => date.DayOfWeek == DayOfWeek.Saturday;

    // The next sale, today included.
    public static DateTime NextAuction(DateTime from)
    {
        var d = from.Date;
        return d.AddDays(((int)DayOfWeek.Saturday - (int)d.DayOfWeek + 7) % 7);
    }

    // The first sale strictly after a date.
    public static DateTime AuctionAfter(DateTime date) => NextAuction(date.Date.AddDays(1));

    // The number of the bulletin: 1 for the first sale of the game, then one more each week.
    public static int IssueNumber(DateTime saturday)
    {
        var first = NextAuction(GameCalendar.Start);
        return Mathf.Max(1, (saturday.Date - first).Days / 7 + 1);
    }

    private static int WeekKey(DateTime saturday) => GameCalendar.DayOf(saturday.Date) / 7;

    // The theme of the sale of a Saturday; never twice in a row.
    public static AuctionTheme ThemeFor(DateTime date)
    {
        var saturday = NextAuction(date);
        var pick = (int)(StoneOptics.StableHash("auction|theme|" + WeekKey(saturday)) % (uint)Themes.Length);
        var previous = (int)(StoneOptics.StableHash("auction|theme|" + (WeekKey(saturday) - 1)) % (uint)Themes.Length);
        if (pick == previous) pick = (pick + 1) % Themes.Length;
        return Themes[pick];
    }

    // ---- Lots --------------------------------------------------------------------------------------------------------

    public static List<AuctionLot> GenerateLots(DateTime date, GemDatabase database)
    {
        var lots = new List<AuctionLot>();
        if (database == null || database.allGems.Count == 0) return lots;

        var theme = ThemeFor(date);
        var rng = new System.Random((int)(StoneOptics.StableHash("auction|lots|" + WeekKey(NextAuction(date))) & 0x7FFFFFFF));
        var count = 6 + rng.Next(3);

        var candidates = new List<(GemDefinition def, GemOrigin origin)>();
        foreach (var def in database.allGems)
        {
            if (def == null || def.isTunnelExclusive || !theme.Matches(def)) continue;
            if (theme.OriginKeyword != null)
            {
                foreach (var origin in def.origins)
                    if (origin.name != null && origin.name.Contains(theme.OriginKeyword)) candidates.Add((def, origin));
            }
            else candidates.Add((def, null));
        }
        if (candidates.Count == 0)
        {
            foreach (var def in database.allGems)
                if (def != null && !def.isTunnelExclusive && Natural(def)) candidates.Add((def, null));
        }

        for (var n = 0; n < count && candidates.Count > 0; n++)
        {
            GemInstance gem = null;
            for (var attempt = 0; attempt < (theme.TreatedOnly ? 40 : 1); attempt++)
            {
                var (def, origin) = candidates[rng.Next(candidates.Count)];
                gem = origin != null ? new GemInstance(def, origin) : new GemInstance(def);
                if (!theme.TreatedOnly || gem.IsTreated) break;
            }
            if (gem == null) continue;
            gem.MarkIdentified();
            lots.Add(MakeLot(lots.Count + 1, gem, rng.Next()));
        }
        return lots;
    }

    // A lot from a stone: the catalogue (honest or not), the buyers in the room and the price list of the sale.
    public static AuctionLot MakeLot(int number, GemInstance gem, int seed, CatalogHonesty? forced = null)
    {
        var rng = new System.Random(seed);
        var lot = new AuctionLot { Number = number, Gem = gem, Seed = seed };

        if (forced.HasValue) lot.Honesty = forced.Value;
        else if (!gem.IsTreated) lot.Honesty = rng.Next(100) < 60 ? CatalogHonesty.Untreated : CatalogHonesty.Silent;
        else
        {
            var roll = rng.Next(100);
            lot.Honesty = roll < 50 ? CatalogHonesty.Declared : roll < 80 ? CatalogHonesty.Silent : CatalogHonesty.Lied;
        }

        var believed = BelievedValue(lot);
        var buyers = 2 + rng.Next(3);
        for (var i = 0; i < buyers; i++)
            lot.Limits.Add(Mathf.Max(1, Mathf.RoundToInt(believed * Mathf.Lerp(0.55f, 1.1f, (float)rng.NextDouble()))));

        lot.Step = NiceStep(believed * 0.04f);
        lot.Start = Mathf.Max(lot.Step, RoundTo(believed * 0.3f, lot.Step));
        lot.EstimateLow = Mathf.Max(1, RoundTo(believed * 0.65f, lot.Step));
        lot.EstimateHigh = Mathf.Max(lot.EstimateLow + lot.Step, RoundTo(believed * 1.0f, lot.Step));
        return lot;
    }

    // What the room thinks the stone is worth, from what the catalogue says: the declared treatment lowers the price, a silent catalogue makes the
    // buyers wary, a catalogue that lies has them pay for an untreated stone.
    public static float BelievedValue(AuctionLot lot)
    {
        var gem = lot.Gem;
        if (!gem.IsTreated) return gem.FinalValue;
        switch (lot.Honesty)
        {
            case CatalogHonesty.Declared: return TreatmentDisclosure.DeclaredValue(gem);
            case CatalogHonesty.Silent: return Mathf.Lerp(TreatmentDisclosure.DeclaredValue(gem), TreatmentDisclosure.HiddenValue(gem), 0.5f);
            default: return TreatmentDisclosure.HiddenValue(gem);
        }
    }

    public static string TreatmentLine(AuctionLot lot)
    {
        switch (lot.Honesty)
        {
            case CatalogHonesty.Declared: return $"Traitement déclaré : {lot.Gem.TreatmentGrade}.";
            case CatalogHonesty.Lied: return "Aucun traitement.";
            case CatalogHonesty.Silent: return "Traitements : non précisés.";
            default: return "Aucun traitement connu.";
        }
    }

    public static string Catalog(AuctionLot lot)
    {
        var gem = lot.Gem;
        var state = gem.IsRough ? gem.RoughLabel : $"{gem.ShapeName}, {gem.CutStyleName.ToLowerInvariant()}";
        return $"{gem.Definition.displayName} — {gem.CaratWeight:F2} ct ({state})\nOrigine : {gem.Origin.name}\n{TreatmentLine(lot)}";
    }

    // ---- The auction itself ------------------------------------------------------------------------------------------

    // Where the price stands after the buyers of the room have gone through the price `price`: how many are still in.
    public static int BuyersStillIn(AuctionLot lot, int price)
    {
        var n = 0;
        foreach (var limit in lot.Limits) if (limit >= price) n++;
        return n;
    }

    // Without the player: the highest buyer wins at the price where the second one gave up, one step above, but never below the start.
    public static int HammerWithoutPlayer(AuctionLot lot, out bool sold)
    {
        var first = 0;
        var second = 0;
        foreach (var limit in lot.Limits)
        {
            if (limit > first) { second = first; first = limit; }
            else if (limit > second) second = limit;
        }
        sold = first >= lot.Start;
        return Mathf.Max(lot.Start, Mathf.Min(first, RoundUp(second + 1, lot.Step)));
    }

    public static int HammerWithPlayer(AuctionLot lot, int playerPrice) => playerPrice;

    public static int NiceStep(float value)
    {
        var step = Mathf.Max(1f, value);
        var p = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(step)));
        var n = step / p;
        return Mathf.Max(1, Mathf.RoundToInt((n <= 1.5f ? 1f : n <= 3.5f ? 2f : n <= 7f ? 5f : 10f) * p));
    }

    public static int RoundTo(float value, int step) => Mathf.Max(step, Mathf.RoundToInt(value / step) * step);

    public static int RoundUp(int value, int step) => ((value + step - 1) / step) * step;

    public static int WithPremium(int hammer) => Mathf.RoundToInt(hammer * (1f + BuyerPremium));

    public static int AfterCommission(int hammer) => Mathf.RoundToInt(hammer * (1f - SellerCommission));
}
