using UnityEngine;

// Who handles Escape in a frame. A window that closes (or goes back one screen) on Escape calls Consume(); the controller of the pause menu, which runs at the end of
// the frame, then leaves the pause menu closed. Dialogs that sit above a window use ConsumesEscape so the window below does not react to the same key.
public static class EscapeGate
{
    private static int frame = -1;

    public static void Consume() => frame = Time.frameCount;

    public static bool Consumed => frame == Time.frameCount;
}
