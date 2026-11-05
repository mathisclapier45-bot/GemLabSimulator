using UnityEngine;

// The small "E" key that shows above the player's head when they stand where a machine, a staircase or any other interactive thing can be used.
// Put it on the object that has the trigger of the interaction: the key appears while the player is inside the trigger.
public class InteractPrompt : MonoBehaviour
{
    private const float Height = 1.9f;
    private const float FadeSpeed = 8f;

    private static Sprite keySprite;

    private bool playerInRange;
    private Transform player;
    private SpriteRenderer key;
    private float alpha;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        player = other.transform;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    private void OnDisable()
    {
        playerInRange = false;
        if (key != null) key.gameObject.SetActive(false);
    }

    private void Update()
    {
        // The key is not shown while a window of the game is open on top of the world.
        var shown = playerInRange && player != null && !PlayerController.InputLocked;
        alpha = Mathf.MoveTowards(alpha, shown ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
        if (alpha <= 0f)
        {
            if (key != null) key.gameObject.SetActive(false);
            return;
        }

        if (key == null)
        {
            var go = new GameObject("InteractKey", typeof(SpriteRenderer));
            key = go.GetComponent<SpriteRenderer>();
            key.sprite = KeySprite();
            key.sortingOrder = 40;
        }
        key.gameObject.SetActive(true);
        key.transform.position = player.position + new Vector3(0f, Height + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f, 0f);
        key.color = new Color(1f, 1f, 1f, alpha);
    }

    private void OnDestroy()
    {
        if (key != null) Destroy(key.gameObject);
    }

    // A small keycap with the letter E, drawn pixel by pixel.
    private static Sprite KeySprite()
    {
        if (keySprite != null) return keySprite;

        const int size = 16;
        var dark = new Color32(34, 38, 52, 255);
        var light = new Color32(244, 240, 228, 255);
        var shade = new Color32(196, 190, 174, 255);
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                if (corner) continue;
                var border = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                pixels[y * size + x] = border ? dark : y <= 2 ? shade : light;
            }
        }

        // The letter E, 5 pixels wide and 7 high, in the middle of the key.
        string[] glyph = { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "XXXXX" };
        for (var row = 0; row < glyph.Length; row++)
            for (var column = 0; column < glyph[row].Length; column++)
                if (glyph[row][column] == 'X') pixels[(11 - row) * size + (5 + column)] = dark;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply();
        keySprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        return keySprite;
    }
}
