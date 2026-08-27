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

    private DishwashingMiniGame miniGame;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        miniGame = FindFirstObjectByType<DishwashingMiniGame>();
    }

    // =====================================================
    // RINSE / DRY STATE
    // =====================================================

    public void EnableRinsing()
    {
        canRinse = true;
        canDry = false;
        isRinsed = false;
        alreadyDried = false;

        Debug.Log("Plate ready for rinsing.");
    }

    public void DisableRinsing()
    {
        canRinse = false;

        Debug.Log("Plate rinsing disabled.");
    }

    public void EnableDrying()
    {
        canRinse = false;
        canDry = true;
        isRinsed = true;
        alreadyDried = false;

        Debug.Log("Plate ready for drying rack.");
    }

    // =====================================================
    // DRAG
    // =====================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canRinse && !canDry)
            return;

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        CanvasGroup group = GetComponent<CanvasGroup>();

        if (group != null)
            group.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canRinse && !canDry)
            return;

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canRinse && !canDry)
            return;

        CanvasGroup group = GetComponent<CanvasGroup>();

        if (group != null)
            group.blocksRaycasts = true;

        // =================================================
        // RINSING
        // =================================================

        if (canRinse)
        {
            GameObject rinseObject =
                GameObject.FindWithTag("RinsingArea");

            if (rinseObject != null)
            {
                RectTransform rinseRect =
                    rinseObject.GetComponent<RectTransform>();

                if (rinseRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        rinseRect,
                        rectTransform.position,
                        eventData.pressEventCamera))
                {
                    transform.SetParent(rinseObject.transform);

                    rectTransform.anchoredPosition =
                        Vector2.zero;

                    DishRinseArea rinseArea =
                        rinseObject.GetComponent<DishRinseArea>();

                    if (rinseArea != null)
                    {
                        Debug.Log(
                            "Plate placed in rinsing area."
                        );

                        rinseArea.ReceivePlate(this);
                    }

                    return;
                }
            }
        }

        // =================================================
        // DRYING RACK
        // =================================================

        if (canDry)
        {
            GameObject rackObject =
                GameObject.FindWithTag("DryingRack");

            if (rackObject != null)
            {
                RectTransform rackRect =
                    rackObject.GetComponent<RectTransform>();

                if (rackRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        rackRect,
                        rectTransform.position,
                        eventData.pressEventCamera))
                {
                    PutOnDryingRack(rackObject);

                    return;
                }
            }
        }

        // =================================================
        // RETURN
        // =================================================

        transform.SetParent(originalParent);

        rectTransform.anchoredPosition =
            originalPosition;

        Debug.Log(
            "Plate returned to previous position."
        );
    }

    // =====================================================
    // DRYING RACK
    // =====================================================

    private void PutOnDryingRack(GameObject rack)
    {
        if (alreadyDried)
            return;

        alreadyDried = true;

        canDry = false;
        canRinse = false;

        transform.SetParent(rack.transform);

        rectTransform.anchoredPosition =
            Vector2.zero;

        Debug.Log("==============================");
        Debug.Log("PLATE PLACED ON DRYING RACK!");
        Debug.Log("==============================");

        if (miniGame == null)
        {
            miniGame =
                FindFirstObjectByType<DishwashingMiniGame>();
        }

        if (miniGame != null)
        {
            Debug.Log(
                "Sending PlateDried() to DishwashingMiniGame."
            );

            miniGame.PlateDried();
        }
        else
        {
            Debug.LogError(
                "DishwashingMiniGame not found!"
            );
        }
    }

    // =====================================================
    // RINSE COMPLETE
    // =====================================================

   public void FinishRinsing()
{
    if (isRinsed)
        return;

    isRinsed = true;
    canRinse = false;

    Debug.Log("Plate rinsing finished!");

    DishwashingMiniGame miniGame =
        FindFirstObjectByType<DishwashingMiniGame>();

    if (miniGame != null)
    {
        miniGame.PlateRinsed(this);
    }
}
}