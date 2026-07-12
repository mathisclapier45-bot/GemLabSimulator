using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MailboxSendTabButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => MailboxUI.Instance.ShowSendTab());
    }
}
