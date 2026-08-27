using UnityEngine;
using UnityEngine.EventSystems;

public class DishLeftOver : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private DishwashingMiniGame miniGame;

    private Vector2 dragOffset;
    private Vector2 spawnPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetManager(DishwashingMiniGame manager)
    {
        miniGame = manager;
    }

    public void SetSpawnPosition(Vector2 position)
    {
        spawnPosition = position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransform parentRect =
            rectTransform.parent as RectTransform;

        Camera cam =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        Vector2 localPosition;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                cam,
                out localPosition
            );

        dragOffset =
            rectTransform.anchoredPosition -
            localPosition;

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform parentRect =
            rectTransform.parent as RectTransform;

        Camera cam =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
            ? null
            : eventData.pressEventCamera;

        Vector2 localPosition;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                cam,
                out localPosition
            );

        rectTransform.anchoredPosition =
            localPosition + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject trashBin =
            GameObject.FindWithTag("TrashBin");

        if (trashBin != null)
        {
            RectTransform trashRect =
                trashBin.GetComponent<RectTransform>();

            if (trashRect != null &&
                RectTransformUtility
                .RectangleContainsScreenPoint(
                    trashRect,
                    rectTransform.position,
                    eventData.pressEventCamera
                ))
            {
                ThrowIntoTrash();
                return;
            }
        }

        rectTransform.anchoredPosition =
            spawnPosition;

        Debug.Log("Leftover returned.");
    }

    public void ThrowIntoTrash()
    {
        Debug.Log("Leftover thrown into trash!");

        if (miniGame != null)
        {
            miniGame.LeftoverRemoved();
        }

        Destroy(gameObject);
    }
}