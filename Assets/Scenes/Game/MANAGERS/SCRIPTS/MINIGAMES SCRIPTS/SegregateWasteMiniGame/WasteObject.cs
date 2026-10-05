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
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        DropZone dropZone = null;

        if (eventData.pointerEnter != null)
        {
            dropZone = eventData.pointerEnter.GetComponentInParent<DropZone>();
        }

        if (dropZone != null && dropZone.CanAccept(wasteType))
        {
            if (miniGame != null)
            {
                miniGame.OnWasteSegregated(this, dropZone);
            }

            return;
        }

        transform.position = originalPosition;
    }
}
