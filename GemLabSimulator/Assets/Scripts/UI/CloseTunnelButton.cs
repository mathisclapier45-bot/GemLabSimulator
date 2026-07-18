using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseTunnelButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => TunnelUI.Instance.Close());
    }
}
