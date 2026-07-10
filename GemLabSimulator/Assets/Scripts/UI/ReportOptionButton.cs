using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ReportOptionButton : MonoBehaviour
{
    [SerializeField] private string speciesName;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => ReportUIController.Instance.SubmitSpecies(speciesName));
    }
}
