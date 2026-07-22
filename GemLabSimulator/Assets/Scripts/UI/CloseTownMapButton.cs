using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseTownMapButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => TownMapUI.Instance.Close());
    }
}
