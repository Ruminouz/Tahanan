using UnityEngine;

public class DayManager : MonoBehaviour
{

    public static DayManager Instance;


    [SerializeField] private int currentDay = 1;



    [Header("Daily Chores")]
    [SerializeField] private Chore washDishes;
    [SerializeField] private Chore mopFloor;
    [SerializeField] private Chore sweepDust;



    [Header("Additional Chores")]
    [SerializeField] private Chore feedDog;
    [SerializeField] private Chore cleanLeaves;
    [SerializeField] private Chore throwTrash;



    [Header("Garbage System")]
    [SerializeField] private GarbageChore garbageChore;



    private TimeManager timeManager;
    private ChoreManager choreManager;
    private SuddenTaskManager suddenTaskManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;



    private Chore[] activeChores;

    private int requiredChores;

    private bool dayFinished = false;



    public int CurrentDay => currentDay;



    public int CurrentDifficulty
    {
        get
        {
            if(currentDay <= 2)
                return 0;

            return currentDay - 2;
        }
    }



    public Chore MopFloorChore => mopFloor;

    public Chore SweepDustChore => sweepDust;





    private void Awake()
    {

        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }



        timeManager =
            FindFirstObjectByType<TimeManager>();


        choreManager =
            FindFirstObjectByType<ChoreManager>();


        suddenTaskManager =
            FindFirstObjectByType<SuddenTaskManager>();


        sweepingManager =
            FindFirstObjectByType<SweepingManager>();



        FindSweepDustChore();

    }






    private void Start()
    {
        StartDay();
    }






   private void Update()
{
    if (dayFinished || choreManager == null)
        return;


    // Completed + Missed = finished daily chores
    int finishedChores =
        choreManager.finishedChores +
        choreManager.missedChores;


    if (finishedChores < requiredChores)
        return;



    // Prevent ending day while sudden mop task is active
    if (suddenTaskManager != null &&
        suddenTaskManager.HasActiveMopTask)
    {
        return;
    }



    // Prevent ending day while water still exists
    if (waterSpawner != null &&
        waterSpawner.HasActiveWater)
    {
        return;
    }



    // Prevent ending day if mopping is not completed
    if (waterSpawner != null &&
        !waterSpawner.IsMoppingCompleted)
    {
        return;
    }



    FinishDay();
}



    // =========================
    // START DAY
    // =========================

    private void StartDay()
    {

        if(waterSpawner == null)
        {
            waterSpawner =
                FindFirstObjectByType<WaterSpawner>();
        }



        Debug.Log(
            "=== START DAY "
            + currentDay
            + " ==="
        );



        if(timeManager != null)
        {
            timeManager.ResetDayTimer();

            Debug.Log(
                "Timer Reset"
            );
        }



        if(waterSpawner != null)
        {
            waterSpawner.ResetDailyMop();

            Debug.Log(
                "Water Reset"
            );
        }



        if(suddenTaskManager != null)
        {
            suddenTaskManager.ResetMopTask();

            Debug.Log(
                "Mop Task Reset"
            );
        }




        ResetAllChores();



        // SETUP GARBAGE FIRST
        SetupGarbage();




        if(currentDay == 1)
        {
            SetupDay1();
        }
        else
        {
            SetupDay2To7();
        }




        if(waterSpawner != null)
        {
            waterSpawner.StartSpawning();


            Debug.Log(
                "Water Event Started Day "
                + currentDay
            );
        }



        Debug.Log(
            "=== DAY START COMPLETE ==="
        );

    }







    // =========================
    // GARBAGE SYSTEM
    // =========================

   private void SetupGarbage()
{
    if(garbageChore == null)
    {
        Debug.LogWarning(
            "GarbageChore reference missing!"
        );

        return;
    }



    if(currentDay >= 2)
    {
        garbageChore.ResetChore();

        Debug.Log(
            "Garbage Sorting ENABLED Day "
            + currentDay
        );
    }
    else
    {
        garbageChore.DisableChore();

        Debug.Log(
            "Garbage Sorting LOCKED Day "
            + currentDay
        );
    }
}





    // =========================
    // DAY 1
    // =========================

    private void SetupDay1()
    {

        Debug.Log(
            "Loading Day 1"
        );



        requiredChores = 2;



        EnableChore(washDishes);

        EnableChore(sweepDust);



        DisableChore(mopFloor);

        DisableChore(feedDog);

        DisableChore(cleanLeaves);

        DisableChore(throwTrash);



        activeChores = new Chore[]
        {
            washDishes,
            sweepDust
        };



        StartSweeping();



        Debug.Log(
            "Day 1 Loaded"
        );

    }
    // =========================
    // DAY 2-7
    // =========================

    private void SetupDay2To7()
    {

        Debug.Log(
            "Loading Day "
            + currentDay
        );



        // Normal chores
        requiredChores = 4;



        EnableChore(washDishes);

        EnableChore(sweepDust);

        EnableChore(feedDog);

        EnableChore(cleanLeaves);



        DisableChore(mopFloor);

        DisableChore(throwTrash);





      // Garbage sorting Day 3-7 only
if(currentDay >= 2)
{
    EnableChore(garbageChore);

    requiredChores = 5;

    Debug.Log(
        "Garbage Chore Enabled Day "
        + currentDay
    );
}
else
{
    DisableChore(garbageChore);

    Debug.Log(
        "Garbage Chore Locked Day "
        + currentDay
    );
}





       if(currentDay >= 2)
{
    activeChores = new Chore[]
    {
        washDishes,
        sweepDust,
        feedDog,
        cleanLeaves,
        garbageChore
    };
}
else
{
    activeChores = new Chore[]
    {
        washDishes,
        sweepDust,
        feedDog,
        cleanLeaves
    };
}





        StartSweeping();



        Debug.Log(
            "Day "
            + currentDay
            + " Loaded"
        );

    }







    // =========================
    // SWEEPING
    // =========================

    private void StartSweeping()
    {

        if(sweepingManager == null)
            return;



        if(sweepDust != null)
        {

            sweepingManager.SetSweepDustChore(
                sweepDust
            );

        }



        sweepingManager.StartSweepingTask();

    }








    // =========================
    // CHORES
    // =========================
// =========================
// CHORES
// =========================

private void EnableChore(Chore chore)
{

    if(chore != null)
    {
        chore.ResetChore();
    }

}





private void DisableChore(Chore chore)
{

    if(chore != null)
    {
        chore.DisableChore();
    }

}






// =========================
// MARK MISSED CHORES
// =========================

private void MarkUnfinishedChoresAsMissed()
{

    Chore[] chores =
    {
        washDishes,
        mopFloor,
        sweepDust,
        feedDog,
        cleanLeaves,
        garbageChore
    };



    foreach(Chore chore in chores)
    {

        if(chore == null)
            continue;



        // only mark active unfinished chores
        if(chore.gameObject.activeSelf &&
           !chore.IsCompleted)
        {

            chore.MarkAsMissed();



            if(choreManager != null)
            {
                choreManager.MissChore(chore);
            }



            Debug.Log(
                chore.ChoreName 
                + " marked as MISSED"
            );

        }

    }

}







private void ResetAllChores()
{

    DisableChore(washDishes);

    DisableChore(mopFloor);

    DisableChore(sweepDust);

    DisableChore(feedDog);

    DisableChore(cleanLeaves);

    DisableChore(throwTrash);

    DisableChore(garbageChore);

}







public Chore[] GetActiveChores()
{
    return activeChores;
}








// =========================
// FINISH DAY
// =========================

private void FinishDay()
{

    if(dayFinished)
        return;



    dayFinished = true;



    Debug.Log(
        "DAY "
        + currentDay
        + " COMPLETE!"
    );



    // CHECK MISSED TASKS FIRST
    MarkUnfinishedChoresAsMissed();



    // THEN DISABLE OLD DAY CHORES
    ResetAllChores();



    Invoke(
        nameof(StartNextDay),
        3f
    );

}







private void StartNextDay()
{
    CancelInvoke();


    currentDay++;


    if (choreManager != null)
    {
        // Reset daily chore progress
        choreManager.ResetDailyProgress();
    }


    dayFinished = false;


    Debug.Log(
        "Starting Next Day: "
        + currentDay
    );


    StartDay();
}



// =========================
// AUTO FIND SWEEP
// =========================

private void FindSweepDustChore()
{

    if(sweepDust != null)
        return;



    Chore[] chores =
        FindObjectsByType<Chore>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );



    foreach(Chore chore in chores)
    {

        if(chore.ChoreName == "Sweep Dust")
        {

            sweepDust = chore;

            return;

        }

    }

}
}