using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CancelReturnToMenuButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => ReturnToMenuUI.Instance.Cancel());
    }
}
