using System;
using UnityEngine;
using UnityEngine.UI;

// Generic "are you sure?" window: dims the screen, shows a message and two buttons. Built at runtime on the Canvas.
public class ConfirmDialog : MonoBehaviour
{
    private static ConfirmDialog instance;

    private GameObject root;
    private Text titleText;
    private Text messageText;
    private Text confirmLabel;
    private Action onConfirm;
    private Action onSecond;
    private Button confirmButton;
    private Button secondButton;
    private Text secondLabel;
    private Button cancelButton;

    public static void Show(string title, string message, string confirmLabel, Action onConfirm)
    {
        if (!Ensure()) return;
        instance.Open(title, message, confirmLabel, onConfirm, null, null);
    }

    // Two ways to go on, and a cancel button: the first label runs `onConfirm`, the second one `onSecond`.
    public static void ShowChoice(string title, string message, string confirmLabel, Action onConfirm, string secondLabel, Action onSecond)
    {
        if (!Ensure()) return;
        instance.Open(title, message, confirmLabel, onConfirm, secondLabel, onSecond);
    }

    private static bool Ensure()
    {
        if (instance == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return false;
            instance = canvas.GetComponent<ConfirmDialog>() ?? canvas.AddComponent<ConfirmDialog>();
        }
        return true;
    }

    private void Awake()
    {
        instance = this;
        Build();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (!root.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Cancel();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm();
    }

    private void Open(string title, string message, string confirmText, Action confirmAction, string secondText, Action secondAction)
    {
        titleText.text = title;
        messageText.text = message;
        confirmLabel.text = confirmText;
        onConfirm = confirmAction;
        onSecond = secondAction;

        var two = secondAction != null;
        secondButton.gameObject.SetActive(two);
        if (two)
        {
            secondLabel.text = secondText;
            RuntimeUIKit.SetTopLeft(confirmButton.GetComponent<RectTransform>(), new Vector2(12f, -154f), new Vector2(132f, 38f));
            RuntimeUIKit.SetTopLeft(secondButton.GetComponent<RectTransform>(), new Vector2(154f, -154f), new Vector2(132f, 38f));
            RuntimeUIKit.SetTopLeft(cancelButton.GetComponent<RectTransform>(), new Vector2(296f, -154f), new Vector2(132f, 38f));
        }
        else
        {
            RuntimeUIKit.SetTopLeft(confirmButton.GetComponent<RectTransform>(), new Vector2(50f, -154f), new Vector2(160f, 38f));
            RuntimeUIKit.SetTopLeft(cancelButton.GetComponent<RectTransform>(), new Vector2(230f, -154f), new Vector2(160f, 38f));
        }
        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    private void Confirm()
    {
        var action = onConfirm;
        Close();
        action?.Invoke();
    }

    private void ConfirmSecond()
    {
        var action = onSecond;
        Close();
        action?.Invoke();
    }

    private void Cancel()
    {
        Close();
    }

    private void Close()
    {
        onConfirm = null;
        onSecond = null;
        root.SetActive(false);
    }

    private void Build()
    {
        // Full-screen dim layer: it also swallows clicks meant for the windows behind.
        root = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(440f, 210f);
        boxRect.anchoredPosition = Vector2.zero;
        StardewKit.FramePanel(box.GetComponent<Image>());

        titleText = RuntimeUIKit.MakeText("Title", box.transform, "", 19, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.60f),
            new Vector2(20f, -14f), new Vector2(400f, 28f));
        messageText = RuntimeUIKit.MakeText("Message", box.transform, "", 15, TextAnchor.UpperCenter, new Color(0.9f, 0.9f, 0.9f),
            new Vector2(24f, -52f), new Vector2(392f, 90f));

        var confirm = RuntimeUIKit.MakeButton("Confirm", box.transform, "Confirmer", 15, new Color(0.25f, 0.50f, 0.30f, 1f),
            new Vector2(50f, -154f), new Vector2(160f, 38f));
        confirm.onClick.AddListener(Confirm);
        confirmLabel = confirm.GetComponentInChildren<Text>();
        confirmButton = confirm;

        secondButton = RuntimeUIKit.MakeButton("Second", box.transform, "", 15, new Color(0.45f, 0.35f, 0.12f, 1f),
            new Vector2(154f, -154f), new Vector2(132f, 38f));
        secondButton.onClick.AddListener(ConfirmSecond);
        secondLabel = secondButton.GetComponentInChildren<Text>();
        secondButton.gameObject.SetActive(false);

        var cancel = RuntimeUIKit.MakeButton("Cancel", box.transform, "Annuler", 15, new Color(0.40f, 0.22f, 0.20f, 1f),
            new Vector2(230f, -154f), new Vector2(160f, 38f));
        cancel.onClick.AddListener(Cancel);
        cancelButton = cancel;
        StardewKit.Skin(box.transform);
    }
}
