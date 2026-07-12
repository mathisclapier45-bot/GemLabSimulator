using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseMarketButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => MarketUI.Instance.Close());
    }
}
