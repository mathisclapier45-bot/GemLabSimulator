using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseSpectrumButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => SpectrumUI.Instance.Close());
    }
}
