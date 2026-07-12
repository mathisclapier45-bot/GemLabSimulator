using UnityEngine;
using UnityEngine.UI;

public class SpectrumUI : MonoBehaviour
{
    public static SpectrumUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Image spectrumImage;
    [SerializeField] private Text descriptionText;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Show(GemDefinition gem)
    {
        descriptionText.text = gem.absorptionSpectrum;
        spectrumImage.sprite = SpectrumRenderer.BuildSpectrumSprite(gem.spectrumLines);
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}
