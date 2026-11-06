using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrimoireUI : MonoBehaviour
{
    public static GrimoireUI Instance { get; private set; }

    private const int Columns = 5;
    private const float CardSize = 96f;
    private const float CardSpacing = 10f;

    [SerializeField] private GameObject gridPanel;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private Text progressText;
    [SerializeField] private Sprite lockedIcon;

    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Text detailTitleText;
    [SerializeField] private Text detailBodyText;
    [SerializeField] private Image detailSpectrumImage;

    private ScrollRect bodyScroll;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gridPanel != null) gridPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);
        if (GemCodex.Instance != null) GemCodex.Instance.OnCodexChanged += RefreshIfOpen;
    }

    private void OnDestroy()
    {
        if (GemCodex.Instance != null) GemCodex.Instance.OnCodexChanged -= RefreshIfOpen;
    }

    // The grimoire lives in a tab of the bag (G opens it there): the bag takes its two windows over.
    public GameObject GridPanel => gridPanel;
    public GameObject DetailPanel => detailPanel;

    public void OpenInBag()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
        Refresh();
    }

    private void Toggle()
    {
        if (detailPanel != null && detailPanel.activeSelf)
        {
            HideDetail();
            return;
        }

        var opening = gridPanel != null && !gridPanel.activeSelf;
        if (gridPanel != null) gridPanel.SetActive(opening);
        if (opening) Refresh();
    }

    private void RefreshIfOpen()
    {
        if (gridPanel != null && gridPanel.activeSelf) Refresh();
    }

    private void Refresh()
    {
        if (contentContainer == null || GemDatabase.Instance == null) return;

        for (var i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        var allGems = GemDatabase.Instance.allGems;
        var discoveredCount = 0;

        for (var i = 0; i < allGems.Count; i++)
        {
            var def = allGems[i];
            if (def == null) continue;
            var discovered = GemCodex.Instance != null && GemCodex.Instance.IsDiscovered(def.name);
            if (discovered) discoveredCount++;
            CreateCard(def, discovered, i);
        }

        var totalRows = Mathf.CeilToInt(allGems.Count / (float)Columns);
        contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, totalRows * (CardSize + CardSpacing) + 10f);

        if (progressText != null)
            progressText.text = $"{discoveredCount} / {allGems.Count} espèces découvertes";
    }

    private void CreateCard(GemDefinition def, bool discovered, int index)
    {
        var col = index % Columns;
        var row = index / Columns;
        var x = 10f + col * (CardSize + CardSpacing);
        var y = -10f - row * (CardSize + CardSpacing);

        var go = new GameObject($"Card_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(contentContainer, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(CardSize, CardSize);
        go.GetComponent<Image>().color = discovered
            ? new Color(0.20f, 0.20f, 0.24f, 1f)
            : new Color(0.10f, 0.10f, 0.12f, 1f);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0, -6f);
        iconRect.sizeDelta = new Vector2(CardSize - 24f, CardSize - 24f);
        var iconImage = iconGO.GetComponent<Image>();
        iconImage.sprite = discovered ? def.GetIcon() : lockedIcon;
        iconImage.preserveAspect = true;

        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(go.transform, false);
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0, 2f);
        nameRect.sizeDelta = new Vector2(0, 22f);
        var nameText = nameGO.AddComponent<Text>();
        nameText.text = discovered ? def.displayName : "???";
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 10;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = discovered ? Color.white : new Color(0.5f, 0.5f, 0.5f);

        var button = go.GetComponent<Button>();
        if (discovered)
        {
            var assetName = def.name;
            button.onClick.AddListener(() => ShowDetail(assetName));
        }
        else
        {
            button.interactable = false;
        }
    }

    public void ShowDetail(string definitionAssetName)
    {
        var def = GemDatabase.Instance != null ? GemDatabase.Instance.FindByAssetName(definitionAssetName) : null;
        if (def == null) return;

        if (gridPanel != null) gridPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailTitleText != null) detailTitleText.text = def.displayName;
        EnsureScroll();
        if (detailBodyText != null)
        {
            detailBodyText.text = BuildDetailText(def);
            if (bodyScroll != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailBodyText.rectTransform);
                bodyScroll.verticalNormalizedPosition = 1f;
            }
        }
        if (detailSpectrumImage != null) detailSpectrumImage.sprite = SpectrumRenderer.BuildSpectrumSprite(def.spectrumLines);
    }

    // The sheet is long once the instruments of the laboratory are in it: the text goes into a scroll view (mouse wheel, or drag).
    private void EnsureScroll()
    {
        if (bodyScroll != null || detailBodyText == null) return;

        var body = detailBodyText.rectTransform;
        var viewGO = new GameObject("BodyScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewGO.transform.SetParent(body.parent, false);
        var view = viewGO.GetComponent<RectTransform>();
        view.anchorMin = body.anchorMin;
        view.anchorMax = body.anchorMax;
        view.pivot = body.pivot;
        view.anchoredPosition = body.anchoredPosition;
        view.sizeDelta = body.sizeDelta;
        viewGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);

        body.SetParent(viewGO.transform, false);
        body.anchorMin = new Vector2(0f, 1f);
        body.anchorMax = new Vector2(1f, 1f);
        body.pivot = new Vector2(0.5f, 1f);
        body.anchoredPosition = Vector2.zero;
        body.sizeDelta = new Vector2(-18f, 0f);
        detailBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        detailBodyText.supportRichText = true;
        var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bodyScroll = viewGO.GetComponent<ScrollRect>();
        bodyScroll.content = body;
        bodyScroll.horizontal = false;
        bodyScroll.vertical = true;
        bodyScroll.movementType = ScrollRect.MovementType.Clamped;
        bodyScroll.scrollSensitivity = 30f;
    }

    public void HideDetail()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
    }

    public void Close()
    {
        if (gridPanel != null) gridPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    // The whole sheet of a species: its properties, the treatments that can be done to it, what the instruments of the laboratory show.
    public static string BuildDetailText(GemDefinition def)
    {
        return BuildSheetText(def) + "\n\n" + BuildTreatmentsText(def) + BuildInstrumentsText(def, true);
    }

    // The properties of the species and its notes.
    public static string BuildSheetText(GemDefinition def)
    {
        var lines = new List<string>
        {
            $"Éclat : {def.luster}",
            $"Couleur : {def.color}",
            $"Forme : {def.shape}    Style de taille : {def.cutStyle}",
            $"Transparence : {def.TransparencyLabel}",
            $"Indice de réfraction : {def.refractiveIndex:F3}",
            $"Biréfringence : {OpticalDescriptions.BirefringenceText(def)}",
            $"Densité : {def.density:F2}",
            $"Dureté (Mohs) : {def.hardness:F2}",
            $"Caractère optique : {def.opticalCharacter}",
            def.pleochroic ? $"Pléochroïsme : {def.pleochroicDescription}" : "Pléochroïsme : Aucun.",
            $"Fluorescence UV longue onde : {def.fluorescenceLongWave}",
            $"Fluorescence UV courte onde : {def.fluorescenceShortWave}",
            $"Spectre d'absorption : {def.absorptionSpectrum}",
            $"Inclusions typiques : {def.inclusions}",
            $"Figure d'interférence : {def.interferenceFigure}",
            $"Filtre Chelsea : {def.chelseaFilterReaction}",
            $"Magnétisme : {def.magnetism}",
            $"Conductivité thermique : {(def.isThermallyConductiveLikeDiamond ? "élevée, comme le diamant" : "faible")}",
            $"Conductivité électrique : {(def.isElectricallyConductive ? "conductrice" : "isolante")}",
            $"Valeur de base : {def.baseValue}€/ct (avant modificateurs d'origine, pureté, taille...)",
        };

        var text = string.Join("\n", lines);
        if (!string.IsNullOrEmpty(def.description))
            text += $"\n\nNotes : {def.description}";
        return text;
    }

    // The treatments the laboratory can apply to the species.
    public static string BuildTreatmentsText(GemDefinition def)
    {
        var text = "<color=#F2D9A6><b>Traitements possibles</b></color>";
        foreach (var line in TreatmentReference.Lines(def)) text += $"\n· {line}";
        return text;
    }

    // What the advanced instruments of the laboratory show for the species (empty when there is nothing to show).
    public static string BuildInstrumentsText(GemDefinition def, bool leadingBreak)
    {
        var text = "";
        var entries = LabFindings.ReferenceEntries(def);
        if (entries.Count > 0)
        {
            var origin = LabFindings.ReferenceOrigin(def);
            text += (leadingBreak ? "\n\n" : "") + "<color=#F2D9A6><b>Instruments du laboratoire</b></color>\n";
            text += $"<color=#9AA3B5>Pierre de référence naturelle, non traitée{(string.IsNullOrEmpty(origin) ? "" : $", d'origine {origin}")} : une pierre réelle varie selon son gisement, sa couleur et ses traitements.</color>";
            foreach (var entry in entries)
            {
                text += $"\n\n<color=#F2D9A6><b>{entry.Title}</b></color>";
                foreach (var line in entry.Lines) text += $"\n· {line}";
            }
        }
        return text;
    }
}
