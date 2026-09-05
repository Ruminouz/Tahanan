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


    private List<Transform> availableSpawnPoints =
        new List<Transform>();


    private List<WetArea> activeWetAreas =
        new List<WetArea>();


    private bool spawning = false;

    private float spawnTimer = 0f;



    private bool mopTaskStarted = false;

    private bool mopCompleted = false;



    private SuddenTaskManager suddenTaskManager;

    private DayManager dayManager;



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



    public bool HasActiveWater
    {
        get
        {
            return activeWetAreas.Count > 0;
        }
    }



   private void Start()
{
    InitializeSpawner();
}


private void InitializeSpawner()
{
    if(initialized)
        return;


    suddenTaskManager =
        FindFirstObjectByType<SuddenTaskManager>();


    dayManager =
        FindFirstObjectByType<DayManager>();


    ResetSpawnPoints();


    initialized = true;


    Debug.Log("WaterSpawner Initialized");
}

private void Update()
{
    if(!spawning)
        return;


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


    activeWetAreas.Clear();


    mopTaskStarted = false;

    mopCompleted = false;


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
        float difficulty = 0;


        if(dayManager != null)
        {
            difficulty =
                dayManager.CurrentDifficulty;
        }



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




        spawnTimer =
            Random.Range(
                currentMin,
                currentMax
            );



        Debug.Log(
            "Next water spawn in "
            + spawnTimer
            + " seconds"
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



        availableSpawnPoints.RemoveAt(index);



        mopTaskStarted = true;

        mopCompleted = false;



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


            Debug.Log(
                "ALL WATER CLEANED"
            );
        }
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
    spawning = true;


    mopTaskStarted = true;

    mopCompleted = false;


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