using UnityEngine;


public class GarbageChore : Chore
{

    public static GarbageChore Instance;


    [Header("Garbage Sorting")]
    [SerializeField] private GarbageSortingMiniGame miniGame;


    [Header("Garbage Bag")]
    [SerializeField] private GarbageBag garbageBag;



    private bool hasGarbageBag = false;
    private DayManager dayManager;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    private void Start()
{
    gameObject.SetActive(true);
}

    private DayManager ResolveDayManager()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        }

        return dayManager;
    }




    public void SpawnGarbageBag()
    {

        DayManager resolvedDayManager = ResolveDayManager();
        int day = resolvedDayManager != null ? resolvedDayManager.CurrentDay : 1;



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

    public override void Complete()
    {
        if (IsCompleted || IsMissed)
            return;

        if (GarbageCarry.Instance != null)
            GarbageCarry.Instance.ClearBag();

        if (garbageBag != null)
            garbageBag.gameObject.SetActive(false);

        hasGarbageBag = false;
        base.Complete();
    }







        public override bool CanInteract()
    {
        if (!base.CanInteract()) return false;

        DayManager resolvedDayManager = ResolveDayManager();
        int day = resolvedDayManager != null ? resolvedDayManager.CurrentDay : 1;
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