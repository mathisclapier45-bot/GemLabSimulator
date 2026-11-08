using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    public int Money { get; private set; }
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnMoneyDelta;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
        OnMoneyDelta?.Invoke(amount);
    }

    public void SetMoney(int amount)
    {
        Money = amount;
        OnMoneyChanged?.Invoke(Money);
    }

    // A credit left at the GemStore: it pays the purchases of the shop before the money does.
    public int StoreCredit { get; private set; }

    public void SetStoreCredit(int amount)
    {
        StoreCredit = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(Money);
    }

    public void AddStoreCredit(int amount) => SetStoreCredit(StoreCredit + amount);

    public bool CanAffordAtShop(int price) => Money + StoreCredit >= price;

    // Spends the credit first, then the money. Returns false (and spends nothing) when both are not enough.
    public bool TrySpendAtShop(int price)
    {
        if (!CanAffordAtShop(price)) return false;
        var fromCredit = Mathf.Min(StoreCredit, price);
        StoreCredit -= fromCredit;
        var rest = price - fromCredit;
        if (rest > 0) TrySpend(rest);
        else OnMoneyChanged?.Invoke(Money);
        return true;
    }

    public bool TrySpend(int amount)
    {
        if (Money < amount) return false;
        Money -= amount;
        OnMoneyChanged?.Invoke(Money);
        OnMoneyDelta?.Invoke(-amount);
        return true;
    }
}
