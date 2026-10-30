using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

// Makes the game fill the whole window: the pixel-perfect camera no longer crops the picture to its reference
// resolution (which left black bars), it shows more of the world instead, at a whole-number zoom.
// F11 (or Alt+Enter) switches between full screen and windowed mode. Runs on every scene, so existing scenes
// need no regeneration.
public class FullScreenView : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("FullScreenView");
        DontDestroyOnLoad(go);
        go.AddComponent<FullScreenView>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    private static void Apply()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var pixelPerfect = cam.GetComponent<PixelPerfectCamera>();
        if (pixelPerfect == null) return;

        pixelPerfect.cropFrameX = false;
        pixelPerfect.cropFrameY = false;
        pixelPerfect.upscaleRT = false;
    }

    private void Update()
    {
        var altEnter = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.Return);
        if (Input.GetKeyDown(KeyCode.F11) || altEnter)
            Screen.fullScreen = !Screen.fullScreen;
    }
}
