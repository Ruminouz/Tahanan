using UnityEngine;
using System.Collections.Generic;

public class SweepingManager : MonoBehaviour
{
    [Header("Dust Setup")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private Transform[] dustSpawnPoints;


    [Header("Difficulty Scaling")]
    [SerializeField] private int[] dustPerDay =
    {
        1, // Day 1
        2, // Day 2
        3, // Day 3
        4, // Day 4
        5, // Day 5
        6, // Day 6
        7  // Day 7
    };


    [Header("Sweeping Minigame")]
    [SerializeField] private SweepingMinigame sweepingMinigame;



    private List<DustSpot> activeDust =
        new List<DustSpot>();


    private ChoreManager choreManager;
    private DayManager dayManager;


    private bool sweepingCompleted = false;
    private Chore sweepDustChore;
    private int totalDustSpawned;
    private int cleanedDustCount;

    public int TotalDustSpawned => totalDustSpawned;
    public int CleanedDustCount => cleanedDustCount;
    public void SetSweepDustChore(Chore chore)
    {
        sweepDustChore = chore;
    }


    public int RemainingDust
    {
        get
        {
            return activeDust.Count;
        }
    }



    public bool IsSweepingCompleted
    {
        get
        {
            return sweepingCompleted;
        }
    }





    private void Awake()
    {
        choreManager = FindFirstObjectByType<ChoreManager>();
        ResolveDayManager();
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





    public void StartSweepingTask()
    {
        sweepingCompleted = false;
        totalDustSpawned = 0;
        cleanedDustCount = 0;

        SpawnDust();

        Debug.Log(
            "SWEEPING TASK STARTED!"
        );
    }





    private void SpawnDust()
    {
        foreach (DustSpot dust in activeDust)
        {
            if (dust != null)
                Destroy(dust.gameObject);
        }

        activeDust.Clear();

        if (dustPrefab == null || dustSpawnPoints == null || dustSpawnPoints.Length == 0)
        {
            Debug.LogWarning("Dust prefab or spawn points are missing.");
            sweepingCompleted = true;
            return;
        }



        int currentDay = 1;


        DayManager resolvedDayManager = ResolveDayManager();

        if(resolvedDayManager != null)
        {
            currentDay =
                resolvedDayManager.CurrentDay;
        }



        int dustAmount =
            GetDustAmount(currentDay);



        dustAmount =
            Mathf.Clamp(
                dustAmount,
                1,
                dustSpawnPoints.Length
            );



        Debug.Log(
            "Today's Dust Amount: "
            + dustAmount
        );



        List<Transform> availablePoints =
            new List<Transform>(
                dustSpawnPoints
            );



        for(int i = 0; i < dustAmount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availablePoints.Count
                );



            Transform point =
                availablePoints[randomIndex];



            availablePoints.RemoveAt(
                randomIndex
            );



            GameObject dust =
                Instantiate(
                    dustPrefab,
                    point.position,
                    point.rotation
                );



            DustSpot dustSpot =
                dust.GetComponent<DustSpot>();



            if(dustSpot != null)
            {
                dustSpot.SetSweepingMinigame(
                    sweepingMinigame
                );

                dustSpot.SetSweepingManager(this);


                activeDust.Add(
                    dustSpot
                );
            }
            else
            {
                Debug.LogWarning(
                    "Dust prefab missing DustSpot component."
                );


                Destroy(dust);
            }
        }



        Debug.Log(
            "Dust spawned: "
            + activeDust.Count
        );

        totalDustSpawned = activeDust.Count;
    }





    private int GetDustAmount(int day)
    {
        int index = day - 1;


        if(index >= 0 &&
           index < dustPerDay.Length)
        {
            return dustPerDay[index];
        }


        // Default for future days
        return dustPerDay[dustPerDay.Length - 1];
    }





    public void CompleteDust(DustSpot cleanedDust)
    {
        if(activeDust.Contains(cleanedDust))
        {
            activeDust.Remove(cleanedDust);
            cleanedDustCount++;
        }



        Debug.Log(
            "Remaining dust: "
            + activeDust.Count
        );



        if(activeDust.Count == 0)
        {
            CompleteSweepingTask();
        }
    }





    private void CompleteSweepingTask()
    {
        if(sweepingCompleted)
            return;



        sweepingCompleted = true;



        Debug.Log(
            "ALL DUST CLEANED! SWEEPING COMPLETE!"
        );



        if (sweepDustChore != null)
        {
            sweepDustChore.Complete();

            Debug.Log(
                "Sweeping chore completed."
            );
        }
        else if(choreManager != null)
        {
            choreManager.CompleteChore(1);


            Debug.Log(
                "Sweeping completed through ChoreManager."
            );
        }
        else
        {
            Debug.LogWarning(
                "ChoreManager not found."
            );
        }
    }
}