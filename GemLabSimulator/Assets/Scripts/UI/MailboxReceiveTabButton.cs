using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MailboxReceiveTabButton : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => MailboxUI.Instance.ShowReceiveTab());
    }
}
