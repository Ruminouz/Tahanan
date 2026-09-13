using UnityEngine;
using UnityEngine.UI;

public static class SpeechBubbleCanvas
{
    public static Canvas GetOrCreate(Transform anchor)
    {
        Canvas canvas = anchor.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            return canvas;

        GameObject canvasObject = new GameObject("Speech Bubble Canvas");
        canvasObject.transform.SetParent(anchor, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        canvasObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }
}