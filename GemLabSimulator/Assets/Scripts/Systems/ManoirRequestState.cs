using System;
using System.Collections.Generic;
using UnityEngine;

public class ManoirRequestState : MonoBehaviour
{
    public static ManoirRequestState Instance { get; private set; }

    public enum RequestCategory { RareGem, ForbiddenGem, RoughGem }

    public class Request
    {
        public GemDefinition TargetDefinition;
        public RequestCategory Category;
    }

    private const float NewRequestChance = 0.4f;
    private const float RewardMultiplier = 2.0f;
    private const int RareValueThreshold = 150;

    public Request CurrentRequest { get; private set; }

    public event Action OnManoirChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    private void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged += OnDayChanged;
        if (CurrentRequest == null) MaybeGenerateRequest();
    }

    private void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.OnDayChanged -= OnDayChanged;
    }

    private void OnDayChanged(int day)
    {
        MaybeGenerateRequest();
    }

    private void MaybeGenerateRequest()
    {
        if (CurrentRequest != null) return;
        if (UnityEngine.Random.value > NewRequestChance) return;
        GenerateRequest();
    }

    private void GenerateRequest()
    {
        var database = GemDatabase.Instance;
        if (database == null || database.allGems.Count == 0) return;

        var roll = UnityEngine.Random.value;
        var category = roll < 0.5f ? RequestCategory.RareGem : roll < 0.7f ? RequestCategory.ForbiddenGem : RequestCategory.RoughGem;

        var pool = new List<GemDefinition>();
        foreach (var def in database.allGems)
        {
            if (def == null) continue;
            switch (category)
            {
                case RequestCategory.RareGem:
                    if (!def.isTunnelExclusive && def.baseValue >= RareValueThreshold) pool.Add(def);
                    break;
                case RequestCategory.ForbiddenGem:
                    if (def.isTunnelExclusive) pool.Add(def);
                    break;
                case RequestCategory.RoughGem:
                    if (!def.isTunnelExclusive) pool.Add(def);
                    break;
            }
        }

        // Fall back to any non-exclusive species if a category pool happens to be empty.
        if (pool.Count == 0)
        {
            foreach (var def in database.allGems)
                if (def != null && !def.isTunnelExclusive) pool.Add(def);
            category = RequestCategory.RareGem;
        }
        if (pool.Count == 0) return;

        CurrentRequest = new Request
        {
            TargetDefinition = pool[UnityEngine.Random.Range(0, pool.Count)],
            Category = category,
        };
        OnManoirChanged?.Invoke();
    }

    public string DescribeRequest()
    {
        if (CurrentRequest == null) return "";
        var name = CurrentRequest.TargetDefinition.displayName;
        switch (CurrentRequest.Category)
        {
            case RequestCategory.ForbiddenGem:
                return $"\"J'ai besoin de mettre la main sur {name}, une pièce... disons, difficile à trouver légalement. Discrétion assurée.\"";
            case RequestCategory.RoughGem:
                return $"\"Rapportez-moi un brut de {name}. Peu importe sa taille, tant qu'il n'a pas encore été taillé.\"";
            default:
                return $"\"Je collectionne les pierres rares. Trouvez-moi un {name} de belle qualité et vous serez généreusement récompensé.\"";
        }
    }

    public bool Matches(GemInstance candidate)
    {
        if (CurrentRequest == null || candidate == null) return false;
        if (candidate.Definition != CurrentRequest.TargetDefinition) return false;
        if (!candidate.IsIdentified) return false;
        var wantsRough = CurrentRequest.Category == RequestCategory.RoughGem;
        return candidate.IsRough == wantsRough;
    }

    public bool TryFulfill(GemInstance candidate, PlayerInventory inventory, PlayerWallet wallet, out int reward)
    {
        reward = 0;
        if (!Matches(candidate) || inventory == null || wallet == null) return false;

        reward = Mathf.RoundToInt(candidate.FinalValue * RewardMultiplier);
        inventory.RemoveGem(candidate);
        wallet.AddMoney(reward);

        CurrentRequest = null;
        OnManoirChanged?.Invoke();
        return true;
    }
}
