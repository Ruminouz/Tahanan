using UnityEngine;
using UnityEngine.UI;
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

    [Header("Sponge Sprite")]
    [SerializeField] private Image spongeImage;

    [SerializeField] private Sprite normalSpongeSprite;
    [SerializeField] private Sprite soapedSpongeSprite;

    [Header("Soaped Sponge Movement Sprites")]
    [SerializeField] private Sprite soapedSpongeIdleSprite;
    [SerializeField] private Sprite soapedSpongeLeftSprite;
    [SerializeField] private Sprite soapedSpongeRightSprite;
    [SerializeField] private Sprite soapedSpongeUpSprite;
    [SerializeField] private Sprite soapedSpongeDownSprite;

    private Vector2 lastMousePosition;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        // Initial sprite
        if (spongeImage != null && normalSpongeSprite != null)
        {
            spongeImage.sprite = normalSpongeSprite;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = transform.position;

        lastMousePosition = eventData.position;

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

        if (hasSoap)
        {
            float movementX = eventData.position.x - lastMousePosition.x;
            float movementY = eventData.position.y - lastMousePosition.y;

            // Use whichever direction has the greater movement.
            if (Mathf.Abs(movementX) > Mathf.Abs(movementY))
            {
                if (movementX < 0)
                {
                    // Moving Left
                    if (spongeImage != null && soapedSpongeLeftSprite != null)
                    {
                        spongeImage.sprite = soapedSpongeLeftSprite;
                    }
                }
                else if (movementX > 0)
                {
                    // Moving Right
                    if (spongeImage != null && soapedSpongeRightSprite != null)
                    {
                        spongeImage.sprite = soapedSpongeRightSprite;
                    }
                }
            }
            else
            {
                if (movementY < 0)
                {
                    // Moving Down
                    if (spongeImage != null && soapedSpongeDownSprite != null)
                    {
                        spongeImage.sprite = soapedSpongeDownSprite;
                    }
                }
                else if (movementY > 0)
                {
                    // Moving Up
                    if (spongeImage != null && soapedSpongeUpSprite != null)
                    {
                        spongeImage.sprite = soapedSpongeUpSprite;
                    }
                }
            }

            lastMousePosition = eventData.position;
        }
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

        // Return to idle sprite
        if (hasSoap)
        {
            if (spongeImage != null && soapedSpongeIdleSprite != null)
            {
                spongeImage.sprite = soapedSpongeIdleSprite;
            }
        }
        else
        {
            if (spongeImage != null && normalSpongeSprite != null)
            {
                spongeImage.sprite = normalSpongeSprite;
            }
        }

        Debug.Log("Sponge returned.");
    }

    public void AddSoap()
    {
        if (hasSoap)
            return;

        hasSoap = true;

        // Change to soaped idle sprite
        if (spongeImage != null && soapedSpongeIdleSprite != null)
        {
            spongeImage.sprite = soapedSpongeIdleSprite;
        }
        else if (spongeImage != null && soapedSpongeSprite != null)
        {
            spongeImage.sprite = soapedSpongeSprite;
        }

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

