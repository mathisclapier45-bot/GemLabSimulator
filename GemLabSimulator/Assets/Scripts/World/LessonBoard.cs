using UnityEngine;

// The board of the lessons, at the entrance of the laboratory: E opens the list of the lessons of Professor Thouvenot (see LessonMenuUI). It is not in the scene: the
// StoryDirector puts it there when the laboratory is loaded.
public class LessonBoard : MonoBehaviour
{
    // Against the south wall of the entrance room, opposite the mailbox.
    private static readonly Vector2 BoardPosition = new Vector2(-3.5f, -2.9f);

    private bool playerInRange;

    public static void Place()
    {
        if (FindFirstObjectByType<LessonBoard>() != null) return;

        var go = new GameObject("PanneauLecons");
        go.transform.position = BoardPosition;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = BoardSprite();
        renderer.sortingOrder = 4;

        var solid = go.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(0.9f, 0.35f);
        solid.offset = new Vector2(0f, 0.18f);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.8f, 2.4f);
        trigger.offset = new Vector2(0f, 0.7f);
        go.AddComponent<LessonBoard>();
        go.AddComponent<InteractPrompt>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.E)) return;
        if (PlayerController.InputLocked || DialogueBoxUI.IsShowing || StoryChoiceUI.IsOpen) return;
        LessonMenuUI.Show();
    }

    private static Sprite boardSprite;

    // A wooden easel with a sheet pinned on it: a small gem at the top and lines of writing.
    private static Sprite BoardSprite()
    {
        if (boardSprite != null) return boardSprite;

        const int w = 32;
        const int h = 40;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                texture.SetPixel(x, y, Color.clear);

        var wood = new Color(0.55f, 0.38f, 0.22f, 1f);
        var woodDark = new Color(0.38f, 0.26f, 0.15f, 1f);
        var paper = new Color(0.95f, 0.91f, 0.80f, 1f);
        var ink = new Color(0.40f, 0.34f, 0.28f, 1f);
        var gem = new Color(0.45f, 0.85f, 1f, 1f);

        // The legs.
        for (var y = 0; y < 14; y++)
        {
            texture.SetPixel(7 + y / 7, y, woodDark);
            texture.SetPixel(8 + y / 7, y, woodDark);
            texture.SetPixel(24 - y / 7, y, woodDark);
            texture.SetPixel(23 - y / 7, y, woodDark);
        }
        for (var y = 0; y < 10; y++)
        {
            texture.SetPixel(15, y, woodDark);
            texture.SetPixel(16, y, woodDark);
        }

        // The board and its sheet.
        for (var y = 12; y <= 37; y++)
        {
            for (var x = 2; x <= 29; x++)
            {
                var frame = x < 4 || x > 27 || y < 14 || y > 35;
                texture.SetPixel(x, y, frame ? wood : paper);
            }
        }
        for (var x = 2; x <= 29; x++) texture.SetPixel(x, 12, woodDark);

        // The gem and the lines.
        for (var d = 0; d < 3; d++)
        {
            for (var x = 8 - d; x <= 8 + d; x++)
            {
                texture.SetPixel(x, 32 - d, gem);
                texture.SetPixel(x, 28 + d, gem);
            }
        }
        for (var x = 13; x <= 24; x++) texture.SetPixel(x, 30, ink);
        var lengths = new[] { 20, 22, 14, 18 };
        for (var line = 0; line < lengths.Length; line++)
            for (var x = 6; x < 6 + lengths[line]; x++)
                texture.SetPixel(x, 24 - line * 3, ink);
        texture.Apply();

        boardSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
        return boardSprite;
    }
}
