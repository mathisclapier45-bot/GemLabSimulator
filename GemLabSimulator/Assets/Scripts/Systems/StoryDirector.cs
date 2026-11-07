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
            Route = new[] { new Vector2(13.8f, 1.3f) },
            Lines = new[]
            {
                "Ensuite, la balance à carats, sur la rangée du bas, qui donne le poids, et la balance hydrostatique, ici, qui donne la densité. Avec la loupe, ce sont vos trois premiers outils : on commence par eux.",
                "Les autres instruments se découvrent avec le temps. Avancez doucement : chaque mesure compte.",
                "Ce matin, une première pierre est arrivée pour vous par la poste : elle vous attend dans la boîte aux lettres. Prenez-la, essuyez-la à la chamoisine, puis examinez-la à la loupe.",
                "À gauche de la loupe, des onglets expliquent chaque information qu'elle donne : l'éclat, la couleur, la transparence, la taille, les inclusions... Servez-vous-en.",
                "Je reste ici, dans le laboratoire. Quand vous aurez terminé, rapportez-moi la pierre et nous passerons à la suite.",
            },
        },
    };

    private Transform thouvenot;
    private SpriteRenderer thouvenotSprite;
    private Vector2 basePosition;
    private float walkClock;
    private float followHintTimer;

    public static StoryDirector Instance { get; private set; }

    private bool talking;

    private void Awake()
    {
        Instance = this;
    }

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
        talking = false;
        StoryChoiceUI.Close();
        StoryStonePickerUI.Close();
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
        if (state == null || state.Legacy) yield break;

        // During the tutorial she waits in the laboratory, at her post.
        if (state.Phase != StoryState.PhaseLab)
        {
            if (state.ThouvenotWaits)
            {
                SpawnThouvenot(LabPost);
                MakeThouvenotNpc();
            }
            yield break;
        }

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

        // The visit is over: the first stone is in the mailbox. She goes to her post in the laboratory and waits there.
        state.SetPhase(StoryState.PhaseTutorialLoupe);
        ToolFeedbackUI.Instance?.ShowMessage("Prenez la pierre arrivée par la poste dans la boîte aux lettres (touche E), essuyez-la, puis examinez-la à la loupe.");
        yield return WalkRoute(from, new Stop { Route = new[] { LabPost } });
        MakeThouvenotNpc();
    }

    // Where she waits during the tutorial, in the middle of the laboratory, near the loupe.
    private static readonly Vector2 LabPost = new Vector2(10.4f, 0.6f);

    // She becomes a person the player can talk to: a body in the way, a trigger around her, the key E above the player.
    private void MakeThouvenotNpc()
    {
        if (thouvenot == null) return;
        var go = thouvenot.gameObject;
        if (go.GetComponent<ThouvenotNPC>() != null) return;

        var solid = go.AddComponent<BoxCollider2D>();
        solid.size = new Vector2(0.7f, 0.4f);
        solid.offset = new Vector2(0f, 0.2f);
        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.8f, 2.6f);
        trigger.offset = new Vector2(0f, 0.6f);
        go.AddComponent<ThouvenotNPC>();
        go.AddComponent<InteractPrompt>();
    }

    // ---- The conversations of the tutorial -----------------------------------------------------------------------------------

    public void TalkToThouvenot(GameObject npc)
    {
        if (talking) return;
        StartCoroutine(TalkRoutine(npc));
    }

    private static GemInstance TutorialGem()
    {
        var state = StoryState.Instance;
        if (state == null || PlayerInventory.Instance == null || !state.TutorialStoneTaken) return null;
        foreach (var gem in PlayerInventory.Instance.Gems)
            if (state.IsTutorialStone(gem)) return gem;
        return null;
    }

    private IEnumerator Say(GameObject npc, params string[] lines)
    {
        foreach (var line in lines)
        {
            var advanced = false;
            DialogueBoxUI.SayAndWait(Speaker, line, npc, () => advanced = true);
            while (!advanced) yield return null;
        }
    }

    private IEnumerator Ask(string[] options, System.Action<int> result)
    {
        var picked = -2;
        StoryChoiceUI.Show(options, index => picked = index);
        while (picked == -2) yield return null;
        result(picked);
    }

    private IEnumerator TalkRoutine(GameObject npc)
    {
        var state = StoryState.Instance;
        if (state == null || !state.InTutorial) yield break;
        talking = true;

        switch (state.Phase)
        {
            case StoryState.PhaseTutorialLoupe:
                if (!state.TutorialStoneTaken)
                    yield return Say(npc, "Une pierre est arrivée par la poste pour vous. Elle est dans la boîte aux lettres, près de l'entrée : appuyez sur E devant la boîte, ouvrez la lettre, puis prenez la pierre.");
                else
                    yield return Say(npc,
                        "Alors ? Souvenez-vous : on essuie la pierre à la chamoisine, sur le bureau de la loupe, puis on la dépose sous la loupe et on la tourne dans tous les sens.",
                        "Les onglets, à gauche de la loupe, vous disent ce que chaque information signifie. Quand vous aurez tout regardé, revenez me voir.");
                break;

            case StoryState.PhaseTutorialReturn:
                yield return TalkReturn(npc, state);
                break;

            case StoryState.PhaseTutorialWeigh:
                yield return Say(npc,
                    "La balance à carats est sur la rangée du bas, au milieu du laboratoire. Allumez-la, déposez la pierre sur le plateau, et lisez le poids.",
                    "Elle peut aussi l'afficher en grammes, si vous préférez comparer. Revenez me dire ce que vous avez lu.");
                break;

            case StoryState.PhaseTutorialReport:
                yield return TalkReport(npc, state);
                break;

            case StoryState.PhaseTutorialDensity:
                yield return Say(npc,
                    "La balance hydrostatique est sur la rangée du haut, près de la loupe. Déposez la pierre dans le panier plongé dans l'eau : la balance la pèse dans l'eau.",
                    "Comme vous avez déjà pesé la pierre dans l'air, la densité s'affichera toute seule. Revenez me la dire.");
                break;

            case StoryState.PhaseTutorialDensityReport:
                yield return TalkDensityReport(npc, state);
                break;

            case StoryState.PhaseTutorialIdentify:
                yield return TalkIdentify(npc, state);
                break;
        }

        DialogueBoxUI.Hide();
        talking = false;
    }

    // A question with a few answers: the question stays on the screen while the player chooses. Returns the index of the answer in `index`.
    private IEnumerator Quiz(GameObject npc, string question, string[] options, System.Action<int> index)
    {
        DialogueBoxUI.SayAndWait(Speaker, question, npc, () => { });
        var picked = -2;
        StoryChoiceUI.Show(options, i => picked = i);
        while (picked == -2) yield return null;
        index(picked);
    }

    // Three answers, the right one and two others, in an order that depends on the stone (so it does not always come first).
    private static string[] ThreeAnswers(string right, string[] others, string seed, out int rightIndex)
    {
        var pool = new System.Collections.Generic.List<string>();
        foreach (var other in others)
            if (other != right && !pool.Contains(other)) pool.Add(other);

        var hash = StoneOptics.StableHash(seed);
        var wrong1 = pool[(int)(hash % (uint)pool.Count)];
        pool.Remove(wrong1);
        var wrong2 = pool[(int)((hash / 7u) % (uint)pool.Count)];

        var shift = (int)((hash / 3u) % 3u);
        var all = new[] { right, wrong1, wrong2 };
        var result = new string[3];
        rightIndex = 0;
        for (var i = 0; i < 3; i++)
        {
            result[i] = all[(i + shift) % 3];
            if ((i + shift) % 3 == 0) rightIndex = i;
        }
        return result;
    }

    private static string FirstWord(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var end = text.IndexOfAny(new[] { ' ', ',' });
        var word = end > 0 ? text.Substring(0, end) : text;
        return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
    }

    private static string ClarityAnswer(string grade)
    {
        switch (grade)
        {
            case "Loupe clean": return "Aucune inclusion, même à la loupe";
            case "Eye-clean": return "Quelques inclusions très fines, invisibles à l'œil nu";
            case "Inclus": return "Des inclusions bien visibles à la loupe";
            default: return "Nombreuses inclusions, visibles à l'œil nu";
        }
    }

    // The professor asks what the loupe has shown: four questions with three answers each. A wrong answer is corrected and the lesson goes on.
    private IEnumerator LoupeQuiz(GameObject npc, GemInstance gem)
    {
        var def = gem.Definition;
        var seed = gem.SpecimenId ?? "";
        var right = 0;
        int picked;
        int rightIndex;

        // 1. The transparency.
        var transparency = def.TransparencyLabel;
        var options = ThreeAnswers(transparency, new[] { "Transparent", "Semi-transparent", "Semi-opaque", "Opaque" }, seed + "t", out rightIndex);
        picked = -1;
        yield return Quiz(npc, "Première question : quelle est la transparence de la pierre ?", options, i => picked = i);
        if (picked == rightIndex) { right++; yield return Say(npc, "Oui : " + transparency.ToLowerInvariant() + (transparency == "Semi-transparent" ? " (on dit aussi translucide)." : ". On voit à travers, ou pas.")); }
        else yield return Say(npc, "Pas tout à fait : elle est " + transparency.ToLowerInvariant() + ". Regardez l'onglet « Transparence » à côté de la loupe.");

        // 2. The luster.
        var luster = FirstWord(def.luster);
        options = ThreeAnswers(luster, new[] { "Vitreux", "Adamantin", "Résineux", "Cireux", "Nacré", "Soyeux", "Métallique" }, seed + "l", out rightIndex);
        picked = -1;
        yield return Quiz(npc, "Ensuite, son éclat : comment la surface renvoie-t-elle la lumière ?", options, i => picked = i);
        if (picked == rightIndex) { right++; yield return Say(npc, "Exact : un éclat " + luster.ToLowerInvariant() + "."); }
        else yield return Say(npc, "Non : l'éclat est " + luster.ToLowerInvariant() + ". L'onglet « Éclat » décrit chaque cas.");

        // 3. The cut.
        var cut = gem.CutDisplay;
        options = ThreeAnswers(cut, new[] { "Brillant", "Mixte", "À degrés", "Cabochon" }, seed + "c", out rightIndex);
        picked = -1;
        yield return Quiz(npc, "Troisième question : comment cette pierre est-elle taillée ?", options, i => picked = i);
        if (picked == rightIndex) { right++; yield return Say(npc, "Bien vu : " + cut.ToLowerInvariant() + "."); }
        else yield return Say(npc, "La taille est « " + cut.ToLowerInvariant() + " ». Les facettes sont toutes disposées différemment d'une taille à l'autre.");

        // 4. The inclusions.
        var clarity = ClarityAnswer(gem.ClarityGrade);
        options = ThreeAnswers(clarity, new[] { ClarityAnswer("Loupe clean"), ClarityAnswer("Eye-clean"), ClarityAnswer("Inclus"), ClarityAnswer("Fortement inclus") }, seed + "i", out rightIndex);
        picked = -1;
        yield return Quiz(npc, "Dernière question : qu'avez-vous vu à l'intérieur de la pierre ?", options, i => picked = i);
        if (picked == rightIndex) { right++; yield return Say(npc, "C'est cela. Les inclusions racontent l'histoire de la pierre."); }
        else yield return Say(npc, "Pas exactement : " + clarity.ToLowerInvariant() + ". Les inclusions se cherchent en tournant la pierre sous la loupe.");

        yield return Say(npc, right == 4 ? "Quatre sur quatre : vous avez l'œil !" : right >= 2 ? $"{right} sur 4 : c'est un bon début. L'œil s'entraîne avec le temps." : $"{right} sur 4 : ce n'est pas grave. Les onglets sont là pour ça, servez-vous-en.");
    }

    private int loupeQuizRound;       // the round of the tutorial whose questions about the loupe were asked

    private IEnumerator TalkReturn(GameObject npc, StoryState state)
    {
        var gem = TutorialGem();
        if (gem == null)
        {
            yield return Say(npc, "Où est votre pierre ? Il me faut l'avoir sous les yeux : elle doit être dans votre inventaire.");
            yield break;
        }

        var round = state.TutorialRound;
        if (loupeQuizRound != round)
        {
            yield return Say(npc, round == 1
                ? "Alors, vous avez examiné la pierre à la loupe ? Voyons ce que vous avez retenu : je vous pose quelques questions."
                : "Vous l'avez examinée ? Mêmes questions que tout à l'heure. Faites attention : celle-ci ne se laisse pas regarder comme la première.");
            yield return LoupeQuiz(npc, gem);
            loupeQuizRound = round;
        }

        if (round > 1)
        {
            yield return Say(npc, "Pesez-la maintenant sur la balance à carats, comme la première. Revenez me dire le poids.");
            state.SetPhase(StoryState.PhaseTutorialWeigh);
            ToolFeedbackUI.Instance?.ShowMessage("Pesez la pierre sur la balance à carats (rangée du bas du laboratoire).");
            yield break;
        }

        DialogueBoxUI.SayAndWait(Speaker, "Voulez-vous passer à la suite ?", npc, () => { });
        var choice = -1;
        yield return Ask(new[] { "Demander la suite", "Pas tout de suite, je regarde encore la pierre" }, index => choice = index);
        if (choice != 0)
        {
            yield return Say(npc, "Prenez votre temps. Je suis là.");
            yield break;
        }

        yield return Say(npc,
            "Regarder ne suffit pas : en gemmologie, on mesure. Commençons par la mesure la plus simple, le poids. Une gemme se pèse en carats.",
            "Un carat vaut 0,2 gramme : un cinquième de gramme. Le mot vient de la graine du caroubier. Dans l'Antiquité, les marchands s'en servaient comme poids, parce que ces graines passaient pour toutes à peu près identiques.",
            "Allez à la balance à carats, sur la rangée du bas du laboratoire. Déposez la pierre sur le plateau, allumez la balance et lisez le poids. Puis revenez me le dire.");
        state.SetPhase(StoryState.PhaseTutorialWeigh);
        ToolFeedbackUI.Instance?.ShowMessage("Pesez la pierre sur la balance à carats (rangée du bas du laboratoire).");
    }

    private IEnumerator TalkReport(GameObject npc, StoryState state)
    {
        var gem = TutorialGem();
        if (gem == null)
        {
            yield return Say(npc, "Où est votre pierre ? Il me faut l'avoir sous les yeux.");
            yield break;
        }

        var truth = Mathf.Round(gem.CaratWeight * 100f) / 100f;
        var labels = new[] { $"{truth:F2} ct", $"{Mathf.Round(truth * 0.5f * 100f) / 100f:F2} ct", $"{Mathf.Round((truth + 0.6f) * 100f) / 100f:F2} ct" };
        var options = ThreeAnswers(labels[0], labels, (gem.SpecimenId ?? "") + "w", out var rightIndex);

        var picked = -1;
        yield return Quiz(npc, "Alors, combien pèse la pierre ?", options, i => picked = i);
        if (picked != rightIndex)
        {
            yield return Say(npc, "Hum... pas tout à fait. Relisez la balance : le chiffre est en carats, avec deux décimales. Revenez me le dire.");
            yield break;
        }

        if (state.TutorialRound > 1)
        {
            yield return Say(npc,
                $"Exactement : {truth:F2} carat, soit {truth * 0.2f:F2} gramme.",
                "Maintenant, la densité : balance hydrostatique, comme tout à l'heure. Revenez me la dire.");
            state.SetPhase(StoryState.PhaseTutorialDensity);
            ToolFeedbackUI.Instance?.ShowMessage("Mesurez la densité sur la balance hydrostatique (rangée du haut du laboratoire).");
            yield break;
        }

        yield return Say(npc,
            $"Exactement : {truth:F2} carat, soit {truth * 0.2f:F2} gramme. Voilà votre première mesure.",
            "Avec la loupe, vous avez décrit la pierre ; avec la balance, vous l'avez mesurée. Mais le poids seul ne dit rien de la pierre : une petite pierre dense et une grosse pierre légère peuvent peser pareil.",
            "Pour les distinguer, on mesure la densité : le poids rapporté au volume. Archimède l'avait compris dans son bain : un objet plongé dans l'eau paraît plus léger, d'autant plus que son volume est grand.",
            "Allez à la balance hydrostatique, sur la rangée du haut, près de la loupe. Elle pèse la pierre dans l'eau, et comme vous connaissez déjà son poids dans l'air, elle en tire la densité. Revenez me la dire.");
        state.SetPhase(StoryState.PhaseTutorialDensity);
        ToolFeedbackUI.Instance?.ShowMessage("Mesurez la densité sur la balance hydrostatique (rangée du haut du laboratoire).");
    }

    private IEnumerator TalkDensityReport(GameObject npc, StoryState state)
    {
        var gem = TutorialGem();
        if (gem == null)
        {
            yield return Say(npc, "Où est votre pierre ? Il me faut l'avoir sous les yeux.");
            yield break;
        }

        var truth = gem.HasMeasured(ExaminationTool.MeasurementType.Density.ToString())
            ? gem.GetMeasuredValue(ExaminationTool.MeasurementType.Density.ToString())
            : gem.Definition.density;
        truth = Mathf.Round(truth * 100f) / 100f;
        var labels = new[] { $"{truth:F2}", $"{truth + 0.55f:F2}", $"{Mathf.Round(truth * 0.5f * 100f) / 100f:F2}" };
        var options = ThreeAnswers(labels[0], labels, (gem.SpecimenId ?? "") + "d", out var rightIndex);

        var picked = -1;
        yield return Quiz(npc, "Quelle densité la balance a-t-elle donnée ?", options, i => picked = i);
        if (picked != rightIndex)
        {
            yield return Say(npc, "Non... relisez le chiffre « Densité » à l'écran de la balance hydrostatique, et revenez me le dire.");
            yield break;
        }

        if (state.TutorialRound > 1)
            yield return Say(npc, $"Oui : {truth:F2}.");
        else
            yield return Say(npc,
                $"Oui : {truth:F2}. C'est un nombre sans unité : la pierre pèse {truth:F2} fois plus qu'un même volume d'eau.",
                "Chaque espèce a sa densité, ou presque. Avec la loupe et ces deux mesures, vous avez déjà de quoi éliminer beaucoup de candidates.");
        state.SetPhase(StoryState.PhaseTutorialIdentify);
        yield return TalkIdentify(npc, state);
    }

    // The conclusion: three stones are suggested with their data, and the player picks the one that matches. A wrong answer is explained and the question is asked again.
    private IEnumerator TalkIdentify(GameObject npc, StoryState state)
    {
        var gem = TutorialGem();
        if (gem == null)
        {
            yield return Say(npc, "Où est votre pierre ? Il me faut l'avoir sous les yeux.");
            yield break;
        }

        var round = state.TutorialRound;
        var right = gem.Definition;
        var density = gem.HasMeasured(ExaminationTool.MeasurementType.Density.ToString())
            ? gem.GetMeasuredValue(ExaminationTool.MeasurementType.Density.ToString())
            : right.density;

        // The suggestions: the species of the stone and two others of the same colour and the same look, but not the same density.
        var candidates = new System.Collections.Generic.List<GemDefinition> { right };
        var database = GemDatabase.Instance;
        foreach (var wanted in TutorialStones.Others(round))
        {
            if (candidates.Count >= 3 || database == null) break;
            foreach (var definition in database.allGems)
            {
                if (definition == null || definition.name != wanted || definition == right) continue;
                candidates.Add(definition);
                break;
            }
        }
        if (database != null)
            foreach (var definition in database.allGems)
            {
                if (candidates.Count >= 3) break;
                if (definition == null || candidates.Contains(definition) || definition.isTunnelExclusive) continue;
                if (Mathf.Abs(definition.density - right.density) >= 0.3f && definition.TransparencyLabel == right.TransparencyLabel) candidates.Add(definition);
            }

        // Not always in the same order.
        var shift = (int)(StoneOptics.StableHash((gem.SpecimenId ?? "") + "id") % (uint)candidates.Count);
        var shown = new GemDefinition[candidates.Count];
        for (var i = 0; i < shown.Length; i++) shown[i] = candidates[(i + shift) % candidates.Count];
        var eliminated = new bool[shown.Length];

        if (round == 1)
            yield return Say(npc,
                "Vous avez décrit la pierre à la loupe, pesé, mesuré sa densité. Il est temps de conclure : de quelle pierre s'agit-il ?",
                "Je vous en propose quelques-unes, avec leurs données à côté. Comparez-les avec ce que vous avez trouvé.");
        else
            yield return Say(npc, "Même question que tout à l'heure : de quelle pierre s'agit-il ? Comparez les données avec ce que vous avez trouvé.");
        DialogueBoxUI.Hide();

        var subtitle = $"Votre pierre : {gem.ColorName.ToLowerInvariant()}, {right.TransparencyLabel.ToLowerInvariant()}, d'éclat {right.luster.ToLowerInvariant()}, densité mesurée {density.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}.";
        while (true)
        {
            var picked = -2;
            StoryStonePickerUI.Show(subtitle, shown, eliminated, i => picked = i);
            while (picked == -2) yield return null;

            if (picked < 0)
            {
                yield return Say(npc, "Prenez votre temps, comparez bien. Revenez me voir quand vous serez décidé.");
                yield break;
            }

            if (shown[picked] == right) break;

            eliminated[picked] = true;
            var wrong = shown[picked];
            yield return Say(npc,
                $"Non : {wrong.displayName.ToLowerInvariant()} a une densité de {wrong.density.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}, alors que vous avez mesuré {density.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}. Regardez bien les chiffres : la couleur ne suffit pas, c'est la mesure qui tranche.");
        }

        // The identification, once right, counts like a report validated by the director: the stone is identified and gives experience.
        gem.MarkIdentified();
        PlayerInventory.Instance?.NotifyChanged();
        ReputationState.Instance?.AddPoint();
        GemCodex.Instance?.MarkDiscovered(right.name);
        var density2 = right.density.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        if (round < StoryState.TutorialRounds)
        {
            if (round == 1)
                yield return Say(npc,
                    $"Oui, c'est cela : {right.displayName} ! Sa densité est de {density2}, comme votre mesure. Les autres avaient la même couleur, mais elles sont bien plus denses : voilà pourquoi on mesure.",
                    "Vous venez d'identifier votre première pierre : je la valide, et vous gagnez de l'expérience. Regardez la jauge, en haut à gauche.",
                    "Le facteur vient de passer : une autre pierre est arrivée pour vous, dans la boîte aux lettres. Celle-là ne se laisse pas traverser aussi facilement par la lumière. Même démarche : loupe, poids, densité.");
            else
                yield return Say(npc,
                    $"Oui, c'est cela : {right.displayName} ! Plusieurs pierres vertes se font passer pour du jade, et la couleur ne les trahit pas : la densité, si. Ici la lumière passe mais l'image est floue : la pierre est semi-transparente, on dit aussi translucide.",
                    "Une dernière pierre vient d'arriver dans la boîte aux lettres. Celle-là ne laisse passer aucune lumière.");
            state.NextTutorialRound();
            ToolFeedbackUI.Instance?.ShowMessage("Une nouvelle pierre est arrivée par la poste : prenez-la dans la boîte aux lettres du laboratoire.");
            DialogueBoxUI.Hide();
            yield break;
        }

        yield return Say(npc,
            $"Oui, c'est cela : {right.displayName} ! Une pierre opaque : aucune lumière ne la traverse, on la juge donc à son éclat, ici métallique, et à sa densité très élevée ({density2}) : elle est lourde pour sa taille.",
            "Trois pierres identifiées : votre réputation grandit, regardez la jauge en haut à gauche.",
            "Et une chose importante : chaque fois que vous identifiez une pierre, une page s'ajoute à votre grimoire. Ouvrez-le avec la touche G : vous y avez maintenant trois pages, une par pierre que nous venons d'étudier.",
            "Sur chaque page, vous retrouvez les informations que vous avez récoltées sur cette espèce : l'éclat, la couleur, la transparence, la densité... Ce que vous n'avez pas encore mesuré reste marqué « ??? ». Chaque mesure, sur n'importe quelle pierre de cette espèce, remplit une nouvelle ligne.",
            "Le grimoire sera votre mémoire : consultez-le quand vous hésitez devant une pierre.",
            "Avec une seule mesure, un doute peut rester : plusieurs espèces ont la même densité. On croise donc les mesures. Le réfractomètre, juste à côté, sera la prochaine étape. Mais vous avez bien travaillé pour aujourd'hui.",
            "Le courrier vous apportera d'autres pierres, désormais. Chaque rapport remis au directeur fait gagner de l'XP. Et si vous avez un doute, appelez-moi.");

        state.SetPhase(StoryState.PhaseDone);
        ToolFeedbackUI.Instance?.ShowMessage("Trois pages ont été ajoutées à votre grimoire : touche G pour les consulter. Le courrier vous apportera d'autres pierres.");
        DialogueBoxUI.Hide();

        // She goes back to her work: she fades away.
        var sprite = npc != null ? npc.GetComponent<SpriteRenderer>() : null;
        for (var t = 0f; t < 1.2f && sprite != null; t += Time.deltaTime)
        {
            sprite.color = new Color(1f, 1f, 1f, 1f - t / 1.2f);
            yield return null;
        }
        if (npc != null) Destroy(npc);
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
