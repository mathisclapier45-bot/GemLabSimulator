using System;
using System.Collections;
using System.Collections.Generic;
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
                "Mais avant de toucher le moindre instrument, une question toute simple : qu'est-ce qui fait qu'une pierre est une gemme ? Regardez.",
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
            StartCoroutine(RunStranger());
        }
        else if (scene.name == "MaisonJoueur") StartCoroutine(RunHouse());
        else if (scene.name == "Home")
        {
            AddLoupeCloth();
            LessonBoard.Place();
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

    // A lit lantern in the hand of someone (Professor Thouvenot at night): a small lantern drawn beside them, and the light it gives.
    private static void GiveLantern(Transform carrier)
    {
        if (carrier == null || carrier.GetComponentInChildren<NightLight>() != null) return;
        var lantern = new GameObject("Lantern");
        lantern.transform.SetParent(carrier, false);
        lantern.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var light = lantern.AddComponent<NightLight>();
        light.Type = NightLight.Kind.Carried;
        light.LightColor = new Color(1f, 0.80f, 0.48f);
        light.Radius = 4.6f;
        light.Intensity = 1.0f;
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

            // At the first stop, the lesson on what a gemstone is.
            if (index == 0)
            {
                yield return PlayLesson("gemmes");
                yield return Talk(new[]
                {
                    "Retenez ces cinq mots : beauté, rareté, durabilité, valeur, acceptabilité. Une pierre qui les réunit est une gemme ; c'est avec eux que vous jugerez, plus tard, ce qu'on vous apportera.",
                    "Et ce n'est pas tout : toutes mes leçons, celle-ci comme celles des instruments, sont affichées sur le panneau, dans l'entrée du laboratoire. Revenez les lire quand vous voulez.",
                });
            }
        }

        // The visit is over: the first stone is in the mailbox. She goes to her post in the laboratory and waits there.
        state.SetPhase(StoryState.PhaseTutorialLoupe);
        ToolFeedbackUI.Instance?.ShowMessage("Prenez la pierre arrivée par la poste dans la boîte aux lettres (touche E), essuyez-la, puis examinez-la à la loupe.");
        yield return WalkRoute(from, new Stop { Route = new[] { LabPost } });
        MakeThouvenotNpc();
    }

    // A lesson of the board, played in the middle of a conversation: the dialogue goes away, and comes back when the lesson is closed.
    private IEnumerator PlayLesson(string id)
    {
        DialogueBoxUI.Hide();
        var finished = false;
        if (!LessonUI.Show(id, () => finished = true)) yield break;
        while (!finished) yield return null;
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
        if (state == null || !state.ThouvenotWaits) yield break;
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

            case StoryState.PhaseStoneLab:
                yield return TalkStoneLab(npc, state);
                break;

            case StoryState.PhaseStoneValidate:
                yield return TalkStoneValidate(npc, state);
                break;

            case StoryState.PhaseRefractometer:
                yield return Say(npc,
                    "Le réfractomètre est sur la rangée du haut, à côté de la loupe. N'oubliez pas : une goutte de liquide de contact sur l'hémicylindre d'abord, la pierre ensuite, puis tournez la lentille.",
                    "Les onglets, à gauche de l'instrument, vous donnent la liste des pierres avec leurs indices et le signe optique, et expliquent les situations possibles.");
                break;

            case StoryState.PhaseTwinsLab:
                yield return TalkTwinsLab(npc, state);
                break;

            case StoryState.PhaseTwinsRefracto:
                yield return Say(npc,
                    "Mesurez l'indice des deux pierres au réfractomètre, sur la rangée du haut : la pierre du client et ma pierre de référence. N'oubliez pas la goutte de liquide pour chacune.",
                    "Revenez me voir quand vous aurez les deux.");
                break;

            case StoryState.PhaseTwinsLesson:
                yield return TalkTwinsLesson(npc, state);
                break;

            case StoryState.PhaseTwinsPolariscope:
                yield return Say(npc,
                    "Le polariscope est sur la rangée du haut, à droite. Passez-y les deux pierres, et tournez chacune d'un tour complet entre les filtres croisés.",
                    "Puis prenez le conoscope et placez-le devant la pierre du client, en la regardant le long d'un axe optique. Revenez me dire ce que vous avez vu.");
                break;

            case StoryState.PhaseTwinsReport:
                yield return TalkTwinsReport(npc, state);
                break;

            case StoryState.PhaseTwinsIdentify:
                yield return Say(npc,
                    "Alors, c'est un spinelle bleu ou non ? Réfléchissez à ce que vous avez vu : indice, polariscope, conoscope. Quand vous êtes sûr, remettez votre rapport au directeur.");
                break;

            case StoryState.PhaseTwinsValidate:
                yield return TalkTwinsValidate(npc, state);
                break;

            case StoryState.PhaseStoneIdentify:
                yield return Say(npc,
                    "Alors, qu'avez-vous trouvé ? Croisez vos mesures : l'indice, la densité, l'éclat, la couleur. Les onglets du réfractomètre listent les pierres possibles.",
                    "Quand vous serez sûr de vous, remettez votre rapport au directeur : c'est lui qui valide l'identification.");
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
            "Vous avez très bien travaillé aujourd'hui. Il est temps de vous reposer : rentrez chez vous, dormez, et nous reprendrons demain. À bientôt !");

        state.SetPhase(StoryState.PhaseRest);
        ToolFeedbackUI.Instance?.ShowMessage("Trois pages ont été ajoutées à votre grimoire (touche G). Vous avez bien travaillé : rentrez vous reposer chez vous.");
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

    // ---- The next day: the stone of the stranger -----------------------------------------------------------------------------------

    // The stone given by the man of the town: a pink tourmaline, cut, natural and untreated, that can be found with the refractometer (uniaxial, negative).
    private static GemInstance MakeStrangerStone()
    {
        return MakeStone("Tourmaline_Rose", 0.8f, 2.6f);
    }

    // A cut stone of the species, natural and untreated, of a weight between the two limits (the best of a few tries).
    private static GemInstance MakeStone(string speciesName, float minCarat, float maxCarat)
    {
        var database = GemDatabase.Instance;
        if (database == null) return null;
        GemDefinition species = null;
        foreach (var definition in database.allGems)
            if (definition != null && definition.name == speciesName) species = definition;
        if (species == null) return null;

        GemInstance best = null;
        for (var i = 0; i < 300; i++)
        {
            var candidate = new GemInstance(species);
            best = candidate;
            if (candidate.IsDonation || candidate.IsSynthetic || candidate.IsRough || candidate.TreatmentGrade != "Non traité") continue;
            if (candidate.ClarityGrade != "Eye-clean" && candidate.ClarityGrade != "Inclus") continue;
            if (candidate.CaratWeight < minCarat || candidate.CaratWeight > maxCarat) continue;
            break;
        }
        return best;
    }

    private static GemInstance StrangerGem()
    {
        var state = StoryState.Instance;
        if (state == null || PlayerInventory.Instance == null) return null;
        foreach (var gem in PlayerInventory.Instance.Gems)
            if (state.IsStrangerStone(gem)) return gem;
        return null;
    }

    private IEnumerator SayAs(string speaker, GameObject source, params string[] lines)
    {
        foreach (var line in lines)
        {
            var advanced = false;
            DialogueBoxUI.SayAndWait(speaker, line, source, () => advanced = true);
            while (!advanced) yield return null;
        }
    }

    private static readonly Dictionary<string, Sprite[]> StrangerFrames = new Dictionary<string, Sprite[]>();

    private static Sprite[] StrangerFramesOf(string direction)
    {
        if (!StrangerFrames.TryGetValue(direction, out var frames) || frames == null || frames.Length == 0)
        {
            frames = Resources.LoadAll<Sprite>($"Walkers/W3/{direction}");
            System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            StrangerFrames[direction] = frames;
        }
        return frames;
    }

    // Walks a person (a townsman with a Walkers sprite) toward a point, with the walking frames of the right side. Returns when there.
    private IEnumerator WalkPerson(Transform person, Func<Vector2> target, float stopDistance, float speed)
    {
        var renderer = person.GetComponent<SpriteRenderer>();
        var clock = 0f;
        while (person != null)
        {
            var goal = target();
            var position = (Vector2)person.position;
            var delta = goal - position;
            if (delta.magnitude <= stopDistance) break;

            var step = Mathf.Min(delta.magnitude - stopDistance, speed * Time.deltaTime);
            person.position = position + delta.normalized * step;
            clock += Time.deltaTime;

            var direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x >= 0f ? "Right" : "Left") : (delta.y >= 0f ? "Up" : "Down");
            var frames = StrangerFramesOf(direction);
            if (renderer != null && frames.Length > 0) renderer.sprite = frames[(int)(clock / 0.11f) % frames.Length];
            yield return null;
        }

        if (renderer != null)
        {
            var idle = StrangerFramesOf("Down");
            if (idle.Length > 0) renderer.sprite = idle[0];
        }
    }

    // After the lessons, on the way home: a man of the town stops the player, welcomes them, and gives them a stone he found on the ground.
    private IEnumerator RunStranger()
    {
        yield return WaitForGame();
        var state = StoryState.Instance;
        if (state == null || state.Legacy) yield break;

        if (state.Phase == StoryState.PhaseRest) yield return StrangerFirstMeeting(state);
        else if (state.Phase == StoryState.PhaseStoneReturn) yield return StrangerReturn(state);
    }

    private static readonly Vector2 StrangerMeet = new Vector2(-52f, -30f);

    // The man appears out of sight, south of the street of the house, and walks up to the player.
    private IEnumerator StrangerAppears(System.Action<GameObject> created)
    {
        var man = new GameObject("Anselme");
        var manRenderer = man.AddComponent<SpriteRenderer>();
        manRenderer.sortingOrder = 4;
        var idleFrames = StrangerFramesOf("Up");
        if (idleFrames.Length > 0) manRenderer.sprite = idleFrames[0];
        man.transform.position = new Vector3(StrangerMeet.x, StrangerMeet.y - 10f, 0f);
        created(man);
        yield return WalkPerson(man.transform, () => PlayerTransform != null ? (Vector2)PlayerTransform.position : StrangerMeet, 3.4f, 5.5f);
    }

    private IEnumerator StrangerLeaves(GameObject man)
    {
        if (man == null) yield break;
        var away = new Vector2(man.transform.position.x + 14f, man.transform.position.y - 6f);
        yield return WalkPerson(man.transform, () => away, 0.3f, 5f);
        if (man != null) Destroy(man);
    }

    // Anselme waits at the end of the footbridge on the way to the player's house, at any hour. When the player comes, he walks up to them: the player gives the stone
    // back, and he thanks them with 50 euros and gives their number.
    private IEnumerator StrangerReturn(StoryState state)
    {
        var post = new Vector2(StrangerMeet.x, StrangerMeet.y - 1.5f);
        var man = new GameObject("Anselme");
        var manRenderer = man.AddComponent<SpriteRenderer>();
        manRenderer.sortingOrder = 4;
        var idleFrames = StrangerFramesOf("Up");
        if (idleFrames.Length > 0) manRenderer.sprite = idleFrames[0];
        man.transform.position = new Vector3(post.x, post.y, 0f);

        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        GemInstance gem = null;
        while (state.Phase == StoryState.PhaseStoneReturn)
        {
            if (man == null) yield break;
            if (DistanceToPlayer(post) > 7f) { yield return null; continue; }

            // He comes to meet the player.
            yield return WalkPerson(man.transform, () => PlayerTransform != null ? (Vector2)PlayerTransform.position : post, 3.4f, 5.5f);
            gem = StrangerGem();
            if (gem != null) break;

            // Without the stone: he says so, goes back to his place and waits until the player has gone away and comes back.
            yield return SayAs("Anselme", man, "Ah, vous voilà ! Et ma pierre ? Vous ne l'avez plus sur vous ? Rapportez-la-moi quand vous l'aurez retrouvée : je reste souvent près de la passerelle.");
            DialogueBoxUI.Hide();
            yield return WalkPerson(man.transform, () => post, 0.3f, 4f);
            while (state.Phase == StoryState.PhaseStoneReturn && DistanceToPlayer(post) < 12f) yield return null;
        }
        if (state.Phase != StoryState.PhaseStoneReturn || gem == null)
        {
            if (man != null) Destroy(man);
            yield break;
        }

        yield return SayAs("Anselme", man, "Ah, vous voilà ! Alors, cette pierre : vous avez trouvé ce que c'était ?");
        yield return SayAs("Vous", player, "Oui : c'est une tourmaline rose. Tenez, elle est à vous.");
        PlayerInventory.Instance?.RemoveGem(gem);
        Sfx.Play(Sfx.Id.Pickup);
        yield return SayAs("Anselme", man,
            "Une tourmaline ! Ça alors... Et moi qui la prenais pour un simple caillou. Vous êtes bien le neveu de Vincent : vous avez l'œil.",
            "Tenez, voici 50 €, comme promis. Merci beaucoup, vraiment.");
        PlayerWallet.Instance?.AddMoney(50);
        Sfx.Play(Sfx.Id.Coin);
        ToolFeedbackUI.Instance?.ShowMessage("Récompense reçue : 50 €.");
        yield return SayAs("Anselme", man,
            "Et prenez aussi mon numéro, au cas où. Si je tombe sur d'autres pierres, ou s'il vous faut un coup de main en ville, appelez-moi.");
        state.AddContact("anselme");
        ToolFeedbackUI.Instance?.ShowMessage("Nouveau contact dans votre téléphone : Anselme.");
        yield return SayAs("Vous", player, "Merci, Anselme. À bientôt !");
        yield return SayAs("Anselme", man, "Bonne journée, et bienvenue à Crystal Grove !");
        DialogueBoxUI.Hide();
        state.SetPhase(StoryState.PhaseTwinsSleep);
        ToolFeedbackUI.Instance?.ShowMessage("Rentrez vous coucher : demain, la professeure Thouvenot vous attend au laboratoire.");
        yield return StrangerLeaves(man);
    }

    private IEnumerator StrangerFirstMeeting(StoryState state)
    {
        // He shows up when the player is near the street of their house.
        var meet = StrangerMeet;
        while (DistanceToPlayer(meet) > 15f)
        {
            if (state.Phase != StoryState.PhaseRest) yield break;
            yield return null;
        }

        // He walks up to the player.
        GameObject man = null;
        yield return StrangerAppears(created => man = created);

        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        yield return SayAs("Anselme", man,
            "Tiens, bonjour ! Je ne vous avais jamais vu dans le coin. Bienvenue à Crystal Grove !");
        yield return SayAs("Vous", player,
            "Merci. Je suis arrivé hier soir : je viens reprendre le laboratoire de mon oncle, Vincent Pardieu.");
        yield return SayAs("Anselme", man,
            "Le neveu de Vincent ! Alors c'est vous... Quelle histoire. Mais vous tombez bien : justement, j'ai trouvé cette pierre par terre, près du pont, et j'aimerais bien savoir ce que c'est.",
            "Tenez, prenez-la. Si vous parvenez à l'identifier, je vous promets une belle récompense.");

        // He gives the stone.
        var gem = MakeStrangerStone();
        if (gem != null && PlayerInventory.Instance != null)
        {
            while (!PlayerInventory.Instance.TryAddGem(gem))
            {
                ToolFeedbackUI.Instance?.ShowMessage("Votre inventaire est plein : faites de la place pour recevoir la pierre.");
                yield return new WaitForSeconds(2.5f);
            }
            Sfx.Play(Sfx.Id.Pickup);
            state.OnStrangerGave(gem);
            ToolFeedbackUI.Instance?.ShowMessage("Vous avez reçu une pierre trouvée par terre.");
        }
        else state.SetPhase(StoryState.PhaseStoneSleep);

        yield return SayAs("Vous", player, "Je ferai de mon mieux. Merci !");
        yield return SayAs("Anselme", man, "À bientôt, alors. Et reposez-vous : vous avez l'air fatigué.");
        DialogueBoxUI.Hide();
        ToolFeedbackUI.Instance?.ShowMessage("Rentrez vous coucher : demain, vous présenterez la pierre à la professeure Thouvenot.");

        // He goes his way and is gone.
        yield return StrangerLeaves(man);
    }

    // The next morning: the player shows the stone to Thouvenot, who knows it at first sight, but wants the player to find it with the tests, and teaches the refractometer.
    private IEnumerator TalkStoneLab(GameObject npc, StoryState state)
    {
        var gem = StrangerGem();
        if (gem == null)
        {
            yield return Say(npc, "Bonjour ! On m'a dit qu'un habitant vous avait confié une pierre. Où est-elle ? Il me faut l'avoir sous les yeux : elle doit être dans votre inventaire.");
            yield break;
        }

        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        yield return Say(npc, "Bonjour ! Vous avez l'air de tenir quelque chose d'important. Une pierre ? Montrez-moi.");
        yield return SayAs("Vous", player, "Un habitant me l'a donnée hier soir : il l'a trouvée par terre, et il m'a promis une récompense si je l'identifie.");
        yield return Say(npc,
            "Anselme, bien sûr : il ramasse tout ce qui brille au bord de la rivière. Voyons...",
            "Oh. Je sais ce que c'est, dès le premier coup d'œil.");
        yield return SayAs("Vous", player, "Alors dites-le-moi !");
        yield return Say(npc,
            "Non. Un gemmologue ne se fie jamais à son œil : il doit prouver ce qu'il avance. Je vous propose de la trouver vous-même, avec les tests.",
            "Vous avez déjà la loupe, la balance et la densité. Il vous manque un outil essentiel : le réfractomètre, sur la rangée du haut. C'est un instrument délicat : avant de vous en servir, laissez-moi vous expliquer comment il fonctionne.");

        // The lesson, with the light drawn.
        DialogueBoxUI.Hide();
        var finished = false;
        RefractionLessonUI.Show(() => finished = true);
        while (!finished) yield return null;

        yield return Say(npc,
            "Voilà pour le principe. En pratique : prenez la fiole de liquide de contact, déposez une goutte sur l'hémicylindre, puis posez la pierre dessus.",
            "Tournez la lentille d'un tour complet en suivant les limites d'ombre : notez les valeurs extrêmes, nα, nβ, nγ.",
            "À gauche de l'instrument, des onglets vous aideront : la liste des pierres avec leurs indices et leur signe optique, et les situations possibles (hors limite, une ou deux lectures, uniaxe ou biaxe, positif ou négatif).",
            "Je vous laisse chercher. Quand vous saurez, remettez votre rapport au directeur. Bonne chance !");
        state.SetPhase(StoryState.PhaseRefractometer);
        ToolFeedbackUI.Instance?.ShowMessage("Mesurez l'indice de la pierre au réfractomètre (rangée du haut) : la goutte d'abord, la pierre ensuite.");
    }

    // The director has validated the identification: Thouvenot confirms the answer, explains how the measures lead to it, and lets the player keep the stone to give it back.
    private IEnumerator TalkStoneValidate(GameObject npc, StoryState state)
    {
        var gem = StrangerGem();
        if (gem == null)
        {
            yield return Say(npc, "Alors, votre pierre ? Il me faut l'avoir sous les yeux pour confirmer votre réponse : elle doit être dans votre inventaire.");
            yield break;
        }

        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        yield return Say(npc, "Alors, cette pierre ? Le directeur m'a dit que vous aviez remis votre rapport. Quelle est votre réponse ?");
        yield return SayAs("Vous", player, $"Une {gem.Definition.displayName.ToLowerInvariant()}.");
        yield return Say(npc,
            "C'est exact ! Et c'est ce que j'avais vu du premier coup d'œil. Mais maintenant, vous savez le prouver.",
            "Un indice vers 1,625, deux lectures, une seule limite qui bouge, la basse : c'est un uniaxe négatif. La densité autour de 3,06 et la couleur rose confirment : une tourmaline. Retenez bien cette méthode : une mesure ne suffit jamais, c'est leur accord qui fait la certitude.",
            "Gardez la pierre un moment : elle n'est pas à vous. Anselme vous a promis une récompense, et il serait ravi de la revoir. Rendez-la-lui en rentrant chez vous : il vous attend près de la passerelle, du côté de votre maison.",
            "En attendant, profitez du réfractomètre : passez-y les autres gemmes de votre inventaire, une par une, avec une goutte de liquide chacune. Chaque indice relevé, chaque limite d'ombre observée complète les pages de votre grimoire.",
            "Plus vous mesurez, plus vos pages se remplissent. Et quand un doute reste, vous saurez quoi mesurer pour le lever.",
            "Autre chose : passez donc au GemStore. Léa, la vendeuse, a quelque chose à vous remettre de la part de Vincent. Elle vous attend.");
        state.SetPhase(StoryState.PhaseLeaVisit);
        ToolFeedbackUI.Instance?.ShowMessage("Passez vos autres gemmes au réfractomètre, puis rendez visite à Léa, au GemStore : elle a quelque chose pour vous.");
    }

    // ---- The visit to Léa, at the GemStore --------------------------------------------------------------------------------------------

    public void TalkToLea(GameObject npc)
    {
        if (talking) return;
        StartCoroutine(TalkLeaRoutine(npc));
    }

    // Léa tells about Vincent, gives the credit he left, his card binder (and a booster in it) and says that a booster arrives every morning.
    private IEnumerator TalkLeaRoutine(GameObject npc)
    {
        var state = StoryState.Instance;
        if (state == null || state.Phase != StoryState.PhaseLeaVisit) yield break;
        talking = true;

        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        yield return SayAs("Léa", npc,
            "Bonjour ! Vous êtes le neveu de Vincent, n'est-ce pas ? La professeure Thouvenot m'a prévenue que vous passeriez. Je suis Léa, je tiens le GemStore.",
            "Votre oncle était l'un de mes meilleurs clients... et un ami. Il venait presque tous les jours, il restait des heures devant les vitrines. Il parlait souvent de vous.");
        yield return SayAs("Vous", player, "Il ne m'a jamais dit qu'il venait ici si souvent.");
        yield return SayAs("Léa", npc,
            "Il était comme ça, discret. Avant de partir, il m'a laissé deux choses pour vous. Je les ai gardées bien au chaud.",
            "D'abord, un avoir de 60 € à dépenser ici. Il disait que vous auriez besoin d'une bonne loupe : la Loupe 10× est sur l'étagère du fond, elle coûte 59 €. Votre avoir couvre presque tout.");

        PlayerWallet.Instance?.AddStoreCredit(60);
        Sfx.Play(Sfx.Id.Coin);
        ToolFeedbackUI.Instance?.ShowMessage("Avoir de 60 € reçu : il sera déduit de vos achats au GemStore.");

        yield return SayAs("Léa", npc,
            "Et puis ceci. Vincent était un grand collectionneur : des cartes, surtout. Il raffolait des boosters, il en ouvrait un chaque matin avant de commencer sa journée !",
            "Il vous laisse son classeur. Il est vide pour l'instant : il vous revient de le remplir. Et pour bien commencer, voici un booster offert.");

        state.UnlockBinder();
        Sfx.Play(Sfx.Id.Pickup);
        ToolFeedbackUI.Instance?.ShowMessage("Le classeur de Vincent est dans votre sac : onglet « Classeur », touche B.");

        // The booster opens on the screen.
        var cards = PlayerCards.Ensure();
        if (cards != null)
        {
            var draws = cards.OpenBooster(true);
            DialogueBoxUI.Hide();
            BoosterUI.Show(draws);
            yield return null;
            while (BoosterUI.IsShowing) yield return null;
        }

        yield return SayAs("Léa", npc,
            "Voilà un bon début de collection ! Un conseil : les cartes se rangent toutes seules dans le classeur, par pages.",
            "Et une bonne nouvelle : un booster est livré au magasin chaque matin. Un seul par jour, alors ne tardez pas : il est sur le comptoir, à 11 €.",
            "Passez quand vous voulez. Et si vous avez des nouvelles de Vincent... dites-le-moi, d'accord ?");
        DialogueBoxUI.Hide();

        state.SetPhase(StoryState.PhaseStoneReturn);
        ToolFeedbackUI.Instance?.ShowMessage("Rendez la pierre à Anselme : il vous attend près de la passerelle, vers votre maison. En attendant, la Loupe 10× vous attend au GemStore.");
        talking = false;
    }

    // ---- Chapter 2: the stones that look alike ---------------------------------------------------------------------------------------

    private static string Fr(float value, string format)
    {
        return value.ToString(format, System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
    }

    // Thouvenot gives the client's stone (a kyanite that was sold as a blue spinel) and a reference spinel from her collection.
    private IEnumerator TalkTwinsLab(GameObject npc, StoryState state)
    {
        yield return Say(npc,
            "Bonjour ! Bien dormi ? Tant mieux : j'ai une affaire un peu particulière pour vous ce matin.",
            "Un client m'a apporté une pierre bleue. Il l'a achetée comme un spinelle bleu, et il voudrait savoir si c'est vrai.",
            "Pour que vous puissiez comparer, voici aussi une pierre de référence tirée de ma collection : un vrai spinelle bleu. Je vous la prête, ne la perdez pas.");

        var client = MakeStone("Kyanite", 0.8f, 2.8f);
        var reference = MakeStone("Spinelle_Bleu", 0.8f, 2.8f);
        if (client == null || reference == null || PlayerInventory.Instance == null)
        {
            yield return Say(npc, "Hmm, je ne retrouve plus mes pierres... Repassez me voir dans un instant.");
            yield break;
        }
        reference.MarkIdentified();

        while (PlayerInventory.Instance.FreeSlots < 2)
        {
            ToolFeedbackUI.Instance?.ShowMessage("Votre inventaire est plein : faites de la place pour deux pierres.");
            yield return new WaitForSeconds(2.5f);
        }
        PlayerInventory.Instance.TryAddGem(client);
        PlayerInventory.Instance.TryAddGem(reference);
        Sfx.Play(Sfx.Id.Pickup);
        state.OnTwinsGiven(client, reference);
        ToolFeedbackUI.Instance?.ShowMessage("Vous avez reçu deux pierres bleues : la pierre du client et la pierre de référence.");

        yield return Say(npc,
            "Commencez comme hier : le réfractomètre. Mesurez l'indice des deux pierres, avec la goutte de liquide de contact pour chacune.",
            "Revenez me voir quand vous aurez les deux indices.");
    }

    // The two indices are alike: the refractometer cannot tell them apart. The lessons on the crystal systems and the polariscope, then the polariscope opens.
    private IEnumerator TalkTwinsLesson(GameObject npc, StoryState state)
    {
        var client = state.TwinGem;
        var reference = state.ReferenceGem;
        if (client == null || reference == null)
        {
            yield return Say(npc, "Il me faut les deux pierres sous les yeux : la pierre du client et ma pierre de référence doivent être dans votre inventaire.");
            yield break;
        }

        var key = ExaminationTool.MeasurementType.RefractiveIndex.ToString();
        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        yield return Say(npc, "Alors ? Qu'est-ce que le réfractomètre a donné ?");
        yield return SayAs("Vous", player,
            $"{Fr(reference.GetMeasuredValue(key), "F2")} pour la pierre de référence, {Fr(client.GetMeasuredValue(key), "F2")} pour celle du client. C'est presque pareil...");
        yield return Say(npc,
            "Exactement. Les deux indices sont presque identiques : le réfractomètre seul ne tranche pas. Et la densité n'est guère plus parlante.",
            "Il faut interroger la pierre autrement. Pour cela, il faut comprendre comment un cristal est construit : c'est ce qui décide de la façon dont il traite la lumière.");

        DialogueBoxUI.Hide();
        yield return PlayLesson("systemes_cristallins");

        yield return Say(npc,
            "Une pierre cubique est isotrope : la lumière s'y comporte de la même façon dans toutes les directions. Les autres systèmes sont anisotropes : la lumière s'y sépare en deux rayons.",
            "Pour voir cette différence, voici un nouvel instrument : le polariscope, avec son conoscope. Laissez-moi vous le présenter.");

        DialogueBoxUI.Hide();
        yield return PlayLesson("polariscope");

        state.SetPhase(StoryState.PhaseTwinsPolariscope);
        yield return Say(npc,
            "Le polariscope est maintenant à vous : sur la rangée du haut, à droite. Passez-y les deux pierres, et tournez chacune d'un tour complet entre les filtres croisés.",
            "Puis servez-vous du conoscope sur la pierre du client. Vous retrouverez ces explications sur le panneau des leçons, dans l'entrée du laboratoire. Revenez me dire ce que vous avez vu.");
        ToolFeedbackUI.Instance?.ShowMessage("Passez les deux pierres au polariscope (rangée du haut, à droite), puis la pierre du client au conoscope.");
    }

    // What the polariscope and the conoscope have shown: three questions, and the conclusion.
    private IEnumerator TalkTwinsReport(GameObject npc, StoryState state)
    {
        var client = state.TwinGem;
        var reference = state.ReferenceGem;
        if (client == null || reference == null)
        {
            yield return Say(npc, "Il me faut les deux pierres sous les yeux : elles doivent être dans votre inventaire.");
            yield break;
        }

        var dark = "Elle reste sombre : isotrope";
        var flashes = "Elle s'éclaire quatre fois par tour : anisotrope";
        var bright = "Elle reste claire : agrégat";
        var seed = client.SpecimenId ?? "";

        yield return Say(npc, "Alors ? Qu'est-ce que le polariscope vous a montré ? Commençons par ma pierre de référence, le spinelle.");
        var picked = -1;
        yield return Quiz(npc, "Entre les filtres croisés, que fait le spinelle quand on le tourne ?", ThreeAnswers(dark, new[] { dark, flashes, bright }, seed + "p1", out var rightIndex), i => picked = i);
        if (picked != rightIndex)
        {
            yield return Say(npc, "Non : retournez au polariscope et regardez le spinelle faire un tour complet. Un spinelle est cubique, et une pierre cubique...  Revenez me le dire.");
            yield break;
        }
        yield return Say(npc, "Oui : sombre, quel que soit l'angle. Le spinelle est cubique, donc isotrope : il ne change pas la lumière polarisée.");

        picked = -1;
        yield return Quiz(npc, "Et la pierre du client, entre les filtres croisés ?", ThreeAnswers(flashes, new[] { dark, flashes, bright }, seed + "p2", out rightIndex), i => picked = i);
        if (picked != rightIndex)
        {
            yield return Say(npc, "Non : retournez au polariscope et faites tourner la pierre du client d'un tour complet. Regardez bien : s'éclaire-t-elle ?");
            yield break;
        }
        yield return Say(npc,
            "Oui : elle s'éclaire et s'éteint quatre fois par tour. C'est donc une pierre anisotrope. Ce n'est déjà plus un spinelle !",
            "Reste à savoir de quelle sorte d'anisotrope il s'agit : uniaxe ou biaxe. C'est le travail du conoscope.");

        var biaxial = "Deux « yeux » : biaxe";
        var uniaxial = "Une croix noire entourée d'anneaux : uniaxe";
        var none = "Aucune figure : isotrope";
        picked = -1;
        yield return Quiz(npc, "Que montre le conoscope sur la pierre du client ?", ThreeAnswers(biaxial, new[] { biaxial, uniaxial, none }, seed + "p3", out rightIndex), i => picked = i);
        if (picked != rightIndex)
        {
            yield return Say(npc, "Non : reprenez le conoscope devant la pierre du client, en la regardant le long d'un axe optique. Comptez les « yeux » : un ou deux ?");
            yield break;
        }

        state.SetPhase(StoryState.PhaseTwinsIdentify);
        yield return Say(npc,
            "Oui : deux axes optiques, donc une pierre biaxe. Pas un spinelle, donc : le spinelle est isotrope, la pierre du client est biaxe.",
            "Un indice vers 1,72, une densité autour de 3,7, une couleur bleue, des propriétés biaxes : à vous de trouver laquelle. Les onglets du réfractomètre listent les pierres possibles.",
            "Quand vous serez sûr de vous, remettez votre rapport au directeur.");
        ToolFeedbackUI.Instance?.ShowMessage("Identifiez la pierre du client et remettez votre rapport au directeur.");
    }

    // The director has validated: Thouvenot confirms the answer, shows where the stone came from, and gives a first clue about Vincent.
    private IEnumerator TalkTwinsValidate(GameObject npc, StoryState state)
    {
        var client = state.TwinGem;
        var reference = state.ReferenceGem;
        if (client == null)
        {
            yield return Say(npc, "Alors, la pierre du client ? Il me faut l'avoir sous les yeux pour confirmer votre réponse : elle doit être dans votre inventaire.");
            yield break;
        }

        var player = PlayerTransform != null ? PlayerTransform.gameObject : null;
        yield return Say(npc, "Le directeur m'a dit que votre rapport est validé. Alors, quelle est votre réponse ?");
        yield return SayAs("Vous", player, $"Une {client.Definition.displayName.ToLowerInvariant()}.");
        yield return Say(npc,
            "C'est exact ! Un disthène, comme on l'appelle aussi. Bleue comme un spinelle, mais elle n'a rien de cubique : c'est un cristal triclinique, qui se laisse rayer dans un sens et pas dans l'autre. Même sa dureté dépend de la direction.",
            "Vous voyez pourquoi j'ai tenu à vous faire comprendre les systèmes cristallins : une pierre se reconnaît à ce qu'elle est à l'intérieur, pas seulement à sa couleur ni à son indice.");
        yield return Say(npc,
            "Il y a autre chose. Dans le sachet du client, j'ai trouvé un petit cachet : « Valmont ». Il avait acheté la pierre sous le nom de spinelle bleu à cette maison.",
            "Ce nom me dit quelque chose. Vincent l'avait souligné dans son carnet, la semaine avant de disparaître. Je ne sais pas encore ce que cela veut dire, mais gardez l'œil ouvert.");
        yield return SayAs("Vous", player, "Valmont... Je m'en souviendrai.");
        yield return Say(npc,
            "Je reprends les deux pierres : je rendrai la sienne au client, avec votre conclusion. Il vous envoie 60 € pour votre peine.");

        PlayerInventory.Instance?.RemoveGem(client);
        if (reference != null) PlayerInventory.Instance?.RemoveGem(reference);
        PlayerWallet.Instance?.AddMoney(60);
        Sfx.Play(Sfx.Id.Coin);
        ToolFeedbackUI.Instance?.ShowMessage("Récompense reçue : 60 €.");
        state.SetPhase(StoryState.PhaseTwinsDone);
        yield return Say(npc, "Bon travail. Vous venez de faire votre premier pas contre ceux qui vendent une pierre pour une autre.");
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
            NightLight.StoryLantern = false;
            yield break;
        }

        // The walk is at night: the player carries a lantern, and so does Professor Thouvenot.
        NightLight.StoryLantern = true;

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
        GiveLantern(thouvenot);

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
        NightLight.StoryLantern = false;
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
