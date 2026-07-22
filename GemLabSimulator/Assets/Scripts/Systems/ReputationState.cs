using System;
using UnityEngine;

public class ReputationState : MonoBehaviour
{
    public static ReputationState Instance { get; private set; }

    public int Points { get; private set; }
    public int Level { get; private set; } = 1;

    public event Action OnReputationChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public void AddPoint()
    {
        Points++;
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

    // Cumulative points needed to reach a given level: 0, 2, 4, 8, 16, 32... (doubles from level 2 onward).
    public static int PointsRequiredForLevel(int level)
    {
        return level <= 1 ? 0 : (int)Mathf.Pow(2, level - 1);
    }

    public float ProgressToNextLevel()
    {
        var currentThreshold = PointsRequiredForLevel(Level);
        var nextThreshold = PointsRequiredForLevel(Level + 1);
        if (nextThreshold <= currentThreshold) return 1f;
        return Mathf.Clamp01((float)(Points - currentThreshold) / (nextThreshold - currentThreshold));
    }

    public int PointsIntoCurrentLevel() => Points - PointsRequiredForLevel(Level);
    public int PointsNeededForNextLevel() => PointsRequiredForLevel(Level + 1) - PointsRequiredForLevel(Level);
}
