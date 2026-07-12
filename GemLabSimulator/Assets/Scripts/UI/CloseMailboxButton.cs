using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CloseMailboxButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => MailboxUI.Instance.Close());
    }
}
