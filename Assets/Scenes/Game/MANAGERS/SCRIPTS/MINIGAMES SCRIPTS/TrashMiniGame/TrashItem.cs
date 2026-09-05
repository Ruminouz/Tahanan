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



        PointerEventData pointerData =
        new PointerEventData(EventSystem.current);



        pointerData.position =
        eventData.position;



        List<RaycastResult> results =
        new List<RaycastResult>();



        EventSystem.current.RaycastAll(
            pointerData,
            results
        );



        foreach(RaycastResult result in results)
        {

            Debug.Log(
                "Hit UI: "
                + result.gameObject.name
            );



            TrashBin bin =
            result.gameObject.GetComponentInParent<TrashBin>();


            if(bin != null)
            {

                Debug.Log(
                    "Dropped on BIN: "
                    + bin.name
                );


                GarbageSortingMiniGame.Instance
                .CheckTrash(this, bin);


                return;

            }

        }



        Debug.Log("No bin detected");



        rectTransform.anchoredPosition =
        startPosition;

    }

}