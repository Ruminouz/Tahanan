using UnityEngine;
using UnityEngine.EventSystems;

public class DishRinsePlate : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private Transform originalParent;
    private Vector2 originalPosition;


    private bool canRinse = false;
    private bool isRinsed = false;

    private bool canDry = false;
    private bool alreadyDried = false;


    private bool countedForRinse = false;


    private DishwashingMiniGame miniGame;



    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvas =
            GetComponentInParent<Canvas>();

        miniGame =
            FindFirstObjectByType<DishwashingMiniGame>();
    }




    // ================================
    // STATE CONTROL
    // ================================


    public void EnableRinsing()
    {
        canRinse = true;

        canDry = false;

        isRinsed = false;

        alreadyDried = false;

        countedForRinse = false;


        Debug.Log(
            "Plate enabled for rinsing."
        );
    }



    public void DisableRinsing()
    {
        canRinse = false;


        Debug.Log(
            "Plate rinse disabled."
        );
    }




    public void EnableDrying()
{
    canRinse = false;

    canDry = true;

    isRinsed = true;

    alreadyDried = false;
}



    // ================================
    // DRAG
    // ================================


    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if(!canRinse && !canDry)
            return;



        originalParent =
            transform.parent;


        originalPosition =
            rectTransform.anchoredPosition;



        transform.SetParent(
            canvas.transform
        );


        transform.SetAsLastSibling();



        CanvasGroup group =
            GetComponent<CanvasGroup>();


        if(group != null)
        {
            group.blocksRaycasts = false;
        }
    }




    public void OnDrag(
        PointerEventData eventData)
    {
        if(!canRinse && !canDry)
            return;


        rectTransform.position =
            eventData.position;
    }




    public void OnEndDrag(
        PointerEventData eventData)
    {
        if(!canRinse && !canDry)
            return;



        CanvasGroup group =
            GetComponent<CanvasGroup>();


        if(group != null)
        {
            group.blocksRaycasts = true;
        }



        // ==========================
        // RINSE
        // ==========================

        if(canRinse)
        {
            GameObject rinseObject =
                GameObject.FindWithTag(
                    "RinsingArea"
                );


            if(rinseObject != null)
            {
                RectTransform rinseRect =
                    rinseObject
                    .GetComponent<RectTransform>();


                if(rinseRect != null &&
                RectTransformUtility
                .RectangleContainsScreenPoint(
                    rinseRect,
                    rectTransform.position,
                    eventData.pressEventCamera))
                {

                    transform.SetParent(
                        rinseObject.transform
                    );


                    rectTransform
                    .anchoredPosition =
                        Vector2.zero;



                    if(!countedForRinse)
                    {
                        countedForRinse = true;


                        if(miniGame == null)
                        {
                            miniGame =
                            FindFirstObjectByType
                            <DishwashingMiniGame>();
                        }



                        if(miniGame != null)
                        {
                            miniGame
                            .PlateMovedToRinsing(this);
                        }
                    }



                    DishRinseArea area =
                        rinseObject
                        .GetComponent
                        <DishRinseArea>();


                    if(area != null)
                    {
                        area.ReceivePlate(this);
                    }


                    return;
                }
            }
        }





        // ==========================
        // DRYING
        // ==========================


        if(canDry)
        {
            GameObject rack =
                GameObject.FindWithTag(
                    "DryingRack"
                );


            if(rack != null)
            {
                RectTransform rackRect =
                    rack.GetComponent
                    <RectTransform>();


                if(rackRect != null &&
                RectTransformUtility
                .RectangleContainsScreenPoint(
                    rackRect,
                    rectTransform.position,
                    eventData.pressEventCamera))
                {
                    PutOnDryingRack(rack);

                    return;
                }
            }
        }





        // RETURN

        transform.SetParent(
            originalParent
        );


        rectTransform.anchoredPosition =
            originalPosition;
    }





    // ================================
    // DRY
    // ================================


    private void PutOnDryingRack(
        GameObject rack)
    {
        if(alreadyDried)
            return;


        alreadyDried = true;

        canDry = false;

        canRinse = false;



        transform.SetParent(
            rack.transform
        );


        rectTransform
        .anchoredPosition =
            Vector2.zero;



        if(miniGame == null)
        {
            miniGame =
            FindFirstObjectByType
            <DishwashingMiniGame>();
        }



        if(miniGame != null)
        {
            miniGame.PlateDried();
        }
    }





    // ================================
    // RINSE COMPLETE
    // ================================


    public void FinishRinsing()
    {
        if(isRinsed)
            return;



        isRinsed = true;

        canRinse = false;



        Debug.Log(
            "Plate rinsed."
        );



        if(miniGame == null)
        {
            miniGame =
            FindFirstObjectByType
            <DishwashingMiniGame>();
        }



        if(miniGame != null)
        {
            miniGame.PlateRinsed(this);
        }
    }
}