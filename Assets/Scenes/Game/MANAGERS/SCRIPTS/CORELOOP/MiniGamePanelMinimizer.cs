using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MiniGamePanelMinimizer : MonoBehaviour
{
    private static bool hasShownDishwashingTutorial;

    [Tooltip("Assign a sprite to use it instead of the text minimize button.")]
    [SerializeField] private Sprite minimizeButtonSprite;
    [SerializeField] private bool showFirstOpenTutorial;

    private Action beforeMinimize;
    private RectTransform minimizeButtonRect;
    private Vector3 minimizeButtonOriginalScale;
    private GameObject tutorialHint;
    private Coroutine tutorialHintCoroutine;

    public static void Attach(GameObject panel, Action onMinimize = null)
    {
        if (panel == null)
            return;

        MiniGamePanelMinimizer minimizer = panel.GetComponent<MiniGamePanelMinimizer>();
        if (minimizer == null)
            minimizer = panel.AddComponent<MiniGamePanelMinimizer>();

        minimizer.beforeMinimize = onMinimize;
        minimizer.EnsureButton();
        if (minimizer.showFirstOpenTutorial)
            minimizer.TryShowFirstOpenTutorial();
    }

    public void EnableFirstOpenTutorial()
    {
        showFirstOpenTutorial = true;
        EnsureButton();
        TryShowFirstOpenTutorial();
    }

    private void OnEnable()
    {
        if (showFirstOpenTutorial)
            TryShowFirstOpenTutorial();
    }

    private void OnDisable()
    {
        if (tutorialHintCoroutine != null)
        {
            StopCoroutine(tutorialHintCoroutine);
            tutorialHintCoroutine = null;
        }

        if (minimizeButtonRect != null)
            minimizeButtonRect.localScale = minimizeButtonOriginalScale;

        if (tutorialHint != null)
        {
            Destroy(tutorialHint);
            tutorialHint = null;
        }
    }

    private void EnsureButton()
    {
        Transform existingButton = transform.Find("MiniGameMinimizeButton");
        if (existingButton != null)
        {
            minimizeButtonRect = existingButton as RectTransform;
            ApplyButtonSprite(existingButton);
            return;
        }

        GameObject buttonObject = new GameObject(
            "MiniGameMinimizeButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = gameObject.layer;

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        minimizeButtonRect = buttonRect;
        buttonRect.SetParent(transform, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.anchoredPosition = new Vector2(-18f, -18f);
        buttonRect.sizeDelta = minimizeButtonSprite != null
            ? new Vector2(70f, 70f)
            : new Vector2(112f, 36f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = minimizeButtonSprite != null
            ? Color.white
            : new Color(0.12f, 0.08f, 0.04f, 0.94f);
        background.sprite = minimizeButtonSprite;
        background.type = minimizeButtonSprite != null
            ? Image.Type.Simple
            : Image.Type.Sliced;
        background.preserveAspect = false;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(Minimize);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        labelObject.layer = gameObject.layer;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "MINIMIZE";
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 14;
        label.color = Color.white;
        label.raycastTarget = false;
        label.gameObject.SetActive(minimizeButtonSprite == null);
    }

    private void ApplyButtonSprite(Transform buttonTransform)
    {
        Image background = buttonTransform.GetComponent<Image>();
        if (background == null)
            return;

        background.sprite = minimizeButtonSprite;
        background.color = minimizeButtonSprite != null
            ? Color.white
            : new Color(0.12f, 0.08f, 0.04f, 0.94f);
        background.preserveAspect = minimizeButtonSprite != null;
        background.type = minimizeButtonSprite != null
            ? Image.Type.Simple
            : Image.Type.Sliced;
        if (buttonTransform is RectTransform buttonRect)
        {
            buttonRect.sizeDelta = minimizeButtonSprite != null
                ? new Vector2(48f, 48f)
                : new Vector2(112f, 36f);
        }

        Transform label = buttonTransform.Find("Label");
        if (label != null)
            label.gameObject.SetActive(minimizeButtonSprite == null);
    }

    private void TryShowFirstOpenTutorial()
    {
        if (hasShownDishwashingTutorial ||
            !gameObject.activeInHierarchy ||
            minimizeButtonRect == null)
        {
            return;
        }

        hasShownDishwashingTutorial = true;
        minimizeButtonOriginalScale = minimizeButtonRect.localScale;

        GameObject hintObject = new GameObject(
            "MiniGameMinimizeHint",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        hintObject.layer = gameObject.layer;
        tutorialHint = hintObject;

        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.SetParent(transform, false);
        hintRect.anchorMin = Vector2.one;
        hintRect.anchorMax = Vector2.one;
        hintRect.pivot = Vector2.one;
        hintRect.anchoredPosition = new Vector2(-18f, -76f);
        hintRect.sizeDelta = new Vector2(260f, 62f);

        Image hintBackground = hintObject.GetComponent<Image>();
        hintBackground.color = new Color(0.08f, 0.06f, 0.04f, 0.96f);
        hintBackground.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Hint Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = gameObject.layer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(hintRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        TMP_Text hintText = textObject.GetComponent<TMP_Text>();
        hintText.text = "Click if you want to minimize this panel.";
        hintText.font = TMP_Settings.defaultFontAsset;
        hintText.fontSize = 18f;
        hintText.color = Color.white;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.raycastTarget = false;

        tutorialHintCoroutine = StartCoroutine(AnimateFirstOpenTutorial());
    }

    private IEnumerator AnimateFirstOpenTutorial()
    {
        const float duration = 3f;
        const float pulseCount = 3f;
        float elapsed = 0f;

        while (elapsed < duration && minimizeButtonRect != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 0.5f - 0.5f * Mathf.Cos(
                elapsed / duration * pulseCount * Mathf.PI * 2f);
            minimizeButtonRect.localScale =
                minimizeButtonOriginalScale * (1f + pulse * 0.35f);
            yield return null;
        }

        if (minimizeButtonRect != null)
            minimizeButtonRect.localScale = minimizeButtonOriginalScale;

        if (tutorialHint != null)
        {
            Destroy(tutorialHint);
            tutorialHint = null;
        }

        tutorialHintCoroutine = null;
    }

    private void Minimize()
    {
        beforeMinimize?.Invoke();
        gameObject.SetActive(false);
    }
}
