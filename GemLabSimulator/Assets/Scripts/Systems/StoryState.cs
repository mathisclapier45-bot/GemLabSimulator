using System;
using System.Collections.Generic;
using UnityEngine;

// Where the player is in the story: the prologue (the arrival at Crystal Grove, the walk with Professor Thouvenot, the letter of Vincent in the mailbox of
// the house), then the free game. It is saved with the game. An old save, made before the story existed, is "settled": nothing is locked and nothing is told.
// Lives on the player (added on demand, like the other persistent states).
public class StoryState : MonoBehaviour
{
    public static StoryState Instance { get; private set; }

    // The stages of the prologue.
    public const int Arrival = 0;        // the player has just landed: Professor Thouvenot waits at the airport
    public const int Tour = 1;           // the walk through the town is going on (TourIndex says where)
    public const int GoHome = 2;         // the walk is over: the letter of Vincent is waiting in the mailbox of the house
    public const int Settled = 3;        // the letter has been read: the game is free

    // After the prologue, the first morning: the player sleeps, the phone rings, the player goes to the laboratory where Thouvenot shows the place.
    public const int PhaseSleep = 1;     // the letter has been read: the player has to sleep
    public const int PhaseCall = 2;      // the next morning: the phone rings outside, until the player answers
    public const int PhaseLab = 3;       // the player has to join Thouvenot at the laboratory (the office of gemology)
    public const int PhaseDone = 4;      // the tutorial is over: the game is free

    // The first stone (the numbers come after PhaseDone so that the saves already made keep their meaning): one easy stone comes by post, the player examines it with the loupe,
    // takes it to Professor Thouvenot, who has it weighed, and the player tells her the weight.
    public const int PhaseTutorialLoupe = 5;     // take the stone from the mailbox, wipe it, examine it with the loupe
    public const int PhaseTutorialReturn = 6;    // bring the stone to Thouvenot and ask her what comes next
    public const int PhaseTutorialWeigh = 7;     // weigh the stone on the carat balance
    public const int PhaseTutorialReport = 8;    // tell Thouvenot the weight
    public const int PhaseTutorialDensity = 9;   // measure the density on the hydrostatic balance (the weight in the air is known)
    public const int PhaseTutorialDensityReport = 10;   // tell Thouvenot the density
    public const int PhaseTutorialIdentify = 11;        // pick the species of the stone among the ones Thouvenot suggests

    public int Stage { get; private set; } = Settled;
    public int Phase { get; private set; } = PhaseDone;
    public int TourIndex { get; private set; }
    public bool Legacy { get; private set; } = true;       // a save from before the story: everything is open

    // The letters of the story the player has received (ids of StoryLetters), and the ones that were read.
    private readonly List<string> received = new List<string>();
    private readonly HashSet<string> read = new HashSet<string>();

    public IReadOnlyList<string> Received => received;

    private readonly List<string> calls = new List<string>();
    public IReadOnlyList<string> Calls => calls;

    // The stone of the tutorial (the specimen id of the stone taken from the mailbox).
    public string TutorialSpecimen { get; private set; } = "";
    public bool TutorialStoneTaken => !string.IsNullOrEmpty(TutorialSpecimen);

    // The tutorial has three stones, one after the other: a transparent one, a translucent one, an opaque one (see TutorialStones).
    public const int TutorialRounds = 3;
    public int TutorialRound { get; private set; } = 1;

    // The stone is identified: the next one arrives in the mailbox.
    public void NextTutorialRound()
    {
        TutorialRound = Mathf.Min(TutorialRounds, TutorialRound + 1);
        TutorialSpecimen = "";
        SetPhase(PhaseTutorialLoupe);
    }

    public bool InTutorial => !Legacy && Phase >= PhaseTutorialLoupe && Phase <= PhaseTutorialIdentify;

    // Until the tutorial is over, the mailbox gives one stone only.
    public bool MailLimited => !Legacy && Phase != PhaseDone;

    // Professor Thouvenot waits in the laboratory during the tutorial.
    public bool ThouvenotWaits => InTutorial;

    // The phone exists from the first call (an old save has it from the start).
    public bool PhoneUnlocked => Legacy || Phase >= PhaseCall;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
    }

    public static StoryState Ensure()
    {
        if (Instance == null && PlayerController.Instance != null)
            PlayerController.Instance.gameObject.AddComponent<StoryState>();
        return Instance;
    }

    // A game that starts with the story: the cinematic, then the arrival.
    public void BeginStory()
    {
        Legacy = false;
        Stage = Arrival;
        Phase = 0;
        TourIndex = 0;
        received.Clear();
        read.Clear();
        calls.Clear();
        TutorialSpecimen = "";
        TutorialRound = 1;
        OnChanged?.Invoke();
    }

    public void SetStage(int stage, int tourIndex = 0)
    {
        Stage = stage;
        TourIndex = tourIndex;
        OnChanged?.Invoke();
    }

    public bool PrologueOver => Stage >= Settled;

    public void SetPhase(int phase)
    {
        Phase = phase;
        OnChanged?.Invoke();
    }

    // The player has slept: the first morning begins, with the call of Thouvenot.
    public void OnSlept()
    {
        if (!Legacy && Phase == PhaseSleep) SetPhase(PhaseCall);
    }

    // The player takes the stone of the tutorial out of the mailbox.
    public void OnTutorialStoneTaken(GemInstance gem)
    {
        TutorialSpecimen = gem.SpecimenId ?? "";
        OnChanged?.Invoke();
    }

    public bool IsTutorialStone(GemInstance gem) => gem != null && TutorialStoneTaken && gem.SpecimenId == TutorialSpecimen;

    // An instrument has recorded something about a stone: the tutorial moves on when it is the stone of the tutorial.
    public void OnObserved(GemInstance gem, string key)
    {
        if (!InTutorial || !IsTutorialStone(gem)) return;

        if (Phase == PhaseTutorialLoupe && gem.HasObserved(ExaminationTool.MeasurementType.VisualExamination.ToString()) && gem.HasObserved("LoupeInspection"))
        {
            SetPhase(PhaseTutorialReturn);
            ToolFeedbackUI.Instance?.ShowMessage("Examen à la loupe terminé ! Rapportez la pierre à la professeure Thouvenot, dans le laboratoire.");
        }
        else if (Phase == PhaseTutorialWeigh && key == ExaminationTool.MeasurementType.Weight.ToString())
        {
            SetPhase(PhaseTutorialReport);
            ToolFeedbackUI.Instance?.ShowMessage("Poids relevé ! Allez le dire à la professeure Thouvenot.");
        }
        else if (Phase == PhaseTutorialDensity && key == ExaminationTool.MeasurementType.Density.ToString())
        {
            SetPhase(PhaseTutorialDensityReport);
            ToolFeedbackUI.Instance?.ShowMessage("Densité mesurée ! Allez la dire à la professeure Thouvenot.");
        }
    }

    public void AddCall(string text)
    {
        calls.Add(text);
        OnChanged?.Invoke();
    }

    // What the player has to do now, for the phone.
    public string Objective
    {
        get
        {
            if (Legacy) return "Examinez les pierres du courrier avec les instruments du laboratoire.";
            if (Stage == Arrival || Stage == Tour) return "Suivez la professeure Thouvenot dans la ville.";
            if (Stage == GoHome) return "Entrez dans votre maison et lisez la lettre de Vincent, dans la boîte aux lettres.";
            switch (Phase)
            {
                case PhaseSleep: return "Dormez dans votre lit (touche E) pour passer à demain matin.";
                case PhaseCall: return "Répondez au téléphone : il sonne.";
                case PhaseLab: return "Rejoignez la professeure Thouvenot à la Maison et Bureau de Gemmologie.";
                case PhaseTutorialLoupe:
                    return TutorialStoneTaken
                        ? "Essuyez la pierre à la chamoisine, puis examinez-la à la loupe (onglets à côté de la loupe), en la tournant dans tous les sens."
                        : "Prenez la pierre arrivée par la poste dans la boîte aux lettres du laboratoire (touche E).";
                case PhaseTutorialReturn: return "Rapportez la pierre à la professeure Thouvenot, dans le laboratoire, et demandez-lui la suite.";
                case PhaseTutorialWeigh: return "Pesez la pierre sur la balance à carats (rangée du bas du laboratoire).";
                case PhaseTutorialReport: return "Dites le poids de la pierre à la professeure Thouvenot.";
                case PhaseTutorialDensity: return "Mesurez la densité de la pierre sur la balance hydrostatique (rangée du haut du laboratoire, près de la loupe).";
                case PhaseTutorialDensityReport: return "Dites la densité de la pierre à la professeure Thouvenot.";
                case PhaseTutorialIdentify: return "Retrouvez la professeure Thouvenot : choisissez, parmi les pierres qu'elle propose, celle qui correspond à vos mesures.";
                default: return "Prenez une pierre dans la boîte aux lettres du bureau, essuyez-la à la chamoisine, puis examinez-la à la loupe.";
            }
        }
    }

    // ---- Letters ---------------------------------------------------------------------------------------------------------

    public void Receive(string id)
    {
        if (received.Contains(id)) return;
        received.Add(id);
        OnChanged?.Invoke();
    }

    public bool HasReceived(string id) => received.Contains(id);

    public bool IsRead(string id) => read.Contains(id);

    public int UnreadCount
    {
        get
        {
            var count = 0;
            foreach (var id in received) if (!read.Contains(id)) count++;
            return count;
        }
    }

    public void MarkRead(string id)
    {
        if (!received.Contains(id) || !read.Add(id)) return;
        OnChanged?.Invoke();
    }

    // ---- Save ------------------------------------------------------------------------------------------------------------

    public void Save(SaveData data)
    {
        data.storyStarted = !Legacy;
        data.storyStage = Stage;
        data.storyTourIndex = TourIndex;
        data.storyPhase = Phase;
        data.storyCalls = new List<string>(calls);
        data.storyTutorialSpecimen = TutorialSpecimen;
        data.storyTutorialRound = TutorialRound;
        data.storyLettersReceived = new List<string>(received);
        data.storyLettersRead = new List<string>(read);
    }

    public void Load(SaveData data)
    {
        received.Clear();
        read.Clear();
        calls.Clear();
        TutorialSpecimen = "";
        TutorialRound = 1;
        if (!data.storyStarted)
        {
            // A save from before the story: the game is free and open.
            Legacy = true;
            Stage = Settled;
            Phase = PhaseDone;
            TourIndex = 0;
        }
        else
        {
            Legacy = false;
            Stage = Mathf.Clamp(data.storyStage, Arrival, Settled);
            TourIndex = Mathf.Max(0, data.storyTourIndex);
            Phase = Stage >= Settled && data.storyPhase == 0 ? PhaseDone : data.storyPhase;
            if (data.storyCalls != null) calls.AddRange(data.storyCalls);
            TutorialSpecimen = data.storyTutorialSpecimen ?? "";
            TutorialRound = Mathf.Clamp(data.storyTutorialRound, 1, TutorialRounds);
            if (data.storyLettersReceived != null) received.AddRange(data.storyLettersReceived);
            if (data.storyLettersRead != null) foreach (var id in data.storyLettersRead) read.Add(id);
        }
        OnChanged?.Invoke();
    }
}

// What is open and what is not yet. The places of the second half of the game open with the reputation of the player (a good report to the director gives
// a point, and the level doubles each time): this is a first rule, until the chapters of the story decide it. A save from before the story has everything open.
public static class StoryGate
{
    private struct Rule
    {
        public string Scene;            // the name of the scene, or its beginning
        public int Level;               // the reputation level needed
        public string Message;          // what the closed door says, or null for the usual sentence
    }

    private static readonly Rule[] Rules =
    {
        new Rule { Scene = "Musee", Level = 2 },
        new Rule { Scene = "MarcheBrutes", Level = 2 },
        new Rule { Scene = "AtelierLapidaire", Level = 2 },
        new Rule { Scene = "Manoir", Level = 3 },
        // The big laboratory, with the complex instruments (spectrometers, FTIR, Raman, EDXRF, MEB, LA-ICP-MS...): the office of gemology has the classic ones.
        new Rule { Scene = "Laboratoire", Level = 3, Message = "Le grand laboratoire de gemmologie est fermé pour le moment : ses instruments sont trop complexes pour débuter. Il ouvrira avec la réputation de niveau 3." },
        new Rule { Scene = "LaboTraitements", Level = 3 },
        new Rule { Scene = "Diamantaires", Level = 4 },       // the Rapaport is on the 6th floor
        new Rule { Scene = "Bijouterie", Level = 5 },
        new Rule { Scene = "Banque", Level = 5 },              // the bank, and its auctions
        new Rule { Scene = "Tunnel", Level = 6 },
    };

    private static bool Everything => StoryState.Instance == null || StoryState.Instance.Legacy;

    private static int Level => ReputationState.Instance != null ? ReputationState.Instance.Level : 1;

    public static int RequiredLevel(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return 0;
        foreach (var rule in Rules)
            if (scene.StartsWith(rule.Scene, StringComparison.Ordinal)) return rule.Level;
        return 0;
    }

    public static bool IsOpen(string scene, out string message)
    {
        message = null;
        if (Everything) return true;
        foreach (var rule in Rules)
        {
            if (!scene.StartsWith(rule.Scene, StringComparison.Ordinal)) continue;
            if (Level >= rule.Level) return true;
            message = rule.Message ?? $"Ce lieu ouvrira plus tard : il faut la réputation de niveau {rule.Level} (chaque pierre identifiée et validée par le directeur du laboratoire donne de l'XP).";
            return false;
        }
        return true;
    }

    public static bool IsOpen(string scene) => IsOpen(scene, out _);

    // The auctions of the bank, and everything that talks about them (the bulletin, the calendar, the catalogue in the mailbox).
    public static bool AuctionsOpen => IsOpen("Banque");

    // The Rapaport price list, sold in the Diamond Quarter.
    public static bool RapaportOpen => IsOpen("Diamantaires");
}

// The three stones of the tutorial: what comes by post, and the species suggested beside it in the question of identification.
public static class TutorialStones
{
    public static string Species(int round) => round == 2 ? "Jade_Nephrite" : round == 3 ? "Hematite" : "Amethyste";

    // The two other species suggested with the right one (they look alike, but their density is not the same).
    public static string[] Others(int round)
    {
        switch (round)
        {
            case 2: return new[] { "Serpentine_Bowenite", "Jade_Jadeite" };
            case 3: return new[] { "Pyrite", "Lapis_Lazuli" };
            default: return new[] { "Fluorite", "Tanzanite" };
        }
    }

    // The reason the client gives in the letter.
    public static string Reason(int round)
    {
        switch (round)
        {
            case 2: return "Cette pierre verte, polie en cabochon, vient d'un voyage de mon père. Il disait que c'était du jade, mais on m'a dit qu'il existait beaucoup d'imitations. J'aimerais savoir ce qu'il en est.";
            case 3: return "J'ai trouvé cette pierre sombre et très lourde dans le tiroir d'un vieil établi. Elle brille comme un métal poli, mais elle ne ressemble à aucun bijou que je connaisse. Pouvez-vous me renseigner ?";
            default: return "Cette pierre violette appartenait à ma grand-mère, qui la portait en bague depuis toujours. Personne dans la famille ne sait ce que c'est. On m'a dit que le laboratoire de Vincent Pardieu saurait me renseigner : j'espère qu'il fonctionne encore.";
        }
    }
}
