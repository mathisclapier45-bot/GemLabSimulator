using System;
using UnityEngine;
using UnityEngine.UI;

public class MeasurementMinigameUI : MonoBehaviour
{
    public static MeasurementMinigameUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform track;
    [SerializeField] private RectTransform needle;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private Text instructionText;

    private const float Speed = 1.4f;
    private const float PerfectHalfWidth01 = 0.04f;
    private const float GoodHalfWidth01 = 0.12f;

    private float targetCenter01;
    private bool running;
    private Action<float> onLocked;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Open(string label, Action<float> onLockedCallback)
    {
        onLocked = onLockedCallback;
        instructionText.text = $"{label} : appuie sur ESPACE quand le curseur est dans la zone.";

        targetCenter01 = UnityEngine.Random.Range(0.2f, 0.8f);
        var trackWidth = track.rect.width;
        targetZone.anchoredPosition = new Vector2((targetCenter01 - 0.5f) * trackWidth, 0f);
        targetZone.sizeDelta = new Vector2(GoodHalfWidth01 * 2f * trackWidth, targetZone.sizeDelta.y);

        panel.SetActive(true);
        running = true;
    }

    private void Update()
    {
        if (!running) return;

        var t = Mathf.PingPong(Time.time * Speed, 1f);
        var trackWidth = track.rect.width;
        needle.anchoredPosition = new Vector2((t - 0.5f) * trackWidth, 0f);

        if (!Input.GetKeyDown(KeyCode.Space)) return;

        var distance = Mathf.Abs(t - targetCenter01);
        float precision;
        if (distance <= PerfectHalfWidth01) precision = 1f;
        else if (distance <= GoodHalfWidth01) precision = 0.6f;
        else precision = 0.15f;

        running = false;
        panel.SetActive(false);
        onLocked?.Invoke(precision);
    }
}
