using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Text moneyText;
    [SerializeField] private Text dayText;

    private void Start()
    {
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.OnMoneyChanged += UpdateMoneyText;
            UpdateMoneyText(PlayerWallet.Instance.Money);
        }

        if (GameClock.Instance != null)
        {
            GameClock.Instance.OnDayChanged += UpdateDayText;
            UpdateDayText(GameClock.Instance.CurrentDay);
        }
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyChanged -= UpdateMoneyText;
        if (GameClock.Instance != null)
            GameClock.Instance.OnDayChanged -= UpdateDayText;
    }

    private void UpdateMoneyText(int amount)
    {
        moneyText.text = $"Argent : {amount}€";
    }

    private void UpdateDayText(int day)
    {
        if (dayText != null) dayText.text = $"Jour {day}";
    }
}
