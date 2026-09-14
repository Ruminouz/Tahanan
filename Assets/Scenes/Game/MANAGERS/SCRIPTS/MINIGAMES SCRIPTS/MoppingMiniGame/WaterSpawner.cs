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


    private List<Transform> availableSpawnPoints =
        new List<Transform>();


    private List<WetArea> activeWetAreas =
        new List<WetArea>();


    private bool spawning = false;

    private float spawnTimer = 0f;
    private int waterSpawnCount;
    private int cleanedWaterCount;



    private bool mopTaskStarted = false;

    private bool mopCompleted = false;
    private bool mopMissed = false;
    private bool mopCounted = false;



    private SuddenTaskManager suddenTaskManager;
    private ChoreManager choreManager;

    private DayManager dayManager;


    private DayManager ResolveDayManager()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();
        }

        return dayManager;
    }


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

    public bool IsMoppingMissed => mopMissed;



    public bool HasActiveWater
    {
        get
        {
            return activeWetAreas.Count > 0;
        }
    }

    public bool IsSpawning => spawning;

    public int WaterSpawnCount => waterSpawnCount;
    public int TotalWaterSpawned => waterSpawnCount;
    public int CleanedWaterCount => cleanedWaterCount;

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
{
    InitializeSpawner();
}


private void InitializeSpawner()
{
        if (initialized)
            return;

        suddenTaskManager = FindFirstObjectByType<SuddenTaskManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        dayManager = ResolveDayManager();

        ResetSpawnPoints();
        initialized = true;
}

private void Update()
{
    if(!spawning)
        return;

    if (waterSpawnCount >= MaxWaterSpawnsToday)
    {
        spawning = false;
        return;
    }


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
       if (moppingMinigame != null)
           moppingMinigame.ResetMopping();

       foreach(WetArea area in activeWetAreas)
    {
        if(area != null)
            Destroy(area.gameObject);
    }

    activeWetAreas.Clear();


    mopTaskStarted = false;

    mopCompleted = false;
    mopMissed = false;
    mopCounted = false;
    waterSpawnCount = 0;
    cleanedWaterCount = 0;


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


        if (waterSpawnPoints == null)
            return;

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
        var resolvedDayManager = ResolveDayManager();

        float difficulty = 0f;

        if (resolvedDayManager != null)
        {
            difficulty = resolvedDayManager.CurrentDifficulty;
        }



        float currentMin = minSpawnDelay - (difficulty * 8f);
        float currentMax = maxSpawnDelay - (difficulty * 10f);


        currentMin = Mathf.Max(currentMin, 3f);



        currentMax = Mathf.Max(currentMax, 5f);

        if (currentMax <= currentMin)
        {
            currentMax = currentMin + 5f;
        }




        spawnTimer = Random.Range(currentMin, currentMax);



        Debug.Log(
            "Next water spawn in "
            + spawnTimer
            + " seconds | difficulty="
            + difficulty
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

        waterSpawnCount++;



        availableSpawnPoints.RemoveAt(index);



        mopTaskStarted = true;

        mopCompleted = false;
        mopMissed = false;



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
            cleanedWaterCount++;
        }



        Debug.Log(
            "Remaining water: "
            + activeWetAreas.Count
        );



        if(activeWetAreas.Count == 0)
        {
            mopCompleted = true;

            if (!mopCounted && choreManager != null)
            {
                choreManager.CompleteDynamicChore(1);
                mopCounted = true;
            }


            Debug.Log(
                "ALL WATER CLEANED"
            );
        }
    }

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
    InitializeSpawner();

    if (moppingMinigame != null)
        moppingMinigame.ResetMopping();

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


    ResetSpawnPoints();


    SetNextSpawnTime();


    Debug.Log(
        "Water spawning enabled. Points: "
        + availableSpawnPoints.Count
    );
}

    public void StopSpawning()
    {
        spawning = false;

        if (moppingMinigame != null)
            moppingMinigame.ResetMopping();


        Debug.Log(
            "Water spawning disabled."
        );
    }
}