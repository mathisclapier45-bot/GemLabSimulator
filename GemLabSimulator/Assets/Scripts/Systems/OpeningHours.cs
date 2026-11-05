using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opening hours of the places of the town. The shops, the markets, the workshop and the museum open at 9h and close at 20h; the Diamond
// Exchange opens at 8h. The home, the laboratory, the manor and the tunnel have no hours.
public static class OpeningHours
{
    private struct Hours
    {
        public float Open;
        public float Close;
    }

    private static readonly Dictionary<string, Hours> Table = new Dictionary<string, Hours>
    {
        { "GemStore", new Hours { Open = 9f, Close = 20f } },
        { "WatchStore", new Hours { Open = 9f, Close = 20f } },
        { "Animalerie", new Hours { Open = 9f, Close = 20f } },
        { "AtelierLapidaire", new Hours { Open = 9f, Close = 20f } },
        { "Marche", new Hours { Open = 9f, Close = 20f } },
        { "MarcheBrutes", new Hours { Open = 9f, Close = 20f } },
        { "Musee", new Hours { Open = 9f, Close = 20f } },
        { "Banque", new Hours { Open = 9f, Close = 20f } },
        { "Bijouterie", new Hours { Open = 9f, Close = 20f } },
        { "Concessionnaire", new Hours { Open = 9f, Close = 20f } },
        { "Diamantaires", new Hours { Open = 8f, Close = 20f } },
    };

    // The hour of the in-game day, from 0 to 24.
    public static float CurrentHour => DayNightOverlay.GetCurrentPhase() * 24f;

    private static bool TryGet(string sceneName, out Hours hours)
    {
        hours = default;
        if (string.IsNullOrEmpty(sceneName)) return false;
        // Every floor of the tower belongs to the Diamond Exchange.
        var key = sceneName.StartsWith("Diamantaires") ? "Diamantaires" : sceneName;
        return Table.TryGetValue(key, out hours);
    }

    public static bool HasHours(string sceneName) => TryGet(sceneName, out _);

    public static bool IsOpen(string sceneName)
    {
        if (!TryGet(sceneName, out var hours)) return true;
        var now = CurrentHour;
        return now >= hours.Open && now < hours.Close;
    }

    public static string Describe(string sceneName)
    {
        return TryGet(sceneName, out var hours) ? $"{hours.Open:0}h à {hours.Close:0}h" : "";
    }
}

// Closing time: someone still inside a shop when it closes is politely shown the door.
public class ClosingTimeWatcher : MonoBehaviour
{
    private const float GraceSeconds = 8f;

    private bool warned;
    private float warnedAt;
    private string sceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach(scene);
    }

    private static void Attach(Scene scene)
    {
        if (!OpeningHours.HasHours(scene.name)) return;
        var go = new GameObject("ClosingTimeWatcher");
        go.AddComponent<ClosingTimeWatcher>().sceneName = scene.name;
    }

    private void Update()
    {
        if (warned)
        {
            if (Time.unscaledTime - warnedAt >= GraceSeconds)
            {
                warned = false;
                Leave();
                enabled = false;
            }
            return;
        }

        if (OpeningHours.IsOpen(sceneName)) return;
        warned = true;
        warnedAt = Time.unscaledTime;
        ToolFeedbackUI.Instance?.ShowMessage($"C'est l'heure de la fermeture : il faut sortir. (Ouvert de {OpeningHours.Describe(sceneName)}.)");
    }

    private void Leave()
    {
        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
        {
            if (door.TargetScene != "Town") continue;
            door.Use();
            return;
        }

        // The upper floors of the tower have no door: leave by the entrance hall.
        PlayerController.PendingSpawnPointName = "SpawnPoint_Diamantaires";
        SceneFader.FadeToScene("Town");
    }
}
