using System;
using UnityEngine;

public class ReputationState : MonoBehaviour
{
    public static ReputationState Instance { get; private set; }

    public int Points { get; private set; }
    public int Level { get; private set; } = 1;

    public event Action OnReputationChanged;

    // A stone identified and validated by the director gives this much experience; the level follows the number of stones.
    public const int XpPerStone = 10;

    // Raised with the new level when the player goes up a level.
    public event Action<int> OnLevelUp;

    public int Xp => Points * XpPerStone;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public void AddPoint()
    {
        var before = Level;
        Points++;
        RecomputeLevel();
        OnReputationChanged?.Invoke();
        if (Level > before) OnLevelUp?.Invoke(Level);
    }

    public void RemovePoints(int amount)
    {
        Points = Mathf.Max(0, Points - Mathf.Max(0, amount));
        RecomputeLevel();
        OnReputationChanged?.Invoke();
    }

    public void SetPoints(int points)
    {
        Points = Mathf.Max(0, points);
        RecomputeLevel();
        OnReputationChanged?.Invoke();
    }

    private void RecomputeLevel()
    {
        var level = 1;
        while (Points >= PointsRequiredForLevel(level + 1))
            level++;
        Level = level;
    }

    // Cumulative stones (points) needed to reach a given level: 0, 3, 6, 12, 24, 48... (doubles from level 3 onward).
    public static int PointsRequiredForLevel(int level)
    {
        return level <= 1 ? 0 : 3 * (int)Mathf.Pow(2, level - 2);
    }

    public float ProgressToNextLevel()
    {
        var currentThreshold = PointsRequiredForLevel(Level);
        var nextThreshold = PointsRequiredForLevel(Level + 1);
        if (nextThreshold <= currentThreshold) return 1f;
        return Mathf.Clamp01((float)(Points - currentThreshold) / (nextThreshold - currentThreshold));
    }

    // The same in experience points, for the gauge.
    public int XpIntoCurrentLevel => PointsIntoCurrentLevel() * XpPerStone;
    public int XpNeededForNextLevel => PointsNeededForNextLevel() * XpPerStone;

    public int PointsIntoCurrentLevel() => Points - PointsRequiredForLevel(Level);
    public int PointsNeededForNextLevel() => PointsRequiredForLevel(Level + 1) - PointsRequiredForLevel(Level);
}
