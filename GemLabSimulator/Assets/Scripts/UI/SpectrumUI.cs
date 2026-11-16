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
        descriptionText.text = SpectrumLibrary.Description(gem);
        if (spectrumImage.sprite != null)
        {
            if (spectrumImage.sprite.texture != null) Destroy(spectrumImage.sprite.texture);
            Destroy(spectrumImage.sprite);
        }
        spectrumImage.sprite = SpectrumRenderer.BuildSpectrumSprite(SpectrumLibrary.Lines(gem));
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
    }
}
