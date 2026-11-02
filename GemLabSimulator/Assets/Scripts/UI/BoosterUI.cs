using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Opening of a booster: the sealed pack in the middle of the screen, click it to tear it open, then the five cards lie face down:
// click each one to turn it over (the rare ones light up), or reveal them all. The cards are already in the binder.
public class BoosterUI : MonoBehaviour
{
    public static BoosterUI Instance { get; private set; }

    private const float CardWidth = 150f;
    private const float CardHeight = 210f;
    private const float CardGap = 16f;
    private const float FlipSeconds = 0.4f;
    private const float OpenSeconds = 0.6f;

    private static int escapeFrame = -1;
    public static bool IsShowing => Instance != null && Instance.root != null && Instance.root.activeSelf;
    public static bool ConsumesEscape => IsShowing || escapeFrame == Time.frameCount;

    private enum Stage { Pack, Opening, Cards }

    private class Slot
    {
        public CardDraw Draw;
        public RectTransform Rect;
        public Image Back;
        public CardView Face;
        public Text Label;
        public Image Glow;
        public bool Flipping;
        public bool Revealed;
        public bool Swapped;
        public float Progress;
        public float Delay;
    }

    private GameObject root;
    private RectTransform stageRect;
    private Text promptText;
    private RectTransform packRect;
    private Image packImage;
    private Button revealAllButton;
    private Button doneButton;
    private Button binderButton;
    private Stage stage;
    private float openTimer;
    private bool lockedInput;
    private readonly List<Slot> slots = new List<Slot>();
    private List<CardDraw> draws;

    public static void Show(List<CardDraw> draws)
    {
        if (Instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            canvas.AddComponent<BoosterUI>();
        }
        Instance?.Begin(draws);
    }

    private void Awake()
    {
        Instance = this;
        BuildRoot();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (lockedInput) PlayerController.InputLocked = false;
    }

    private void Begin(List<CardDraw> newDraws)
    {
        draws = newDraws;
        foreach (var slot in slots) if (slot.Rect != null) Destroy(slot.Rect.gameObject);
        slots.Clear();
        if (packRect != null) Destroy(packRect.gameObject);

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        if (!PlayerController.InputLocked)
        {
            PlayerController.InputLocked = true;
            lockedInput = true;
        }

        // The sealed pack.
        var sprite = Resources.Load<Sprite>("Cards/booster_pack");
        var go = new GameObject("Pack", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(stageRect, false);
        packRect = go.GetComponent<RectTransform>();
        packRect.anchorMin = packRect.anchorMax = packRect.pivot = new Vector2(0.5f, 0.5f);
        packRect.sizeDelta = new Vector2(240f, 360f);
        packImage = go.GetComponent<Image>();
        packImage.sprite = sprite;
        packImage.preserveAspect = true;
        packImage.color = Color.white;
        go.GetComponent<Button>().transition = Selectable.Transition.None;
        go.GetComponent<Button>().onClick.AddListener(OpenPack);

        stage = Stage.Pack;
        promptText.text = "Cliquez sur le booster pour l'ouvrir";
        revealAllButton.gameObject.SetActive(false);
        doneButton.gameObject.SetActive(false);
        binderButton.gameObject.SetActive(false);
    }

    private void OpenPack()
    {
        if (stage != Stage.Pack) return;
        stage = Stage.Opening;
        openTimer = 0f;
        promptText.text = "";
    }

    private void Close()
    {
        root.SetActive(false);
        if (lockedInput)
        {
            PlayerController.InputLocked = false;
            lockedInput = false;
        }
    }

    private void OpenBinder()
    {
        Close();
        BinderUI.Instance?.Open();
    }

    // ---- Animation ---------------------------------------------------------------------------------------------

    private void Update()
    {
        if (!IsShowing) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            escapeFrame = Time.frameCount;
            if (stage == Stage.Cards && AllRevealed()) Close();
            else if (stage == Stage.Cards) RevealAll();
            else if (stage == Stage.Pack) OpenPack();
        }

        switch (stage)
        {
            case Stage.Pack:
                packRect.anchoredPosition = new Vector2(0f, Mathf.Sin(Time.unscaledTime * 2f) * 8f);
                packRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.3f) * 2f);
                break;

            case Stage.Opening:
                openTimer += Time.unscaledDeltaTime;
                var p = Mathf.Clamp01(openTimer / OpenSeconds);
                packRect.localScale = Vector3.one * (1f + 0.45f * p);
                packRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * 40f) * 6f * (1f - p));
                packImage.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((p - 0.5f) * 2f));
                if (p >= 1f) ShowCards();
                break;

            case Stage.Cards:
                UpdateSlots();
                break;
        }
    }

    private void ShowCards()
    {
        if (packRect != null) Destroy(packRect.gameObject);
        stage = Stage.Cards;
        promptText.text = "Cliquez sur une carte pour la retourner";

        var backSprite = Resources.Load<Sprite>("Cards/card_back");
        var total = draws.Count * CardWidth + (draws.Count - 1) * CardGap;
        for (var i = 0; i < draws.Count; i++)
        {
            var slot = new Slot { Draw = draws[i] };

            var go = new GameObject("Slot" + i, typeof(RectTransform));
            go.transform.SetParent(stageRect, false);
            slot.Rect = go.GetComponent<RectTransform>();
            slot.Rect.anchorMin = slot.Rect.anchorMax = slot.Rect.pivot = new Vector2(0.5f, 0.5f);
            slot.Rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            slot.Rect.anchoredPosition = new Vector2(-total / 2f + CardWidth / 2f + i * (CardWidth + CardGap), 10f);

            // Glow behind the card, for the rare ones.
            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(go.transform, false);
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.anchorMin = glowRect.anchorMax = glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(CardWidth * 1.9f, CardHeight * 1.5f);
            slot.Glow = glowGo.GetComponent<Image>();
            slot.Glow.sprite = RuntimeUIKit.Glow;
            slot.Glow.raycastTarget = false;
            slot.Glow.color = new Color(1f, 1f, 1f, 0f);

            // The face (hidden until turned over) and the back.
            slot.Face = CardView.Create(go.transform, slot.Draw.Card, new Vector2(CardWidth, CardHeight), false, 0);
            var faceRect = slot.Face.GetComponent<RectTransform>();
            faceRect.anchorMin = faceRect.anchorMax = faceRect.pivot = new Vector2(0.5f, 0.5f);
            faceRect.anchoredPosition = Vector2.zero;
            slot.Face.GetComponent<Image>().raycastTarget = false;
            slot.Face.gameObject.SetActive(false);

            var backGo = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(go.transform, false);
            var backRect = backGo.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;
            slot.Back = backGo.GetComponent<Image>();
            slot.Back.sprite = backSprite;
            var captured = slot;
            backGo.GetComponent<Button>().transition = Selectable.Transition.None;
            backGo.GetComponent<Button>().onClick.AddListener(() => Flip(captured, 0f));

            // "New!" or the number of copies, under the card.
            var label = RuntimeUIKit.MakeText("Label", go.transform, "", 16, TextAnchor.MiddleCenter, Color.white, Vector2.zero, new Vector2(CardWidth + 20f, 26f));
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -8f);
            label.fontStyle = FontStyle.Bold;
            slot.Label = label;

            slots.Add(slot);
        }

        revealAllButton.gameObject.SetActive(true);
        doneButton.gameObject.SetActive(false);
        binderButton.gameObject.SetActive(false);
    }

    private void Flip(Slot slot, float delay)
    {
        if (slot.Flipping || slot.Revealed) return;
        slot.Flipping = true;
        slot.Progress = 0f;
        slot.Delay = delay;
    }

    private void RevealAll()
    {
        var delay = 0f;
        foreach (var slot in slots)
        {
            if (slot.Revealed || slot.Flipping) continue;
            Flip(slot, delay);
            delay += 0.25f;
        }
    }

    private bool AllRevealed()
    {
        foreach (var slot in slots) if (!slot.Revealed) return false;
        return slots.Count > 0;
    }

    private void UpdateSlots()
    {
        foreach (var slot in slots)
        {
            if (slot.Flipping)
            {
                if (slot.Delay > 0f)
                {
                    slot.Delay -= Time.unscaledDeltaTime;
                }
                else
                {
                    slot.Progress += Time.unscaledDeltaTime / FlipSeconds;
                    var p = Mathf.Clamp01(slot.Progress);
                    slot.Rect.localScale = new Vector3(Mathf.Abs(p < 0.5f ? 1f - 2f * p : 2f * p - 1f), 1f + 0.08f * Mathf.Sin(p * Mathf.PI), 1f);

                    if (p >= 0.5f && !slot.Swapped)
                    {
                        slot.Swapped = true;
                        slot.Back.gameObject.SetActive(false);
                        slot.Face.gameObject.SetActive(true);
                    }
                    if (p >= 1f)
                    {
                        slot.Flipping = false;
                        slot.Revealed = true;
                        slot.Rect.localScale = Vector3.one;
                        slot.Label.text = slot.Draw.IsNew ? "NOUVEAU !" : $"x{slot.Draw.Count}";
                        slot.Label.color = slot.Draw.IsNew ? new Color(0.45f, 1f, 0.55f) : new Color(1f, 0.85f, 0.4f);
                    }
                }
            }

            // Rare cards light up once they are turned over.
            if (slot.Revealed && slot.Draw.Card.Rarity != CardRarity.Commune)
            {
                var color = CardCatalog.RarityColor(slot.Draw.Card.Rarity);
                if (slot.Draw.Card.Rarity == CardRarity.Secret || slot.Draw.Card.Rarity == CardRarity.UltraRare)
                    color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.3f, 1f), 0.6f, 1f);
                color.a = 0.45f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
                slot.Glow.color = color;
            }
        }

        if (slots.Count > 0 && AllRevealed() && !doneButton.gameObject.activeSelf)
        {
            promptText.text = "Les cartes ont été ajoutées à votre classeur (B).";
            revealAllButton.gameObject.SetActive(false);
            doneButton.gameObject.SetActive(true);
            binderButton.gameObject.SetActive(true);
        }
    }

    // ---- Construction ------------------------------------------------------------------------------------------

    private void BuildRoot()
    {
        root = new GameObject("BoosterOverlay", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.9f);

        var stageGo = new GameObject("Stage", typeof(RectTransform));
        stageGo.transform.SetParent(root.transform, false);
        stageRect = stageGo.GetComponent<RectTransform>();
        stageRect.anchorMin = stageRect.anchorMax = stageRect.pivot = new Vector2(0.5f, 0.5f);
        stageRect.sizeDelta = new Vector2(1000f, 520f);
        stageRect.anchoredPosition = new Vector2(0f, 10f);

        promptText = MakeCenteredText("Prompt", "", 20, new Vector2(0f, -218f), new Vector2(900f, 30f), new Color(0.95f, 0.88f, 0.7f));

        revealAllButton = MakeButton("RevealAll", "Tout révéler", new Vector2(0f, -272f), new Color(0.30f, 0.38f, 0.58f, 1f));
        revealAllButton.onClick.AddListener(RevealAll);
        doneButton = MakeButton("Done", "Terminer", new Vector2(-130f, -272f), new Color(0.25f, 0.50f, 0.30f, 1f));
        doneButton.onClick.AddListener(Close);
        binderButton = MakeButton("Binder", "Voir le classeur", new Vector2(130f, -272f), new Color(0.40f, 0.28f, 0.18f, 1f));
        binderButton.onClick.AddListener(OpenBinder);
    }

    private Text MakeCenteredText(string name, string content, int size, Vector2 centerOffset, Vector2 dimensions, Color color)
    {
        var text = RuntimeUIKit.MakeText(name, root.transform, content, size, TextAnchor.MiddleCenter, color, Vector2.zero, dimensions);
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = centerOffset;
        return text;
    }

    private Button MakeButton(string name, string label, Vector2 centerOffset, Color color)
    {
        var button = RuntimeUIKit.MakeButton(name, root.transform, label, 16, color, Vector2.zero, new Vector2(220f, 40f));
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = centerOffset;
        return button;
    }
}
