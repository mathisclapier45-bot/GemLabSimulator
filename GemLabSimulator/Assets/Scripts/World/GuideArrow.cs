using UnityEngine;
using UnityEngine.SceneManagement;

// The small orange arrow that bobs above the thing to use now, during the first steps of the story (the bed, the mailbox, the door of the office, the loupe,
// the balances, Professor Thouvenot...). It follows the phase of StoryState and the scene the player is in. Lives for the whole game.
public class GuideArrow : MonoBehaviour
{
    private Transform arrow;
    private GameObject target;
    private float nextSearch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("GuideArrow");
        DontDestroyOnLoad(go);
        go.AddComponent<GuideArrow>();
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
        target = null;
        nextSearch = 0f;
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.4f;
            target = Resolve();
        }

        if (target == null)
        {
            if (arrow != null && arrow.gameObject.activeSelf) arrow.gameObject.SetActive(false);
            return;
        }

        if (arrow == null)
        {
            var go = new GameObject("Arrow", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = ArrowSprite();
            renderer.sortingOrder = 45;
            arrow = go.transform;
        }

        arrow.gameObject.SetActive(true);
        var top = TopOf(target);
        var bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) * 0.28f;
        arrow.position = new Vector3(top.x, top.y + 0.2f + bob, 0f);
    }

    // The thing to use now, in the scene the player is in.
    private static GameObject Resolve()
    {
        var state = StoryState.Instance;
        if (state == null || state.Legacy) return null;

        var scene = SceneManager.GetActiveScene().name;
        var phase = state.Phase;

        if (scene == "MaisonJoueur")
        {
            if (state.Stage == StoryState.GoHome && state.UnreadCount > 0) return GameObject.Find("Boite aux lettres perso");
            if (state.Stage >= StoryState.Settled && (phase == StoryState.PhaseSleep || phase == StoryState.PhaseStoneSleep || phase == StoryState.PhaseTwinsSleep || phase == StoryState.PhasePleoSleep || phase == StoryState.PhaseSpectroSleep))
            {
                var bed = Object.FindFirstObjectByType<BedInteractable>();
                return bed != null ? bed.gameObject : null;
            }
            return null;
        }

        if (scene == "Marche")
        {
            if (phase != StoryState.PhaseMarketVisit || state.FabienneStep != 0) return null;
            var table = Object.FindFirstObjectByType<MarketVendorSpot>();
            return table != null ? table.gameObject : null;
        }

        if (scene == "GemStore")
        {
            if (phase != StoryState.PhaseLeaVisit) return null;
            var clerk = Object.FindFirstObjectByType<ShopkeeperNPC>();
            return clerk != null ? clerk.gameObject : null;
        }

        var toLab = phase == StoryState.PhaseLab || state.InTutorial || state.StoneQuest || state.TwinsQuest || state.PleoQuest || state.SpectroQuest;
        if (scene == "Town")
        {
            if (phase == StoryState.PhaseLeaVisit)
            {
                // First Anselme, who waits in the street of the shop, then the door of the shop.
                if (!state.HasContact("anselme"))
                {
                    var anselme = GameObject.Find("Anselme");
                    if (anselme != null) return anselme;
                }
                return DoorTo("GemStore");
            }
            if (phase == StoryState.PhaseMarketVisit)
            {
                if (state.FabienneStep == 0) return DoorTo("Marche");
                if (state.FabienneStep == 3) return GameObject.Find("La Crypte");
                return state.FabienneStep == 2 ? GameObject.Find("Fontaine") : null;
            }
            if (phase == StoryState.PhaseRest || phase == StoryState.PhaseStoneSleep || phase == StoryState.PhaseStoneReturn || phase == StoryState.PhaseTwinsSleep || phase == StoryState.PhasePleoSleep || phase == StoryState.PhaseSpectroSleep) return DoorTo("MaisonJoueur");
            return toLab ? DoorTo("Home") : null;
        }

        if (scene == "Home")
        {
            // Chapter 4: the spectroscope, then the Chelsea filter; the director for the report, Thouvenot for the rest.
            if (state.SpectroQuest)
            {
                if (state.SpectroStep == 0) return GameObject.Find(state.SpectroCase == 0 ? "Spectroscope" : "FiltreChelsea");
                if (state.SpectroStep == 2)
                {
                    var boss = Object.FindFirstObjectByType<DirectorNPC>();
                    return boss != null ? boss.gameObject : null;
                }
                var teacher = Object.FindFirstObjectByType<ThouvenotNPC>();
                return teacher != null ? teacher.gameObject : null;
            }

            // Chapter 3: the dichroscope, the director for the report, Thouvenot for the rest.
            if (state.PleoQuest)
            {
                if (state.PleoStep == 0) return GameObject.Find("Dichroscope");
                if (state.PleoStep == 2)
                {
                    var director = Object.FindFirstObjectByType<DirectorNPC>();
                    return director != null ? director.gameObject : null;
                }
                var professor = Object.FindFirstObjectByType<ThouvenotNPC>();
                return professor != null ? professor.gameObject : null;
            }

            switch (phase)
            {
                case StoryState.PhaseTutorialLoupe:
                    return GameObject.Find(state.TutorialStoneTaken ? "Loupe" : "Mailbox");
                case StoryState.PhaseTutorialReturn:
                case StoryState.PhaseTutorialReport:
                case StoryState.PhaseTutorialDensityReport:
                case StoryState.PhaseTutorialIdentify:
                case StoryState.PhaseStoneLab:
                case StoryState.PhaseStoneValidate:
                case StoryState.PhaseTwinsLab:
                case StoryState.PhaseTwinsLesson:
                case StoryState.PhaseTwinsReport:
                case StoryState.PhaseTwinsValidate:
                {
                    var npc = Object.FindFirstObjectByType<ThouvenotNPC>();
                    return npc != null ? npc.gameObject : null;
                }
                case StoryState.PhaseTutorialWeigh: return GameObject.Find("BalanceCarat");
                case StoryState.PhaseTutorialDensity: return GameObject.Find("BalanceHydrostatique");
                case StoryState.PhaseRefractometer:
                case StoryState.PhaseTwinsRefracto:
                    return GameObject.Find("Refractometre");
                case StoryState.PhaseTwinsPolariscope: return GameObject.Find("Polariscope");
                case StoryState.PhaseStoneIdentify:
                case StoryState.PhaseTwinsIdentify:
                {
                    var director = Object.FindFirstObjectByType<DirectorNPC>();
                    return director != null ? director.gameObject : null;
                }
            }
        }
        return null;
    }

    // The building to go to now, for the minimap and the big map: "scene:<name of the scene behind the door>" or "obj:<name of an object of the town>",
    // or null. Independent of the scene the player is in (the maps are also opened from inside a building); empty when the player is already there.
    public static string MapGoal()
    {
        var state = StoryState.Instance;
        if (state == null || state.Legacy) return null;

        var phase = state.Phase;
        string goal = null;
        if (phase == StoryState.PhaseLeaVisit) goal = "scene:GemStore";
        else if (phase == StoryState.PhaseMarketVisit)
        {
            if (state.FabienneStep == 0) goal = "scene:Marche";
            else if (state.FabienneStep == 2) goal = "obj:Fontaine";
            else if (state.FabienneStep == 3) goal = "obj:La Crypte";
        }
        else if (phase == StoryState.PhaseRest || phase == StoryState.PhaseStoneSleep || phase == StoryState.PhaseStoneReturn || phase == StoryState.PhaseTwinsSleep || phase == StoryState.PhasePleoSleep || phase == StoryState.PhaseSpectroSleep)
            goal = "scene:MaisonJoueur";
        else if (phase == StoryState.PhaseLab || state.InTutorial || state.StoneQuest || state.TwinsQuest || state.PleoQuest || state.SpectroQuest)
            goal = "scene:Home";

        if (goal != null && goal.StartsWith("scene:") && SceneManager.GetActiveScene().name == goal.Substring(6)) return null;
        return goal;
    }

    private static GameObject DoorTo(string scene)
    {
        foreach (var door in Object.FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
            if (door.TargetScene == scene) return door.gameObject;
        return null;
    }

    // The point above the object: the top of its sprite, or of its collider (a door).
    private static Vector3 TopOf(GameObject go)
    {
        if (go.GetComponent<DoorTrigger>() == null)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.enabled) return new Vector3(renderer.bounds.center.x, renderer.bounds.max.y, 0f);
        }

        var collider = go.GetComponentInChildren<Collider2D>();
        if (collider != null) return new Vector3(collider.bounds.center.x, collider.bounds.max.y, 0f);
        return go.transform.position;
    }

    private static Sprite arrowSprite;

    // A downward arrow, orange with a dark outline, drawn pixel by pixel.
    public static Sprite ArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;

        const int w = 15;
        const int h = 20;
        var shape = new bool[w, h];
        for (var y = 0; y < h; y++)
        {
            // y = 0 is the tip (bottom), y = h - 1 the end of the shaft.
            for (var x = 0; x < w; x++)
            {
                var dx = Mathf.Abs(x - (w - 1) / 2f);
                var inHead = y < 9 && dx <= (y + 1) * 0.75f;
                var inShaft = y >= 9 && dx <= 2.2f;
                shape[x, y] = inHead || inShaft;
            }
        }

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var outline = new Color(0.45f, 0.20f, 0.04f, 1f);
        var fill = new Color(1f, 0.58f, 0.10f, 1f);
        var light = new Color(1f, 0.80f, 0.40f, 1f);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var color = new Color(0f, 0f, 0f, 0f);
                if (shape[x, y])
                {
                    var edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 || !shape[x - 1, y] || !shape[x + 1, y] || !shape[x, y - 1] || (y + 1 < h && !shape[x, y + 1]);
                    color = edge ? outline : (x < w / 2 - 1 ? light : fill);
                }
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        arrowSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 20f);
        return arrowSprite;
    }
}
