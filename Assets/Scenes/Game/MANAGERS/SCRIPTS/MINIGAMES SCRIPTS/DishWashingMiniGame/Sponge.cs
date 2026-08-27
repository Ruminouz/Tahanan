using UnityEngine;
using UnityEngine.EventSystems;

public class DishSponge : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private DishwashingMiniGame miniGame;
    private Canvas canvas;

    private Transform originalParent;
    private Vector3 originalPosition;

    private bool hasSoap = false;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.position;

        transform.SetParent(canvas.transform);

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        Debug.Log("Sponge picked up.");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (eventData.pointerEnter != null)
        {
            DishSoap dishSoap =
                eventData.pointerEnter.GetComponentInParent<DishSoap>();

            if (dishSoap != null)
            {
                dishSoap.ReceiveSponge(this);
                return;
            }
        }

        transform.SetParent(originalParent);
        transform.position = originalPosition;

        Debug.Log("Sponge returned.");
    }

    public void AddSoap()
    {
        if (hasSoap)
            return;

        hasSoap = true;

        Debug.Log("Sponge now has dishwashing liquid.");

        miniGame = FindFirstObjectByType<DishwashingMiniGame>();

        if (miniGame == null)
        {
            Debug.LogError("DishwashingMiniGame was NOT found!");
            return;
        }

        Debug.Log("DishwashingMiniGame found.");

        miniGame.SoapAdded();

        Debug.Log("SoapAdded() sent to DishwashingMiniGame.");
    }

    public bool HasSoap()
    {
        return hasSoap;
    }
}