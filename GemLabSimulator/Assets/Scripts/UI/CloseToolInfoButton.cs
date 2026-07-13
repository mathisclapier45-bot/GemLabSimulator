using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseToolInfoButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => ToolInfoUI.Instance.Close());
    }
}
