using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The prologue in the game: at the airport of Crystal Grove, Professor Thouvenot welcomes the player and walks them through the town (the GemStore, the library,
// the house of Vincent, the office of gemology with the classic instruments), up to the little house of the player, where Vincent's letter waits in the mailbox. She walks ahead along the streets and
// waits for the player; every line stays on the screen until it is clicked. The progress is saved, so a walk that is left goes on where it stopped.
// Lives for the whole game (added when the game starts).
public class StoryDirector : MonoBehaviour
{
    public const string ArrivalSpawnPoint = "SpawnPoint_Aeroport";

    // Set by the main menu when the cinematic ends: the player lands at the airport as soon as the town is loaded.
    public static bool ArrivalPending;

    // In front of the terminal, on the street of the airport.
    private static readonly Vector2 ArrivalPoint = new Vector2(55f, 4f);

    private const string Speaker = "Professeure Thouvenot";
    private const float WalkSpeed = 5.5f;
    private const float FollowDistance = 13f;
    private const float TalkDistance = 6.5f;

    private class Stop
    {
        public Vector2[] Route;        // the places she walks through to get there; the last one is where she waits
        public string[] Lines;
        public string Remark;          // what she says on the way, halfway, or null
    }

    // The streets of the town: the avenue of the airport and the south side of the buildings (y = 4), the main road going north (x = 0), the north road (y = 25, in front of the office of gemology),
    // the road of the south bank (y = -18) and the path to the house of the player (x = -52).
    private static readonly Stop[] Stops =
    {
        new Stop
        {
            Route = new Vector2[0],
            Lines = new[]
            {
                "Vous devez être la personne que Vincent attendait ! Je suis la professeure Thouvenot. J'enseigne la gemmologie ici, à Crystal Grove.",
                "Bienvenue. Il se fait tard et les boutiques sont déjà fermées, mais je vais vous montrer où se trouvent les endroits importants de la ville. Suivez-moi : je vous attends à chaque arrêt.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(34f, 4f) },
            Remark = "Vincent marchait par ici tous les matins, il y a encore peu. C'était un grand homme, vous savez.",
            Lines = new[]
            {
                "Voici le GemStore. C'est là qu'on achète les instruments : loupes, balances, réfractomètre... Votre oncle y passait des heures.",
                "« Un gemmologue n'est rien sans ses instruments », disait-il. Commencez par une loupe : on découvre déjà beaucoup de choses avec elle.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(-47f, 4f) },
            Lines = new[]
            {
                "La bibliothèque de la ville. On y trouve des livres de référence, comme le Guide du gemmologue : toutes les pierres, leurs propriétés, leurs traitements.",
                "Quand une pierre vous résiste, c'est ici qu'il faut venir chercher la réponse.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(-56f, 4f), new Vector2(-56f, 8.2f) },
            Lines = new[]
            {
                "Et voici la maison de Vincent. Elle est fermée, hélas : les clefs sont introuvables. Personne n'a pu y entrer depuis sa disparition.",
                "Il semblait inquiet, ces dernières semaines. Il parlait peu, il regardait derrière lui... Puis un matin, il n'était plus là.",
                "Je ne sais pas ce qu'il cherchait à fuir. Mais une chose est sûre : il avait confiance en vous.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(-56f, 4f), new Vector2(0f, 4f), new Vector2(0f, 25f), new Vector2(-55f, 25f) },
            Remark = "Vincent répétait qu'il faut bien regarder avant de croire. C'est tout l'art de notre métier.",
            Lines = new[]
            {
                "Voici la Maison et Bureau de Gemmologie : le laboratoire de Vincent, avec les instruments classiques. C'est le vôtre, désormais : il vous l'a légué.",
                "Les gens envoient des pierres à identifier par la poste. Vous les examinerez avec vos instruments, puis vous remettrez vos rapports au directeur : c'est ainsi qu'on gagne sa réputation.",
                "Le grand laboratoire de gemmologie, un peu plus loin, est fermé pour le moment : ses instruments sont bien trop complexes pour débuter. Apprenez d'abord avec ceux-ci.",
                "D'autres lieux resteront fermés tant que vous n'aurez pas fait vos preuves. Mais allons, il ne reste que votre maison.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(0f, 25f), new Vector2(0f, -18f), new Vector2(-52f, -18f), new Vector2(-52f, -44f) },
            Lines = new[]
            {
                "Et voici votre maison. Petite, mais agréable. C'est là que vous dormirez, et que votre partie sera sauvegardée.",
                "Je crois qu'il y a du courrier pour vous dans la boîte aux lettres, près de l'entrée. Entrez donc.",
                "Passez me voir au laboratoire quand vous voulez. Et... bonne chance.",
            },
        },
    };

    private Transform thouvenot;
    private SpriteRenderer thouvenotSprite;
    private Vector2 basePosition;
    private float walkClock;
    private float followHintTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("StoryDirector");
        DontDestroyOnLoad(go);
        go.AddComponent<StoryDirector>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        DialogueBoxUI.Hide();
        thouvenot = null;

        if (scene.name == "Town") StartCoroutine(RunTown());
        else if (scene.name == "MaisonJoueur") StartCoroutine(RunHouse());
    }

    // ---- Helpers ---------------------------------------------------------------------------------------------------------

    private static Transform PlayerTransform => PlayerController.Instance != null ? PlayerController.Instance.transform : null;

    private static float DistanceToPlayer(Vector2 point)
    {
        var player = PlayerTransform;
        return player != null ? Vector2.Distance(player.position, point) : 0f;
    }

    // The player and the state are there once the game has loaded the save.
    private IEnumerator WaitForGame()
    {
        for (var i = 0; i < 180; i++)
        {
            if (PlayerController.Instance != null && !SaveSystem.PendingLoad && StoryState.Instance != null) break;
            yield return null;
        }
        yield return new WaitForSeconds(0.6f);
    }

    private void SpawnThouvenot(Vector2 position)
    {
        var go = new GameObject(Speaker);
        thouvenotSprite = go.AddComponent<SpriteRenderer>();
        thouvenotSprite.sprite = Resources.Load<Sprite>("Story/Thouvenot");
        thouvenotSprite.sortingOrder = 4;
        go.transform.position = position;
        thouvenot = go.transform;
        basePosition = position;
    }

    // ---- The house -------------------------------------------------------------------------------------------------------

    private IEnumerator RunHouse()
    {
        yield return WaitForGame();
        var state = StoryState.Instance;
        if (state == null || state.Legacy || state.Stage != StoryState.GoHome || state.UnreadCount == 0) yield break;

        yield return new WaitForSeconds(1.2f);
        ToolFeedbackUI.Instance?.ShowMessage("Du courrier vous attend : la boîte aux lettres est près de l'entrée (touche E).");
    }

    // ---- The walk through the town ---------------------------------------------------------------------------------------

    private IEnumerator RunTown()
    {
        // The arrival: the player lands in front of the terminal, before the screen has even faded in.
        if (ArrivalPending)
        {
            for (var i = 0; i < 120 && PlayerController.Instance == null; i++) yield return null;
            var landing = PlayerTransform;
            if (landing != null) landing.position = ArrivalPoint;
            ArrivalPending = false;
        }

        yield return WaitForGame();
        MusicManager.Instance?.Refresh();
        var state = StoryState.Instance;
        if (state == null || state.Legacy || state.Stage >= StoryState.GoHome)
        {
            DayNightOverlay.Unfreeze();
            yield break;
        }

        // The night does not move during the walk: it is nightfall, 21h30, until the player gets home.
        DayNightOverlay.Freeze();

        // A game that was left before the walk began: the player is put back at the airport.
        var start = state.Stage == StoryState.Arrival || state.TourIndex <= 0;
        if (state.Stage == StoryState.Arrival && DistanceToPlayer(ArrivalPoint) > 30f)
        {
            var player = PlayerTransform;
            if (player != null) player.position = ArrivalPoint;
        }

        var index = Mathf.Clamp(state.TourIndex, 0, Stops.Length - 1);
        Vector2 from;
        if (start) from = new Vector2(ArrivalPoint.x - 2.5f, ArrivalPoint.y);
        else
        {
            var previous = Stops[index - 1].Route;
            from = previous.Length > 0 ? previous[previous.Length - 1] : ArrivalPoint;
        }
        SpawnThouvenot(from);

        for (; index < Stops.Length; index++)
        {
            state.SetStage(StoryState.Tour, index);
            var stop = Stops[index];

            if (stop.Route.Length > 0) yield return WalkRoute(from, stop);
            yield return WaitForPlayer(TalkDistance);
            yield return Talk(stop.Lines);
            if (stop.Route.Length > 0) from = stop.Route[stop.Route.Length - 1];
        }

        // The walk is over: the letter of Vincent is in the mailbox of the house.
        state.Receive(StoryLetters.Welcome);
        state.SetStage(StoryState.GoHome);
        DayNightOverlay.Unfreeze();
        ToolFeedbackUI.Instance?.ShowMessage("Entrez dans votre maison : une lettre de Vincent vous attend dans la boîte aux lettres.");

        // She goes back to the laboratory: she fades away.
        for (var t = 0f; t < 1.2f && thouvenotSprite != null; t += Time.deltaTime)
        {
            thouvenotSprite.color = new Color(1f, 1f, 1f, 1f - t / 1.2f);
            yield return null;
        }
        if (thouvenot != null) Destroy(thouvenot.gameObject);
    }

    // Walks along the route; halfway, if there is something to say, stops and says it.
    private IEnumerator WalkRoute(Vector2 from, Stop stop)
    {
        var total = 0f;
        var last = from;
        foreach (var point in stop.Route)
        {
            total += Vector2.Distance(last, point);
            last = point;
        }

        var walked = 0f;
        var remarkDone = stop.Remark == null;
        var position = from;
        foreach (var point in stop.Route)
        {
            while (Vector2.Distance(position, point) > 0.05f)
            {
                // She waits for a player who is far behind.
                if (DistanceToPlayer(position) > FollowDistance)
                {
                    followHintTimer -= Time.deltaTime;
                    if (followHintTimer <= 0f)
                    {
                        followHintTimer = 14f;
                        ToolFeedbackUI.Instance?.ShowMessage($"{Speaker} vous attend : suivez-la !");
                    }
                    yield return null;
                    continue;
                }

                var step = Mathf.Min(Vector2.Distance(position, point), WalkSpeed * Time.deltaTime);
                position = Vector2.MoveTowards(position, point, step);
                walked += step;
                walkClock += Time.deltaTime;
                Place(position, true);

                if (!remarkDone && walked >= total * 0.45f)
                {
                    remarkDone = true;
                    Place(position, false);
                    yield return WaitForPlayer(TalkDistance);
                    yield return Talk(new[] { stop.Remark });
                }
                yield return null;
            }
        }
        Place(position, false);
    }

    private void Place(Vector2 position, bool walking)
    {
        if (thouvenot == null) return;
        basePosition = position;
        var bob = walking ? Mathf.Abs(Mathf.Sin(walkClock * 11f)) * 0.07f : 0f;
        thouvenot.position = new Vector3(position.x, position.y + bob, 0f);
    }

    private IEnumerator WaitForPlayer(float distance)
    {
        while (thouvenot != null && DistanceToPlayer(basePosition) > distance)
        {
            followHintTimer -= Time.deltaTime;
            if (followHintTimer <= 0f)
            {
                followHintTimer = 14f;
                ToolFeedbackUI.Instance?.ShowMessage($"{Speaker} vous attend : rejoignez-la.");
            }
            yield return null;
        }
    }

    private IEnumerator Talk(string[] lines)
    {
        foreach (var line in lines)
        {
            var advanced = false;
            DialogueBoxUI.SayAndWait(Speaker, line, thouvenot != null ? thouvenot.gameObject : null, () => advanced = true);
            while (!advanced) yield return null;
        }
        DialogueBoxUI.Hide();
    }
}
