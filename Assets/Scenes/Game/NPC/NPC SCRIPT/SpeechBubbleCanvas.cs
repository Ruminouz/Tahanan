using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SpeechBubbleCanvas
{
    private const string BubbleContainerName = "Speech Bubble Container";
    private const float HorizontalPadding = 28f;
    private const float VerticalPadding = 16f;
    private const float MinimumBubbleWidth = 120f;
    private const float MinimumBubbleHeight = 54f;
    private const float MaximumBubbleWidth = 360f;
    private const float CanvasEdgePadding = 16f;
    private const float MinimumFontSize = 10f;

    private static Sprite backgroundSprite;
    private static TMP_FontAsset fontAsset;

    public static void SetAppearance(TMP_FontAsset font, Sprite sprite)
    {
        fontAsset = font;
        backgroundSprite = sprite;
    }

    public static GameObject Show(Transform anchor, string message, float duration)
    {
        return Show(anchor, message, duration, new Vector2(MinimumBubbleWidth, MinimumBubbleHeight), 22);
    }

    public static GameObject Show(
        Transform anchor,
        string message,
        float duration,
        Vector2 bubbleSize,
        int fontSize)
    {
        if (anchor == null || string.IsNullOrWhiteSpace(message))
            return null;

        Canvas canvas = FindCanvasMain();
        Camera worldCamera = Camera.main;
        if (canvas == null || worldCamera == null)
        {
            Debug.LogWarning(
                "Speech bubbles require an active CanvasMain and a camera tagged MainCamera.");
            return null;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return null;

        RectTransform bubbleContainer = GetBubbleContainer(canvasRect);
        GameObject bubble = new GameObject("NPC Speech Bubble");
        RectTransform bubbleRect = bubble.AddComponent<RectTransform>();
        bubbleRect.SetParent(bubbleContainer, false);
        bubbleRect.localScale = Vector3.one;
        bubbleRect.SetAsLastSibling();

        Image background = bubble.AddComponent<Image>();
        background.sprite = backgroundSprite;
        background.color = backgroundSprite != null
            ? Color.white
            : new Color(0.04f, 0.04f, 0.04f, 0.9f);
        background.raycastTarget = false;

        TMP_Text text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(bubbleRect, false);
        text.text = message;
        text.font = fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        RectTransform textRect = text.rectTransform;
        Vector2 bubbleDimensions = CalculateBubbleSize(
            text,
            message,
            bubbleSize,
            canvasRect.rect.size);
        bubbleRect.sizeDelta = bubbleDimensions;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(
            bubbleDimensions.x - HorizontalPadding,
            bubbleDimensions.y - VerticalPadding);
        textRect.anchoredPosition = Vector2.zero;

        ScreenSpaceBubbleFollower follower = bubble.AddComponent<ScreenSpaceBubbleFollower>();
        follower.Initialize(anchor, canvas, worldCamera, duration);
        return bubble;
    }

    private static RectTransform GetBubbleContainer(RectTransform canvasRect)
    {
        Transform existingContainer = canvasRect.Find(BubbleContainerName);
        RectTransform containerRect;
        if (existingContainer != null)
        {
            containerRect = existingContainer as RectTransform;
        }
        else
        {
            GameObject container = new GameObject(
                BubbleContainerName,
                typeof(RectTransform));
            containerRect = container.GetComponent<RectTransform>();
            containerRect.SetParent(canvasRect, false);
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            containerRect.pivot = new Vector2(0.5f, 0.5f);
        }

        containerRect.SetAsFirstSibling();
        return containerRect;
    }

    private static Vector2 CalculateBubbleSize(
        TMP_Text text,
        string message,
        Vector2 minimumSize,
        Vector2 canvasSize)
    {
        float canvasMaxWidth = Mathf.Max(
            MinimumBubbleWidth,
            canvasSize.x - CanvasEdgePadding * 2f);
        float maxWidth = Mathf.Min(
            canvasMaxWidth,
            MaximumBubbleWidth);
        float maxHeight = Mathf.Max(
            MinimumBubbleHeight,
            canvasSize.y - CanvasEdgePadding * 2f);
        float minWidth = Mathf.Min(
            maxWidth,
            Mathf.Max(MinimumBubbleWidth, minimumSize.x));
        float minHeight = Mathf.Min(
            maxHeight,
            Mathf.Max(MinimumBubbleHeight, minimumSize.y));
        float contentMaxWidth = maxWidth - HorizontalPadding;
        Vector2 preferredSize = text.GetPreferredValues(message, contentMaxWidth, 0f);
        float width = Mathf.Clamp(
            preferredSize.x + HorizontalPadding,
            minWidth,
            maxWidth);
        float contentWidth = width - HorizontalPadding;
        Vector2 size = GetBubbleSize(text, message, contentWidth, width, minHeight);

        while (size.y > maxHeight && text.fontSize > MinimumFontSize)
        {
            text.fontSize = Mathf.Max(MinimumFontSize, text.fontSize - 1f);
            preferredSize = text.GetPreferredValues(message, contentMaxWidth, 0f);
            width = Mathf.Clamp(
                preferredSize.x + HorizontalPadding,
                minWidth,
                maxWidth);
            contentWidth = width - HorizontalPadding;
            size = GetBubbleSize(text, message, contentWidth, width, minHeight);
        }

        text.overflowMode = size.y > maxHeight
            ? TextOverflowModes.Overflow
            : TextOverflowModes.Truncate;

        size.y = Mathf.Min(size.y, maxHeight);
        return size;
    }

    private static Vector2 GetBubbleSize(
        TMP_Text text,
        string message,
        float contentWidth,
        float bubbleWidth,
        float minHeight)
    {
        Vector2 preferredSize = text.GetPreferredValues(message, contentWidth, 0f);
        float height = Mathf.Max(
            minHeight,
            preferredSize.y + VerticalPadding);
        return new Vector2(bubbleWidth, height);
    }

    private static Canvas FindCanvasMain()
    {
        GameObject canvasObject = GameObject.Find("CanvasMain");
        if (canvasObject != null)
        {
            Canvas namedCanvas = canvasObject.GetComponent<Canvas>();
            if (namedCanvas != null && namedCanvas.isActiveAndEnabled)
                return namedCanvas;
        }

        GameObject sceneRoot = GameObject.Find("CoreLoopScene");
        if (sceneRoot != null)
        {
            Canvas sceneCanvas = sceneRoot.GetComponentInChildren<Canvas>(true);
            if (sceneCanvas != null && sceneCanvas.isActiveAndEnabled)
                return sceneCanvas;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        foreach (Canvas candidate in canvases)
        {
            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay)
                return candidate;
        }

        return null;
    }
}
