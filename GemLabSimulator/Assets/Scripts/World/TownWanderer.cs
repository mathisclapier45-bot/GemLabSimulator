using System;
using UnityEngine;

// Townsperson strolling inside a rectangular zone (a street or the main road): walks straight legs along one axis,
// stops for a moment, then picks another leg. No collider, so it never blocks the player.
public class TownWanderer : MonoBehaviour
{
    public int variant;
    public Rect zone = new Rect(-5f, -5f, 10f, 2f);
    public float speed = 1.4f;
    public float maxLegLength = 14f;

    private const float FrameDuration = 0.11f;

    private enum Facing { Right, Up, Left, Down }

    private SpriteRenderer spriteRenderer;
    private Sprite[][] frames;
    private Facing facing = Facing.Down;
    private Vector2 target;
    private float pauseTimer;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 4;

        var names = new[] { "Right", "Up", "Left", "Down" };
        frames = new Sprite[names.Length][];
        for (var i = 0; i < names.Length; i++)
        {
            var loaded = Resources.LoadAll<Sprite>($"Walkers/W{variant % 8}/{names[i]}");
            Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
            frames[i] = loaded;
        }

        pauseTimer = UnityEngine.Random.Range(0f, 3f);
        target = transform.position;
        ApplyIdle();
    }

    private void Update()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f) PickNextLeg();
            return;
        }

        var position = (Vector2)transform.position;
        var step = speed * Time.deltaTime;
        var toTarget = target - position;
        if (toTarget.magnitude <= step)
        {
            transform.position = new Vector3(target.x, target.y, 0f);
            pauseTimer = UnityEngine.Random.Range(1.5f, 6f);
            ApplyIdle();
            return;
        }

        transform.position = position + toTarget.normalized * step;

        frameTimer += Time.deltaTime;
        if (frameTimer >= FrameDuration)
        {
            frameTimer = 0f;
            var set = frames[(int)facing];
            if (set != null && set.Length > 0)
            {
                frameIndex = (frameIndex + 1) % set.Length;
                spriteRenderer.sprite = set[frameIndex];
            }
        }
    }

    private void PickNextLeg()
    {
        var position = (Vector2)transform.position;
        var horizontal = zone.width >= zone.height * 1.5f || (zone.height < zone.width * 1.5f && UnityEngine.Random.value < 0.5f);
        if (horizontal)
        {
            var minX = Mathf.Max(zone.xMin, position.x - maxLegLength);
            var maxX = Mathf.Min(zone.xMax, position.x + maxLegLength);
            var nextX = UnityEngine.Random.Range(minX, maxX);
            // Occasionally drift to the other side of a wide street.
            var nextY = UnityEngine.Random.value < 0.3f ? UnityEngine.Random.Range(zone.yMin, zone.yMax) : position.y;
            target = new Vector2(nextX, Mathf.Clamp(nextY, zone.yMin, zone.yMax));
            facing = target.x >= position.x ? Facing.Right : Facing.Left;
        }
        else
        {
            var minY = Mathf.Max(zone.yMin, position.y - maxLegLength);
            var maxY = Mathf.Min(zone.yMax, position.y + maxLegLength);
            var nextY = UnityEngine.Random.Range(minY, maxY);
            var nextX = UnityEngine.Random.value < 0.3f ? UnityEngine.Random.Range(zone.xMin, zone.xMax) : position.x;
            target = new Vector2(Mathf.Clamp(nextX, zone.xMin, zone.xMax), nextY);
            facing = target.y >= position.y ? Facing.Up : Facing.Down;
        }

        // A target barely different from the current spot would just look like jitter: rest again.
        if ((target - position).magnitude < 1.5f)
        {
            pauseTimer = UnityEngine.Random.Range(0.5f, 2f);
            target = position;
        }
    }

    private void ApplyIdle()
    {
        var set = frames[(int)facing];
        if (set != null && set.Length > 0)
        {
            frameIndex = 0;
            spriteRenderer.sprite = set[0];
        }
    }
}
