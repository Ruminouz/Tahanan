using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class TrashItem : MonoBehaviour,
IBeginDragHandler,
IDragHandler,
IEndDragHandler
{

    public TrashType trashType;


    private RectTransform rectTransform;

    private Canvas canvas;

    private CanvasGroup canvasGroup;

    private FallingTrash fallingTrash;


    private Vector2 startPosition;

    private Vector2 offset;
    private TrashBin hoveredBin;



    private void Awake()
    {

        rectTransform = GetComponent<RectTransform>();

        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();

        fallingTrash = GetComponent<FallingTrash>();


        Debug.Log(
            gameObject.name +
            " Type = " +
            trashType
        );
    }





    public void OnBeginDrag(PointerEventData eventData)
    {

        startPosition =
        rectTransform.anchoredPosition;



        if(fallingTrash != null)
        {
            fallingTrash.enabled = false;
        }

        UpdateHoveredBin(null);

        Vector2 localPoint;


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint
        );



        offset =
        rectTransform.anchoredPosition - localPoint;



        if(canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }



        transform.SetAsLastSibling();

    }






    public void OnDrag(PointerEventData eventData)
    {

        Vector2 localPoint;


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out localPoint
        );



        rectTransform.anchoredPosition =
        localPoint + offset;

        TrashBin bin = eventData.pointerCurrentRaycast.gameObject != null
            ? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<TrashBin>()
            : null;
        UpdateHoveredBin(bin);

    }





    public void OnEndDrag(PointerEventData eventData)
    {


        if(fallingTrash != null)
        {
            fallingTrash.enabled = true;
        }



        if(canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }



        Debug.Log("Released trash");

        UpdateHoveredBin(null);

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            TrashBin bin = result.gameObject.GetComponentInParent<TrashBin>();
            if (bin == null)
                continue;

            bool wasCorrect = trashType == bin.binType;
            bin.PlayDropFeedback(wasCorrect);

            if (GarbageSortingMiniGame.Instance != null)
                GarbageSortingMiniGame.Instance.CheckTrash(this, bin);

            if (wasCorrect)
                return;

            rectTransform.anchoredPosition = startPosition;
            return;
        }



        Debug.Log("No bin detected");



        rectTransform.anchoredPosition =
        startPosition;

    }


    private void UpdateHoveredBin(TrashBin nextBin)
    {
        if (hoveredBin == nextBin)
            return;

        if (hoveredBin != null)
            hoveredBin.SetDropTarget(false);

        hoveredBin = nextBin;

        if (hoveredBin != null)
            hoveredBin.SetDropTarget(true);
    }

}