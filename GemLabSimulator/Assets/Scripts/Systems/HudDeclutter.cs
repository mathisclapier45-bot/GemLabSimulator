using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the screen clear: hides the key reminders and the day counter that older generated scenes still carry
// (new scenes no longer create them). The day is still shown in the inventory (Tab); the money counter stays.
public class HudDeclutter : MonoBehaviour
{
    private static readonly string[] HiddenNames =
    {
        "InventoryHint", "GrimoireHint", "MenuHint", "QuestHint", "MapHint", "DayText",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("HudDeclutter");
        DontDestroyOnLoad(go);
        go.AddComponent<HudDeclutter>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Hide();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Hide();
    }

    private static void Hide()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        foreach (var name in HiddenNames)
        {
            var child = canvas.transform.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
    }
}
