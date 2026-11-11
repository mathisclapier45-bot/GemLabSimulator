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

    // The stone of the stranger (the next day): a man of the town gives the player a stone found on the ground and promises a reward if they identify it;
    // Thouvenot introduces the refractometer; the player finds the species by themself and reports it to the director.
    public const int PhaseRest = 12;               // the tutorial is over: go home to rest (on the way, a man of the town stops the player)
    public const int PhaseStoneSleep = 13;         // the man has given a stone: go to sleep
    public const int PhaseStoneLab = 14;           // the next morning: show the stone to Thouvenot, in the laboratory
    public const int PhaseRefractometer = 15;      // measure the index of the stone with the refractometer (the drop of liquid first)
    public const int PhaseStoneIdentify = 16;      // identify the stone and report it to the director
    public const int PhaseStoneValidate = 17;      // the director has validated: tell Thouvenot, who confirms the answer and lets the player keep the stone
    public const int PhaseStoneReturn = 18;        // give the stone back to the man, who waits at the footbridge on the way home (any hour)

    // Chapter 2, "the stones that look alike": the next day Thouvenot gives the player a client's stone (a kyanite sold as a blue spinel) and a reference spinel. The two
    // have almost the same index: the lesson on the crystal systems and the polariscope (with the conoscope) tell them apart.
    public const int PhaseTwinsSleep = 19;         // Anselme has his stone back: go to sleep
    public const int PhaseTwinsLab = 20;           // the next morning: Thouvenot gives the two stones
    public const int PhaseTwinsRefracto = 21;      // measure the index of both stones with the refractometer
    public const int PhaseTwinsLesson = 22;        // the two indices are alike: Thouvenot gives the lessons (crystal systems, polariscope) and opens the polariscope
    public const int PhaseTwinsPolariscope = 23;   // examine both stones with the polariscope, and the client's stone with the conoscope
    public const int PhaseTwinsReport = 24;        // tell Thouvenot what the polariscope showed (questions)
    public const int PhaseTwinsIdentify = 25;      // identify the client's stone and report it to the director
    public const int PhaseTwinsValidate = 26;      // the director has validated: tell Thouvenot
    public const int PhaseTwinsDone = 27;          // chapter 2 is over

    // Chapter 3, "the colour does not lie" (the dichroscope): two cases, each with a client's stone and a reference stone lent by Thouvenot.
    // Case 1: a red spinel sold as a ruby. Case 2: an iolite sold as a tanzanite. The phases of a case are Observe, Report, Identify, Validate (four numbers each).
    public const int PhasePleoSleep = 30;          // chapter 2 is over: go to sleep
    public const int PhasePleoLab = 31;            // the next morning: Thouvenot teaches the dichroscope and gives the first two stones
    public const int PhasePleoObserve = 32;        // case 1: examine both stones with the dichroscope (+4 for case 2)
    public const int PhasePleoReport = 33;         // tell Thouvenot what the dichroscope showed (questions)
    public const int PhasePleoIdentify = 34;       // identify the client's stone and report it to the director
    public const int PhasePleoValidate = 35;       // the director has validated: tell Thouvenot (she gives the second case after the first one)
    public const int PhasePleoDone = 40;           // chapter 3 is over (the dichroscope for now)

    // After the stone of Anselme is confirmed: Léa, the clerk of the GemStore, has something left by Vincent (a store credit, the card binder and a booster).
    public const int PhaseLeaVisit = 28;

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
    public bool MailLimited => !Legacy && Phase != PhaseDone && Phase < PhaseRest;

    // The stone given by the man of the town (the specimen id), and the quest of that stone.
    public string StrangerSpecimen { get; private set; } = "";
    public bool StoneQuest => !Legacy && Phase >= PhaseStoneLab && Phase <= PhaseStoneValidate;

    // The stones of chapter 2: the client's stone and the reference stone lent by Thouvenot (specimen ids).
    public string TwinSpecimen { get; private set; } = "";
    public string TwinReference { get; private set; } = "";
    public bool TwinsQuest => !Legacy && Phase >= PhaseTwinsLab && Phase <= PhaseTwinsValidate;

    public bool IsTwinStone(GemInstance gem) => gem != null && !string.IsNullOrEmpty(TwinSpecimen) && gem.SpecimenId == TwinSpecimen;
    public bool IsTwinReference(GemInstance gem) => gem != null && !string.IsNullOrEmpty(TwinReference) && gem.SpecimenId == TwinReference;

    public void OnTwinsGiven(GemInstance client, GemInstance reference)
    {
        TwinSpecimen = client.SpecimenId ?? "";
        TwinReference = reference.SpecimenId ?? "";
        SetPhase(PhaseTwinsRefracto);
    }

    private static GemInstance FindGem(string specimen)
    {
        if (string.IsNullOrEmpty(specimen) || PlayerInventory.Instance == null) return null;
        foreach (var gem in PlayerInventory.Instance.Gems)
            if (gem.SpecimenId == specimen) return gem;
        return null;
    }

    public GemInstance TwinGem => FindGem(TwinSpecimen);
    public GemInstance ReferenceGem => FindGem(TwinReference);

    // The stones of chapter 3: for each of the two cases, the client's stone then the reference stone (specimen ids).
    private readonly string[] pleoStones = { "", "", "", "" };

    public bool PleoQuest => !Legacy && Phase >= PhasePleoLab && Phase <= PhasePleoValidate + 4;

    // The case (0 or 1) and the step (0 observe, 1 report, 2 identify, 3 validate) of the phase, or -1.
    public int PleoCase => Phase >= PhasePleoObserve && Phase <= PhasePleoValidate + 4 ? (Phase - PhasePleoObserve) / 4 : -1;
    public int PleoStep => Phase >= PhasePleoObserve && Phase <= PhasePleoValidate + 4 ? (Phase - PhasePleoObserve) % 4 : -1;

    public static int PleoPhase(int pleoCase, int step) => PhasePleoObserve + pleoCase * 4 + step;

    public bool IsPleoClient(int pleoCase, GemInstance gem) => gem != null && !string.IsNullOrEmpty(pleoStones[pleoCase * 2]) && gem.SpecimenId == pleoStones[pleoCase * 2];
    public bool IsPleoReference(int pleoCase, GemInstance gem) => gem != null && !string.IsNullOrEmpty(pleoStones[pleoCase * 2 + 1]) && gem.SpecimenId == pleoStones[pleoCase * 2 + 1];
    public GemInstance PleoClientGem(int pleoCase) => FindGem(pleoStones[pleoCase * 2]);
    public GemInstance PleoReferenceGem(int pleoCase) => FindGem(pleoStones[pleoCase * 2 + 1]);

    // Thouvenot gives the two stones of a case: the examination with the dichroscope begins.
    public void OnPleoGiven(int pleoCase, GemInstance client, GemInstance reference)
    {
        pleoStones[pleoCase * 2] = client.SpecimenId ?? "";
        pleoStones[pleoCase * 2 + 1] = reference.SpecimenId ?? "";
        SetPhase(PleoPhase(pleoCase, 0));
    }

    // Professor Thouvenot waits in the laboratory during the tutorial and the next days.
    public bool ThouvenotWaits => InTutorial || StoneQuest || TwinsQuest || PleoQuest;

    public void OnStrangerGave(GemInstance gem)
    {
        StrangerSpecimen = gem.SpecimenId ?? "";
        SetPhase(PhaseStoneSleep);
    }

    public bool IsStrangerStone(GemInstance gem) => gem != null && !string.IsNullOrEmpty(StrangerSpecimen) && gem.SpecimenId == StrangerSpecimen;

    // The director has validated the identification of a stone: for the one of the stranger, Thouvenot has to confirm the answer.
    public void OnStoneIdentified(GemInstance gem)
    {
        if (Legacy) return;
        if (Phase == PhaseTwinsIdentify && IsTwinStone(gem))
        {
            SetPhase(PhaseTwinsValidate);
            ToolFeedbackUI.Instance?.ShowMessage("Identification validée ! Allez la confirmer à la professeure Thouvenot.");
            return;
        }
        if (PleoStep == 2 && IsPleoClient(PleoCase, gem))
        {
            SetPhase(Phase + 1);
            ToolFeedbackUI.Instance?.ShowMessage("Identification validée ! Allez la confirmer à la professeure Thouvenot.");
            return;
        }
        if (Phase != PhaseStoneIdentify || !IsStrangerStone(gem)) return;
        SetPhase(PhaseStoneValidate);
        ToolFeedbackUI.Instance?.ShowMessage("Identification validée ! Allez la confirmer à la professeure Thouvenot.");
    }

    // The card binder of Vincent, given by Léa: its tab appears in the bag once it is given. A save that is past that visit has it already.
    public bool BinderUnlocked { get; private set; }

    public static bool BinderOpen => Instance == null || Instance.Legacy || Instance.BinderUnlocked;

    public void UnlockBinder()
    {
        if (BinderUnlocked) return;
        BinderUnlocked = true;
        OnChanged?.Invoke();
    }

    // The people whose number the player has (Thouvenot is always there once the phone exists).
    private readonly HashSet<string> contacts = new HashSet<string>();
    public IEnumerable<string> Contacts => contacts;

    public bool HasContact(string id) => id == "thouvenot" ? PhoneUnlocked : contacts.Contains(id);

    public void AddContact(string id)
    {
        if (contacts.Add(id)) OnChanged?.Invoke();
    }

    // ---- The instruments of the laboratory --------------------------------------------------------------------------------
    // They are not all usable from the start: each one opens when Professor Thouvenot has told the player about it (the loupe for the first stone,
    // then the carat balance, the hydrostatic balance and the refractometer; the other classic instruments of the office wait for a later chapter).
    private readonly HashSet<string> tools = new HashSet<string>();

    private static readonly ExaminationTool.MeasurementType[] LaterTools =
    {
        ExaminationTool.MeasurementType.OpticalCharacter,
        ExaminationTool.MeasurementType.Pleochroism,
        ExaminationTool.MeasurementType.FluorescenceLongWave,
        ExaminationTool.MeasurementType.FluorescenceShortWave,
        ExaminationTool.MeasurementType.ChelseaFilter,
        ExaminationTool.MeasurementType.Magnetism,
        ExaminationTool.MeasurementType.Dimensions,
        ExaminationTool.MeasurementType.Inclusions,
        ExaminationTool.MeasurementType.AbsorptionSpectrum,
        ExaminationTool.MeasurementType.InterferenceFigure,
    };

    private static bool IsLockedByStory(ExaminationTool.MeasurementType type)
    {
        switch (type)
        {
            case ExaminationTool.MeasurementType.VisualExamination:
            case ExaminationTool.MeasurementType.Weight:
            case ExaminationTool.MeasurementType.Density:
            case ExaminationTool.MeasurementType.RefractiveIndex:
                return true;
        }
        return Array.IndexOf(LaterTools, type) >= 0;
    }

    // Can the player use this instrument yet? A save from before the story, and the instruments of the big laboratories, are always open.
    public bool ToolUnlocked(ExaminationTool.MeasurementType type)
    {
        if (Legacy || !IsLockedByStory(type)) return true;
        return tools.Contains(type.ToString());
    }

    public void UnlockTool(ExaminationTool.MeasurementType type)
    {
        if (tools.Add(type.ToString())) OnChanged?.Invoke();
    }

    // What Thouvenot has presented so far, from where the story is (the instruments stay open once they are).
    private void GrantToolsForPhase()
    {
        if (Legacy || !PrologueOver) return;
        var p = Phase;
        var finished = p == PhaseDone || p >= PhaseStoneReturn;
        var later = TutorialRound >= 2;
        if (finished || p >= PhaseTutorialLoupe) tools.Add(ExaminationTool.MeasurementType.VisualExamination.ToString());
        if (finished || later || p >= PhaseTutorialWeigh) tools.Add(ExaminationTool.MeasurementType.Weight.ToString());
        if (finished || later || p >= PhaseTutorialDensity) tools.Add(ExaminationTool.MeasurementType.Density.ToString());
        if (finished || p >= PhaseRefractometer) tools.Add(ExaminationTool.MeasurementType.RefractiveIndex.ToString());
        if (p >= PhaseTwinsPolariscope) tools.Add(ExaminationTool.MeasurementType.OpticalCharacter.ToString());
        if (p >= PhasePleoObserve) tools.Add(ExaminationTool.MeasurementType.Pleochroism.ToString());
        // The other instruments (LaterTools) stay closed until a later chapter of the tutorial presents them: it calls UnlockTool.
    }

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
        StrangerSpecimen = "";
        TwinSpecimen = "";
        TwinReference = "";
        for (var i = 0; i < pleoStones.Length; i++) pleoStones[i] = "";
        BinderUnlocked = false;
        contacts.Clear();
        tools.Clear();
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
        GrantToolsForPhase();
        OnChanged?.Invoke();
    }

    // The player has slept: the first morning begins, with the call of Thouvenot.
    public void OnSlept()
    {
        if (Legacy) return;
        if (Phase == PhaseSleep) SetPhase(PhaseCall);
        else if (Phase == PhasePleoSleep)
        {
            SetPhase(PhasePleoLab);
            ToolFeedbackUI.Instance?.ShowMessage("Un nouveau jour : la professeure Thouvenot vous attend au laboratoire.");
        }
        else if (Phase == PhaseTwinsSleep)
        {
            SetPhase(PhaseTwinsLab);
            ToolFeedbackUI.Instance?.ShowMessage("Un nouveau jour : la professeure Thouvenot vous attend au laboratoire.");
        }
        else if (Phase == PhaseStoneSleep)
        {
            SetPhase(PhaseStoneLab);
            ToolFeedbackUI.Instance?.ShowMessage("Un nouveau jour : allez présenter la pierre à la professeure Thouvenot, au laboratoire.");
        }
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
        if (Legacy) return;

        // The stone of the stranger: its index has been read with the refractometer.
        if (Phase == PhaseRefractometer && IsStrangerStone(gem) && key == "RefractiveIndex")
        {
            SetPhase(PhaseStoneIdentify);
            ToolFeedbackUI.Instance?.ShowMessage("Indice relevé ! Tournez la lentille pour lire toutes les valeurs, consultez les onglets à gauche du réfractomètre, puis identifiez la pierre et remettez votre rapport au directeur.");
            return;
        }

        // Chapter 2: both indices read, then both stones seen at the polariscope (and the client's one at the conoscope).
        if (Phase == PhaseTwinsRefracto && key == "RefractiveIndex" && (IsTwinStone(gem) || IsTwinReference(gem)))
        {
            var client = TwinGem;
            var reference = ReferenceGem;
            var refractive = ExaminationTool.MeasurementType.RefractiveIndex.ToString();
            if (client != null && reference != null && client.HasMeasured(refractive) && reference.HasMeasured(refractive))
            {
                SetPhase(PhaseTwinsLesson);
                ToolFeedbackUI.Instance?.ShowMessage("Les deux indices sont relevés... et ils sont presque identiques ! Allez en parler à la professeure Thouvenot.");
            }
            return;
        }
        if (Phase == PhaseTwinsPolariscope && (IsTwinStone(gem) || IsTwinReference(gem)))
        {
            var client = TwinGem;
            var reference = ReferenceGem;
            var optical = ExaminationTool.MeasurementType.OpticalCharacter.ToString();
            var figure = ExaminationTool.MeasurementType.InterferenceFigure.ToString();
            if (client != null && reference != null && client.HasObserved(optical) && reference.HasObserved(optical) && client.HasObserved(figure))
            {
                SetPhase(PhaseTwinsReport);
                ToolFeedbackUI.Instance?.ShowMessage("Polariscope et conoscope : vous avez tout observé. Allez dire à la professeure Thouvenot ce que vous avez vu.");
            }
            return;
        }

        // Chapter 3: both stones of the case seen with the dichroscope.
        if (PleoStep == 0 && key == ExaminationTool.MeasurementType.Pleochroism.ToString())
        {
            var pleoCase = PleoCase;
            if (IsPleoClient(pleoCase, gem) || IsPleoReference(pleoCase, gem))
            {
                var client = PleoClientGem(pleoCase);
                var reference = PleoReferenceGem(pleoCase);
                var seen = ExaminationTool.MeasurementType.Pleochroism.ToString();
                if (client != null && reference != null && client.HasObserved(seen) && reference.HasObserved(seen))
                {
                    SetPhase(Phase + 1);
                    ToolFeedbackUI.Instance?.ShowMessage("Dichroscope : vous avez observé les deux pierres. Allez dire à la professeure Thouvenot ce que vous avez vu.");
                }
            }
            return;
        }

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
                case PhaseRest: return "Vous avez bien travaillé : rentrez vous reposer chez vous.";
                case PhaseStoneSleep: return "Allez dormir dans votre lit : demain, présentez la pierre trouvée à la professeure Thouvenot, au laboratoire.";
                case PhaseStoneLab: return "Présentez la pierre à la professeure Thouvenot, au laboratoire (Maison et Bureau de Gemmologie).";
                case PhaseRefractometer: return "Mesurez l'indice de réfraction de la pierre avec le réfractomètre : une goutte de liquide sur l'hémicylindre d'abord, la pierre ensuite.";
                case PhaseStoneIdentify: return "Identifiez la pierre (onglets à gauche du réfractomètre, loupe, densité...) puis remettez votre rapport au directeur.";
                case PhaseStoneValidate: return "Allez confirmer votre réponse à la professeure Thouvenot, au laboratoire.";
                case PhaseStoneReturn: return "Rendez la pierre à Anselme : il vous attend près de la passerelle, sur le chemin de votre maison. En attendant, passez vos autres gemmes au réfractomètre pour compléter votre grimoire.";
                case PhasePleoSleep: return "Reposez-vous : demain, la professeure Thouvenot vous attend au laboratoire. Dormez dans votre lit.";
                case PhasePleoLab: return "Retrouvez la professeure Thouvenot au laboratoire : elle va vous apprendre à lire la couleur d'une pierre.";
                case PhaseLeaVisit: return "Passez vos anciennes pierres au réfractomètre pour compléter votre grimoire, puis rendez visite à Léa, la vendeuse du GemStore : elle a quelque chose à vous remettre de la part de Vincent.";
                case PhaseTwinsSleep: return "Rentrez chez vous et dormez : demain, la professeure Thouvenot vous attend au laboratoire.";
                case PhaseTwinsLab: return "Retrouvez la professeure Thouvenot au laboratoire : elle a une affaire pour vous.";
                case PhaseTwinsRefracto: return "Mesurez l'indice de réfraction des deux pierres bleues (la pierre du client et la pierre de référence) avec le réfractomètre.";
                case PhaseTwinsLesson: return "Les deux indices sont presque identiques : allez en parler à la professeure Thouvenot.";
                case PhaseTwinsPolariscope: return "Passez les deux pierres au polariscope (tournez-les d'un tour complet), puis la pierre du client au conoscope.";
                case PhaseTwinsReport: return "Dites à la professeure Thouvenot ce que le polariscope et le conoscope ont montré.";
                case PhaseTwinsIdentify: return "Identifiez la pierre du client (est-ce bien un spinelle bleu ?) et remettez votre rapport au directeur.";
                case PhaseTwinsValidate: return "Allez confirmer votre réponse à la professeure Thouvenot, au laboratoire.";
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
        data.storyStrangerSpecimen = StrangerSpecimen;
        data.storyContacts = new List<string>(contacts);
        data.storyTwinSpecimen = TwinSpecimen;
        data.storyBinder = BinderUnlocked;
        data.storyPleoStones = new List<string>(pleoStones);
        data.storyTwinReference = TwinReference;
        data.storyTools = new List<string>(tools);
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
        StrangerSpecimen = "";
        TwinSpecimen = "";
        TwinReference = "";
        for (var i = 0; i < pleoStones.Length; i++) pleoStones[i] = "";
        BinderUnlocked = false;
        contacts.Clear();
        tools.Clear();
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
            StrangerSpecimen = data.storyStrangerSpecimen ?? "";
            if (data.storyContacts != null) foreach (var id in data.storyContacts) contacts.Add(id);
            TwinSpecimen = data.storyTwinSpecimen ?? "";
            TwinReference = data.storyTwinReference ?? "";
            BinderUnlocked = data.storyBinder;
            if (data.storyPleoStones != null)
                for (var i = 0; i < pleoStones.Length && i < data.storyPleoStones.Count; i++) pleoStones[i] = data.storyPleoStones[i] ?? "";
            // A save that had finished chapter 2 goes on with chapter 3.
            if (Phase == PhaseTwinsDone) Phase = PhasePleoSleep;
            // A save that had finished the stone of Anselme goes on with chapter 2.
            if (Phase == PhaseDone && contacts.Contains("anselme")) Phase = PhaseTwinsSleep;
            // A save that is already past the visit of Léa keeps the binder.
            if (Phase == PhaseDone || Phase >= PhaseStoneReturn && Phase != PhaseLeaVisit) BinderUnlocked = true;
            if (data.storyTools != null) foreach (var id in data.storyTools) tools.Add(id);
            GrantToolsForPhase();
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
