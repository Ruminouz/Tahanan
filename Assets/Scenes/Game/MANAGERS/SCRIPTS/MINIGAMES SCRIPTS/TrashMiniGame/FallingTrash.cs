using UnityEngine;


public class FallingTrash : MonoBehaviour
{

    private float fallSpeed = 100f;


    private RectTransform rectTransform;



    private void Awake()
    {
        rectTransform =
        GetComponent<RectTransform>();
    }




    public void SetSpeed(float speed)
    {
        fallSpeed = speed;
    }




    private void Update()
    {

        rectTransform.anchoredPosition +=
        Vector2.down *
        fallSpeed *
        Time.deltaTime;



        if(rectTransform.anchoredPosition.y <= -500)
        {

            GarbageSortingMiniGame.Instance
            .TrashMissed();


            Destroy(gameObject);

        }

    }

}