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



    [Header("Sprite")]
    [SerializeField] private Image spongeImage;


    [SerializeField] private Sprite normalSpongeSprite;
    [SerializeField] private Sprite soapedSpongeSprite;


    [Header("Movement Sprites")]
    [SerializeField] private Sprite soapedSpongeIdleSprite;
    [SerializeField] private Sprite soapedSpongeLeftSprite;
    [SerializeField] private Sprite soapedSpongeRightSprite;
    [SerializeField] private Sprite soapedSpongeUpSprite;
    [SerializeField] private Sprite soapedSpongeDownSprite;



    private Vector2 lastMousePosition;



    private void Awake()
    {

        canvas = GetComponentInParent<Canvas>();


        if(canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

    }



    private void Start()
    {
        SaveOriginalPosition();

        ResetSponge();
    }





    private void SaveOriginalPosition()
    {

        if(originalParent == null)
        {
            originalParent = transform.parent;
        }


        if(originalParent != null)
        {
            originalPosition = transform.localPosition;
        }
        else
        {
            Debug.LogWarning(
                "Sponge has no parent. Assign it inside the Dishwashing Panel."
            );

            originalPosition = transform.position;
        }

    }





    public void OnBeginDrag(PointerEventData eventData)
    {

        lastMousePosition = eventData.position;


        if(canvas != null)
        {
            transform.SetParent(canvas.transform);
        }


        CanvasGroup group =
            GetComponent<CanvasGroup>();


        if(group != null)
        {
            group.blocksRaycasts = false;
        }


        Debug.Log("Sponge picked");

    }






    public void OnDrag(PointerEventData eventData)
    {

        transform.position =
            eventData.position;



        lastMousePosition =
            eventData.position;

    }





    public void OnEndDrag(PointerEventData eventData)
    {


        CanvasGroup group =
            GetComponent<CanvasGroup>();


        if(group != null)
        {
            group.blocksRaycasts = true;
        }



        if(eventData.pointerEnter != null)
        {

            DishSoap soap =
            eventData.pointerEnter
            .GetComponentInParent<DishSoap>();


            if(soap != null)
            {
                soap.ReceiveSponge(this);
                return;
            }

        }



        ReturnToOriginalPosition();

    }





    private void ReturnToOriginalPosition()
    {

        if(originalParent != null)
        {

            transform.SetParent(originalParent);


            transform.localPosition =
                originalPosition;

        }


        UpdateSprite();


        Debug.Log("Sponge returned");

    }






    public void ResetSponge()
    {

        hasSoap = false;


        if(originalParent != null)
        {

            transform.SetParent(originalParent);


            transform.localPosition =
                originalPosition;

        }



        CanvasGroup group =
            GetComponent<CanvasGroup>();


        if(group != null)
        {
            group.blocksRaycasts = true;
        }



        gameObject.SetActive(true);


        UpdateSprite();


        Debug.Log("Sponge Reset");

    }







    public void AddSoap()
    {

        if(hasSoap)
            return;


        hasSoap = true;


        UpdateSprite();



        miniGame =
        FindFirstObjectByType<DishwashingMiniGame>();


        if(miniGame != null)
        {
            miniGame.SoapAdded();
        }


        Debug.Log("Soap Added");

    }






    private void UpdateSprite()
    {

        if(spongeImage == null)
            return;



        if(hasSoap)
        {

            if(soapedSpongeIdleSprite != null)
            {
                spongeImage.sprite =
                soapedSpongeIdleSprite;
            }
            else if(soapedSpongeSprite != null)
            {
                spongeImage.sprite =
                soapedSpongeSprite;
            }

        }
        else
        {

            if(normalSpongeSprite != null)
            {
                spongeImage.sprite =
                normalSpongeSprite;
            }

        }

    }





    public bool HasSoap()
    {
        return hasSoap;
    }

}