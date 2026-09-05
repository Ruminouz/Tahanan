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







        public override bool CanInteract()
    {
        if (!base.CanInteract()) return false;

        int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
        if (day < 2) return false;

        if (!hasGarbageBag) return false;

        if (GarbageCarry.Instance == null || !GarbageCarry.Instance.HasGarbage()) return false;

        return true;
    }

    public override void Interact()
    {
        Debug.Log("Starting Garbage Sorting Mini Game");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
            GarbageCarry.Instance.RemoveBag();
            hasGarbageBag = false;
        }
        else
        {
            Debug.LogWarning("Garbage Sorting MiniGame missing!");
        }
    }


}