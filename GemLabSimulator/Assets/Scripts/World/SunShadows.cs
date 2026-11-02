using UnityEngine;
using UnityEngine.SceneManagement;

// Cast shadows for the sprites of the scenes. Outdoors they follow the sun: long and leaning towards the west in the morning, short at noon,
// leaning towards the east in the evening, gone at night. Indoors they fall softly towards the bottom right, as under a ceiling light.
// The characters also get a soft blob under their feet. Added to every game scene when it loads, so scenes do not need to be regenerated.
public class SunShadows : MonoBehaviour
{
    // Shared by every shadow: the lean of the shadow (degrees), its length, its opacity, and a counter that changes with them.
    public static float Lean { get; private set; }
    public static float Length { get; private set; } = 0.5f;
    public static float Opacity { get; private set; }
    public static int Version { get; private set; }

    private bool indoor;
    private float lastLean = 999f;
    private float lastLength;
    private float lastOpacity = -1f;

    private static Sprite blob;

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
        var go = new GameObject("SunShadows");
        var director = go.AddComponent<SunShadows>();
        director.indoor = scene.name != "Town";
        director.AddShadows();
    }

    // ---- The sun ---------------------------------------------------------------------------------------------------

    private void Update()
    {
        float lean, length, opacity;
        if (indoor)
        {
            lean = -24f;
            length = 0.30f;
            opacity = 0.24f;
        }
        else
        {
            var hour = OpeningHours.CurrentHour;
            var sun = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.0f, 8.5f, hour)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(17.5f, 20.0f, hour)));
            // The shadow swings from the west to the east with the sun and is shortest at noon.
            lean = Mathf.Lerp(52f, -52f, Mathf.Clamp01((hour - 6.5f) / 13f));
            length = Mathf.Lerp(0.38f, 0.85f, Mathf.Abs(lean) / 52f);
            opacity = 0.34f * sun;
        }

        if (Mathf.Abs(lean - lastLean) < 0.4f && Mathf.Abs(length - lastLength) < 0.01f && Mathf.Abs(opacity - lastOpacity) < 0.01f) return;
        lastLean = lean;
        lastLength = length;
        lastOpacity = opacity;
        Lean = lean;
        Length = length;
        Opacity = opacity;
        Version++;
    }

    // ---- Casters ---------------------------------------------------------------------------------------------------

    private void AddShadows()
    {
        // The characters: the player, the people of the town, the shopkeepers, the animals.
        foreach (var walker in FindObjectsByType<TownWanderer>(FindObjectsSortMode.None)) AddCharacter(walker.GetComponent<SpriteRenderer>());
        foreach (var talker in FindObjectsByType<TalkNPC>(FindObjectsSortMode.None)) AddCharacter(talker.GetComponent<SpriteRenderer>());
        foreach (var pet in FindObjectsByType<PetFollower>(FindObjectsSortMode.None)) AddCharacter(pet.GetComponent<SpriteRenderer>());
        if (PlayerController.Instance != null) AddCharacter(PlayerController.Instance.GetComponent<SpriteRenderer>());

        // The props and the buildings: the sprites that stand on the ground and are taller than a man's knee.
        foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null || renderer.sprite == null || renderer.GetComponentInParent<ShadowFollower>() != null) continue;
            if (renderer.GetComponent<TownWanderer>() != null || renderer.GetComponent<TalkNPC>() != null) continue;
            if (renderer.GetComponent<PlayerController>() != null || renderer.GetComponent<PetFollower>() != null) continue;
            if (renderer.GetComponent<ExaminationTool>() != null) continue;
            AddProp(renderer);
        }
    }

    private static void AddProp(SpriteRenderer source)
    {
        var sprite = source.sprite;
        var heightUnits = sprite.rect.height / sprite.pixelsPerUnit;
        var widthUnits = sprite.rect.width / sprite.pixelsPerUnit;
        // Only sprites standing on the ground (pivot at the feet) and tall enough; not the huge backdrops.
        if (sprite.pivot.y > sprite.rect.height * 0.15f) return;
        if (heightUnits < 0.9f || widthUnits > 16f || heightUnits > 14f) return;
        if (source.sortingOrder < 3) return;
        var name = source.gameObject.name;
        if (name.StartsWith("Fleurs") || name.StartsWith("Shadow") || name.StartsWith("Passant")) return;

        CreateShadow(source, false, Mathf.Clamp(2.2f / heightUnits, 0.22f, 1f));
    }

    private static void AddCharacter(SpriteRenderer source)
    {
        if (source == null || source.GetComponentInChildren<ShadowFollower>() != null) return;
        var follower = CreateShadow(source, true, 0.8f);

        var blobGo = new GameObject("ShadowBlob", typeof(SpriteRenderer));
        blobGo.transform.SetParent(source.transform, false);
        blobGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        var width = Mathf.Max(0.5f, source.sprite != null ? source.sprite.bounds.size.x * 0.75f : 0.8f);
        blobGo.transform.localScale = new Vector3(width, width, 1f);
        var blobRenderer = blobGo.GetComponent<SpriteRenderer>();
        blobRenderer.sprite = BlobSprite();
        blobRenderer.color = new Color(0f, 0f, 0f, 0.34f);
        blobRenderer.sortingLayerID = source.sortingLayerID;
        blobRenderer.sortingOrder = source.sortingOrder - 1;
        follower.SetBlob(blobRenderer);
    }

    private static ShadowFollower CreateShadow(SpriteRenderer source, bool animated, float lengthFactor)
    {
        // The root flattens and flips the shadow; its child swings it to the side: together they lean the picture like a real shadow.
        var root = new GameObject("Shadow");
        root.transform.SetParent(source.transform, false);
        var swing = new GameObject("Swing", typeof(SpriteRenderer));
        swing.transform.SetParent(root.transform, false);

        var shadowRenderer = swing.GetComponent<SpriteRenderer>();
        shadowRenderer.sprite = source.sprite;
        shadowRenderer.flipX = source.flipX;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0f);
        shadowRenderer.sortingLayerID = source.sortingLayerID;
        shadowRenderer.sortingOrder = source.sortingOrder - 1;

        var follower = root.AddComponent<ShadowFollower>();
        follower.Setup(source, shadowRenderer, root.transform, swing.transform, animated, lengthFactor);
        return follower;
    }

    // A soft dark ellipse, 1 unit wide.
    private static Sprite BlobSprite()
    {
        if (blob != null) return blob;
        const int w = 32;
        const int h = 16;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var dx = (x - (w - 1) / 2f) / (w / 2f);
                var dy = (y - (h - 1) / 2f) / (h / 2f);
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.Clamp01((1f - d) * 2.2f);
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        blob = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        return blob;
    }
}

// Keeps one cast shadow in step with its caster and with the sun.
public class ShadowFollower : MonoBehaviour
{
    private SpriteRenderer source;
    private SpriteRenderer shadow;
    private Transform root;
    private Transform swing;
    private bool animated;
    private float lengthFactor;
    private int appliedVersion = -1;
    private SpriteRenderer blob;

    // The soft blob under a character shows and hides with it.
    public void SetBlob(SpriteRenderer blobRenderer)
    {
        blob = blobRenderer;
    }

    public void Setup(SpriteRenderer sourceRenderer, SpriteRenderer shadowRenderer, Transform rootTransform, Transform swingTransform, bool isAnimated, float factor)
    {
        source = sourceRenderer;
        shadow = shadowRenderer;
        root = rootTransform;
        swing = swingTransform;
        animated = isAnimated;
        lengthFactor = factor;
    }

    private void LateUpdate()
    {
        if (source == null || shadow == null) return;

        // The caster may be hidden (a townsperson gone home) or animated: the shadow copies its picture.
        var visible = source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
        if (animated && visible)
        {
            shadow.sprite = source.sprite;
            shadow.flipX = source.flipX;
        }

        if (blob != null) blob.enabled = visible;
        var opacity = visible ? SunShadows.Opacity : 0f;
        shadow.enabled = opacity > 0.01f;

        if (appliedVersion == SunShadows.Version && !animated) return;
        appliedVersion = SunShadows.Version;
        root.localScale = new Vector3(1f, -SunShadows.Length * lengthFactor, 1f);
        swing.localRotation = Quaternion.Euler(0f, 0f, SunShadows.Lean);
        shadow.color = new Color(0f, 0f, 0f, opacity);
    }
}
