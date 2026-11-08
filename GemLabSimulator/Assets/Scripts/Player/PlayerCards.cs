using System;
using System.Collections.Generic;
using UnityEngine;

public struct CardDraw
{
    public CardDef Card;
    public bool IsNew;      // first copy of this card
    public int Count;       // copies owned after the draw
}

// The card collection of the player and the boosters of the day. Lives on the player, like the owned tools and the animals.
public class PlayerCards : MonoBehaviour
{
    public static PlayerCards Instance { get; private set; }

    public const int CardsPerBooster = 5;
    public const int BoostersPerDay = 1;
    public const int BoosterPrice = 11;     // whole euros: the wallet has no cents (the booster is 10,99)

    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
    private int boosterDay;                 // the day the boosters counted below were bought
    private int boostersToday;

    public event Action OnCardsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    // Scenes generated before this component existed do not carry it: add it to the player on demand.
    public static PlayerCards Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<PlayerCards>();
        return Instance;
    }

    public int Count(string cardId) => cardId != null && counts.TryGetValue(cardId, out var n) ? n : 0;

    public bool Has(string cardId) => Count(cardId) > 0;

    public int UniqueCards => counts.Count;

    public int TotalCards
    {
        get
        {
            var total = 0;
            foreach (var n in counts.Values) total += n;
            return total;
        }
    }

    // Cards of a series owned (at least once) and the size of the series.
    public void SeriesProgress(CardSeries series, out int owned, out int total)
    {
        owned = 0;
        total = 0;
        foreach (var card in CardCatalog.All)
        {
            if (card.Series != series) continue;
            total++;
            if (Has(card.Id)) owned++;
        }
    }

    private static int CurrentDay => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;

    // A new day, a new booster: the shop offers a limited number of them each day.
    public int BoostersLeftToday => CurrentDay != boosterDay ? BoostersPerDay : Mathf.Max(0, BoostersPerDay - boostersToday);

    // Draws the cards of a booster and adds them to the binder. The caller has already taken the money.
    public List<CardDraw> OpenBooster(bool gift = false)
    {
        // A booster that is given does not count among the boosters of the day.
        if (!gift)
        {
            var day = CurrentDay;
            if (day != boosterDay)
            {
                boosterDay = day;
                boostersToday = 0;
            }
            boostersToday++;
        }

        var draws = new List<CardDraw>();
        foreach (var card in CardCatalog.DrawBooster(CardsPerBooster))
        {
            var before = Count(card.Id);
            counts[card.Id] = before + 1;
            draws.Add(new CardDraw { Card = card, IsNew = before == 0, Count = before + 1 });
        }

        OnCardsChanged?.Invoke();
        return draws;
    }

    // ---- Save ---------------------------------------------------------------------------------------------------

    public void WriteTo(SaveData data)
    {
        data.cardIds.Clear();
        data.cardCounts.Clear();
        foreach (var pair in counts)
        {
            data.cardIds.Add(pair.Key);
            data.cardCounts.Add(pair.Value);
        }
        data.boosterDay = boosterDay;
        data.boostersToday = boostersToday;
    }

    public void LoadFrom(SaveData data)
    {
        counts.Clear();
        for (var i = 0; i < data.cardIds.Count && i < data.cardCounts.Count; i++)
        {
            if (CardCatalog.Find(data.cardIds[i]) == null || data.cardCounts[i] <= 0) continue;
            counts[data.cardIds[i]] = data.cardCounts[i];
        }
        boosterDay = data.boosterDay;
        boostersToday = data.boostersToday;
        OnCardsChanged?.Invoke();
    }
}
