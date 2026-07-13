using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        TryLoad();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var spawn = GameObject.Find("SpawnPoint");
        if (spawn != null) transform.position = spawn.transform.position;

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
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
