using UnityEngine;

public class ReturnToMenuController : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (ComputerUI.ConsumesEscape || BinderUI.ConsumesEscape || BoosterUI.ConsumesEscape) return;
        ReturnToMenuUI.Instance?.Toggle();
    }
}
