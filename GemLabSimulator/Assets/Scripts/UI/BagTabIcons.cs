using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The small logos of the tabs of the bag, drawn in pixel-art (16 x 16): the backpack, the calendar, the book of the gems, the gavel of the auctions, the
// card binder and the diamond of the price list. Each tab shows its logo instead of its title; the title appears when the mouse is over it.
public static class BagTabIcons
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    private static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

    private static readonly Color Outline = new Color(0.16f, 0.11f, 0.08f, 1f);

    // The logo of a tab from its title, or null when it has none.
    public static Sprite For(string label)
    {
        if (label.StartsWith("Sac")) return Get("Sac", SacRows, new Dictionary<char, Color> { { 'b', C(0.66f, 0.43f, 0.24f) }, { 'd', C(0.45f, 0.28f, 0.15f) }, { 'g', C(0.96f, 0.80f, 0.32f) } });
        if (label.StartsWith("Calendrier")) return Get("Calendrier", CalendarRows, new Dictionary<char, Color> { { 'w', C(0.96f, 0.94f, 0.88f) }, { 'r', C(0.82f, 0.28f, 0.26f) }, { 'd', C(0.45f, 0.40f, 0.36f) }, { 'n', C(0.45f, 0.44f, 0.50f) }, { 'g', C(0.97f, 0.78f, 0.25f) } });
        if (label.StartsWith("Grimoire")) return Get("Grimoire", BookRows, new Dictionary<char, Color> { { 'p', C(0.48f, 0.28f, 0.66f) }, { 'g', C(0.92f, 0.74f, 0.28f) }, { 'c', C(0.96f, 0.93f, 0.82f) }, { 'u', C(0.35f, 0.62f, 0.98f) }, { 'l', C(0.85f, 0.94f, 1f) } });
        if (label.StartsWith("Enchères")) return Get("Encheres", GavelRows, new Dictionary<char, Color> { { 'b', C(0.60f, 0.38f, 0.20f) }, { 'w', C(0.82f, 0.60f, 0.34f) }, { 'd', C(0.40f, 0.26f, 0.15f) } });
        if (label.StartsWith("Classeur")) return Get("Classeur", BinderRows, new Dictionary<char, Color> { { 't', C(0.20f, 0.50f, 0.54f) }, { 's', C(0.78f, 0.80f, 0.84f) }, { 'c', C(0.97f, 0.94f, 0.86f) }, { 'y', C(0.96f, 0.84f, 0.48f) }, { 'u', C(0.35f, 0.62f, 0.98f) } });
        if (label.StartsWith("Rapaport")) return Get("Rapaport", DiamondRows, new Dictionary<char, Color> { { 'u', C(0.50f, 0.78f, 0.96f) }, { 'l', C(0.92f, 0.98f, 1f) }, { 'r', C(0.86f, 0.28f, 0.26f) } });
        return null;
    }

    private static Sprite Get(string key, string[] rows, Dictionary<char, Color> palette)
    {
        if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;

        var h = rows.Length;
        var w = rows[0].Length;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var ch = x < rows[y].Length ? rows[y][x] : '.';
                Color color;
                if (ch == '.') color = new Color(0f, 0f, 0f, 0f);
                else if (ch == 'k') color = Outline;
                else color = palette.TryGetValue(ch, out var found) ? found : Color.magenta;
                texture.SetPixel(x, h - 1 - y, color);
            }
        }
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = sprite;
        return sprite;
    }

    private static readonly string[] SacRows =
    {
        "................",
        "......kkkk......",
        ".....kddddk.....",
        ".....kddddk.....",
        "....kbbbbbbk....",
        "...kbbbbbbbbk...",
        "...kbbbbbbbbk...",
        "...kbbbbbbbbk...",
        "...kbbbbbbbbk...",
        "...kbddddddbk...",
        "...kbddggddbk...",
        "...kbddddddbk...",
        "...kbddddddbk...",
        "...kbbbbbbbbk...",
        "....kkkkkkkk....",
        "................",
    };

    private static readonly string[] CalendarRows =
    {
        ".....k....k.....",
        "....kdk..kdk....",
        "..kkkdkkkkdkkk..",
        ".krrrdrrrrdrrrk.",
        ".krrrrrrrrrrrrk.",
        ".krrrrrrrrrrrrk.",
        ".kwwwwwwwwwwwwk.",
        ".kwwnwwnwwnwwwk.",
        ".kwwwwwwwwwwwwk.",
        ".kwwnwwnwwnwwwk.",
        ".kwwwwwwwwwwwwk.",
        ".kwwnwwnwwggwwk.",
        ".kwwwwwwwwggwwk.",
        ".kwwwwwwwwwwwwk.",
        "..kkkkkkkkkkkk..",
        "................",
    };

    private static readonly string[] BookRows =
    {
        "................",
        "...kkkkkkkkkk...",
        "..kggppppppppk..",
        "..kggppppppppk..",
        "..kggppppppppk..",
        "..kggppplupppk..",
        "..kggppuuuuppk..",
        "..kggpppuupppk..",
        "..kggppppppppk..",
        "..kggppppppppk..",
        "..kggppggggppk..",
        "..kggppppppppk..",
        "..kggppppppppk..",
        "..kggppppppppk..",
        "...kkcccccccck..",
        ".....kkkkkkkk...",
    };

    private static readonly string[] GavelRows =
    {
        "................",
        "....kkkkkkkk....",
        "...kbbbbbbbbk...",
        "...kbbbbbbbbk...",
        "...kbbbbbbbbk...",
        "...kbbbbbbbbk...",
        "....kkkkkwwk....",
        "..........kwwk..",
        ".........kwwk...",
        "........kwwk....",
        ".......kwwk.....",
        "......kwwk......",
        ".kkkkkkwwk......",
        ".kddddddkk......",
        ".kddddddk.......",
        ".kkkkkkkk.......",
    };

    private static readonly string[] BinderRows =
    {
        "................",
        "...kkkkkkkkkk...",
        "..kttttttttttk..",
        "..kttttttttttk..",
        "..ktstcccccctk..",
        "..ktttcyyyyctk..",
        "..ktttcyyyyctk..",
        "..ktstcyuuyctk..",
        "..ktttcyuuyctk..",
        "..ktttcyyyyctk..",
        "..ktstcyyyyctk..",
        "..ktttcccccctk..",
        "..kttttttttttk..",
        "..ktsttttttttk..",
        "..kttttttttttk..",
        "...kkkkkkkkkk...",
    };

    private static readonly string[] DiamondRows =
    {
        "................",
        "................",
        "....kkkkkkkk....",
        "...kuuullluuk...",
        "..kuluuuuuluuk..",
        ".kkkkkkkkkkkkkk.",
        "..kulluuuuuuuk..",
        "...kuluuuuuuk...",
        "....kuuuuuuk....",
        ".....kuuuuk.....",
        "......kuuk......",
        ".......kk.......",
        "................",
        "..kkkkkkkkkkkk..",
        "..krrrrrrrrrrk..",
        "..kkkkkkkkkkkk..",
    };
}

// Shows the full title of a tab (and its key) while the mouse is over its logo.
public class BagTabHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Text Target;
    public string Message;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Target == null) return;
        Target.text = Message;
        Target.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Target != null) Target.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (Target != null) Target.gameObject.SetActive(false);
    }
}
