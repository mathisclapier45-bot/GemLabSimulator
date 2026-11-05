using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gives every window of the game the look of the interface without touching each one: every half second, the windows open on the Canvas (the museum,
// the shops, the inventory, the instruments of the laboratory...) get their frame and the bevel of their buttons and boxes. A window with a dark
// background of its own takes the orange frame; one that keeps its own colours, like the instruments, gets a softer bronze edge behind it and a
// softer bevel. What was done already is left alone, so a window that rebuilds its pages is skinned again.
public class StardewAutoSkin : MonoBehaviour
{
    private float timer;

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
        if (canvas != null && canvas.GetComponent<StardewAutoSkin>() == null) canvas.AddComponent<StardewAutoSkin>();
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 0.5f;

        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (!child.gameObject.activeSelf || !IsWindow(child)) continue;
            SkinWindow(child);
        }
    }

    // A window: a block of a window's size, not stretched over the screen, that holds buttons or texts.
    private static readonly string[] Ignored = { "Dialogue", "Watch", "Wrist", "ClockHud", "MoneyHud", "Minimap", "Tooltip", "Feedback", "Floating", "Fader", "Overlay", "Prompt" };

    private static bool IsWindow(Transform t)
    {
        foreach (var name in Ignored)
            if (t.name.Contains(name)) return false;
        var rect = t as RectTransform;
        if (rect == null || rect.anchorMin != rect.anchorMax) return false;
        var size = rect.rect.size;
        if (size.x < 260f || size.x > 1100f || size.y < 150f || size.y > 760f) return false;
        return t.GetComponentInChildren<Button>(true) != null || t.GetComponentInChildren<Text>(true) != null;
    }

    private static void SkinWindow(Transform window)
    {
        var image = window.GetComponent<Image>();
        var soft = true;
        if (image != null && image.sprite != StardewKit.PanelFrame && image.GetComponent<Mask>() == null
            && (image.sprite == null || image.sprite == RuntimeUIKit.Rounded))
        {
            var c = image.color;
            var dark = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) < 0.35f && c.a > 0.5f;
            if (dark)
            {
                StardewKit.FramePanel(image);
                soft = false;
            }
        }
        else if (image != null && image.sprite == StardewKit.PanelFrame) soft = false;

        if (soft) StardewKit.AddSoftBacking(window);
        StardewKit.Skin(window, soft);
    }
}
