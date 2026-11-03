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
    private float sceneMoveSpeed;

    // The walk and idle sprites of one look, by direction (in the order of FacingDirection).
    private class LookSet
    {
        public readonly Sprite[][] Walk = new Sprite[4][];
        public readonly Sprite[] Idle = new Sprite[4];
        public bool Ready => Walk[0] != null && Walk[0].Length > 0 && Idle[0] != null;
    }

    // Bare feet, with the sneakers on, or riding the bicycle.
    private enum Look { Normal, Baskets, Bike }

    private readonly LookSet[] looks = new LookSet[3];
    private bool inTown;
    private Look look = Look.Normal;

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

        looks[(int)Look.Normal] = LoadLook("PlayerWalk", "PlayerIdle");
        looks[(int)Look.Baskets] = LoadLook("PlayerWalkBaskets", "PlayerIdleBaskets");
        looks[(int)Look.Bike] = LoadLook("PlayerWalkBike", "PlayerIdleBike");

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateMoveSpeedForScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateMoveSpeedForScene(string sceneName)
    {
        inTown = sceneName == "Town";
        sceneMoveSpeed = inTown ? moveSpeed * townSpeedMultiplier : moveSpeed;
        currentMoveSpeed = sceneMoveSpeed * SpeedBonus();
    }

    // The sneakers and the bicycle bought at the dealership only help outdoors: inside the buildings the player walks as usual.
    private float SpeedBonus()
    {
        var tools = PlayerOwnedTools.Instance;
        return inTown && tools != null ? tools.SpeedMultiplier : 1f;
    }

    private Look CurrentLookFor()
    {
        var tools = PlayerOwnedTools.Instance;
        var worn = tools != null ? tools.Mobility : "";
        // The player gets off the bicycle to go inside.
        if (worn == PlayerOwnedTools.BikeId && inTown && looks[(int)Look.Bike].Ready) return Look.Bike;
        if (worn == PlayerOwnedTools.SneakersId && looks[(int)Look.Baskets].Ready) return Look.Baskets;
        return Look.Normal;
    }

    private static LookSet LoadLook(string walkRoot, string idleRoot)
    {
        var set = new LookSet();
        var names = new[] { "Down", "Up", "Left", "Right" };
        for (var i = 0; i < names.Length; i++)
        {
            set.Walk[i] = LoadWalkFrames(walkRoot, names[i]);
            set.Idle[i] = Resources.Load<Sprite>($"{idleRoot}/{names[i]}");
        }
        return set;
    }

    private static Sprite[] LoadWalkFrames(string root, string direction)
    {
        var frames = Resources.LoadAll<Sprite>($"{root}/{direction}");
        Array.Sort(frames, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        return frames;
    }

    private Sprite[] CurrentFrames() => looks[(int)look].Walk[(int)facing];

    private Sprite CurrentIdleSprite() => looks[(int)look].Idle[(int)facing];

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
        currentMoveSpeed = sceneMoveSpeed * SpeedBonus();
        look = CurrentLookFor();

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
                var cycle = CurrentFrames().Length;
                frameIndex = (frameIndex + 1) % Mathf.Max(1, cycle);
                // One step each time a foot lands: twice per walk cycle.
                if (look != Look.Bike && frameIndex % Mathf.Max(1, cycle / 2) == 0) Sfx.Step();
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
