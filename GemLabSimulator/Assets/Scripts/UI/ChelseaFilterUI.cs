using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Chelsea filter: drop a gem under the filter and switch on a reflected light (torch). Stones that react
// (chromium) turn red/pink, the others stay a dull green or dark.
public class ChelseaFilterUI : MonoBehaviour
{
    public static ChelseaFilterUI Instance { get; private set; }

    private const float PanelWidth = 560f;
    private const float PanelHeight = 400f;
    private const float FadeSpeed = 3f;

    private static readonly Color PanelColor = new Color(0.09f, 0.10f, 0.12f, 0.97f);
    private static readonly Color Gold = new Color(0.95f, 0.85f, 0.65f);
    private static readonly Color ButtonOff = new Color(0.25f, 0.25f, 0.32f, 1f);
    private static readonly Color ButtonOn = new Color(0.78f, 0.62f, 0.12f, 1f);
    private static readonly Color DullGreen = new Color(0.14f, 0.40f, 0.18f);

    private GameObject panel;
    private Image fieldImage;
    private Image litField;
    private Image stoneBase;
    private Image stoneDull;
    private Image stoneReaction;
    private Image glowImage;
    private Image beamImage;
    private Image lensImage;
    private Text hintText;
    private Text resultText;
    private Button lightButton;
    private Text lightButtonLabel;

    private GemInstance stone;
    private bool lightOn;
    private float lightLevel;
    private ChelseaInfo info;

    public static bool TryOpen()
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            canvas.AddComponent<ChelseaFilterUI>();
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

    private static string ReactionWords(ChelseaInfo info)
    {
        if (!info.Reacts) return "pas de rouge";
        Color.RGBToHSV(info.Color, out var hue, out var saturation, out _);
        var name = hue > 0.04f && hue < 0.12f ? "orangé" : saturation < 0.65f ? "rose" : "rouge";
        var strength = info.Intensity >= 0.9f ? "vif" : info.Intensity >= 0.6f ? "marqué" : info.Intensity >= 0.3f ? "modéré" : "faible";
        return $"{name}, {strength}";
    }

    // What the stone looks like through the filter: the dull tint, brought toward the colour of the reaction.
    private static Color ReactionChip(ChelseaInfo info)
    {
        var dull = info.Dull.a > 0f ? info.Dull : DullGreen;
        return info.Reacts ? Color.Lerp(dull, info.Color, Mathf.Clamp01(info.Intensity)) : dull;
    }

    private void BuildIndex()
    {
        if (index != null) return;
        var database = GemDatabase.Instance;
        if (database == null) return;

        index = SpeciesIndexPanel.Create(panel.transform, new Vector2(-206f, 0f), new Vector2(198f, PanelHeight), "Cas possibles",
            "Ce que chaque pierre devient sous le filtre.", PanelColor, Gold);

        var species = new HashSet<string>();
        var stones = new List<GemDefinition>();
        foreach (var definition in database.allGems)
            if (definition != null) { stones.Add(definition); species.Add(definition.name); }

        // The cases of the table of the course that are not a species of their own (a synthetic spinel, a blue topaz, the glasses...).
        index.AddHeader("Cas du tableau du cours");
        var special = new List<ChelseaEntry>();
        foreach (var entry in ChelseaLibrary.All)
            if (!species.Contains(entry.key)) special.Add(entry);
        special.Sort((a, b) => string.CompareOrdinal(TextEntry.Normalize(a.key), TextEntry.Normalize(b.key)));
        foreach (var entry in special)
        {
            var info = ChelseaInfo.FromEntry(entry);
            index.AddRow(entry.key.Replace('_', ' '), new SpeciesIndexPanel.Line { Label = "Filtre", Words = ReactionWords(info), Chip = ReactionChip(info) });
        }

        stones.Sort((a, b) => string.CompareOrdinal(TextEntry.Normalize(a.displayName), TextEntry.Normalize(b.displayName)));
        index.AddHeader("Pierres, de A à Z");
        foreach (var definition in stones)
        {
            var entry = ChelseaLibrary.Entry(definition.name);
            var info = entry != null ? ChelseaInfo.FromEntry(entry) : ChelseaInfo.ForGem(definition);
            index.AddRow(definition.displayName, new SpeciesIndexPanel.Line { Label = "Filtre", Words = ReactionWords(info), Chip = ReactionChip(info) });
        }
        index.Finish();
    }

    public void Open()
    {
        stone = null;
        lightOn = false;
        lightLevel = 0f;
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
        lightOn = false;
        GemTooltipUI.Instance?.Hide();
    }

    private void RefreshIfOpen()
    {
        if (panel != null && panel.activeSelf) Refresh();
    }

    // ---- Behaviour -------------------------------------------------------------------------------------------

    private void SetStone(GemInstance gem)
    {
        stone = gem;
        info = TreatmentSignatures.Chelsea(gem);
        Refresh();
    }

    private void ClearStone()
    {
        stone = null;
        Refresh();
    }

    private void ToggleLight()
    {
        lightOn = !lightOn;

        if (lightOn && stone != null)
        {
            stone.RecordObservation(ExaminationTool.MeasurementType.ChelseaFilter.ToString());
            StoryState.Instance?.OnObserved(stone, ExaminationTool.MeasurementType.ChelseaFilter.ToString());
            ToolFeedbackUI.Instance?.ShowMessage($"Filtre Chelsea — {ChelseaLibrary.Reaction(stone)}");
        }

        Refresh();
    }

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        lightLevel = Mathf.MoveTowards(lightLevel, lightOn ? 1f : 0f, FadeSpeed * Time.deltaTime);
        ApplyVisuals();
    }

    private void Refresh()
    {
        if (stone != null && !ContainsGem(stone))
        {
            stone = null;
        }

        if (stone == null)
        {
            stoneBase.enabled = false;
            stoneDull.enabled = false;
            stoneReaction.enabled = false;
            hintText.text = "Déposez une gemme sous le filtre";
        }
        else
        {
            var shape = stone.IsRough ? "Rond" : stone.ShapeName;
            var style = stone.IsRough ? "" : stone.CutStyleName;
            stoneBase.enabled = true;
            stoneDull.enabled = true;
            stoneReaction.enabled = info.Reacts;
            stoneBase.sprite = SolidOf(stone, shape, style, new Color(0.02f, 0.03f, 0.02f), new Color(0.06f, 0.08f, 0.06f));
            var dull = info.Dull.a > 0f ? info.Dull : DullGreen;
            stoneDull.sprite = SolidOf(stone, shape, style, dull, Color.Lerp(dull, Color.white, 0.25f));
            if (info.Reacts)
                stoneReaction.sprite = SolidOf(stone, shape, style, info.Color, Color.Lerp(info.Color, Color.white, 0.4f));
            hintText.text = "";
        }

        lightButton.GetComponent<Image>().color = lightOn ? ButtonOn : ButtonOff;
        lightButtonLabel.text = lightOn ? "Éteindre la maglite" : "Allumer la maglite";
        resultText.text = BuildResultText();
        ApplyVisuals();
    }

    private string BuildResultText()
    {
        if (stone == null) return "Aucune gemme sous le filtre.";
        if (!stone.HasObserved(ExaminationTool.MeasurementType.ChelseaFilter.ToString()))
            return "Allumez la lumière réfléchie pour observer la gemme à travers le filtre.";
        return ChelseaLibrary.Reaction(stone);
    }

    private void ApplyVisuals()
    {
        // The torch lights the field (bright green center like the real filter) and the stone.
        litField.color = new Color(1f, 1f, 1f, lightLevel);
        glowImage.color = new Color(1f, 0.95f, 0.7f, 0.35f * lightLevel);
        beamImage.color = new Color(1f, 0.95f, 0.65f, 0.28f * lightLevel);
        lensImage.color = Color.Lerp(new Color(0.35f, 0.33f, 0.25f, 1f), new Color(1f, 0.96f, 0.6f, 1f), lightLevel);

        if (stone == null) return;

        stoneDull.color = new Color(1f, 1f, 1f, lightLevel * 0.85f);
        if (info.Reacts)
            stoneReaction.color = new Color(1f, 1f, 1f, lightLevel * Mathf.Clamp01(info.Intensity));
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
        panel = new GameObject("ChelseaFilterPanel", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        // Shifted left so the inventory panel (right side of the screen) stays reachable for drag and drop.
        rect.anchoredPosition = new Vector2(-80f, 0f);

        RuntimeUIKit.MakeBox("Body", panel.transform, Vector2.zero, new Vector2(PanelWidth, PanelHeight), PanelColor);
        RuntimeUIKit.MakeText("Title", panel.transform, "Filtre Chelsea", 20, TextAnchor.UpperLeft, Gold,
            new Vector2(24f, -12f), new Vector2(300f, 28f));

        BuildFilter();
        BuildSidePanel();
    }

    private void BuildFilter()
    {
        // White folding case with the filter window, like the real instrument.
        RuntimeUIKit.MakeBox("Case", panel.transform, new Vector2(20f, -44f), new Vector2(300f, 340f), new Color(0.93f, 0.93f, 0.95f, 1f));

        fieldImage = MakeDisc("Field", new Vector2(60f, -62f), 220f, MakeFieldSprite(128, false));
        fieldImage.gameObject.AddComponent<LapidarySlotDropZone>().OnGemDropped = SetStone;
        fieldImage.gameObject.AddComponent<Button>().onClick.AddListener(ClearStone);

        litField = MakeDisc("LitField", new Vector2(60f, -62f), 220f, MakeFieldSprite(128, true));
        litField.raycastTarget = false;

        stoneBase = MakeStoneImage("StoneBase", new Vector2(130f, -132f));
        stoneDull = MakeStoneImage("StoneDull", new Vector2(130f, -132f));
        stoneReaction = MakeStoneImage("StoneReaction", new Vector2(130f, -132f));

        hintText = RuntimeUIKit.MakeText("Hint", panel.transform, "", 12, TextAnchor.MiddleCenter, new Color(0.35f, 0.5f, 0.35f),
            new Vector2(60f, -250f), new Vector2(220f, 18f));

        var engraved = RuntimeUIKit.MakeText("Engraved", panel.transform, "CHELSEA FILTER", 17, TextAnchor.MiddleCenter, new Color(0.08f, 0.08f, 0.1f),
            new Vector2(20f, -312f), new Vector2(300f, 28f));
        engraved.fontStyle = FontStyle.Bold;
        RuntimeUIKit.MakeBox("ScrewLeft", panel.transform, new Vector2(40f, -320f), new Vector2(12f, 12f), new Color(0.62f, 0.64f, 0.68f, 1f));
        RuntimeUIKit.MakeBox("ScrewRight", panel.transform, new Vector2(288f, -320f), new Vector2(12f, 12f), new Color(0.62f, 0.64f, 0.68f, 1f));
    }

    private Image MakeDisc(string name, Vector2 topLeft, float size, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), topLeft, new Vector2(size, size));
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private Image MakeStoneImage(string name, Vector2 topLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(go.GetComponent<RectTransform>(), topLeft, new Vector2(80f, 80f));
        var image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private void BuildSidePanel()
    {
        RuntimeUIKit.MakeText("LightHeader", panel.transform, "Lumière réfléchie", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(344f, -44f), new Vector2(200f, 20f));

        // Torch drawn from boxes: body, head, lens and the beam when it is on.
        RuntimeUIKit.MakeBox("TorchBody", panel.transform, new Vector2(344f, -74f), new Vector2(120f, 22f), new Color(0.16f, 0.16f, 0.19f, 1f));
        RuntimeUIKit.MakeBox("TorchHead", panel.transform, new Vector2(464f, -68f), new Vector2(26f, 34f), new Color(0.55f, 0.56f, 0.60f, 1f));
        lensImage = RuntimeUIKit.MakeBox("TorchLens", panel.transform, new Vector2(490f, -72f), new Vector2(6f, 26f), new Color(0.35f, 0.33f, 0.25f, 1f), false);

        var glow = new GameObject("TorchGlow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(panel.transform, false);
        RuntimeUIKit.SetTopLeft(glow.GetComponent<RectTransform>(), new Vector2(472f, -42f), new Vector2(84f, 84f));
        glowImage = glow.GetComponent<Image>();
        glowImage.sprite = RuntimeUIKit.Glow;
        glowImage.raycastTarget = false;
        glowImage.color = new Color(1f, 0.95f, 0.7f, 0f);

        beamImage = RuntimeUIKit.MakeBox("Beam", panel.transform, new Vector2(498f, -66f), new Vector2(40f, 38f), new Color(1f, 0.95f, 0.65f, 0f), false);
        beamImage.raycastTarget = false;

        lightButton = RuntimeUIKit.MakeButton("Light", panel.transform, "Allumer la maglite", 14, ButtonOff,
            new Vector2(344f, -124f), new Vector2(200f, 36f));
        lightButtonLabel = lightButton.GetComponentInChildren<Text>();
        lightButton.onClick.AddListener(ToggleLight);

        RuntimeUIKit.MakeText("ResultHeader", panel.transform, "Observation", 14, TextAnchor.UpperLeft, Gold,
            new Vector2(344f, -176f), new Vector2(200f, 20f));
        RuntimeUIKit.MakeBox("ResultBox", panel.transform, new Vector2(340f, -200f), new Vector2(208f, 96f), new Color(0.04f, 0.05f, 0.07f, 1f));
        resultText = RuntimeUIKit.MakeText("Result", panel.transform, "", 12, TextAnchor.UpperLeft, new Color(0.85f, 0.92f, 1f),
            new Vector2(348f, -206f), new Vector2(192f, 84f));

        var remove = RuntimeUIKit.MakeButton("Remove", panel.transform, "Retirer la gemme", 13, ButtonOff,
            new Vector2(344f, -312f), new Vector2(200f, 28f));
        remove.onClick.AddListener(ClearStone);
        var close = RuntimeUIKit.MakeButton("Close", panel.transform, "Fermer", 13, ButtonOff,
            new Vector2(344f, -346f), new Vector2(200f, 28f));
        close.onClick.AddListener(Close);
    }

    // Filter window: nearly black when unlit, with a bright green core like the real filter when lit.
    private static Sprite MakeFieldSprite(int size, bool lit)
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
                var edge = Mathf.Clamp01((1f - r) * center);
                if (r > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                if (!lit)
                {
                    texture.SetPixel(x, y, new Color(0.01f, 0.03f, 0.015f, edge));
                    continue;
                }

                // Hot spot slightly below the center, fading to a dark rim.
                var hx = dx;
                var hy = dy + 0.25f;
                var hot = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx + hy * hy) * 1.15f);
                var g = 0.10f + 0.80f * hot * hot;
                texture.SetPixel(x, y, new Color(0.0f, g, g * 0.12f, edge * Mathf.Clamp01(0.25f + hot * 1.4f)));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // The flat sprite of a stone for the effects of the lamps: a rough crystal shows its habit, the other stones their shape and cut style.
    private static Sprite SolidOf(GemInstance stone, string shape, string style, Color body, Color facet)
    {
        var habit = stone != null && stone.IsRough ? stone.Habit : null;
        var crystal = habit != null ? GemIconFactory.GetHabitSolid(habit.Id, body, facet) : null;
        return crystal != null ? crystal : GemIconFactory.GetSolid(shape, style, body, facet);
    }
}
