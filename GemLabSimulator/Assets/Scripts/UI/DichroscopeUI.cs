using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Dichroscope: the stone lies on a flat light, the dichroscope above it splits the view into two windows seen
// through perpendicular polarizers. Turn the stone in every direction: a pleochroic stone shows two (dichroism)
// or three (trichroism) different colors depending on the angle.
public class DichroscopeUI : MonoBehaviour
{
    public static DichroscopeUI Instance { get; private set; }

    private const float PanelWidth = 604f;
    private const float PanelHeight = 420f;
    private const float LampX = 20f;
    private const float LampY = -44f;
    private const float LampSize = 320f;
    private const float WindowRadius = 112f;
    private const float ConclusionRotation = 300f;     // degrees explored before the observation is logged

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonColor = new Color(0.25f, 0.25f, 0.32f, 1f);

    private GameObject panel;
    private GemView3D view3D;
    private Image leftStone;
    private Image rightStone;
    private RectTransform leftStoneRect;
    private RectTransform rightStoneRect;
    private Image contrastFill;
    private Text hintText;
    private Text resultText;
    private Text contrastText;

    private GemInstance stone;
    private PleochroismInfo optics;
    private Quaternion opticFrame = Quaternion.identity;
    private Quaternion orientation = Quaternion.identity;
    private float explored;      // degrees
    private bool observed;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<DichroscopeUI>();
        }

        if (Instance == null) return false;
        Instance.Open();
        return true;
    }

    private void Awake()
    {
        Instance = this;
        BuildPanel();
        panel.SetActive(false);
    }

    private void Start()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.OnInventoryChanged -= RefreshIfOpen;
    }

    // ---- The index of the cases, on the left ------------------------------------------------------------------------------

    private SpeciesIndexPanel index;

    private static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.04f;

    // The colours a stone shows along its directions: none, two (dichroism) or three (trichroism), as little swatches, and the words of the species.
    private void BuildIndex()
    {
        if (index != null) return;
        var database = GemDatabase.Instance;
        if (database == null) return;

        index = SpeciesIndexPanel.Create(panel.transform, new Vector2(-206f, 0f), new Vector2(198f, PanelHeight), "Cas possibles",
            "Les couleurs que montre chaque pierre selon la direction.", PanelColor, Gold);

        index.AddHeader("Les trois cas");
        index.AddRow("Aucune couleur de plus", new SpeciesIndexPanel.Line { Label = "Pléo.", Words = "pas de pléochroïsme : les deux fenêtres montrent la même couleur", TextRows = 3 });
        index.AddRow("Dichroïque", new SpeciesIndexPanel.Line { Label = "Pléo.", Words = "deux couleurs différentes (pierres uniaxes)", TextRows = 2 });
        index.AddRow("Trichroïque", new SpeciesIndexPanel.Line { Label = "Pléo.", Words = "trois couleurs différentes (pierres biaxes)", TextRows = 2 });

        var stones = new List<GemDefinition>();
        foreach (var definition in database.allGems)
            if (definition != null) stones.Add(definition);
        stones.Sort((a, b) => string.CompareOrdinal(TextEntry.Normalize(a.displayName), TextEntry.Normalize(b.displayName)));
        index.AddHeader("Pierres, de A à Z");
        foreach (var definition in stones)
        {
            var info = PleochroismInfo.ForGem(definition);
            if (!info.Pleochroic)
            {
                index.AddRow(definition.displayName, new SpeciesIndexPanel.Line { Label = "Pléo.", Words = "aucun", Swatches = new[] { info.X } });
                continue;
            }

            var trichroic = !Same(info.X, info.Y) && !Same(info.Y, info.Z) && !Same(info.X, info.Z);
            var swatches = trichroic ? new[] { info.X, info.Y, info.Z } : new[] { info.X, info.Z };
            index.AddRow(definition.displayName,
                new SpeciesIndexPanel.Line { Label = "Pléo.", Words = trichroic ? "3 couleurs" : "2 couleurs", Swatches = swatches },
                new SpeciesIndexPanel.Line { Label = "Texte", Words = string.IsNullOrEmpty(definition.pleochroicDescription) ? "—" : definition.pleochroicDescription, TextRows = 2 });
        }
        index.Finish();
    }

    public void Open()
    {
        stone = null;
        BuildIndex();
        index?.ResetSearch();
        panel.SetActive(true);
        InventoryUI.Instance?.Open();
        Refresh();
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private static uint StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 16777619u;
        }
        return hash;
    }

    private static float Unit(uint hash, int salt)
    {
        var h = hash ^ (uint)(salt * 2654435761u);
        h ^= h >> 13;
        h *= 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFF) / 65535f;
    }

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        optics = TreatmentSignatures.Pleochroism(gem);

        // The principal directions of the indicatrix sit at a fixed, specimen-specific orientation in the stone.
        opticFrame = StoneOptics.OpticFrame(gem);
        orientation = Quaternion.identity;
        explored = 0f;
        observed = gem.HasObserved(ExaminationTool.MeasurementType.Pleochroism.ToString());
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void ResetOrientation()
    {
        orientation = Quaternion.identity;
        UpdateView();
    }

    private void OnDragDelta(Vector2 delta)
    {
        if (stone == null) return;

        var yaw = Quaternion.AngleAxis(-delta.x * 0.7f, Vector3.up);
        var pitch = Quaternion.AngleAxis(delta.y * 0.7f, Vector3.right);
        orientation = yaw * pitch * orientation;
        explored += delta.magnitude * 0.7f;
        AfterRotation();
    }

    private void Roll(float degrees)
    {
        if (stone == null) return;

        orientation = Quaternion.AngleAxis(degrees, Vector3.forward) * orientation;
        explored += Mathf.Abs(degrees);
        AfterRotation();
    }

    private void AfterRotation()
    {
        if (!observed && explored >= ConclusionRotation) FinishObservation();
        UpdateView();
    }

    private void FinishObservation()
    {
        observed = true;
        stone.RecordObservation(ExaminationTool.MeasurementType.Pleochroism.ToString());
        StoryState.Instance?.OnObserved(stone, ExaminationTool.MeasurementType.Pleochroism.ToString());
        var def = stone.Definition;
        ToolFeedbackUI.Instance?.ShowMessage(
            $"Dichroscope — {(def.pleochroic ? def.pleochroicDescription : "Aucun pléochroïsme visible.")}");
        UpdateTexts();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone)) stone = null;

        if (stone == null)
        {
            leftStone.enabled = false;
            rightStone.enabled = false;
            hintText.text = "Déposez une gemme sur la lampe";
        }
        else
        {
            leftStone.enabled = true;
            rightStone.enabled = true;
            hintText.text = "";
        }

        UpdateView();
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        if (stone == null)
        {
            resultText.text = "Aucune gemme sur la lampe.";
            return;
        }

        var def = stone.Definition;
        resultText.text = observed
            ? (def.pleochroic ? def.pleochroicDescription : "Aucun pléochroïsme visible.")
            : "Tournez la pierre sous tous les angles pour comparer les couleurs des deux fenêtres.";
    }

    private void UpdateView()
    {
        if (stone == null)
        {
            contrastFill.rectTransform.sizeDelta = new Vector2(0f, 12f);
            contrastText.text = "";
            return;
        }

        // The two windows look through polarizers vibrating along world X and world Y.
        var left = optics.Seen(orientation, opticFrame, Vector3.right);
        var right = optics.Seen(orientation, opticFrame, Vector3.up);
        leftStone.color = left;
        rightStone.color = right;

        // One 3D render, shown in both windows with their own tint.
        view3D.Draw(GemView3D.MeshFor(stone), GemView3D.HalfExtents(stone), orientation, Color.white, new Color(0.9f, 0.9f, 0.94f));
        var longest = Mathf.Max(stone.LengthMm, stone.WidthMm, 0.1f);
        var k = Mathf.Min(14f, 190f / longest);
        var side = GemView3D.MaxDimensionMm(stone) * k / GemView3D.FitFraction;
        foreach (var rect in new[] { leftStoneRect, rightStoneRect })
        {
            rect.sizeDelta = new Vector2(side, side);
            rect.localEulerAngles = Vector3.zero;
        }

        var difference = Mathf.Clamp01(Mathf.Sqrt(
            Mathf.Pow(left.r - right.r, 2f) + Mathf.Pow(left.g - right.g, 2f) + Mathf.Pow(left.b - right.b, 2f)) / 0.9f);
        contrastFill.rectTransform.sizeDelta = new Vector2(216f * difference, 12f);
        contrastText.text = difference < 0.06f ? "Écart : nul" : difference < 0.3f ? "Écart : faible" : difference < 0.6f ? "Écart : net" : "Écart : fort";
    }

    private static bool ContainsGem(GemInstance gem)
    {
        if (PlayerInventory.Instance == null) return false;
        foreach (var g in PlayerInventory.Instance.Gems)
        {
            if (ReferenceEquals(g, gem)) return true;
        }
        return false;
    }

    // ---- UI construction -------------------------------------------------------------------------------------

    private void BuildPanel()
    {
        panel = new GameObject("DichroscopePanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-30f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Dichroscope", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildLamp();
        BuildSidePanel();
    }

    private void BuildLamp()
    {
        // The stone is drawn in 3D pixel art (white, tinted per window), so it has real volume when turned.
        view3D = new GemView3D(48);

        // Flat light box: dark bezel, bright surface. The whole area takes drops and drags.
        var bezel = RuntimeUIKit.MakeBox("Bezel", panel.transform, new Vector2(LampX, LampY), new Vector2(LampSize, LampSize), new Color(0.07f, 0.07f, 0.08f, 1f));
        bezel.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        var drag = bezel.gameObject.AddComponent<DichroscopeDragArea>();
        drag.OnDragDelta = OnDragDelta;
        drag.OnRoll = Roll;

        var surface = RuntimeUIKit.MakeBox("Surface", panel.transform, new Vector2(LampX + 14f, LampY - 14f), new Vector2(LampSize - 28f, LampSize - 28f),
            new Color(0.97f, 0.97f, 0.94f, 1f), false);
        surface.raycastTarget = false;

        var center = new Vector2(LampX + LampSize / 2f, LampY - LampSize / 2f);

        // Two half-windows, each clipped to its side of the dichroscope aperture.
        leftStone = BuildHalf("LeftHalf", new Vector2(center.x - WindowRadius, center.y + WindowRadius), true, out leftStoneRect);
        rightStone = BuildHalf("RightHalf", new Vector2(center.x, center.y + WindowRadius), false, out rightStoneRect);

        // Dichroscope body: dark plate with a round aperture, plus the dividing line between the two images.
        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(frame.GetComponent<RectTransform>(), new Vector2(LampX + 14f, LampY - 14f), new Vector2(LampSize - 28f, LampSize - 28f));
        var frameImage = frame.GetComponent<Image>();
        frameImage.sprite = MakeApertureSprite(256, WindowRadius / ((LampSize - 28f) / 2f));
        frameImage.raycastTarget = false;

        var divider = RuntimeUIKit.MakeBox("Divider", panel.transform, new Vector2(center.x - 1f, center.y + WindowRadius), new Vector2(2f, WindowRadius * 2f),
            new Color(0.16f, 0.17f, 0.19f, 1f), false);
        divider.raycastTarget = false;

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.45f, 0.47f, 0.52f),
            new Vector2(LampX + 14f, LampY - LampSize + 44f), new Vector2(LampSize - 28f, 18f));

        RuntimeUIKit.MakeText("Label", panel.transform, "Dichroscope", 11, TextAnchor.UpperLeft, new Color(0.7f, 0.72f, 0.78f),
            new Vector2(LampX + 22f, LampY - 22f), new Vector2(120f, 16f));

        // Rotation helpers below the lamp.
        var rollLeft = RuntimeUIKit.MakeButton("RollLeft", panel.transform, "Rouler  <", 12, ButtonColor,
            new Vector2(LampX, LampY - LampSize - 8f), new Vector2(96f, 28f));
        rollLeft.onClick.AddListener(() => Roll(15f));
        var rollRight = RuntimeUIKit.MakeButton("RollRight", panel.transform, "Rouler  >", 12, ButtonColor,
            new Vector2(LampX + 106f, LampY - LampSize - 8f), new Vector2(96f, 28f));
        rollRight.onClick.AddListener(() => Roll(-15f));
        var reset = RuntimeUIKit.MakeButton("Reset", panel.transform, "Remettre à plat", 12, ButtonColor,
            new Vector2(LampX + 212f, LampY - LampSize - 8f), new Vector2(108f, 28f));
        reset.onClick.AddListener(ResetOrientation);
    }

    private Image BuildHalf(string name, Vector2 topLeft, bool isLeft, out RectTransform stoneRect)
    {
        var clip = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
        clip.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(clip.GetComponent<RectTransform>(), topLeft, new Vector2(WindowRadius, WindowRadius * 2f));

        var stoneGO = new GameObject("Stone", typeof(RectTransform), typeof(Image));
        stoneGO.transform.SetParent(clip.transform, false);
        stoneRect = stoneGO.GetComponent<RectTransform>();
        stoneRect.anchorMin = stoneRect.anchorMax = new Vector2(0f, 1f);
        stoneRect.pivot = new Vector2(0.5f, 0.5f);
        // The window center sits on the right edge of the left half and on the left edge of the right half.
        stoneRect.anchoredPosition = new Vector2(isLeft ? WindowRadius : 0f, -WindowRadius);
        stoneRect.sizeDelta = new Vector2(100f, 80f);
        var image = stoneGO.GetComponent<Image>();
        image.raycastTarget = false;
        image.enabled = false;
        image.sprite = view3D.Sprite;
        return image;
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("ResultHeader", panel.transform, "Observation", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(364f, -44f), new Vector2(220f, 20f));
        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(360f, -68f), new Vector2(224f, 112f), new Color(0.04f, 0.05f, 0.07f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(368f, -74f), new Vector2(208f, 100f));

        contrastText = RuntimeUIKit.MakeText("ContrastLabel", panel.transform, "", 12, TextAnchor.UpperLeft, Color.white,
            new Vector2(364f, -156f), new Vector2(200f, 16f));
        RuntimeUIKit.MakeBox("ContrastBack", panel.transform, new Vector2(364f, -176f), new Vector2(216f, 12f), new Color(0.04f, 0.05f, 0.07f, 1f), false);
        contrastFill = RuntimeUIKit.MakeBox("ContrastFill", panel.transform, new Vector2(364f, -176f), new Vector2(0f, 12f), new Color(0.95f, 0.78f, 0.25f, 1f), false);

        RuntimeUIKit.MakeText("Tip", panel.transform,
            "Faites glisser la souris sur la lampe pour incliner la pierre dans tous les sens, la molette la fait pivoter à plat. " +
            "Comparez les couleurs des deux fenêtres.", 11, TextAnchor.UpperLeft, new Color(0.6f, 0.62f, 0.68f),
            new Vector2(360f, -204f), new Vector2(224f, 90f));

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonColor,
            new Vector2(364f, -336f), new Vector2(216f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonColor,
            new Vector2(364f, -370f), new Vector2(216f, 28f));
        close.onClick.AddListener(Close);
    }

    // Square dark plate with a round hole (the dichroscope aperture), antialiased edge.
    private static Sprite MakeApertureSprite(int size, float holeRatio)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x - center) / center;
                var dy = (y - center) / center;
                var r = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.Clamp01((r - holeRatio) * center + 0.5f);        // 0 inside the hole
                var rim = Mathf.Abs(r - holeRatio) < 0.015f ? 0.32f : 0.20f + 0.03f * (-dx - dy) * 0.5f;
                texture.SetPixel(x, y, new Color(rim, rim, rim * 1.05f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
