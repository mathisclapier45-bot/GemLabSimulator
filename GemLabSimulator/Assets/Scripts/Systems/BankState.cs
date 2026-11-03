using System;
using UnityEngine;

// The account of the player at the bank of the town. The bank announces a rate each month, sometimes a very good one (up to 10 % a month).
// On the 1st of the next month the interest is paid on the lowest balance of the month, so a deposit only earns from the month after it.
// The account has a ceiling. Lives on the player (added on demand, like the other persistent states).
public class BankState : MonoBehaviour
{
    public static BankState Instance { get; private set; }

    public const int Ceiling = 20000;

    public bool HasAccount { get; private set; }
    public int Balance { get; private set; }
    public int LastInterest { get; private set; }
    public float LastRate { get; private set; }

    private int monthKey;
    private int minBalance;
    private float checkTimer;
    private string pendingMessage;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static BankState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<BankState>();
        return Instance;
    }

    // ---- Rates ---------------------------------------------------------------------------------------------------

    public static int MonthKeyOf(DateTime date) => date.Year * 12 + date.Month - 1;

    // The rate of a month (0.034 is 3.4 %): mostly modest, now and then excellent.
    public static float RateOf(int key)
    {
        var hash = StoneOptics.StableHash("bank|rate|" + key);
        var roll = (hash % 1000) / 1000f;
        var u = ((hash / 1000) % 1000) / 1000f;
        float rate;
        if (roll < 0.45f) rate = Mathf.Lerp(0.005f, 0.015f, u);
        else if (roll < 0.75f) rate = Mathf.Lerp(0.015f, 0.035f, u);
        else if (roll < 0.92f) rate = Mathf.Lerp(0.035f, 0.065f, u);
        else rate = Mathf.Lerp(0.065f, 0.10f, u);
        return Mathf.Round(rate * 1000f) / 1000f;
    }

    public static int CurrentMonthKey => MonthKeyOf(GameCalendar.Today);

    public float CurrentRate => RateOf(CurrentMonthKey);

    // The interest the account would earn this month: on the lowest balance since the 1st.
    public int ProjectedInterest => Mathf.FloorToInt(minBalance * CurrentRate);

    public int MinBalance => minBalance;

    // ---- Operations ------------------------------------------------------------------------------------------------

    public void Open()
    {
        if (HasAccount) return;
        HasAccount = true;
        monthKey = CurrentMonthKey;
        minBalance = Balance;
        OnChanged?.Invoke();
    }

    public bool TryDeposit(int amount, out string message)
    {
        message = null;
        var wallet = PlayerWallet.Instance;
        if (!HasAccount || wallet == null || amount <= 0) return false;

        var room = Ceiling - Balance;
        if (room <= 0)
        {
            message = $"Le plafond du compte est atteint ({Ceiling:N0} €).";
            return false;
        }
        amount = Mathf.Min(amount, room);
        if (!wallet.TrySpend(amount))
        {
            message = "Vous n'avez pas assez d'argent sur vous.";
            return false;
        }
        Balance += amount;
        OnChanged?.Invoke();
        return true;
    }

    public bool TryWithdraw(int amount, out string message)
    {
        message = null;
        var wallet = PlayerWallet.Instance;
        if (!HasAccount || wallet == null || amount <= 0) return false;
        if (Balance <= 0)
        {
            message = "Le compte est vide.";
            return false;
        }
        amount = Mathf.Min(amount, Balance);
        Balance -= amount;
        minBalance = Mathf.Min(minBalance, Balance);
        wallet.AddMoney(amount);
        OnChanged?.Invoke();
        return true;
    }

    // ---- Monthly payment -----------------------------------------------------------------------------------------

    private void Update()
    {
        if (!HasAccount || GameClock.Instance == null) return;

        checkTimer -= Time.unscaledDeltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = 0.5f;
            PayInterest();
        }

        if (pendingMessage != null && ToolFeedbackUI.Instance != null && SceneManagerIsGame())
        {
            ToolFeedbackUI.Instance.ShowMessage(pendingMessage);
            pendingMessage = null;
        }
    }

    private static bool SceneManagerIsGame() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu";

    private void PayInterest()
    {
        var key = CurrentMonthKey;
        if (key <= monthKey) return;

        var paid = 0;
        var lastRate = 0f;
        while (monthKey < key)
        {
            lastRate = RateOf(monthKey);
            var interest = Mathf.FloorToInt(minBalance * lastRate);
            Balance += interest;
            paid += interest;
            minBalance = Balance;
            monthKey++;
        }
        LastInterest = paid;
        LastRate = lastRate;
        if (paid > 0)
            pendingMessage = $"La banque a versé {paid:N0} € d'intérêts sur votre compte (taux de {lastRate * 100f:0.0} % le mois dernier).".Replace('.', ',');
        OnChanged?.Invoke();
    }

    // ---- Save --------------------------------------------------------------------------------------------------------

    public void WriteTo(SaveData data)
    {
        data.bankOpen = HasAccount;
        data.bankBalance = Balance;
        data.bankMonthKey = monthKey;
        data.bankMinBalance = minBalance;
        data.bankLastInterest = LastInterest;
        data.bankLastRate = LastRate;
    }

    public void LoadFrom(SaveData data)
    {
        HasAccount = data.bankOpen;
        Balance = Mathf.Max(0, data.bankBalance);
        monthKey = data.bankMonthKey;
        minBalance = Mathf.Min(data.bankMinBalance, Balance);
        LastInterest = data.bankLastInterest;
        LastRate = data.bankLastRate;
        OnChanged?.Invoke();
    }
}
