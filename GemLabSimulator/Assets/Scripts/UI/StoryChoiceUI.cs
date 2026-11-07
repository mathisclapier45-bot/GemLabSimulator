using System;
using UnityEngine;
using UnityEngine.UI;

// The choices of a conversation of the story: a few buttons above the dialogue box ("Demander la suite", "Plus tard"...). Escape picks the last one.
public class StoryChoiceUI : MonoBehaviour
{
    private static GameObject current;

    public static bool IsOpen => current != null;

    public static void Show(string[] options, Action<int> chosen)
    {
        Close();
        var canvas = GameObject.Find("Canvas");
        if (canvas == null || options == null || options.Length == 0)
        {
            chosen?.Invoke(options != null ? options.Length - 1 : -1);
            return;
        }

        const float width = 560f;
        const float height = 46f;
        const float gap = 8f;
        var root = new GameObject("StoryChoicePanel", typeof(RectTransform), typeof(StoryChoiceUI));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        // Above the dialogue box (140 high, 30 from the bottom of the screen).
        rect.anchoredPosition = new Vector2(0f, 30f + 140f + 18f);
        rect.sizeDelta = new Vector2(width, options.Length * (height + gap));

        var done = false;
        for (var i = 0; i < options.Length; i++)
        {
            var index = i;
            var button = RuntimeUIKit.MakeButton("Choice" + i, rect, options[i], 18, new Color(0.40f, 0.28f, 0.16f, 1f),
                new Vector2(0f, -i * (height + gap)), new Vector2(width, height));
            button.onClick.AddListener(() =>
            {
                if (done) return;
                done = true;
                Sfx.Play(Sfx.Id.Click, 0.6f);
                Close();
                chosen?.Invoke(index);
            });
        }
        StardewKit.Skin(rect);

        var component = root.GetComponent<StoryChoiceUI>();
        component.onEscape = () =>
        {
            if (done) return;
            done = true;
            Close();
            chosen?.Invoke(options.Length - 1);
        };
        current = root;
    }

    public static void Close()
    {
        if (current != null) Destroy(current);
        current = null;
    }

    private Action onEscape;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) onEscape?.Invoke();
    }
}
