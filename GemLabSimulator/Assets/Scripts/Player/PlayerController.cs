using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    // Set by DoorTrigger just before loading a scene, so the player lands at a door-specific spawn point instead of the default one.
    public static string PendingSpawnPointName;

    // Set by full-screen interfaces (the lab computer) so the arrow keys do not walk the player around behind them.
    public static bool InputLocked;

    private enum FacingDirection { Down, Up, Left, Right }

    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float townSpeedMultiplier = 1.6f;
    private const float FrameDuration = 0.12f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private SpriteRenderer spriteRenderer;
    private float currentMoveSpeed;

    private Sprite[] downFrames;
    private Sprite[] upFrames;
    private Sprite[] leftFrames;
    private Sprite[] rightFrames;

    private Sprite downIdle;
    private Sprite upIdle;
    private Sprite leftIdle;
    private Sprite rightIdle;

    private FacingDirection facing = FacingDirection.Down;
    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        downFrames = LoadWalkFrames("Down");
        upFrames = LoadWalkFrames("Up");
        leftFrames = LoadWalkFrames("Left");
        rightFrames = LoadWalkFrames("Right");

        downIdle = Resources.Load<Sprite>("PlayerIdle/Down");
        upIdle = Resources.Load<Sprite>("PlayerIdle/Up");
        leftIdle = Resources.Load<Sprite>("PlayerIdle/Left");
        rightIdle = Resources.Load<Sprite>("PlayerIdle/Right");

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateMoveSpeedForScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateMoveSpeedForScene(string sceneName)
    {
        currentMoveSpeed = sceneName == "Town" ? moveSpeed * townSpeedMultiplier : moveSpeed;
    }

    private static Sprite[] LoadWalkFrames(string direction)
    {
        var frames = Resources.LoadAll<Sprite>($"PlayerWalk/{direction}");
        Array.Sort(frames, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return frames;
    }

    private Sprite[] CurrentFrames()
    {
        switch (facing)
        {
            case FacingDirection.Up: return upFrames;
            case FacingDirection.Left: return leftFrames;
            case FacingDirection.Right: return rightFrames;
            default: return downFrames;
        }
    }

    private Sprite CurrentIdleSprite()
    {
        switch (facing)
        {
            case FacingDirection.Up: return upIdle;
            case FacingDirection.Left: return leftIdle;
            case FacingDirection.Right: return rightIdle;
            default: return downIdle;
        }
    }

    private void Start()
    {
        TryLoad();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateMoveSpeedForScene(scene.name);

        var spawnName = string.IsNullOrEmpty(PendingSpawnPointName) ? "SpawnPoint" : PendingSpawnPointName;
        var spawn = GameObject.Find(spawnName) ?? GameObject.Find("SpawnPoint");
        if (spawn != null) transform.position = spawn.transform.position;
        PendingSpawnPointName = null;

        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.SetTarget(transform);

        TryLoad();
    }

    private static void TryLoad()
    {
        if (!SaveSystem.PendingLoad) return;
        SaveSystem.PendingLoad = false;
        SaveSystem.Load();
    }

    private void Update()
    {
        moveInput.x = InputLocked ? 0f : Input.GetAxisRaw("Horizontal");
        moveInput.y = InputLocked ? 0f : Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        AnimateSprite();
    }

    private void AnimateSprite()
    {
        var isMoving = moveInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            facing = Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y)
                ? (moveInput.x > 0 ? FacingDirection.Right : FacingDirection.Left)
                : (moveInput.y > 0 ? FacingDirection.Up : FacingDirection.Down);

            var frameDuration = FrameDuration * (moveSpeed / currentMoveSpeed);
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % CurrentFrames().Length;
            }
        }
        else
        {
            frameIndex = 0;
            frameTimer = 0f;

            var idleSprite = CurrentIdleSprite();
            if (idleSprite != null && spriteRenderer != null)
                spriteRenderer.sprite = idleSprite;
            return;
        }

        var frames = CurrentFrames();
        if (frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[frameIndex];
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * currentMoveSpeed * Time.fixedDeltaTime);
    }
}
