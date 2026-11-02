using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip townMusic;
    [SerializeField] private AudioClip laboratoryMusic;
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip tunnelMusic;
    [SerializeField] private AudioClip manoirMusic;
    [SerializeField] private AudioClip marketMusic;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float volume = 0.5f;

    private const string MutedPrefKey = "MusicMuted";

    public bool IsMuted { get; private set; }

    private AudioSource source;
    private AudioClip currentClip;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = 0f;

        try { IsMuted = PlayerPrefs.GetInt(MutedPrefKey, 0) == 1; } catch (System.Exception) { IsMuted = false; }
        source.mute = IsMuted;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        if (source != null) source.mute = muted;
        try
        {
            PlayerPrefs.SetInt(MutedPrefKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
        catch (System.Exception)
        {
        }
    }

    private void Start()
    {
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        var target = sceneName == "Town" || sceneName == "GemStore" || sceneName == "WatchStore" || sceneName == "Animalerie" ? townMusic
            : sceneName == "Laboratoire" || sceneName == "Home" || sceneName == "MaisonJoueur" || sceneName.StartsWith("Diamantaires") ? laboratoryMusic
            : sceneName == "MainMenu" ? mainMenuMusic
            : sceneName == "Tunnel" ? tunnelMusic
            : sceneName == "Manoir" ? manoirMusic
            : sceneName == "Marche" || sceneName == "MarcheBrutes" ? marketMusic
            : null;

        if (target == currentClip) return;
        currentClip = target;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToClip(target));
    }

    private IEnumerator FadeToClip(AudioClip clip)
    {
        var startVolume = source.volume;
        var t = 0f;
        while (t < fadeDuration && source.volume > 0f)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        source.volume = 0f;
        source.Stop();

        if (clip == null) yield break;

        source.clip = clip;
        source.Play();

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, volume, t / fadeDuration);
            yield return null;
        }
        source.volume = volume;
    }
}
