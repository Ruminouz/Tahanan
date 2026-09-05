using UnityEngine;


public class GarbageChore : Chore
{

    public static GarbageChore Instance;


    [Header("Garbage Sorting")]
    [SerializeField] private GarbageSortingMiniGame miniGame;


    [Header("Garbage Bag")]
    [SerializeField] private GarbageBag garbageBag;



    private bool hasGarbageBag = false;



    private void Awake()
    {
        Instance = this;
    }
    private void Start()
{
    gameObject.SetActive(true);
}




    public void SpawnGarbageBag()
    {

        int day = 1;


        if(DayManager.Instance != null)
        {
            day = DayManager.Instance.CurrentDay;
        }



        Debug.Log(
            "Garbage Spawn Check Day: "
            + day
        );



        // Garbage available Day 2+
        if(day < 2)
        {

            Debug.Log(
                "Garbage Sorting Locked Day "
                + day
            );

            return;

        }



        hasGarbageBag = false;



        if(garbageBag != null)
        {

            garbageBag.gameObject.SetActive(true);


            Debug.Log(
                "Garbage Bag Spawned Day "
                + day
            );

        }
        else
        {

            Debug.LogWarning(
                "Garbage Bag Reference Missing!"
            );

        }

    }





    public void EnableGarbageBin()
    {

        hasGarbageBag = true;


        Debug.Log(
            "Garbage bag picked up. Trash bin unlocked."
        );

    }







    public override void Interact()
    {


        if(IsCompleted)
        {

            Debug.Log(
                "Garbage chore already completed."
            );
              Debug.Log(
        "GarbageChore Triggered!"
    );
            return;

        }




        int day = 1;


        if(DayManager.Instance != null)
        {
            day = DayManager.Instance.CurrentDay;
        }



        if(day < 2  )
        {

            Debug.Log(
                "Garbage Sorting MiniGame locked Day "
                + day
            );

            return;

        }






        if(!hasGarbageBag)
        {

            Debug.Log(
                "Player needs garbage bag first!"
            );

            return;

        }







        if(GarbageCarry.Instance == null ||
           !GarbageCarry.Instance.HasGarbage())
        {

            Debug.Log(
                "Player is not carrying garbage!"
            );

            return;

        }






        Debug.Log(
            "Starting Garbage Sorting Mini Game"
        );





        if(miniGame != null)
        {

            miniGame.StartGame(this);


            GarbageCarry.Instance.RemoveBag();


            hasGarbageBag = false;

        }
        else
        {

            Debug.LogWarning(
                "Garbage Sorting MiniGame missing!"
            );

        }


    }


}