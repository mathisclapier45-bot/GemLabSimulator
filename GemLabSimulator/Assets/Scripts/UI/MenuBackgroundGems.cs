using UnityEngine;
using UnityEngine.UI;

public class MenuBackgroundGems : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private int gemCount = 28;
    [SerializeField] private float minSize = 18f;
    [SerializeField] private float maxSize = 42f;
    [SerializeField] private float minAlpha = 0.10f;
    [SerializeField] private float maxAlpha = 0.25f;

    private void Start()
    {
        Populate();
    }

    private void Populate()
    {
        if (container == null || GemDatabase.Instance == null) return;

        var allGems = GemDatabase.Instance.allGems;
        if (allGems.Count == 0) return;

        var width = container.rect.width;
        var height = container.rect.height;

        for (var i = 0; i < gemCount; i++)
        {
            var definition = allGems[Random.Range(0, allGems.Count)];
            if (definition == null || definition.GetIcon() == null) continue;

            var go = new GameObject("BackgroundGem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(container, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                Random.Range(-width / 2f, width / 2f),
                Random.Range(-height / 2f, height / 2f));
            var size = Random.Range(minSize, maxSize);
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var image = go.GetComponent<Image>();
            image.sprite = definition.GetIcon();
            image.preserveAspect = true;
            image.raycastTarget = false;
            var color = Color.white;
            color.a = Random.Range(minAlpha, maxAlpha);
            image.color = color;
        }
    }
}
