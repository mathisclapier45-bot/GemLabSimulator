using System;
using UnityEngine;

// Uses a piece of field equipment from the bag: opens the matching tool window anywhere on the map.
// While that window is open, the bag shows a "Retour au sac" button (see BagUI).
public static class BagToolLauncher
{
    private static Func<bool> isToolOpen;
    private static Action closeTool;

    // True while a tool opened from the bag is still on screen.
    public static bool ToolOpenFromBag => isToolOpen != null && isToolOpen();

    public static void CloseTool()
    {
        closeTool?.Invoke();
        isToolOpen = null;
        closeTool = null;
    }

    // Returns true when a tool window was opened.
    public static bool Use(string toolId)
    {
        switch (toolId)
        {
            case "Loupe10X": return Track(LoupeUI.TryOpen(10f), () => LoupeUI.Instance != null && LoupeUI.Instance.IsOpen, () => LoupeUI.Instance.Close());
            case "Loupe20X": return Track(LoupeUI.TryOpen(20f), () => LoupeUI.Instance != null && LoupeUI.Instance.IsOpen, () => LoupeUI.Instance.Close());
            // The torch and the UV lamp are used with the loupe viewer, with the best lens the player owns.
            case "Maglite":
            case "LampeUVLongs": return Track(LoupeUI.TryOpen(BestLens()), () => LoupeUI.Instance != null && LoupeUI.Instance.IsOpen, () => LoupeUI.Instance.Close());
            case "DichroscopeCalcite":
            case "DichroscopeLondres": return Track(DichroscopeUI.TryOpen(), () => DichroscopeUI.Instance != null && DichroscopeUI.Instance.IsOpen, () => DichroscopeUI.Instance.Close());
            case "SpectroscopePoche": return Track(SpectroscopeUI.TryOpen(), () => SpectroscopeUI.Instance != null && SpectroscopeUI.Instance.IsOpen, () => SpectroscopeUI.Instance.Close());
            case "FiltreChelseaPortable": return Track(ChelseaFilterUI.TryOpen(), () => ChelseaFilterUI.Instance != null && ChelseaFilterUI.Instance.IsOpen, () => ChelseaFilterUI.Instance.Close());
            case "AimantGemmologie": return Track(MagnetUI.TryOpen(), () => MagnetUI.Instance != null && MagnetUI.Instance.IsOpen, () => MagnetUI.Instance.Close());
            case "BalanceCaratPoche": return Track(WeightScaleUI.TryOpen(), () => WeightScaleUI.Instance != null && WeightScaleUI.Instance.IsOpen, () => WeightScaleUI.Instance.Close());
            case "TesteurThermique": return Track(ConductivityTesterUI.TryOpen(ConductivityTesterUI.Mode.Thermal), () => ConductivityTesterUI.Instance != null && ConductivityTesterUI.Instance.IsOpen, () => ConductivityTesterUI.Instance.Close());
            case "TesteurElectrique": return Track(ConductivityTesterUI.TryOpen(ConductivityTesterUI.Mode.Electrical), () => ConductivityTesterUI.Instance != null && ConductivityTesterUI.Instance.IsOpen, () => ConductivityTesterUI.Instance.Close());
            // The lantern is not a tool window: a click lights it or puts it out.
            case "Lanterne":
            {
                var tools = PlayerOwnedTools.Ensure();
                if (tools == null) return false;
                tools.SetLantern(!tools.LanternLit);
                ToolFeedbackUI.Instance?.ShowMessage(tools.LanternLit
                    ? "Lanterne allumée : elle éclaire la nuit autour de vous."
                    : "Lanterne éteinte.");
                return false;
            }
            case "Chamoisine":
                ToolFeedbackUI.Instance?.ShowMessage("Chamoisine : elle servira à nettoyer les gemmes (bientôt).");
                return false;
            default:
                return false;
        }
    }

    private static bool Track(bool opened, Func<bool> isOpen, Action close)
    {
        if (!opened) return false;
        isToolOpen = isOpen;
        closeTool = close;
        return true;
    }

    private static float BestLens()
    {
        var tools = PlayerOwnedTools.Ensure();
        if (tools == null) return 0f;
        if (tools.Has("Loupe20X")) return 20f;
        return tools.Has("Loupe10X") ? 10f : 0f;
    }
}
