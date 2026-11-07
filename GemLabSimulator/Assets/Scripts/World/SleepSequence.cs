using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Going to bed, in the bed of the player's house: the light goes off and the room turns to a blue night, the player lies down and really sleeps (a few "z" rise
// above the pillow), then the screen fades to black on "La nuit porte conseil.", the day changes and the game is saved, and the day rises with the cry of a rooster.
public class SleepSequence : MonoBehaviour
{
    public static bool Running { get; private set; }

    private Transform bed;
    private Image fade;
    private Text title;
    private SpriteRenderer sleeper;
    private SpriteRenderer playerRenderer;
    private readonly List<SpriteRenderer> zs = new List<SpriteRenderer>();
    private bool morning;

    public static void Begin(Transform bed)
    {
        if (Running || bed == null) return;
        var go = new GameObject("SleepSequence");
        go.AddComponent<SleepSequence>().bed = bed;
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        // Whatever happened, the room is lit again and the player can move.
        Running = false;
        PlayerController.InputLocked = false;
        DayNightLighting.IndoorNight = 0f;
        if (playerRenderer != null) playerRenderer.enabled = true;
        if (sleeper != null) Destroy(sleeper.gameObject);
        foreach (var z in zs) if (z != null) Destroy(z.gameObject);
        if (fade != null) Destroy(fade.gameObject);
    }

    private IEnumerator Run()
    {
        Running = true;
        PlayerController.InputLocked = true;
        var player = PlayerController.Instance;
        playerRenderer = player != null ? player.GetComponent<SpriteRenderer>() : null;
        BuildOverlay();

        // The light goes off: the room turns to night.
        Sfx.Play(Sfx.Id.LightSwitch);
        yield return new WaitForSeconds(0.3f);
        yield return Tween(0f, 1f, 1.6f, v => DayNightLighting.IndoorNight = v);

        // The player lies down and sleeps.
        Sfx.Play(Sfx.Id.Sleep);
        if (playerRenderer != null) playerRenderer.enabled = false;
        MakeSleeper();
        for (var t = 0f; t < 3.6f; t += Time.unscaledDeltaTime)
        {
            AnimateZs(t);
            yield return null;
        }

        // To black, with the words of the night.
        if (fade != null) yield return Tween(0f, 1f, 1.6f, v => SetFade(v));
        if (title != null) yield return Tween(0f, 1f, 1.2f, v => SetTitle(v));

        // While the screen is dark, the night passes: the day changes, the story moves on, the game is saved.
        foreach (var z in zs) if (z != null) z.enabled = false;
        GameClock.Instance?.AdvanceDay();
        StoryState.Instance?.OnSlept();
        DayNightOverlay.ResetToDay();
        SaveSystem.Save();
        if (sleeper != null) Destroy(sleeper.gameObject);
        if (playerRenderer != null) playerRenderer.enabled = true;
        DayNightLighting.IndoorNight = 0.8f;
        yield return new WaitForSeconds(2.4f);

        // The day rises, with a rooster.
        morning = true;
        Sfx.Play(Sfx.Id.Rooster);
        StartCoroutine(Tween(0.8f, 0f, 4.5f, v => DayNightLighting.IndoorNight = v));
        if (title != null) StartCoroutine(Tween(1f, 0f, 1.4f, v => SetTitle(v)));
        yield return new WaitForSeconds(0.8f);
        if (fade != null) yield return Tween(1f, 0f, 2.4f, v => SetFade(v));

        ToolFeedbackUI.Instance?.ShowMessage("Partie sauvegardée. Vous vous réveillez à 7h.");
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    // A value that slides from one number to another (not stopped by the pause).
    private static IEnumerator Tween(float from, float to, float seconds, Action<float> apply)
    {
        for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            var u = Mathf.SmoothStep(0f, 1f, t / seconds);
            apply(Mathf.Lerp(from, to, u));
            yield return null;
        }
        apply(to);
    }

    private void SetFade(float alpha)
    {
        if (fade != null) fade.color = new Color(0f, 0f, 0f, alpha);
    }

    private void SetTitle(float alpha)
    {
        if (title != null) title.color = new Color(0.96f, 0.90f, 0.74f, alpha);
    }

    // The black screen and its words, over everything on the canvas.
    private void BuildOverlay()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        var go = new GameObject("SleepFade", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        fade = go.GetComponent<Image>();
        fade.color = new Color(0f, 0f, 0f, 0f);
        fade.raycastTarget = false;

        title = RuntimeUIKit.MakeText("SleepTitle", go.transform, "La nuit porte conseil.", 40, TextAnchor.MiddleCenter, new Color(0.96f, 0.90f, 0.74f, 0f), Vector2.zero, new Vector2(900f, 80f));
        var titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = Vector2.zero;
        title.fontStyle = FontStyle.Italic;
    }

    // ---- The sleeper -----------------------------------------------------------------------------------------------------

    private static Sprite sleeperSprite;
    private static Sprite zSprite;

    private void MakeSleeper()
    {
        if (sleeperSprite == null) sleeperSprite = BuildSleeperSprite();
        var go = new GameObject("Sleeper");
        sleeper = go.AddComponent<SpriteRenderer>();
        sleeper.sprite = sleeperSprite;
        sleeper.sortingOrder = 6;
        go.transform.position = bed.position + new Vector3(0f, 0.2f, 0f);

        if (zSprite == null) zSprite = BuildZSprite();
        for (var i = 0; i < 3; i++)
        {
            var z = new GameObject("Z" + i);
            var renderer = z.AddComponent<SpriteRenderer>();
            renderer.sprite = zSprite;
            renderer.sortingOrder = 8;
            renderer.enabled = false;
            zs.Add(renderer);
        }
    }

    // Three "z" that rise one after the other above the pillow, growing and fading.
    private void AnimateZs(float time)
    {
        var origin = bed.position + new Vector3(0.3f, 1.0f, 0f);
        for (var i = 0; i < zs.Count; i++)
        {
            var age = Mathf.Repeat(time - i * 0.9f, 2.7f);
            var started = time - i * 0.9f >= 0f;
            var u = age / 2.7f;
            zs[i].enabled = started;
            zs[i].transform.position = origin + new Vector3(u * 0.7f + Mathf.Sin(age * 3f) * 0.05f, u * 1.0f, 0f);
            zs[i].transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.6f, u);
            zs[i].color = new Color(0.90f, 0.95f, 1f, Mathf.Sin(u * Mathf.PI));
        }
    }

    // A head on the pillow, eyes closed, and the sheet pulled up to the chin (drawn pixel by pixel).
    private static Sprite BuildSleeperSprite()
    {
        const int w = 30;
        const int h = 26;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color32(0, 0, 0, 0);
        var pixels = new Color32[w * h];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;
        void Put(int x, int y, Color32 c) { if (x >= 0 && x < w && y >= 0 && y < h) pixels[y * w + x] = c; }

        var sheet = new Color32(226, 233, 247, 255);
        var sheetShade = new Color32(170, 186, 218, 255);
        var sheetFold = new Color32(200, 212, 236, 255);
        var skin = new Color32(238, 198, 164, 255);
        var skinShade = new Color32(210, 166, 134, 255);
        var hair = new Color32(92, 62, 42, 255);
        var dark = new Color32(60, 44, 40, 255);

        // The sheet, with its folded edge and a fold line.
        for (var y = 0; y <= 11; y++)
            for (var x = 0; x < w; x++)
                Put(x, y, y == 11 ? sheetShade : (y == 6 ? sheetFold : sheet));
        for (var x = 0; x < w; x++) Put(x, 10, sheetFold);

        // The head, turned a little: hair on top, a closed eye, a cheek.
        for (var y = 12; y <= 22; y++)
            for (var x = 9; x <= 20; x++)
            {
                var corner = (x == 9 || x == 20) && (y == 12 || y == 22);
                if (corner) continue;
                Put(x, y, skin);
            }
        for (var x = 9; x <= 20; x++) { Put(x, 22, hair); Put(x, 21, hair); }
        for (var y = 17; y <= 21; y++) { Put(9, y, hair); Put(20, y, hair); }
        Put(11, 22, hair); Put(18, 22, hair);
        Put(12, 17, dark); Put(13, 17, dark); Put(17, 17, dark); Put(18, 17, dark);
        Put(15, 15, skinShade); Put(16, 15, skinShade);
        for (var x = 10; x <= 19; x++) Put(x, 12, skinShade);

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
    }

    private static Sprite BuildZSprite()
    {
        var rows = new[]
        {
            "#######",
            ".....#.",
            "....#..",
            "...#...",
            "..#....",
            ".#.....",
            "#######",
        };
        var texture = new Texture2D(7, 7, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < 7; y++)
            for (var x = 0; x < 7; x++)
                texture.SetPixel(x, 6 - y, rows[y][x] == '#' ? new Color(1f, 1f, 1f, 1f) : new Color(0f, 0f, 0f, 0f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 16f);
    }
}
