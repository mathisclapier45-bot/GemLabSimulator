using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseReportButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => ReportUIController.Instance.Close());
    }
}
