using UnityEngine;


public class TrashBin : MonoBehaviour
{

    public TrashType binType;


    private RectTransform rectTransform;



    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();


        Debug.Log(
            gameObject.name 
            + " BIN TYPE = "
            + binType
        );
    }



    public bool IsInsideBin(Vector2 screenPosition)
    {

        bool inside =
        RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            screenPosition
        );


        Debug.Log(
            gameObject.name
            + " Inside Bin? "
            + inside
        );


        return inside;

    }

}