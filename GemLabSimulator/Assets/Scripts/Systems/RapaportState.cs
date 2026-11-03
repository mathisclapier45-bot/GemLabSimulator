using System;
using System.Collections.Generic;
using UnityEngine;

// The monthly subscription to the Rapaport price list, sold at the office of Martin Rapaport (6th floor of the Diamond Quarter). While it
// lasts, the bag has a tab with the list. Lives on the player (added on demand, like the other persistent states).
public class RapaportState : MonoBehaviour
{
    public static RapaportState Instance { get; private set; }

    public const int MonthlyPrice = 250;
    private const int MaxMonthsAhead = 12;

    // Last day (counter of GameClock) the subscription gives access to; 0 when there never was one.
    public int PaidUntilDay { get; private set; }

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static RapaportState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<RapaportState>();
        return Instance;
    }

    private static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    public static bool HasAccess => Instance != null && Instance.PaidUntilDay >= Today;

    public DateTime ExpiryDate => GameCalendar.DateOf(PaidUntilDay);

    public bool WasSubscribed => PaidUntilDay > 0;

    // One more month, from today or from the end of the running subscription.
    public bool TrySubscribe(out string message)
    {
        message = null;
        var wallet = PlayerWallet.Instance;
        if (wallet == null) return false;

        var runningUntil = HasAccess ? GameCalendar.DateOf(PaidUntilDay).AddDays(1) : GameCalendar.Today;
        var newEnd = runningUntil.AddMonths(1).AddDays(-1);
        if ((newEnd - GameCalendar.Today).Days > MaxMonthsAhead * 31)
        {
            message = $"Vous êtes déjà abonné pour {MaxMonthsAhead} mois : inutile de payer davantage.";
            return false;
        }
        if (!wallet.TrySpend(MonthlyPrice))
        {
            message = $"Il vous faut {MonthlyPrice} € pour un mois d'abonnement.";
            return false;
        }

        PaidUntilDay = GameCalendar.DayOf(newEnd);
        OnChanged?.Invoke();
        return true;
    }

    public void Load(int paidUntilDay)
    {
        PaidUntilDay = Mathf.Max(0, paidUntilDay);
        OnChanged?.Invoke();
    }
}

// The Rapaport price list: the price per carat of the round brilliant diamonds, by weight, color and clarity, published every Thursday. The
// prices follow the value of the game for the stone, multiplied by a market index that wanders from week to week.
public static class RapaportPrices
{
    public static readonly string[] SizeLabels =
    {
        "0,30 – 0,39", "0,40 – 0,49", "0,50 – 0,69", "0,70 – 0,89", "0,90 – 0,99", "1,00 – 1,49",
        "1,50 – 1,99", "2,00 – 2,99", "3,00 – 3,99", "4,00 – 4,99", "5,00 – 5,99", "10,00 – 10,99",
    };

    // The weight each size range stands for.
    private static readonly float[] SizeCarats = { 0.35f, 0.45f, 0.60f, 0.80f, 0.95f, 1.25f, 1.75f, 2.50f, 3.50f, 4.50f, 5.50f, 10.5f };

    // The rows of the list: FL/IF, then VVS1 to I3 (indexes in the clarity scale of the game).
    public static readonly string[] ClarityRows = { "FL / IF", "VVS1", "VVS2", "VS1", "VS2", "SI1", "SI2", "I1", "I2", "I3" };
    private static readonly int[] ClarityIndexes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    public const int ColorCount = 10;     // D to M

    private static readonly DateTime FirstIssue = new DateTime(2025, 8, 28);     // the Thursday before the start of the game
    private static readonly List<float> Index = new List<float> { 100f };

    // ---- Issues --------------------------------------------------------------------------------------------------

    // The Thursday of the last issue published on or before a date.
    public static DateTime IssueDate(DateTime date)
    {
        var back = ((int)date.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
        return date.Date.AddDays(-back);
    }

    // Number of the issue (1 for the first one), 0 before it.
    public static int IssueNumber(DateTime issueDate)
    {
        return Mathf.Max(0, (issueDate.Date - FirstIssue).Days / 7 + 1);
    }

    public static DateTime DateOfIssue(int number) => FirstIssue.AddDays(7 * (Mathf.Max(1, number) - 1));

    // ---- Market ---------------------------------------------------------------------------------------------------

    private static float Noise(string salt)
    {
        return (StoneOptics.StableHash(salt) % 2001) / 1000f - 1f;
    }

    // The market index of an issue: 100 at the first one, then a slow wander (stronger towards the holidays, weaker in summer).
    public static float MarketIndex(int number)
    {
        number = Mathf.Max(1, number);
        while (Index.Count < number)
        {
            var k = Index.Count + 1;
            var month = DateOfIssue(k).Month;
            var season = month >= 10 ? 0.0025f : (month >= 6 && month <= 8 ? -0.0015f : 0f);
            var change = season + Noise("rapaport|index|" + k) * 0.014f;
            Index.Add(Index[Index.Count - 1] * (1f + change));
        }
        return Index[number - 1];
    }

    public static float WeeklyChange(int number)
    {
        return number <= 1 ? 0f : MarketIndex(number) / MarketIndex(number - 1) - 1f;
    }

    // ---- Prices ---------------------------------------------------------------------------------------------------

    // Price per carat, in euros, of a size range, a color (0 = D ... 9 = M) and a clarity row (0 = FL/IF ... 9 = I3).
    public static int PricePerCarat(int number, int size, int color, int clarityRow)
    {
        var carat = SizeCarats[Mathf.Clamp(size, 0, SizeCarats.Length - 1)];
        var value = DiamondGrading.Value(carat, color, ClarityIndexes[Mathf.Clamp(clarityRow, 0, ClarityIndexes.Length - 1)]) / carat;
        var cell = 1f + Noise($"rapaport|cell|{number}|{size}|{color}|{clarityRow}") * 0.012f;
        return Mathf.Max(1, Mathf.RoundToInt(value * MarketIndex(number) / 100f * cell));
    }
}
