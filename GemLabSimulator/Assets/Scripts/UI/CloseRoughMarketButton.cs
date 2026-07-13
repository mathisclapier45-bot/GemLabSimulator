using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseRoughMarketButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => RoughMarketUI.Instance.Close());
    }
}
