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
    private const string VolumePrefKey = "MusicVolume";

    private float musicVolume;
    private bool fading;

    // Volume of the music, from 0 to 1, kept between games.
    public float MusicVolume => musicVolume;

    public bool IsMuted { get; private set; }

    private AudioSource source;
    private AudioClip currentClip;
    private AudioClip storyMusic;
    private AudioClip cryptMusic;

    // True while the cinematic of the introduction plays (it runs over the main menu).
    public static bool IntroPlaying;
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
        try { musicVolume = PlayerPrefs.GetFloat(VolumePrefKey, volume); } catch (System.Exception) { musicVolume = volume; }
        source.mute = IsMuted;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        // A music that is not fading in or out follows the slider at once, up as well as down.
        if (source != null && !fading) source.volume = musicVolume;
        else if (source != null && source.isPlaying) source.volume = Mathf.Min(source.volume, musicVolume);
        try
        {
            PlayerPrefs.SetFloat(VolumePrefKey, musicVolume);
            PlayerPrefs.Save();
        }
        catch (System.Exception)
        {
        }
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
        if (scene.name == "MainMenu") IntroPlaying = false;
        PlayForScene(scene.name);
    }

    // Asks the music to follow the situation again (the story has moved on).
    public void Refresh()
    {
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    // The music of the story: the cinematic of the introduction, the walk through the town with Professor Thouvenot, and the house of the player.
    private AudioClip StoryClip(string sceneName)
    {
        // The crypt, and the walk that leads to it (the procession of Fabienne), have their own music.
        if (sceneName == "Crypte" || (sceneName == "Town" && StoryDirector.CryptMusic))
        {
            if (cryptMusic == null) cryptMusic = Resources.Load<AudioClip>("Music/Music_Crypte");
            if (cryptMusic != null) return cryptMusic;
        }

        var wanted = sceneName == "MaisonJoueur"
            || (sceneName == "MainMenu" && IntroPlaying)
            || (sceneName == "Town" && (StoryDirector.ArrivalPending || (StoryState.Instance != null && !StoryState.Instance.Legacy && StoryState.Instance.Stage <= StoryState.GoHome)));
        if (!wanted) return null;
        if (storyMusic == null) storyMusic = Resources.Load<AudioClip>("Music/Music_Histoire");
        return storyMusic;
    }

    private void PlayForScene(string sceneName)
    {
        var target = StoryClip(sceneName) != null ? StoryClip(sceneName) : sceneName == "Town" || sceneName == "GemStore" || sceneName == "WatchStore" || sceneName == "Animalerie" || sceneName == "Banque" || sceneName == "Bijouterie" || sceneName == "Concessionnaire" ? townMusic
            : sceneName.StartsWith("Labo") || sceneName == "Home" || sceneName == "MaisonJoueur" || sceneName.StartsWith("Diamantaires") ? laboratoryMusic
            : sceneName == "MainMenu" ? mainMenuMusic
            : sceneName == "Tunnel" || sceneName == "Crypte" ? tunnelMusic
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
        fading = true;
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

        if (clip == null)
        {
            fading = false;
            yield break;
        }

        source.clip = clip;
        source.Play();

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, musicVolume, t / fadeDuration);
            yield return null;
        }
        source.volume = musicVolume;
        fading = false;
    }
}
