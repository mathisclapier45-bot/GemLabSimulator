using System.Collections.Generic;
using UnityEngine;

// A source of light in an outdoor scene, seen by DayNightLighting: street lamps and shop fronts that glow at night, and the lantern the
// player carries (a faint glow until the lantern of the bag is lit).
public class NightLight : MonoBehaviour
{
    // Lantern: the one the player carries (lit with the lantern of the bag). Carried: a lantern someone carries that is always lit (Professor Thouvenot at night).
    public enum Kind { Lamp, Window, Lantern, Carried, Torch }

    public static readonly List<NightLight> All = new List<NightLight>();

    public Kind Type = Kind.Lamp;
    public Color LightColor = new Color(1f, 0.78f, 0.42f);
    public float Radius = 5f;            // in world units
    public float Intensity = 1f;
    public string Building;              // scene of the building, for the shop fronts: dim when it is closed

    private float seed;
    private SpriteRenderer held;

    // The story lends the player a lit lantern for the walk through the town at night (the prologue), whatever the bag holds.
    public static bool StoryLantern;

    private static bool LanternLit => StoryLantern || (PlayerOwnedTools.Instance != null && PlayerOwnedTools.Instance.LanternLit);

    private void OnEnable()
    {
        All.Add(this);
        seed = Random.value * 100f;
        if (Type == Kind.Lantern || Type == Kind.Carried) held = CreateHeldLantern();
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Update()
    {
        // The type is set after the component is added, so the small lantern is made here, the first time it is needed.
        if (held == null && (Type == Kind.Lantern || Type == Kind.Carried)) held = CreateHeldLantern();
        // The lantern shows in the hand of the player once it is lit and the light is falling.
        if (held != null) held.enabled = (Type == Kind.Carried || LanternLit) && DayNightLighting.Darkness > 0.08f;
    }

    // Without the lantern of the bag, the player only has the faint glow of the night around him.
    public float CurrentRadius => Type == Kind.Lantern && !LanternLit ? Radius * 0.4f : Radius;

    // The light given now, 0 by day.
    public float CurrentIntensity(float darkness)
    {
        var level = darkness * Intensity;
        if (Type == Kind.Window && !string.IsNullOrEmpty(Building) && !OpeningHours.IsOpen(Building)) level *= 0.3f;
        if (Type == Kind.Lantern && !LanternLit) level *= 0.3f;
        // The flame of a lantern and the old lamps flicker a little.
        // A torch of the crypt flickers a lot more than a lamp.
        var flicker = Type == Kind.Torch ? 0.82f + 0.12f * Mathf.Sin(Time.time * 9f + seed) + 0.06f * Mathf.Sin(Time.time * 23f + seed * 1.7f)
            : Type == Kind.Lantern || Type == Kind.Carried ? 0.94f + 0.06f * Mathf.Sin(Time.time * 7f + seed) : 0.97f + 0.03f * Mathf.Sin(Time.time * 3f + seed);
        return level * flicker;
    }

    // A small lantern drawn in the hand of the player.
    private SpriteRenderer CreateHeldLantern()
    {
        const int w = 8;
        const int h = 11;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var clear = new Color32(0, 0, 0, 0);
        var brass = new Color32(150, 112, 52, 255);
        var dark = new Color32(70, 52, 28, 255);
        var glass = new Color32(255, 214, 120, 255);
        var flame = new Color32(255, 248, 200, 255);
        var pixels = new Color32[w * h];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = clear;
        void Put(int x, int yFromTop, Color32 c) => pixels[(h - 1 - yFromTop) * w + x] = c;
        for (var x = 2; x <= 5; x++) Put(x, 0, dark);
        Put(1, 1, dark); Put(6, 1, dark);
        for (var x = 1; x <= 6; x++) Put(x, 2, brass);
        for (var y = 3; y <= 8; y++)
        {
            Put(1, y, brass);
            Put(6, y, brass);
            for (var x = 2; x <= 5; x++) Put(x, y, glass);
        }
        Put(3, 5, flame); Put(4, 5, flame); Put(3, 6, flame); Put(4, 6, flame);
        for (var x = 1; x <= 6; x++) Put(x, 9, brass);
        for (var x = 2; x <= 5; x++) Put(x, 10, dark);
        texture.SetPixels32(pixels);
        texture.Apply();

        var go = new GameObject("HeldLantern");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.42f, -0.2f, 0f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        renderer.sortingOrder = 6;
        renderer.enabled = false;
        return renderer;
    }
}
