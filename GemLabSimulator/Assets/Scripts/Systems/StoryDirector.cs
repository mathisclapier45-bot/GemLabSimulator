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
    private const float TownWalkSpeed = 8f;            // in the streets (the player walks at 6.4 in town; she went at 5.5 before)
    private const float LabWalkSpeed = 5.5f;           // inside the office the rooms are small
    private float walkSpeed = TownWalkSpeed;
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

    // The visit of the office of gemology (the morning after the arrival). The route is in the coordinates of the scene "Home": the entrance room with the mailbox and
    // the wardrobes, the door in the divider (x = 6, y between -1 and 1), and the desks of the instruments along the north wall of the lab.
    private static readonly Vector2 LabStart = new Vector2(1.6f, -0.6f);
    private static readonly Stop[] LabStops =
    {
        new Stop
        {
            Route = new Vector2[0],
            Lines = new[]
            {
                "Ah, vous voilà ! Bienvenue dans la Maison et Bureau de Gemmologie. Hier soir, tout allait trop vite pour que je vous montre quoi que ce soit : prenons le temps, maintenant.",
                "La gemmologie, c'est d'abord de la patience. On ne devine pas une pierre : on la mesure, instrument après instrument, et on conclut seulement à la fin.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(3.2f, -1.3f) },
            Lines = new[]
            {
                "Voici la boîte aux lettres du laboratoire. Les gens y envoient des pierres à identifier, avec un mot pour vous dire ce qu'ils attendent. Regardez-la chaque matin.",
                "Quand vous prenez une pierre, elle va dans votre inventaire. Et c'est de là que vous la déposerez sous un instrument.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(0.6f, 1.4f) },
            Lines = new[]
            {
                "Et ici, les armoires. Les deux armoires bleues gardent les commandes des clients : les pierres qu'on vous a confiées pour un travail précis.",
                "Les armoires en bois accueillent vos dons et vos pierres de côté. Rangez-y ce que vous ne voulez pas garder dans les poches : l'inventaire n'est pas extensible.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(0.6f, 0f), new Vector2(6.5f, 0f), new Vector2(8.6f, 1.3f) },
            Remark = "Passez par la porte du fond : c'est là que vivent les instruments.",
            Lines = new[]
            {
                "Commençons par le plus important : la loupe. C'est le premier regard sur une pierre. Éclat, transparence, inclusions, défauts de surface : on y apprend déjà beaucoup.",
                "Un conseil que Vincent me répétait sans cesse : essuyez toujours la pierre à la chamoisine avant de la regarder. Cette peau de chamois est posée sur le bureau, à côté de la loupe.",
                "Sans cela, la poussière et le gras des doigts laissent des traces blanchâtres sur la surface, et vous ne verrez rien d'utile. Dans la fenêtre de la loupe, prenez la chamoisine et frottez la pierre jusqu'à ce qu'elle soit nette.",
            },
        },
        new Stop
        {
            Route = new[] { new Vector2(12.5f, 1.3f) },
            Lines = new[]
            {
                "Ensuite, le réfractomètre, qui donne l'indice de réfraction, et la balance hydrostatique, qui donne la densité. Avec la loupe, ce sont vos trois outils principaux : ils suffisent à identifier la plupart des pierres courantes.",
                "Les autres instruments, plus loin sur cette rangée, se découvrent avec le temps. Avancez doucement : chaque mesure compte.",
                "Voilà ! Prenez une pierre dans la boîte aux lettres, essuyez-la, observez-la à la loupe. Et si vous avez besoin de moi, appelez-moi sur votre téléphone. Bon courage !",
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

        if (scene.name == "Town")
        {
            StartCoroutine(RunTown());
            StartCoroutine(RunMorningCall());
        }
        else if (scene.name == "MaisonJoueur") StartCoroutine(RunHouse());
        else if (scene.name == "Home")
        {
            AddLoupeCloth();
            StartCoroutine(RunLab());
        }
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

    // ---- The morning call --------------------------------------------------------------------------------------------------

    // The morning after the arrival, as soon as the player is out of the house: the phone rings until it is clicked.
    private IEnumerator RunMorningCall()
    {
        yield return WaitForGame();
        var state = StoryState.Instance;
        if (state == null || state.Legacy || state.Phase != StoryState.PhaseCall) yield break;

        yield return new WaitForSeconds(2.5f);
        for (var i = 0; i < 120 && PhoneUI.Instance == null; i++) yield return null;
        if (PhoneUI.Instance == null || state.Phase != StoryState.PhaseCall) yield break;

        ToolFeedbackUI.Instance?.ShowMessage("Votre téléphone sonne : cliquez sur le petit téléphone, en haut à droite.");
        PhoneUI.Instance.Ring(Speaker, () => StartCoroutine(AnswerMorningCall(state)));
    }

    private IEnumerator AnswerMorningCall(StoryState state)
    {
        var lines = new[]
        {
            "Allô ? Bonjour, c'est la professeure Thouvenot. J'espère que vous avez bien dormi : hier soir, la journée a été longue pour vous.",
            "Je vous appelle parce que j'aimerais vous proposer quelque chose. Venez au laboratoire, la Maison et Bureau de Gemmologie, et nous commencerons ensemble l'étude de la gemmologie. Vincent l'aurait voulu.",
            "C'est au nord-ouest de la ville, tout au bout de la grande route du nord. Votre minimap vous aidera. Je vous attends là-bas. À tout de suite !",
        };
        var finished = false;
        PhoneUI.Instance.Speak(lines, () => finished = true);
        while (!finished) yield return null;

        state.AddCall($"Entrant — {Speaker} — {DayNightOverlay.GetClockLabel()}");
        state.SetPhase(StoryState.PhaseLab);
        ToolFeedbackUI.Instance?.ShowMessage("Rendez-vous à la Maison et Bureau de Gemmologie, au nord-ouest de la ville.");
    }

    // ---- The visit of the laboratory -----------------------------------------------------------------------------------------

    private IEnumerator RunLab()
    {
        yield return WaitForGame();
        var state = StoryState.Instance;
        if (state == null || state.Legacy || state.Phase != StoryState.PhaseLab) yield break;

        walkSpeed = LabWalkSpeed;
        SpawnThouvenot(LabStart);
        var from = LabStart;
        for (var index = 0; index < LabStops.Length; index++)
        {
            var stop = LabStops[index];
            if (stop.Route.Length > 0) yield return WalkRoute(from, stop);
            yield return WaitForPlayer(TalkDistance);
            yield return Talk(stop.Lines);
            if (stop.Route.Length > 0) from = stop.Route[stop.Route.Length - 1];
        }

        // The visit is over: the game is free, she goes back to her work.
        state.SetPhase(StoryState.PhaseDone);
        ToolFeedbackUI.Instance?.ShowMessage("Prenez une pierre dans la boîte aux lettres, essuyez-la à la chamoisine, puis observez-la à la loupe.");
        for (var t = 0f; t < 1.2f && thouvenotSprite != null; t += Time.deltaTime)
        {
            thouvenotSprite.color = new Color(1f, 1f, 1f, 1f - t / 1.2f);
            yield return null;
        }
        if (thouvenot != null) Destroy(thouvenot.gameObject);
    }

    // The chamois cloth lies on the desk of the loupe (the scene has no such prop): a folded piece of yellow leather.
    private static Texture2D clothTexture;

    private static void AddLoupeCloth()
    {
        if (GameObject.Find("Chamoisine") != null) return;
        var desk = GameObject.Find("Bureau_Loupe");
        if (desk == null) return;

        if (clothTexture == null)
        {
            const int w = 14;
            const int h = 9;
            clothTexture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var leather = new Color(0.86f, 0.70f, 0.30f, 1f);
            var shade = new Color(0.68f, 0.52f, 0.20f, 1f);
            var light = new Color(0.95f, 0.82f, 0.45f, 1f);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    // A rounded rectangle, folded in two (a darker line across it), thicker at its lower edge.
                    var dx = Mathf.Min(x, w - 1 - x);
                    var dy = Mathf.Min(y, h - 1 - y);
                    var corner = dx + dy < 2;
                    if (corner) { clothTexture.SetPixel(x, y, Color.clear); continue; }
                    var color = leather;
                    if (y == 0 || x == w - 1) color = shade;
                    else if (y == h - 1 || x == 0) color = light;
                    else if (y == 4) color = shade;
                    clothTexture.SetPixel(x, y, color);
                }
            }
            clothTexture.Apply();
        }

        var go = new GameObject("Chamoisine");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(clothTexture, new Rect(0, 0, clothTexture.width, clothTexture.height), new Vector2(0.5f, 0f), 32f);
        sr.sortingOrder = 6;
        go.transform.position = desk.transform.position + new Vector3(0.52f, 0.9f, 0f);
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

        walkSpeed = TownWalkSpeed;

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

                var step = Mathf.Min(Vector2.Distance(position, point), walkSpeed * Time.deltaTime);
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
