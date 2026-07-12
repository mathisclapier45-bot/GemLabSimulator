using UnityEngine;

public class ReturnToMenuController : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        ReturnToMenuUI.Instance?.Toggle();
    }
}
