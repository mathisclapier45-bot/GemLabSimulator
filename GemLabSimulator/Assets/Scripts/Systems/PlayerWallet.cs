using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int Money { get; private set; }
    public event Action<int> OnMoneyChanged;

    public void AddMoney(int amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }
}
