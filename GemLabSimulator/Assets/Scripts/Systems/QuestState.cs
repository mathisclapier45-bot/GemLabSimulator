using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestType { IdentifyCount, SendCount, DiscoverCount, EarnAmount }

public class Quest
{
    public QuestType Type;
    public string Description;
    public int TargetAmount;
    public int CurrentProgress;
    public int Reward;
    public bool Claimed;

    public bool IsCompleted => CurrentProgress >= TargetAmount;
}

public class QuestState : MonoBehaviour
{
    public static QuestState Instance { get; private set; }

    private const int QuestSlotCount = 3;

    public List<Quest> ActiveQuests { get; private set; } = new List<Quest>();

    public event Action OnQuestsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (ActiveQuests.Count > 0) return;

        while (ActiveQuests.Count < QuestSlotCount)
            ActiveQuests.Add(GenerateQuest());
        OnQuestsChanged?.Invoke();
    }

    public void ReportIdentification()
    {
        var changed = false;
        foreach (var quest in ActiveQuests)
        {
            if (quest.Claimed || quest.Type != QuestType.IdentifyCount) continue;
            quest.CurrentProgress++;
            changed = true;
        }
        if (changed) OnQuestsChanged?.Invoke();
    }

    public void ReportDiscovery()
    {
        var changed = false;
        foreach (var quest in ActiveQuests)
        {
            if (quest.Claimed || quest.Type != QuestType.DiscoverCount) continue;
            quest.CurrentProgress++;
            changed = true;
        }
        if (changed) OnQuestsChanged?.Invoke();
    }

    public void ReportGemSent(int value)
    {
        var changed = false;
        foreach (var quest in ActiveQuests)
        {
            if (quest.Claimed) continue;
            if (quest.Type == QuestType.SendCount)
            {
                quest.CurrentProgress++;
                changed = true;
            }
            else if (quest.Type == QuestType.EarnAmount)
            {
                quest.CurrentProgress += value;
                changed = true;
            }
        }
        if (changed) OnQuestsChanged?.Invoke();
    }

    public bool TryClaim(Quest quest, PlayerWallet wallet)
    {
        if (quest == null || wallet == null) return false;
        if (!quest.IsCompleted || quest.Claimed) return false;

        quest.Claimed = true;
        wallet.AddMoney(quest.Reward);
        ActiveQuests.Remove(quest);
        ActiveQuests.Add(GenerateQuest());
        OnQuestsChanged?.Invoke();
        return true;
    }

    private static Quest GenerateQuest()
    {
        var type = (QuestType)UnityEngine.Random.Range(0, 4);
        switch (type)
        {
            case QuestType.IdentifyCount:
            {
                var target = UnityEngine.Random.Range(2, 6);
                return new Quest
                {
                    Type = type,
                    TargetAmount = target,
                    Reward = target * 15,
                    Description = $"Identifier {target} gemmes (n'importe quelle espèce).",
                };
            }
            case QuestType.SendCount:
            {
                var target = UnityEngine.Random.Range(2, 5);
                return new Quest
                {
                    Type = type,
                    TargetAmount = target,
                    Reward = target * 20,
                    Description = $"Envoyer {target} gemmes identifiées à leurs clients.",
                };
            }
            case QuestType.DiscoverCount:
            {
                var target = UnityEngine.Random.Range(1, 4);
                return new Quest
                {
                    Type = type,
                    TargetAmount = target,
                    Reward = target * 40,
                    Description = $"Découvrir {target} nouvelle(s) espèce(s) dans le grimoire.",
                };
            }
            default:
            {
                var target = UnityEngine.Random.Range(2, 7) * 50;
                return new Quest
                {
                    Type = QuestType.EarnAmount,
                    TargetAmount = target,
                    Reward = Mathf.RoundToInt(target * 0.3f),
                    Description = $"Gagner {target}€ en envoyant des gemmes.",
                };
            }
        }
    }
}
