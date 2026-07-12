using System;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    public int CurrentDay { get; private set; } = 1;
    public event Action<int> OnDayChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public void AdvanceDay()
    {
        CurrentDay++;
        OnDayChanged?.Invoke(CurrentDay);
    }

    public void SetDay(int day)
    {
        CurrentDay = Mathf.Max(1, day);
        OnDayChanged?.Invoke(CurrentDay);
    }
}
