using UnityEngine;
using UnityEngine.InputSystem;


public class FeedDogSlider : MonoBehaviour
{
    [Header("Slider Objects")]
    [SerializeField] private RectTransform foodIndicator;
    [SerializeField] private RectTransform targetZone;


    [Header("Difficulty Settings")]
    [SerializeField] private float speed = 200f;
    [SerializeField] private float limit = 250f;
    [SerializeField] private float successRange = 50f;


    private bool movingRight = true;
    private bool active = false;


    private FeedDogMiniGame miniGame;



    private void Start()
    {
        miniGame = FindFirstObjectByType<FeedDogMiniGame>();

        if(miniGame == null)
        {
            Debug.LogError("FeedDogMiniGame not found!");
        }
    }



    public void StartSlider()
    {
        if(foodIndicator == null || targetZone == null)
        {
            Debug.LogError("FeedDogSlider references missing!");
            return;
        }


        active = true;


        foodIndicator.anchoredPosition =
            new Vector2(-limit, 0);


        movingRight = true;


        Debug.Log("Feed Dog Slider Started!");
    }




    private void Update()
    {
        if(!active)
            return;



        MoveFood();



        // New Unity Input System
        if(Keyboard.current != null &&
           Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("SPACE PRESSED");

            CheckPosition();
        }
    }





    private void MoveFood()
    {
        float direction = movingRight ? 1 : -1;


        foodIndicator.anchoredPosition +=
            Vector2.right *
            speed *
            direction *
            Time.deltaTime;



        if(foodIndicator.anchoredPosition.x >= limit)
        {
            movingRight = false;
        }



        if(foodIndicator.anchoredPosition.x <= -limit)
        {
            movingRight = true;
        }
    }





    private void CheckPosition()
    {
        float distance =
            Mathf.Abs(
                foodIndicator.position.x -
                targetZone.position.x
            );


        Debug.Log("Food Distance: " + distance);



        if(distance <= successRange)
        {
            Debug.Log("FEED DOG SUCCESS!");

            active = false;


            if(miniGame != null)
            {
                miniGame.CheckResult(true);
            }

        }
        else
        {
            Debug.Log("FEED DOG MISS!");

            active = false;


            if(miniGame != null)
            {
                miniGame.CheckResult(false);
            }
        }
    }
}