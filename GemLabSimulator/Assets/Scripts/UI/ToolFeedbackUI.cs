using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToolFeedbackUI : MonoBehaviour
{
    public static ToolFeedbackUI Instance { get; private set; }

    [SerializeField] private Text feedbackText;

    private Coroutine clearRoutine;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowMessage(string message)
    {
        feedbackText.text = message;
        if (clearRoutine != null) StopCoroutine(clearRoutine);
        clearRoutine = StartCoroutine(ClearAfterDelay());
    }

    private IEnumerator ClearAfterDelay()
    {
        yield return new WaitForSeconds(4f);
        feedbackText.text = string.Empty;
    }
}
