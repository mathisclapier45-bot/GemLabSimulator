using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseGrimoireButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => GrimoireUI.Instance.Close());
    }
}
