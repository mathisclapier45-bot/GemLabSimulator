using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FloatingMoneyUI : MonoBehaviour
{
    public static FloatingMoneyUI Instance { get; private set; }

    [SerializeField] private RectTransform spawnAnchor;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyDelta += ShowDelta;
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null) PlayerWallet.Instance.OnMoneyDelta -= ShowDelta;
    }

    private void ShowDelta(int amount)
    {
        if (amount == 0 || spawnAnchor == null) return;
        StartCoroutine(AnimateFloatingText(amount));
    }

    private IEnumerator AnimateFloatingText(int amount)
    {
        var go = new GameObject("FloatingMoney", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(spawnAnchor, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(160f, 26f);

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = amount > 0 ? $"+{amount}€" : $"{amount}€";
        var color = amount > 0 ? new Color(0.45f, 0.85f, 0.45f) : new Color(0.90f, 0.40f, 0.40f);
        text.color = color;

        const float duration = 1.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            rect.anchoredPosition = new Vector2(0f, t * 26f);
            text.color = new Color(color.r, color.g, color.b, 1f - t);
            yield return null;
        }

        Destroy(go);
    }
}
