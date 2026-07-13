using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseQuestButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => QuestUI.Instance.Close());
    }
}
