using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReportUIController : MonoBehaviour
{
    public static ReportUIController Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text measurementsText;
    [SerializeField] private Text resultText;

    private PlayerGemHolder currentHolder;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Open(PlayerGemHolder holder)
    {
        currentHolder = holder;
        resultText.text = string.Empty;

        var gem = holder.CurrentGem;
        var measured = new List<string>();
        if (holder.HasMeasured(ExaminationTool.MeasurementType.Hardness.ToString()))
            measured.Add($"Dureté ≈ {gem.hardness}");
        if (holder.HasMeasured(ExaminationTool.MeasurementType.RefractiveIndex.ToString()))
            measured.Add($"Indice de réfraction ≈ {gem.refractiveIndex}");
        if (holder.HasMeasured(ExaminationTool.MeasurementType.Density.ToString()))
            measured.Add($"Densité ≈ {gem.density}");

        measurementsText.text = measured.Count > 0
            ? "Mesures relevées :\n" + string.Join("\n", measured)
            : "Aucune mesure effectuée. Retourne examiner la gemme.";

        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        currentHolder = null;
    }

    public void SubmitSpecies(string speciesName)
    {
        if (currentHolder == null || currentHolder.CurrentGem == null) return;

        var gem = currentHolder.CurrentGem;
        if (speciesName == gem.displayName)
        {
            resultText.text = $"Rapport validé par le directeur ! +{gem.baseValue}€";
            currentHolder.GetComponent<PlayerWallet>()?.AddMoney(gem.baseValue);
            currentHolder.ClearGem();
        }
        else
        {
            resultText.text = "Le directeur rejette le rapport : identification incorrecte.";
        }
    }
}
