using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToolFeedbackUI : MonoBehaviour
{
    public static ToolFeedbackUI Instance { get; private set; }

    [SerializeField] private Text feedbackText;

    private Coroutine activeRoutine;

    private void Awake()
    {
        Instance = this;
        if (feedbackText != null)
        {
            var c = feedbackText.color;
            feedbackText.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    public void ShowMessage(string message)
    {
        feedbackText.text = message;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        yield return Fade(0f, 1f, 0.15f);
        yield return new WaitForSeconds(3.5f);
        yield return Fade(1f, 0f, 0.5f);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        var baseColor = feedbackText.color;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var a = Mathf.Lerp(from, to, elapsed / duration);
            feedbackText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            yield return null;
        }
        feedbackText.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
    }
}
