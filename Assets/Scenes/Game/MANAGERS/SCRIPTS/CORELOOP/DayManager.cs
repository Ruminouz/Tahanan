using UnityEngine;

public class DayManager : MonoBehaviour
{
    [SerializeField] private int currentDay = 1;


    [Header("Daily Chores")]
    [SerializeField] private Chore washDishes;
    [SerializeField] private Chore mopFloor;
    [SerializeField] private Chore sweepDust;

    [Header("Additional Chores")]
    [SerializeField] private Chore feedDog;
    [SerializeField] private Chore cleanLeaves;
    [SerializeField] private Chore throwTrash;


    private ChoreManager choreManager;
    private SuddenTaskManager suddenTaskManager;
    private SweepingManager sweepingManager;


    private Chore[] activeChores;

    private int requiredChores;

    private bool dayFinished = false;


    public int CurrentDay => currentDay;


    public int CurrentDifficulty
    {
        get
        {
            if (currentDay <= 2)
                return 0;

            return currentDay - 2;
        }
    }


    public Chore MopFloorChore => mopFloor;
    public Chore SweepDustChore => sweepDust;



    private void Awake()
    {
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


        // Normal chores are not finished yet
        if (choreManager.completedChores < requiredChores)
            return;


        // Wait for active sudden mop task
        if (suddenTaskManager != null &&
            suddenTaskManager.HasActiveMopTask)
        {
            return;
        }


        FinishDay();
    }



    // =========================================================
    // DAY START
    // =========================================================

    private void StartDay()
    {
        Debug.Log("Starting Day " + currentDay);

        Debug.Log(
            "Difficulty Level: " +
            CurrentDifficulty
        );


        ResetAllChores();


        if (currentDay == 1)
        {
            SetupMonday();
        }
        else
        {
            SetupLaterDays();
        }
    }



    // =========================================================
    // DAY 1
    // =========================================================

    private void SetupMonday()
    {
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


        Debug.Log("Monday chores loaded.");
    }



    // =========================================================
    // DAY 2 - DAY 7
    // =========================================================

    private void SetupLaterDays()
    {
        // Mop is NOT included.
        // Mop is handled by SuddenTaskManager.

        requiredChores = 5;


        EnableChore(washDishes);
        EnableChore(sweepDust);

        EnableChore(feedDog);
        EnableChore(cleanLeaves);
        EnableChore(throwTrash);


        DisableChore(mopFloor);



        activeChores = new Chore[]
        {
            washDishes,
            sweepDust,
            feedDog,
            cleanLeaves,
            throwTrash
        };


        StartSweeping();


        Debug.Log(
            "Day " +
            currentDay +
            " chores loaded."
        );
    }



    // =========================================================
    // SWEEPING
    // =========================================================

    private void StartSweeping()
    {
        if (sweepingManager != null)
        {
            if (sweepDust != null)
            {
                sweepingManager.SetSweepDustChore(
                    sweepDust
                );

                Debug.Log(
                    "Sweep Dust chore sent to SweepingManager."
                );
            }
            else
            {
                Debug.LogWarning(
                    "Sweep Dust chore is NULL."
                );
            }


            sweepingManager.StartSweepingTask();
        }
    }



    // =========================================================
    // CHORE CONTROL
    // =========================================================

    private void EnableChore(Chore chore)
    {
        if (chore != null)
        {
            chore.ResetChore();
        }
    }



    private void DisableChore(Chore chore)
    {
        if (chore != null)
        {
            chore.DisableChore();
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
    }



    public Chore[] GetActiveChores()
    {
        return activeChores;
    }



    // =========================================================
    // DAY COMPLETE
    // =========================================================

    private void FinishDay()
    {
        dayFinished = true;


        Debug.Log(
            "DAY " +
            currentDay +
            " COMPLETE!"
        );


        ResetAllChores();



        if (currentDay < 7)
        {
            StartNextDay();
        }
        else
        {
            Debug.Log(
                "WEEK COMPLETE!"
            );
        }
    }



    private void StartNextDay()
    {
        currentDay++;


        if (choreManager != null)
        {
            choreManager.completedChores = 0;
        }


        dayFinished = false;


        Debug.Log(
            "Starting next day: " +
            currentDay
        );


        StartDay();
    }



    // =========================================================
    // FIND SWEEP CHORE
    // =========================================================

    private void FindSweepDustChore()
    {
        if (sweepDust != null)
        {
            Debug.Log(
                "Sweep Dust already assigned."
            );

            return;
        }



        Chore[] chores =
            FindObjectsByType<Chore>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );



        foreach (Chore chore in chores)
        {
            if (chore.ChoreName == "Sweep Dust")
            {
                sweepDust = chore;


                Debug.Log(
                    "Sweep Dust chore automatically found."
                );


                return;
            }
        }



        Debug.LogWarning(
            "Sweep Dust chore could not be found."
        );
    }
}