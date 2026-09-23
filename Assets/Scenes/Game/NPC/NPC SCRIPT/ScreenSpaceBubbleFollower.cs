using UnityEngine;

public class ScreenSpaceBubbleFollower : MonoBehaviour
{
    private Transform anchor;
    private Canvas canvas;
    private Camera worldCamera;
    private float expiresAt;
    private RectTransform bubbleRect;
    private RectTransform canvasRect;

    public void Initialize(
        Transform target,
        Canvas targetCanvas,
        Camera targetCamera,
        float duration)
    {
        anchor = target;
        canvas = targetCanvas;
        worldCamera = targetCamera;
        expiresAt = Time.time + Mathf.Max(0.1f, duration);
        bubbleRect = transform as RectTransform;
        canvasRect = canvas.transform as RectTransform;
        UpdatePosition();
    }

    private void LateUpdate()
    {
        if (anchor == null || canvas == null || worldCamera == null ||
            canvasRect == null || Time.time >= expiresAt)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        Vector3 screenPosition = worldCamera.WorldToScreenPoint(
            anchor.position + Vector3.up * 0.65f);
        if (screenPosition.z < 0f)
        {
            bubbleRect.gameObject.SetActive(false);
            return;
        }

        bubbleRect.gameObject.SetActive(true);
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition))
            return;

        Vector2 halfBubble = bubbleRect.rect.size * 0.5f;
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        localPosition.x = Mathf.Clamp(
            localPosition.x,
            -halfCanvas.x + halfBubble.x,
            halfCanvas.x - halfBubble.x);
        localPosition.y = Mathf.Clamp(
            localPosition.y,
            -halfCanvas.y + halfBubble.y,
            halfCanvas.y - halfBubble.y);
        bubbleRect.anchoredPosition = localPosition;
    }
}
