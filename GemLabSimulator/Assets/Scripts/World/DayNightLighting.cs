using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The ambiance of the day in the town: a full-screen effect on the camera that gives the picture the colour of the hour (cold blue night,
// pink dawn, golden afternoon, red sunset...) and lights the street lamps, the shop fronts and the lantern of the player at night.
// Added to the camera of the town when it loads, so the scene does not need to be regenerated.
[RequireComponent(typeof(Camera))]
public class DayNightLighting : MonoBehaviour
{
    private const int MaxLights = 32;

    private struct Keyframe
    {
        public float Hour;
        public Color Ambient;
        public float Saturation;
        public float Vignette;

        public Keyframe(float hour, float r, float g, float b, float saturation, float vignette)
        {
            Hour = hour;
            Ambient = new Color(r, g, b, 1f);
            Saturation = saturation;
            Vignette = vignette;
        }
    }

    private static readonly Keyframe[] Keyframes =
    {
        new Keyframe(0.0f, 0.22f, 0.27f, 0.50f, 0.72f, 0.52f),
        new Keyframe(4.5f, 0.22f, 0.27f, 0.50f, 0.72f, 0.52f),
        new Keyframe(5.6f, 0.38f, 0.38f, 0.62f, 0.80f, 0.42f),
        new Keyframe(6.4f, 0.92f, 0.66f, 0.64f, 0.92f, 0.28f),    // dawn
        new Keyframe(7.3f, 1.00f, 0.83f, 0.68f, 0.96f, 0.20f),    // sunrise
        new Keyframe(8.6f, 1.00f, 0.95f, 0.88f, 1.00f, 0.12f),
        new Keyframe(10.0f, 1.00f, 1.00f, 1.00f, 1.00f, 0.08f),
        new Keyframe(16.5f, 1.00f, 1.00f, 1.00f, 1.00f, 0.08f),
        new Keyframe(17.8f, 1.00f, 0.88f, 0.66f, 1.00f, 0.14f),   // golden hour
        new Keyframe(19.0f, 1.00f, 0.60f, 0.42f, 0.96f, 0.24f),   // sunset
        new Keyframe(20.0f, 0.76f, 0.47f, 0.58f, 0.90f, 0.34f),   // dusk
        new Keyframe(21.0f, 0.38f, 0.34f, 0.60f, 0.80f, 0.44f),
        new Keyframe(22.3f, 0.22f, 0.27f, 0.50f, 0.72f, 0.52f),
        new Keyframe(24.0f, 0.22f, 0.27f, 0.50f, 0.72f, 0.52f),
    };

    // True while the effect is running: the old translucent veil then stays off.
    public static bool Active { get; private set; }

    // 0 in daylight, 1 in the dark: how much the lamps are lit.
    public static float Darkness { get; private set; }

    private Camera cam;
    private Material material;
    private readonly Vector4[] positions = new Vector4[MaxLights];
    private readonly Vector4[] colors = new Vector4[MaxLights];
    private readonly List<(float distance, int index)> candidates = new List<(float, int)>();

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
        Active = false;
        if (scene.name != "Town") return;

        var camera = Camera.main;
        if (camera == null) return;
        if (camera.GetComponent<DayNightLighting>() == null) camera.gameObject.AddComponent<DayNightLighting>();
        AddLights();
    }

    // Street lamps, shop fronts and the lantern of the player get their light.
    private static void AddLights()
    {
        foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer.gameObject.name != "Lampadaire" || renderer.GetComponent<NightLight>() != null) continue;
            var bounds = renderer.bounds;
            var lamp = new GameObject("LampLight");
            lamp.transform.SetParent(renderer.transform, false);
            lamp.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.5f, 0f);
            var light = lamp.AddComponent<NightLight>();
            light.Type = NightLight.Kind.Lamp;
            light.LightColor = new Color(1f, 0.78f, 0.42f);
            light.Radius = 5.5f;
            light.Intensity = 1.05f;
        }

        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
        {
            if (door.GetComponent<NightLight>() != null) continue;
            var front = new GameObject("FrontLight");
            front.transform.SetParent(door.transform, false);
            front.transform.position = door.transform.position + new Vector3(0f, 1.4f, 0f);
            var light = front.AddComponent<NightLight>();
            light.Type = NightLight.Kind.Window;
            light.LightColor = new Color(1f, 0.84f, 0.55f);
            light.Radius = 4.5f;
            // A decorative building with a locked door is dark.
            light.Intensity = door.IsAccessible ? 0.85f : 0.25f;
            light.Building = door.TargetScene;
        }

        var player = PlayerController.Instance;
        if (player != null && player.GetComponentInChildren<NightLight>() == null)
        {
            var lantern = new GameObject("Lantern");
            lantern.transform.SetParent(player.transform, false);
            lantern.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var light = lantern.AddComponent<NightLight>();
            light.Type = NightLight.Kind.Lantern;
            light.LightColor = new Color(1f, 0.80f, 0.48f);
            light.Radius = 4.6f;
            light.Intensity = 1.0f;
        }
    }

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        var shader = Resources.Load<Shader>("Shaders/DayNightLighting");
        if (shader == null || !shader.isSupported)
        {
            Active = false;
            return;
        }
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        Active = true;
    }

    private void OnDisable()
    {
        Active = false;
        if (material != null) Destroy(material);
    }

    // ---- Colour of the hour --------------------------------------------------------------------------------------

    private static void Evaluate(float hour, out Color ambient, out float saturation, out float vignette)
    {
        for (var i = 0; i < Keyframes.Length - 1; i++)
        {
            var a = Keyframes[i];
            var b = Keyframes[i + 1];
            if (hour < a.Hour || hour > b.Hour) continue;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.Hour, b.Hour, hour));
            ambient = Color.Lerp(a.Ambient, b.Ambient, t);
            saturation = Mathf.Lerp(a.Saturation, b.Saturation, t);
            vignette = Mathf.Lerp(a.Vignette, b.Vignette, t);
            return;
        }
        var last = Keyframes[Keyframes.Length - 1];
        ambient = last.Ambient;
        saturation = last.Saturation;
        vignette = last.Vignette;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (material == null || cam == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Evaluate(OpeningHours.CurrentHour, out var ambient, out var saturation, out var vignette);
        var brightness = (ambient.r + ambient.g + ambient.b) / 3f;
        Darkness = Mathf.Clamp01((0.88f - brightness) / 0.4f);

        var count = GatherLights();
        material.SetColor("_Ambient", ambient);
        material.SetVector("_Grade", new Vector4(saturation, vignette, cam.aspect, count));
        material.SetVectorArray("_Lights", positions);
        material.SetVectorArray("_LightColors", colors);
        Graphics.Blit(source, destination, material);
    }

    // The lights on screen, the nearest to the centre first, as positions in the picture.
    private int GatherLights()
    {
        candidates.Clear();
        var height = cam.orthographicSize * 2f;
        for (var i = 0; i < NightLight.All.Count; i++)
        {
            var light = NightLight.All[i];
            var view = cam.WorldToViewportPoint(light.transform.position);
            var margin = light.CurrentRadius / height + 0.05f;
            if (view.x < -margin / cam.aspect || view.x > 1f + margin / cam.aspect || view.y < -margin || view.y > 1f + margin) continue;
            if (light.CurrentIntensity(Darkness) <= 0.01f) continue;
            candidates.Add(((new Vector2(view.x, view.y) - new Vector2(0.5f, 0.5f)).sqrMagnitude, i));
        }
        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        var count = Mathf.Min(candidates.Count, MaxLights);
        for (var n = 0; n < MaxLights; n++)
        {
            if (n >= count)
            {
                positions[n] = Vector4.zero;
                colors[n] = Vector4.zero;
                continue;
            }
            var light = NightLight.All[candidates[n].index];
            var view = cam.WorldToViewportPoint(light.transform.position);
            positions[n] = new Vector4(view.x, view.y, light.CurrentRadius / height, light.CurrentIntensity(Darkness));
            colors[n] = new Vector4(light.LightColor.r, light.LightColor.g, light.LightColor.b, 1f);
        }
        return count;
    }
}
