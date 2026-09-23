using UnityEngine;
using UnityEngine.UI;

public static class SpeechBubbleCanvas
{
    public static GameObject Show(Transform anchor, string message, float duration)
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

        GameObject bubble = new GameObject("NPC Speech Bubble");
        RectTransform bubbleRect = bubble.AddComponent<RectTransform>();
        bubbleRect.SetParent(canvasRect, false);
        bubbleRect.sizeDelta = new Vector2(360f, 76f);
        bubbleRect.localScale = Vector3.one;
        bubbleRect.SetAsLastSibling();

        Image background = bubble.AddComponent<Image>();
        background.color = new Color(0.04f, 0.04f, 0.04f, 0.9f);
        background.raycastTarget = false;

        Text text = new GameObject("Text").AddComponent<Text>();
        text.transform.SetParent(bubbleRect, false);
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 8f);
        textRect.offsetMax = new Vector2(-14f, -8f);

        ScreenSpaceBubbleFollower follower = bubble.AddComponent<ScreenSpaceBubbleFollower>();
        follower.Initialize(anchor, canvas, worldCamera, duration);
        return bubble;
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
