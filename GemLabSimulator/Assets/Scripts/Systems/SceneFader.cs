using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.35f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void FadeToScene(string sceneName)
    {
        if (Instance == null || Instance.fadeImage == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Instance.StartCoroutine(Instance.FadeRoutine(sceneName));
    }

    private IEnumerator FadeRoutine(string sceneName)
    {
        yield return Fade(0f, 1f);
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return Fade(1f, 0f);
    }

    private IEnumerator Fade(float from, float to)
    {
        var baseColor = fadeImage.color;
        var t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            var a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            yield return null;
        }
        fadeImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
    }
}
