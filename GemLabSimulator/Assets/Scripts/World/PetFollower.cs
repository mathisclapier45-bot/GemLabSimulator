using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The equipped animal in the world. It walks the path the player has walked (so it never cuts through a wall), keeps a short distance
// behind, stands still when the player stops, and is brought next to the player whenever a scene loads.
public class PetFollower : MonoBehaviour
{
    private const float FollowGap = 1.1f;      // distance kept behind the player
    private const float RecordStep = 0.15f;    // spacing of the points of the player's trail
    private const float TeleportDistance = 14f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames = new Sprite[0];
    private PetCatalog.Pet pet;
    private bool ready;

    private readonly Queue<Vector2> trail = new Queue<Vector2>();
    private Vector2 lastRecorded;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 4;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Setup(string petId)
    {
        ready = PetCatalog.TryGet(petId, out pet);
        frames = ready ? PetCatalog.LoadFrames(petId) : new Sprite[0];
        frameIndex = 0;
        if (frames.Length > 0) spriteRenderer.sprite = frames[0];
        Teleport();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Teleport();
    }

    // Next to the player, with no trail behind.
    private void Teleport()
    {
        trail.Clear();
        var player = PlayerController.Instance;
        if (player == null) return;

        transform.position = player.transform.position + new Vector3(0f, -0.5f, 0f);
        lastRecorded = player.transform.position;
    }

    private void Update()
    {
        var player = PlayerController.Instance;
        var visible = ready && player != null && frames.Length > 0 && SceneManager.GetActiveScene().name != "MainMenu";
        spriteRenderer.enabled = visible;
        if (!visible) return;

        Vector2 playerPosition = player.transform.position;
        if (Vector2.Distance(transform.position, playerPosition) > TeleportDistance)
        {
            Teleport();
            return;
        }

        // The player's path, a point every few steps.
        if (trail.Count == 0 || Vector2.Distance(lastRecorded, playerPosition) >= RecordStep)
        {
            trail.Enqueue(playerPosition);
            lastRecorded = playerPosition;
        }

        var moving = false;
        var movement = Vector2.zero;
        var remaining = PathLength(playerPosition);
        if (remaining > FollowGap && trail.Count > 0)
        {
            // Speeds up when it falls behind.
            var speed = Mathf.Clamp(4f + (remaining - FollowGap) * 3f, 3f, 14f);
            var target = trail.Peek();
            var current = (Vector2)transform.position;
            var step = Vector2.MoveTowards(current, target, speed * Time.deltaTime);
            movement = step - current;
            transform.position = new Vector3(step.x, step.y, transform.position.z);
            if (Vector2.Distance(step, target) < 0.02f) trail.Dequeue();
            moving = movement.sqrMagnitude > 1e-6f;
        }

        if (Mathf.Abs(movement.x) > 0.001f) spriteRenderer.flipX = movement.x < 0f;
        Animate(moving);

        // In front of the player when it is below him, behind when it is above.
        var playerRenderer = player.GetComponent<SpriteRenderer>();
        var baseOrder = playerRenderer != null ? playerRenderer.sortingOrder : 5;
        spriteRenderer.sortingOrder = transform.position.y < playerPosition.y ? baseOrder + 1 : baseOrder - 1;
    }

    // Length of the walk from the animal to the player along the trail.
    private float PathLength(Vector2 playerPosition)
    {
        var length = 0f;
        var previous = (Vector2)transform.position;
        foreach (var point in trail)
        {
            length += Vector2.Distance(previous, point);
            previous = point;
        }
        return length + Vector2.Distance(previous, playerPosition);
    }

    private void Animate(bool moving)
    {
        if (frames.Length == 0) return;

        if (!moving)
        {
            // A snake keeps its last pose; the others stand.
            if (!pet.WalkUsesAllFrames) spriteRenderer.sprite = frames[0];
            frameTimer = 0f;
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer < pet.FrameSeconds) return;
        frameTimer = 0f;

        if (pet.WalkUsesAllFrames)
        {
            frameIndex = (frameIndex + 1) % frames.Length;
        }
        else
        {
            // Walk cycle: frames 1 and 2 (frame 0 is the standing pose).
            frameIndex = frameIndex >= frames.Length - 1 || frameIndex < 1 ? 1 : frameIndex + 1;
        }
        spriteRenderer.sprite = frames[frameIndex];
    }
}
