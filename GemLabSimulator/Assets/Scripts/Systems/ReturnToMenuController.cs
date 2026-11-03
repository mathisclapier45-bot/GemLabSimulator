using UnityEngine;

// Escape: the pause menu (volumes, back to the main menu). The interfaces that close on Escape keep it for themselves: when the number of
// open windows goes down in the frame of the key, the pause menu stays closed.
public class ReturnToMenuController : MonoBehaviour
{
    private int lastOpenPanels;
    private Transform canvas;

    private void LateUpdate()
    {
        var openPanels = CountOpenPanels();
        var pressed = Input.GetKeyDown(KeyCode.Escape);
        var closedOne = openPanels < lastOpenPanels;
        lastOpenPanels = openPanels;

        if (!pressed || closedOne) return;
        if (ComputerUI.ConsumesEscape || BinderUI.ConsumesEscape || BoosterUI.ConsumesEscape || BagUI.ConsumesEscape) return;
        PauseMenuUI.Toggle();
    }

    // The windows of the game are children of the canvas named ...Panel, ...Page or ...Window; the pause menu itself does not count.
    private int CountOpenPanels()
    {
        if (canvas == null)
        {
            var found = GameObject.Find("Canvas");
            if (found == null) return 0;
            canvas = found.transform;
        }

        var count = 0;
        for (var i = 0; i < canvas.childCount; i++)
        {
            var child = canvas.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var name = child.name;
            if (name == "PausePanel" || name == "ReturnToMenuPanel") continue;
            if (name.EndsWith("Panel") || name.EndsWith("Page") || name.EndsWith("Window")) count++;
        }
        return count;
    }
}
