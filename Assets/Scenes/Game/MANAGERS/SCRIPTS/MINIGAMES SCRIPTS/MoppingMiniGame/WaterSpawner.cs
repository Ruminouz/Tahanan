using System.Collections.Generic;
using UnityEngine;

public class WaterSpawner : MonoBehaviour
{
    private bool initialized = false;
    [Header("Water Setup")]
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private Transform[] waterSpawnPoints;


    [Header("Mopping Minigame")]
    [SerializeField] private MoppingMinigame moppingMinigame;


    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnDelay = 80f;
<<<<<<< HEAD
    [SerializeField] private float maxSpawnDelay = 120f;

    [Header("Maximum Water Spawns Per Day")]
    [Tooltip("Element 0 is Day 1, element 1 is Day 2, and so on. The last value is used for later days.")]
    [SerializeField] private int[] maxWaterSpawnsPerDay =
    {
        0, // Day 1
        1, // Day 2
        1, // Day 3
        2, // Day 4
        2, // Day 5
        3, // Day 6
        3  // Day 7
    };
=======
[SerializeField] private float maxSpawnDelay = 120f;
>>>>>>> 2ND-MAIN


    private List<Transform> availableSpawnPoints =
        new List<Transform>();


    private List<WetArea> activeWetAreas =
        new List<WetArea>();


    private bool spawning = false;

    private float spawnTimer = 0f;
<<<<<<< HEAD
    private int waterSpawnCount;
=======
>>>>>>> 2ND-MAIN



    private bool mopTaskStarted = false;

    private bool mopCompleted = false;
<<<<<<< HEAD
    private bool mopMissed = false;
    private bool mopCounted = false;
=======
>>>>>>> 2ND-MAIN



    private SuddenTaskManager suddenTaskManager;
<<<<<<< HEAD
    private ChoreManager choreManager;
=======
>>>>>>> 2ND-MAIN

    private DayManager dayManager;


<<<<<<< HEAD
    private DayManager ResolveDayManager()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();
        }

        return dayManager;
    }

=======
>>>>>>> 2ND-MAIN

    // =========================
    // HUD ACCESS
    // =========================

    public int RemainingWetAreas
    {
        get
        {
            return activeWetAreas.Count;
        }
    }



    public bool MopTaskStarted
    {
        get
        {
            return mopTaskStarted;
        }
    }



    public bool IsMoppingCompleted
    {
        get
        {
            return mopCompleted;
        }
    }

<<<<<<< HEAD
    public bool IsMoppingMissed => mopMissed;

=======
>>>>>>> 2ND-MAIN


    public bool HasActiveWater
    {
        get
        {
            return activeWetAreas.Count > 0;
        }
    }

<<<<<<< HEAD
    public bool IsSpawning => spawning;

    public int WaterSpawnCount => waterSpawnCount;

    public int MaxWaterSpawnsToday
    {
        get
        {
            DayManager resolvedDayManager = ResolveDayManager();
            int day = resolvedDayManager != null ? resolvedDayManager.CurrentDay : 1;
            int index = Mathf.Max(0, day - 1);

            if (maxWaterSpawnsPerDay == null || maxWaterSpawnsPerDay.Length == 0)
                return 0;

            if (index >= maxWaterSpawnsPerDay.Length)
                index = maxWaterSpawnsPerDay.Length - 1;

            return Mathf.Max(0, maxWaterSpawnsPerDay[index]);
        }
    }



private void Awake()
=======


   private void Start()
>>>>>>> 2ND-MAIN
{
    InitializeSpawner();
}


private void InitializeSpawner()
{
<<<<<<< HEAD
        if (initialized)
            return;

        suddenTaskManager = FindFirstObjectByType<SuddenTaskManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        dayManager = ResolveDayManager();

        ResetSpawnPoints();
        initialized = true;
=======
    if(initialized)
        return;


    suddenTaskManager =
        FindFirstObjectByType<SuddenTaskManager>();


    dayManager =
        FindFirstObjectByType<DayManager>();


    ResetSpawnPoints();


    initialized = true;


    Debug.Log("WaterSpawner Initialized");
>>>>>>> 2ND-MAIN
}

private void Update()
{
    if(!spawning)
        return;

<<<<<<< HEAD
    if (waterSpawnCount >= MaxWaterSpawnsToday)
    {
        spawning = false;
        return;
    }

=======
>>>>>>> 2ND-MAIN

    spawnTimer -= Time.deltaTime;


    if(spawnTimer <= 0)
    {
        Debug.Log("Trying to spawn water...");

        SpawnWater();

        SetNextSpawnTime();
    }
}

    // =========================
    // DAILY RESET
    // =========================


   public void ResetDailyMop()
{
    foreach(WetArea area in activeWetAreas)
    {
        if(area != null)
            Destroy(area.gameObject);
    }

<<<<<<< HEAD
=======

>>>>>>> 2ND-MAIN
    activeWetAreas.Clear();


    mopTaskStarted = false;

    mopCompleted = false;
<<<<<<< HEAD
    mopMissed = false;
    mopCounted = false;
    waterSpawnCount = 0;
=======
>>>>>>> 2ND-MAIN


    spawning = false;


    spawnTimer = 0f;


    ResetSpawnPoints();


    Debug.Log(
        "WaterSpawner reset."
    );
}


    private void ResetSpawnPoints()
    {
        availableSpawnPoints.Clear();


<<<<<<< HEAD
        if (waterSpawnPoints == null)
            return;

=======
>>>>>>> 2ND-MAIN
        foreach(Transform point in waterSpawnPoints)
        {
            if(point != null)
            {
                availableSpawnPoints.Add(point);
            }
        }


        Debug.Log(
            "Spawn points loaded: "
            + availableSpawnPoints.Count
        );
    }





    // =========================
    // SPAWN TIMER
    // =========================


    private void SetNextSpawnTime()
    {
<<<<<<< HEAD
        var resolvedDayManager = ResolveDayManager();

        float difficulty = 0f;

        if (resolvedDayManager != null)
        {
            difficulty = resolvedDayManager.CurrentDifficulty;
=======
        float difficulty = 0;


        if(dayManager != null)
        {
            difficulty =
                dayManager.CurrentDifficulty;
>>>>>>> 2ND-MAIN
        }



<<<<<<< HEAD
        float currentMin = minSpawnDelay - (difficulty * 8f);
        float currentMax = maxSpawnDelay - (difficulty * 10f);


        currentMin = Mathf.Max(currentMin, 3f);



        currentMax = Mathf.Max(currentMax, 5f);

        if (currentMax <= currentMin)
        {
            currentMax = currentMin + 5f;
        }
=======
       float currentMin =
    minSpawnDelay -
    (difficulty * 8f);


float currentMax =
    maxSpawnDelay -
    (difficulty * 10f);


        currentMin =
            Mathf.Max(currentMin,3f);



        currentMax =
            Mathf.Max(currentMax,5f);
>>>>>>> 2ND-MAIN




<<<<<<< HEAD
        spawnTimer = Random.Range(currentMin, currentMax);
=======
        spawnTimer =
            Random.Range(
                currentMin,
                currentMax
            );
>>>>>>> 2ND-MAIN



        Debug.Log(
            "Next water spawn in "
            + spawnTimer
<<<<<<< HEAD
            + " seconds | difficulty="
            + difficulty
=======
            + " seconds"
>>>>>>> 2ND-MAIN
        );
    }







    private void SpawnWater()
    {
        if(waterPrefab == null)
        {
            Debug.LogWarning(
                "Water prefab missing."
            );

            return;
        }



        if(availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning(
                "No water spawn points."
            );

            return;
        }




        int index =
            Random.Range(
                0,
                availableSpawnPoints.Count
            );



        Transform point =
            availableSpawnPoints[index];



        GameObject water =
            Instantiate(
                waterPrefab,
                point.position,
                point.rotation
            );



        WetArea wetArea =
            water.GetComponent<WetArea>();



        if(wetArea == null)
        {
            Debug.LogWarning(
                "Water prefab missing WetArea."
            );


            Destroy(water);

            return;
        }





        wetArea.SetSpawner(
            this,
            point
        );



        wetArea.SetMoppingMinigame(
            moppingMinigame
        );



        activeWetAreas.Add(
            wetArea
        );

<<<<<<< HEAD
        waterSpawnCount++;

=======
>>>>>>> 2ND-MAIN


        availableSpawnPoints.RemoveAt(index);



        mopTaskStarted = true;

        mopCompleted = false;
<<<<<<< HEAD
        mopMissed = false;
=======
>>>>>>> 2ND-MAIN



        if(suddenTaskManager != null)
        {
            suddenTaskManager.ShowMopTask();
        }



        Debug.Log(
            "Water spawned at: "
            + point.name
        );
    }







    public void RemoveWetArea(WetArea wetArea)
    {
        if(activeWetAreas.Contains(wetArea))
        {
            activeWetAreas.Remove(wetArea);
        }



        Debug.Log(
            "Remaining water: "
            + activeWetAreas.Count
        );



        if(activeWetAreas.Count == 0)
        {
            mopCompleted = true;

<<<<<<< HEAD
            if (!mopCounted && choreManager != null)
            {
                choreManager.CompleteDynamicChore(1);
                mopCounted = true;
            }

=======
>>>>>>> 2ND-MAIN

            Debug.Log(
                "ALL WATER CLEANED"
            );
        }
    }

<<<<<<< HEAD
    public void MarkMoppingMissed()
    {
        if (!mopTaskStarted || mopCompleted)
            return;

        mopMissed = true;
        spawning = false;

        if (!mopCounted && choreManager != null)
        {
            choreManager.MissDynamicChore();
            mopCounted = true;
        }

        foreach (WetArea area in activeWetAreas)
        {
            if (area != null)
                Destroy(area.gameObject);
        }

        activeWetAreas.Clear();
    }

=======
>>>>>>> 2ND-MAIN






    public void FreeSpawnPoint(Transform point)
    {
        if(point == null)
            return;



        if(!availableSpawnPoints.Contains(point))
        {
            availableSpawnPoints.Add(point);
        }
    }






public void StartSpawning()
{
<<<<<<< HEAD
    InitializeSpawner();

    if (waterPrefab == null || availableSpawnPoints.Count == 0)
    {
        Debug.LogWarning("Water spawning cannot start without a prefab and spawn points.");
        return;
    }

    spawning = true;


    mopTaskStarted = false;

    mopCompleted = false;
    mopMissed = false;
    waterSpawnCount = 0;

    if (MaxWaterSpawnsToday <= 0)
    {
        spawning = false;
        DayManager resolvedDayManager = ResolveDayManager();
        int day = resolvedDayManager != null ? resolvedDayManager.CurrentDay : 1;
        Debug.Log("No water spawns configured for Day " + day + ".");
        return;
    }
=======
    spawning = true;


    mopTaskStarted = true;

    mopCompleted = false;
>>>>>>> 2ND-MAIN


    ResetSpawnPoints();


    SetNextSpawnTime();


    if(suddenTaskManager != null)
    {
        suddenTaskManager.ShowMopTask();
    }


    Debug.Log(
        "Water spawning enabled. Points: "
        + availableSpawnPoints.Count
    );
}

    public void StopSpawning()
    {
        spawning = false;


        Debug.Log(
            "Water spawning disabled."
        );
    }
}