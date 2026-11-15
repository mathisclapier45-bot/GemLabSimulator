using UnityEngine;

// The look of the reports of the laboratory (the stone report and the diamond certificate): paper of an off-white, like a page, and a border whose colour tells the stone
// at a glance: yellow for a synthetic stone, blue for a natural diamond, brown for a natural coloured stone.
public static class ReportStyle
{
    public static readonly Color Paper = new Color(0.955f, 0.930f, 0.868f, 1f);

    private static readonly Color SyntheticYellow = new Color(0.93f, 0.74f, 0.10f, 1f);
    private static readonly Color DiamondBlue = new Color(0.17f, 0.35f, 0.68f, 1f);
    private static readonly Color NaturalBrown = new Color(0.43f, 0.27f, 0.14f, 1f);

    public static Color FrameColor(GemInstance gem)
    {
        if (gem == null) return NaturalBrown;
        if (gem.IsSynthetic) return SyntheticYellow;
        if (gem.Definition != null && gem.Definition.name == "Diamant") return DiamondBlue;
        return NaturalBrown;
    }

    // The border: a band along each of the four sides of the page.
    public static void DrawFrame(Transform parent, float pageWidth, float pageHeight, float thickness, GemInstance gem)
    {
        var color = FrameColor(gem);
        RuntimeUIKit.MakeBox("FrameTop", parent, new Vector2(0f, 0f), new Vector2(pageWidth, thickness), color, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameBottom", parent, new Vector2(0f, -pageHeight + thickness), new Vector2(pageWidth, thickness), color, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameLeft", parent, new Vector2(0f, 0f), new Vector2(thickness, pageHeight), color, false).raycastTarget = false;
        RuntimeUIKit.MakeBox("FrameRight", parent, new Vector2(pageWidth - thickness, 0f), new Vector2(thickness, pageHeight), color, false).raycastTarget = false;
    }
}
