using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The gauge of the reputation, at the top left, in the style of the clock and the purse: the level and a bar that fills with the experience (XP) of the level.
// Every stone identified and validated by the director gives XP; 3 stones for level 2, 6 for level 3, 12 for level 4, and so on (it doubles).
// A "+10 XP" rises from the gauge when a stone is validated. Added to the Canvas of every game scene.
public class ReputationHudUI : MonoBehaviour
{
    public const float Width = 230f;
    public const float Height = 58f;
    private const float Margin = 12f;

    private static readonly Color Fill = new Color(0.38f, 0.74f, 0.30f, 1f);
    private static readonly Color FillLight = new Color(0.62f, 0.90f, 0.42f, 1f);

    private Text levelText;
    private Text xpText;
    private RectTransform fillRect;
    private Image fillImage;
    private RectTransform root;
    private RectTransform floaterRoot;
    private int shownPoints = -1;
    private int shownLevel = -1;
    private float pulse;
    private float shownFill;
    private float targetFill;
    private ReputationState subscribed;

    private class Floater
    {
        public Text Label;
        public float Age;
    }

    private readonly List<Floater> floaters = new List<Floater>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    private static void Attach(Scene scene)
    {
        if (scene.name == "MainMenu") return;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null && canvas.GetComponent<ReputationHudUI>() == null) canvas.AddComponent<ReputationHudUI>();
    }

    private void Awake()
    {
        Build();
    }

    private void OnDestroy()
    {
        if (subscribed != null) subscribed.OnLevelUp -= OnLevelUp;
    }

    private void Build()
    {
        var go = new GameObject("ReputationHud", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(1);
        root = go.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(Margin, -Margin);
        root.sizeDelta = new Vector2(Width, Height);

        StardewKit.MakeFrame("Frame", root, Vector2.zero, new Vector2(Width, Height));
        levelText = RuntimeUIKit.MakeText("Level", root, "Niveau 1", 15, TextAnchor.MiddleLeft, StardewKit.Ink, new Vector2(14f, -6f), new Vector2(110f, 20f));
        levelText.fontStyle = FontStyle.Bold;
        xpText = RuntimeUIKit.MakeText("Xp", root, "0 / 30 XP", 13, TextAnchor.MiddleRight, StardewKit.Digits, new Vector2(Width - 14f - 100f, -6f), new Vector2(100f, 20f));
        xpText.fontStyle = FontStyle.Bold;

        // The bar: a cream cell, the green fill that grows inside it.
        const float barX = 12f;
        const float barY = -30f;
        var barWidth = Width - 24f;
        StardewKit.MakeInset("Bar", root, new Vector2(barX, barY), new Vector2(barWidth, 18f));
        var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(root, false);
        fillRect = fillGO.GetComponent<RectTransform>();
        RuntimeUIKit.SetTopLeft(fillRect, new Vector2(barX + 3f, barY - 3f), new Vector2(0f, 12f));
        fillImage = fillGO.GetComponent<Image>();
        fillImage.color = Fill;
        fillImage.raycastTarget = false;

        floaterRoot = new GameObject("Floaters", typeof(RectTransform)).GetComponent<RectTransform>();
        floaterRoot.SetParent(root, false);
        floaterRoot.anchorMin = floaterRoot.anchorMax = new Vector2(0f, 1f);
        floaterRoot.pivot = new Vector2(0f, 1f);
        floaterRoot.anchoredPosition = new Vector2(0f, -Height);
        floaterRoot.sizeDelta = new Vector2(Width, 30f);
    }

    private void Update()
    {
        var rep = ReputationState.Instance;
        if (rep == null) return;

        if (subscribed != rep)
        {
            if (subscribed != null) subscribed.OnLevelUp -= OnLevelUp;
            subscribed = rep;
            rep.OnLevelUp += OnLevelUp;
            shownPoints = rep.Points;
            shownLevel = -1;
        }

        if (shownLevel != rep.Level || shownPoints != rep.Points)
        {
            var first = shownLevel == -1;
            // One stone validated (a jump of several points is a save being loaded: nothing rises then).
            if (shownPoints >= 0 && rep.Points == shownPoints + 1) AddFloater($"+{(rep.Points - shownPoints) * ReputationState.XpPerStone} XP");
            if (shownPoints >= 0 && rep.Points != shownPoints) pulse = 1f;
            shownPoints = rep.Points;
            shownLevel = rep.Level;

            levelText.text = $"Niveau {rep.Level}";
            xpText.text = $"{rep.XpIntoCurrentLevel} / {rep.XpNeededForNextLevel} XP";
            targetFill = rep.ProgressToNextLevel();
            if (first) shownFill = targetFill;
        }

        // The bar slides to its new length; a level up empties it first.
        shownFill = Mathf.MoveTowards(shownFill, targetFill, Time.unscaledDeltaTime * 1.2f);
        if (targetFill < shownFill - 0.5f) shownFill = 0f;
        var innerWidth = Width - 24f - 6f;
        fillRect.sizeDelta = new Vector2(innerWidth * shownFill, 12f);
        fillImage.color = Color.Lerp(Fill, FillLight, pulse);

        if (pulse > 0f)
        {
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 1.6f);
            root.localScale = Vector3.one * (1f + 0.04f * pulse);
        }
        else root.localScale = Vector3.one;

        for (var i = floaters.Count - 1; i >= 0; i--)
        {
            var floater = floaters[i];
            floater.Age += Time.unscaledDeltaTime;
            var t = floater.Age / 1.6f;
            if (t >= 1f)
            {
                Destroy(floater.Label.gameObject);
                floaters.RemoveAt(i);
                continue;
            }
            var rect = floater.Label.rectTransform;
            rect.anchoredPosition = new Vector2(14f, -t * 22f);
            var color = floater.Label.color;
            color.a = 1f - t * t;
            floater.Label.color = color;
        }
    }

    private void OnLevelUp(int level)
    {
        Sfx.Play(Sfx.Id.Success, 0.6f);
        ToolFeedbackUI.Instance?.ShowMessage($"Niveau {level} atteint ! Votre réputation grandit : de nouveaux lieux peuvent s'ouvrir.");
    }

    private void AddFloater(string text)
    {
        var label = RuntimeUIKit.MakeText("Floater", floaterRoot, text, 17, TextAnchor.UpperLeft, new Color(0.45f, 0.95f, 0.40f), new Vector2(14f, 0f), new Vector2(120f, 24f));
        label.fontStyle = FontStyle.Bold;
        var outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.2f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        floaters.Add(new Floater { Label = label });
    }
}
