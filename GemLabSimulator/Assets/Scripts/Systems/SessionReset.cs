using UnityEngine;
using UnityEngine.SceneManagement;

// What lives on the player or in static fields survives a trip to the main menu: between two games it is put back in its first state (the mailbox, the quests, the
// requests of the manor, what the story director has already said, the announcement of the day, the grading sessions). Run when the main menu loads and when a save is loaded.
public static class SessionReset
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") Run();
    }

    public static void Run()
    {
        MailboxState.Instance?.ResetSession();
        QuestState.Instance?.ResetSession();
        ManoirRequestState.Instance?.ResetSession();
        StoryDirector.ResetSession();
        CalendarAnnouncer.ResetDay();
        GradingUI.ResetSessions();
    }
}
