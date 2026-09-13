<<<<<<< HEAD
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    [SerializeField] private int currentDay = 1;

=======
using UnityEngine;

public class DayManager : MonoBehaviour
{

    public static DayManager Instance;


    [SerializeField] private int currentDay = 1;



>>>>>>> 2ND-MAIN
    [Header("Daily Chores")]
    [SerializeField] private Chore washDishes;
    [SerializeField] private Chore mopFloor;
    [SerializeField] private Chore sweepDust;

<<<<<<< HEAD
=======


>>>>>>> 2ND-MAIN
    [Header("Additional Chores")]
    [SerializeField] private Chore feedDog;
    [SerializeField] private Chore cleanLeaves;
    [SerializeField] private Chore throwTrash;

<<<<<<< HEAD
    [Header("Garbage System")]
    [SerializeField] private GarbageChore garbageChore;

    [Header("Segregation System")]
    [SerializeField] private SegregateWasteChore segregateWasteChore;

    [Header("Day Transition UI")]
    [SerializeField] private CanvasGroup transitionFade;
    [SerializeField] private GameObject daySummaryPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Text summaryChoresText;
    [SerializeField] private Text summarySuccessRateText;
    [SerializeField] private Text summaryPointsText;
    [SerializeField] private float transitionDuration = 0.5f;

    [Header("Game Over")]
    [Tooltip("Maximum chores that may be missed in one day before game over.")]
    [Min(0)]
    [SerializeField] private int missedChoresGameOverThreshold = 5;
=======


    [Header("Garbage System")]
    [SerializeField] private GarbageChore garbageChore;


>>>>>>> 2ND-MAIN

    private TimeManager timeManager;
    private ChoreManager choreManager;
    private SuddenTaskManager suddenTaskManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;

<<<<<<< HEAD
    private Chore[] activeChores;
    private bool dayFinished;
    private bool gameOver;

    public int CurrentDay => currentDay;
    public int CurrentDifficulty => Mathf.Max(0, currentDay - 2);
    public Chore MopFloorChore => mopFloor;
    public Chore SweepDustChore => sweepDust;
    public bool IsGameOver => gameOver;
    public bool HasReachedMissedChoreGameOverThreshold => choreManager != null &&
        choreManager.missedChores >= missedChoresGameOverThreshold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
=======


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
>>>>>>> 2ND-MAIN
        {
            Destroy(gameObject);
            return;
        }

<<<<<<< HEAD
        Instance = this;
        CacheSceneManagers();
        FindSweepDustChore();

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void CacheSceneManagers()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        suddenTaskManager = FindFirstObjectByType<SuddenTaskManager>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
    }
=======


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





>>>>>>> 2ND-MAIN

    private void Start()
    {
        StartDay();
    }

<<<<<<< HEAD
    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (dayFinished || choreManager == null)
            return;

        ExpireChoresAtDeadline();

        if (!AllActiveChoresResolved())
            return;

        if (sweepingManager != null && sweepDust != null &&
            !sweepingManager.IsSweepingCompleted)
            return;

        if (waterSpawner != null &&
            (waterSpawner.IsSpawning ||
             (waterSpawner.MopTaskStarted &&
              !waterSpawner.IsMoppingCompleted && !waterSpawner.IsMoppingMissed)))
            return;

        FinishDay();
    }

    private void StartDay()
    {
        ResolveWaterSpawner();

        Debug.Log("=== START DAY " + currentDay + " ===");

        ResetDayDependencies();
        ResetAllChores();
        SetupGarbage();

        if (currentDay == 1)
            SetupDay1();
        else
            SetupDay2To7();

        if (waterSpawner != null)
        {
            waterSpawner.StartSpawning();
            Debug.Log("Water Event Started Day " + currentDay);
        }

        Debug.Log("=== DAY START COMPLETE ===");
    }

    private void ResolveWaterSpawner()
    {
        if (waterSpawner == null)
            waterSpawner = FindFirstObjectByType<WaterSpawner>();
    }

    private void ResetDayDependencies()
    {
        if (timeManager != null)
        {
            timeManager.ResetDayTimer();
            Debug.Log("Timer Reset");
        }

        if (waterSpawner != null)
        {
            waterSpawner.ResetDailyMop();
            Debug.Log("Water Reset");
        }

        if (suddenTaskManager != null)
        {
            suddenTaskManager.ResetMopTask();
            Debug.Log("Mop Task Reset");
        }
    }

    private void SetupGarbage()
    {
        if (garbageChore == null)
        {
            Debug.LogWarning("GarbageChore reference missing!");
            return;
        }

        if (currentDay >= 2)
        {
            garbageChore.ResetChore();
            Debug.Log("Garbage Sorting ENABLED Day " + currentDay);
        }
        else
        {
            garbageChore.DisableChore();
            Debug.Log("Garbage Sorting LOCKED Day " + currentDay);
        }
    }

    private void SetupDay1()
    {
        Debug.Log("Loading Day 1");

        EnableChore(washDishes);
        EnableChore(sweepDust);

        DisableChore(mopFloor);
        DisableChore(feedDog);
        DisableChore(cleanLeaves);
        DisableChore(throwTrash);
        DisableChore(segregateWasteChore);

        List<Chore> chores = new List<Chore>();
        AddActiveChore(chores, washDishes);
        AddActiveChore(chores, sweepDust);
        activeChores = chores.ToArray();
        StartSweeping();
        Debug.Log("Day 1 Loaded");
    }

    private void SetupDay2To7()
    {
        Debug.Log("Loading Day " + currentDay);

        EnableChore(washDishes);
        EnableChore(sweepDust);
        EnableChore(feedDog);
        EnableChore(cleanLeaves);

        DisableChore(mopFloor);
        DisableChore(throwTrash);
        DisableChore(segregateWasteChore);

        List<Chore> chores = new List<Chore>();
        AddActiveChore(chores, washDishes);
        AddActiveChore(chores, sweepDust);
        AddActiveChore(chores, feedDog);
        AddActiveChore(chores, cleanLeaves);

        if (garbageChore != null && currentDay >= 2)
        {
            EnableChore(garbageChore);
            AddActiveChore(chores, garbageChore);
            Debug.Log("Garbage Chore Enabled Day " + currentDay);
        }
        else
        {
            DisableChore(garbageChore);
        }

        if (segregateWasteChore != null && currentDay >= 2 && currentDay <= 7)
        {
            EnableChore(segregateWasteChore);
            AddActiveChore(chores, segregateWasteChore);
            Debug.Log("Segregate Waste Enabled Day " + currentDay);
        }
        else
        {
            DisableChore(segregateWasteChore);
        }

        activeChores = chores.ToArray();
        StartSweeping();
        Debug.Log("Day " + currentDay + " Loaded");
    }

    private void AddActiveChore(List<Chore> chores, Chore chore)
    {
        if (chore != null)
            chores.Add(chore);
    }

    private void StartSweeping()
    {
        if (sweepingManager == null)
            return;

        if (sweepDust != null)
            sweepingManager.SetSweepDustChore(sweepDust);

        sweepingManager.StartSweepingTask();
    }

    private void EnableChore(Chore chore)
    {
        if (chore != null)
            chore.ResetChore();
    }

    private void DisableChore(Chore chore)
    {
        if (chore != null)
            chore.DisableChore();
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
        DisableChore(segregateWasteChore);
    }

    public Chore[] GetActiveChores()
    {
        return activeChores;
    }

    public void TriggerGameOver()
    {
        if (dayFinished || gameOver)
            return;

        gameOver = true;
        FinishDay();
    }

    private bool AllActiveChoresResolved()
    {
        if (activeChores == null || activeChores.Length == 0)
            return true;

        foreach (Chore chore in activeChores)
        {
            if (chore == null)
                continue;

            if (!chore.IsCompleted && !chore.IsMissed)
                return false;
        }

        return true;
    }

    private void FinishDay()
    {
        if (dayFinished)
            return;

        dayFinished = true;
        Debug.Log("DAY " + currentDay + " COMPLETE!");
        MarkUnfinishedChoresAsMissed();
        if (waterSpawner != null)
            waterSpawner.StopSpawning();

        gameOver = choreManager != null &&
            choreManager.missedChores >= missedChoresGameOverThreshold;
        ShowDaySummary();
    }

    private void ExpireChoresAtDeadline()
    {
        if (activeChores != null)
        {
            foreach (Chore chore in activeChores)
            {
                if (chore == null || chore.IsCompleted || chore.IsMissed)
                    continue;

                if (chore.RemainingDeadline <= 0f)
                    choreManager.MissChore(chore);
            }
        }

        if (timeManager != null && timeManager.GetRemainingTime() <= 0f)
        {
            if (waterSpawner != null && waterSpawner.MopTaskStarted &&
                !waterSpawner.IsMoppingCompleted)
            {
                waterSpawner.MarkMoppingMissed();
                if (suddenTaskManager != null)
                    suddenTaskManager.ResetMopTask();
            }

            if (waterSpawner != null)
                waterSpawner.StopSpawning();
        }
    }

    private void ShowDaySummary()
    {
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(true);

        if (summaryChoresText != null)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Chores");

            if (activeChores != null)
            {
                foreach (Chore chore in activeChores)
                {
                    if (chore == null)
                        continue;

                    string marker = chore.IsCompleted ? "✓ " : "- ";
                    string status = chore.IsCompleted ? "COMPLETED" : "MISSED";
                    summary.AppendLine(marker + chore.ChoreName + " - " + status);
                }
            }

            if (waterSpawner != null && waterSpawner.MopTaskStarted)
            {
                string marker = waterSpawner.IsMoppingCompleted ? "✓ " : "- ";
                string status = waterSpawner.IsMoppingCompleted ? "COMPLETED" : "MISSED";
                summary.AppendLine(marker + "Mop Floor - " + status);
            }

            summaryChoresText.text = summary.ToString();
        }

        if (summarySuccessRateText != null)
            summarySuccessRateText.text = "Success Rate: " + choreManager.SuccessRate.ToString("0") + "%";

        if (summaryPointsText != null)
            summaryPointsText.text = "Points Earned: " + choreManager.totalPoints;

        if (continueButton != null)
            continueButton.gameObject.SetActive(!gameOver);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(gameOver);

        if (gameOver && summaryChoresText != null)
            summaryChoresText.text += "\nGAME OVER\nToo many chores were missed.";
    }

    public void ContinueToNextDay()
    {
        if (!dayFinished || gameOver)
            return;

        StartCoroutine(TransitionToNextDay());
    }

    private IEnumerator TransitionToNextDay()
    {
        yield return Fade(1f);
        ResetAllChores();
        yield return new WaitForSeconds(1f);
        StartNextDay();
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(false);
        yield return Fade(0f);
    }

    public void ReturnToMainMenu()
    {
        if (!gameOver)
            return;

        SceneManager.LoadScene(0);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (transitionFade == null)
            yield break;

        float startAlpha = transitionFade.alpha;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            transitionFade.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / transitionDuration);
            yield return null;
        }

        transitionFade.alpha = targetAlpha;
        transitionFade.blocksRaycasts = targetAlpha > 0f;
    }

    private void MarkUnfinishedChoresAsMissed()
    {
        if (activeChores == null)
            return;

        foreach (Chore chore in activeChores)
        {
            if (chore == null || chore.IsCompleted || chore.IsMissed)
                continue;

            if (choreManager != null)
                choreManager.MissChore(chore);
            else
                chore.MarkAsMissed();
        }
    }

    private void StartNextDay()
    {
        CancelInvoke();
        currentDay++;

        if (choreManager != null)
            choreManager.ResetDailyProgress();

        gameOver = false;
        dayFinished = false;
        Debug.Log("Starting Next Day: " + currentDay);
        StartDay();
    }

    private void FindSweepDustChore()
    {
        if (sweepDust != null)
            return;

        Chore[] chores = FindObjectsByType<Chore>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Chore chore in chores)
        {
            if (chore.ChoreName == "Sweep Dust")
            {
                sweepDust = chore;
                return;
            }
        }
    }
}
=======





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
>>>>>>> 2ND-MAIN
