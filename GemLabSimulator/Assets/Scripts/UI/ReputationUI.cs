using UnityEngine;
using UnityEngine.UI;

public class ReputationUI : MonoBehaviour
{
    [SerializeField] private Text levelText;
    [SerializeField] private Image fillImage;

    private void Start()
    {
        if (ReputationState.Instance != null) ReputationState.Instance.OnReputationChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (ReputationState.Instance != null) ReputationState.Instance.OnReputationChanged -= Refresh;
    }

    private void Refresh()
    {
        var rep = ReputationState.Instance;
        if (rep == null) return;

        if (levelText != null) levelText.text = $"Niveau {rep.Level}";
        if (fillImage != null) fillImage.fillAmount = rep.ProgressToNextLevel();
    }
}
