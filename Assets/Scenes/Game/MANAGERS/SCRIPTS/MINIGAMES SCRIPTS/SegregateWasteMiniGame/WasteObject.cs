using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum WasteType
{
    Leaves,
    Bottle,
    Wrapper,
    Worm
}

public class WasteObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private WasteType wasteType;
    [SerializeField] private CanvasGroup canvasGroup;

    private SegregateWasteMiniGame miniGame;
    private Vector3 originalPosition;
    private bool isCollected;

    public bool IsCollected => isCollected;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        originalPosition = transform.position;
    }

    public void SetMiniGame(SegregateWasteMiniGame game)
    {
        miniGame = game;
    }

    public WasteType GetWasteType()
    {
        return wasteType;
    }

    public void SetCollected()
    {
        isCollected = true;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isCollected)
            return;

        originalPosition = transform.position;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isCollected)
            return;

        transform.position += (Vector3)eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isCollected)
            return;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (miniGame != null &&
            miniGame.TryCollectToyAtScreenPosition(this, eventData.position, eventData.pressEventCamera))
        {
            return;
        }

        if (TryCollectFromDropPosition(eventData))
        {
            return;
        }

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        transform.position = originalPosition;
    }

    private bool TryCollectFromDropPosition(PointerEventData eventData)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(eventData, raycastResults);

            foreach (RaycastResult result in raycastResults)
            {
                DropZone zone = result.gameObject.GetComponentInParent<DropZone>();
                if (TryCollectInto(zone))
                    return true;
            }
        }

        DropZone pointerEnterZone = eventData.pointerEnter != null
            ? eventData.pointerEnter.GetComponentInParent<DropZone>()
            : null;
        return TryCollectInto(pointerEnterZone);
    }

    public bool TryCollectInto(DropZone dropZone)
    {
        return !isCollected &&
            dropZone != null &&
            dropZone.CanAccept(wasteType) &&
            miniGame != null &&
            miniGame.TryCollectToy(this, dropZone);
    }
}
