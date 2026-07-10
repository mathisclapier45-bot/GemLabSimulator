using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Text moneyText;
    [SerializeField] private PlayerWallet wallet;

    private void OnEnable()
    {
        wallet.OnMoneyChanged += UpdateText;
        UpdateText(wallet.Money);
    }

    private void OnDisable()
    {
        wallet.OnMoneyChanged -= UpdateText;
    }

    private void UpdateText(int amount)
    {
        moneyText.text = $"Argent : {amount}€";
    }
}
