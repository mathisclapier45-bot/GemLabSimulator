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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            Toggle();
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
        if (detailBodyText != null) detailBodyText.text = BuildDetailText(def);
        if (detailSpectrumImage != null) detailSpectrumImage.sprite = SpectrumRenderer.BuildSpectrumSprite(def.spectrumLines);
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

    private static string BuildDetailText(GemDefinition def)
    {
        var lines = new List<string>
        {
            $"Éclat : {def.luster}",
            $"Couleur : {def.color}",
            $"Forme : {def.shape}    Style de taille : {def.cutStyle}",
            $"Transparence : {def.transparency}",
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
            $"Valeur de base : {def.baseValue}€/ct (avant modificateurs d'origine, pureté, taille...)",
        };

        var text = string.Join("\n", lines);
        if (!string.IsNullOrEmpty(def.description))
            text += $"\n\nNotes : {def.description}";
        return text;
    }
}
