using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BackToGrimoireGridButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => GrimoireUI.Instance.HideDetail());
    }
}
