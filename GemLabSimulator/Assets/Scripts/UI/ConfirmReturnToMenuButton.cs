using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ConfirmReturnToMenuButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => ReturnToMenuUI.Instance.Confirm());
    }
}
