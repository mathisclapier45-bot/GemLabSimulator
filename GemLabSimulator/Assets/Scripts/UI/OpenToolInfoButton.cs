using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenToolInfoButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => ToolInfoUI.Instance.OpenInfo());
    }
}
