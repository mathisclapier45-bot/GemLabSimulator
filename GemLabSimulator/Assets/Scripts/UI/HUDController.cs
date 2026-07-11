using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Text moneyText;

    private void Start()
    {
        if (PlayerWallet.Instance == null) return;
        PlayerWallet.Instance.OnMoneyChanged += UpdateText;
        UpdateText(PlayerWallet.Instance.Money);
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyChanged -= UpdateText;
    }

    private void UpdateText(int amount)
    {
        moneyText.text = $"Argent : {amount}€";
    }
}
