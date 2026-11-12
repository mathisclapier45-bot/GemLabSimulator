using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The introduction of a new game: five pictures (the flat in Paris, the taxi, the airport, the window of the plane, the landing) with a few lines of the player
// telling why they leave for Crystal Grove, and the letter of Vincent Pardieu. A click, Space or Enter goes on; "Passer" (or Escape) skips it all.
// Built at runtime on the Canvas of the main menu; the pictures are in Assets/Resources/Intro. Its texts are set in IM Fell English (Assets/Resources/Fonts, licence
// OFL), a font of old printed books, regular for the narration and the title, italic for the letter; the rest of the game keeps its font.
public class IntroCinematic : MonoBehaviour
{
    private enum Kind { Scene, Line, Letter, Title }

    private class Step
    {
        public Kind Kind;
        public int Scene;
        public string Text;
    }

    private static readonly Color Paper = new Color(0.96f, 0.92f, 0.80f, 1f);
    private static readonly Color Ink = new Color(0.22f, 0.15f, 0.10f, 1f);
    private static readonly Color Cream = new Color(0.97f, 0.94f, 0.86f, 1f);

    // The fonts of the cinematic, or the font of the game if the files are missing.
    private static Font Regular => Resources.Load<Font>("Fonts/IMFellEnglish-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    private static Font Italic => Resources.Load<Font>("Fonts/IMFellEnglish-Italic") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    private static Step Scene(int n) => new Step { Kind = Kind.Scene, Scene = n };
    private static Step Line(string text) => new Step { Kind = Kind.Line, Text = text };

    private static readonly List<Step> Steps = new List<Step>
    {
        Scene(1),
        Line("Paris. Une dernière nuit dans l'appartement, avant le grand départ."),
        Line("Il y a trois semaines, une lettre est arrivée. De mon oncle, Vincent Pardieu."),
        new Step { Kind = Kind.Letter },
        Line("Vincent a disparu. Personne ne sait où il est, ni de qui il se cache."),
        Scene(2),
        Line("Je n'ai pas hésité longtemps. Le taxi attend en bas, direction l'aéroport."),
        Scene(3),
        Line("Crystal Grove. Une petite ville de montagne, que je ne connais pas."),
        Line("On dit qu'elle vit des pierres précieuses, et qu'elle en garde les secrets."),
        Scene(4),
        Line("Là-bas, un laboratoire m'attend. Et peut-être des réponses."),
        Line("Qu'est-il arrivé à mon oncle ? Que cherchait-il à fuir ?"),
        Scene(5),
        Line("L'avion se pose. Tout commence ici."),
        new Step { Kind = Kind.Title, Text = "CRYSTAL GROVE" },
    };

    private Action done;
    private Image picture;
    private CanvasGroup pictureGroup;
    private CanvasGroup textGroup;
    private Text narration;
    private CanvasGroup letterGroup;
    private Text letterText;
    private CanvasGroup titleGroup;
    private Image fade;
    private bool clicked;
    private bool skipped;
    private bool finished;
    private float nextBlip;

    // Plays the cinematic over the main menu; done is called when it ends (or is skipped).
    public static void Play(Transform parent, Action done)
    {
        var go = new GameObject("IntroCinematic", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling();
        MusicManager.IntroPlaying = true;
        MusicManager.Instance?.Refresh();
        var intro = go.AddComponent<IntroCinematic>();
        intro.done = done;
        intro.Build();
        intro.StartCoroutine(intro.Run());
    }

    private static Text Txt(Transform parent, string name, int size, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Font font = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var text = go.GetComponent<Text>();
        text.font = font != null ? font : Regular;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.raycastTarget = false;
        return text;
    }

    private static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void Build()
    {
        // The black background, which also catches the clicks.
        var back = Panel(transform, "Back", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        back.raycastTarget = true;
        var button = back.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => clicked = true);

        // The picture.
        var pictureImage = Panel(transform, "Picture", Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        picture = pictureImage;
        picture.preserveAspect = true;
        pictureGroup = pictureImage.gameObject.AddComponent<CanvasGroup>();
        pictureGroup.alpha = 0f;
        pictureGroup.blocksRaycasts = false;

        // The narration, at the bottom.
        var textBox = Panel(transform, "TextBox", new Color(0.04f, 0.04f, 0.08f, 0.82f), new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);
        textGroup = textBox.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;
        textGroup.blocksRaycasts = false;
        narration = Txt(textBox.transform, "Text", 31, TextAnchor.MiddleLeft, Cream, Vector2.zero, Vector2.one, new Vector2(28f, 12f), new Vector2(-28f, -12f));
        Txt(textBox.transform, "Hint", 16, TextAnchor.LowerRight, new Color(0.7f, 0.7f, 0.78f, 1f), Vector2.zero, Vector2.one, new Vector2(20f, 6f), new Vector2(-18f, -6f)).text = "clic pour continuer";

        // The letter, on cream paper.
        var paperEdge = Panel(transform, "LetterEdge", new Color(0.55f, 0.45f, 0.30f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-430f, -290f), new Vector2(430f, 290f));
        letterGroup = paperEdge.gameObject.AddComponent<CanvasGroup>();
        letterGroup.alpha = 0f;
        letterGroup.blocksRaycasts = false;
        Panel(paperEdge.transform, "Paper", Paper, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
        letterText = Txt(paperEdge.transform, "Letter", 25, TextAnchor.UpperLeft, Ink, Vector2.zero, Vector2.one, new Vector2(44f, 28f), new Vector2(-44f, -28f), Italic);
        letterText.lineSpacing = 1.05f;
        // The letter always fits on the paper, down to the signature: the text shrinks if it is too long.
        letterText.verticalOverflow = VerticalWrapMode.Truncate;
        letterText.resizeTextForBestFit = true;
        letterText.resizeTextMinSize = 16;
        letterText.resizeTextMaxSize = 25;

        // The title of the game at the end.
        var title = Txt(transform, "Title", 84, TextAnchor.MiddleCenter, Cream, new Vector2(0f, 0.38f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero);
        titleGroup = title.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(3f, -3f);

        // The black that comes and goes between two pictures.
        fade = Panel(transform, "Fade", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fade.color = new Color(0f, 0f, 0f, 0f);

        // Skip.
        var skip = RuntimeUIKit.MakeButton("Skip", transform, "Passer >", 14, new Color(0.18f, 0.18f, 0.24f, 0.9f), Vector2.zero, new Vector2(100f, 30f));
        var skipRect = skip.GetComponent<RectTransform>();
        skipRect.anchorMin = skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.anchoredPosition = new Vector2(-20f, -18f);
        skip.onClick.AddListener(() => skipped = true);
    }

    private void Update()
    {
        if (finished) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) clicked = true;
        if (Input.GetKeyDown(KeyCode.Escape)) skipped = true;
    }

    // ---- The story --------------------------------------------------------------------------------------------------------

    private IEnumerator Run()
    {
        foreach (var step in Steps)
        {
            if (skipped) break;
            switch (step.Kind)
            {
                case Kind.Scene:
                    yield return ShowScene(step.Scene);
                    break;
                case Kind.Line:
                    yield return ShowLine(step.Text);
                    break;
                case Kind.Letter:
                    yield return ShowLetter();
                    break;
                case Kind.Title:
                    yield return ShowTitle(step.Text);
                    break;
            }
        }

        yield return Fade(1f, 0.7f);
        finished = true;
        done?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator Fade(float target, float seconds)
    {
        var start = fade.color.a;
        for (var t = 0f; t < seconds && !skipped; t += Time.unscaledDeltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, target, t / seconds));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, target);
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }

    private IEnumerator ShowScene(int number)
    {
        SetAlpha(textGroup, 0f);
        yield return Fade(1f, 0.45f);
        picture.sprite = Resources.Load<Sprite>("Intro/scene" + number);
        pictureGroup.alpha = picture.sprite != null ? 1f : 0f;
        yield return Fade(0f, 0.7f);
    }

    private IEnumerator ShowLine(string text)
    {
        SetAlpha(textGroup, 1f);
        narration.text = "";
        var shown = 0f;
        clicked = false;
        while (shown < text.Length && !skipped)
        {
            var before = Mathf.FloorToInt(shown);
            shown += 55f * Time.unscaledDeltaTime;
            var after = Mathf.Min(text.Length, Mathf.FloorToInt(shown));
            narration.text = text.Substring(0, after);
            if (after > before && Time.unscaledTime >= nextBlip && char.IsLetterOrDigit(text[after - 1]))
            {
                Sfx.Play(Sfx.Id.Blip, 0.4f, UnityEngine.Random.Range(0.8f, 0.9f));
                nextBlip = Time.unscaledTime + 0.06f;
            }
            if (clicked)
            {
                clicked = false;
                shown = text.Length;
            }
            yield return null;
        }
        narration.text = text;

        clicked = false;
        while (!clicked && !skipped) yield return null;
        clicked = false;
    }

    private IEnumerator ShowLetter()
    {
        var letter = StoryLetters.Find(StoryLetters.Paris);
        SetAlpha(textGroup, 0f);
        Sfx.Play(Sfx.Id.Paper);
        letterText.text = letter != null ? letter.Body : "";
        for (var t = 0f; t < 0.5f && !skipped; t += Time.unscaledDeltaTime)
        {
            letterGroup.alpha = t / 0.5f;
            yield return null;
        }
        letterGroup.alpha = 1f;

        clicked = false;
        while (!clicked && !skipped) yield return null;
        clicked = false;

        for (var t = 0f; t < 0.4f && !skipped; t += Time.unscaledDeltaTime)
        {
            letterGroup.alpha = 1f - t / 0.4f;
            yield return null;
        }
        letterGroup.alpha = 0f;
    }

    private IEnumerator ShowTitle(string text)
    {
        SetAlpha(textGroup, 0f);
        titleGroup.GetComponent<Text>().text = text;
        for (var t = 0f; t < 1.2f && !skipped; t += Time.unscaledDeltaTime)
        {
            titleGroup.alpha = t / 1.2f;
            yield return null;
        }
        titleGroup.alpha = 1f;

        var wait = 0f;
        clicked = false;
        while (wait < 3f && !clicked && !skipped)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }
        clicked = false;
    }
}
