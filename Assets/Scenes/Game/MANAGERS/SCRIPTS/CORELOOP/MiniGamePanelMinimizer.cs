using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class MiniGamePanelMinimizer : MonoBehaviour
{
    [Tooltip("Assign a sprite to use it instead of the text minimize button.")]
    [SerializeField] private Sprite minimizeButtonSprite;

    private Action beforeMinimize;

    public static void Attach(GameObject panel, Action onMinimize = null)
    {
        if (panel == null)
            return;

        MiniGamePanelMinimizer minimizer = panel.GetComponent<MiniGamePanelMinimizer>();
        if (minimizer == null)
            minimizer = panel.AddComponent<MiniGamePanelMinimizer>();

        minimizer.beforeMinimize = onMinimize;
        minimizer.EnsureButton();
    }

    private void EnsureButton()
    {
        Transform existingButton = transform.Find("MiniGameMinimizeButton");
        if (existingButton != null)
        {
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
        buttonRect.SetParent(transform, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.anchoredPosition = new Vector2(-18f, -18f);
        buttonRect.sizeDelta = new Vector2(112f, 36f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = minimizeButtonSprite != null
            ? Color.white
            : new Color(0.12f, 0.08f, 0.04f, 0.94f);
        background.sprite = minimizeButtonSprite;
        background.preserveAspect = minimizeButtonSprite != null;

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

        Transform label = buttonTransform.Find("Label");
        if (label != null)
            label.gameObject.SetActive(minimizeButtonSprite == null);
    }

    private void Minimize()
    {
        beforeMinimize?.Invoke();
        gameObject.SetActive(false);
    }
}
