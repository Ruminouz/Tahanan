using UnityEngine;
using System.Collections.Generic;

public class SweepingManager : MonoBehaviour
{
    [Header("Dust Setup")]
    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private Transform[] dustSpawnPoints;


    [Header("Difficulty Scaling")]
    [SerializeField] private int startingDust = 2;
    [SerializeField] private int dustIncreasePerDay = 1;



    [Header("Sweeping Minigame")]
    [SerializeField] private SweepingMinigame sweepingMinigame;



    private List<DustSpot> activeDust = new List<DustSpot>();

    private ChoreManager choreManager;
    private DayManager dayManager;

    private Chore sweepDustChore;

    private bool sweepingCompleted = false;



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
        choreManager =
            FindFirstObjectByType<ChoreManager>();


        dayManager =
            FindFirstObjectByType<DayManager>();
    }





    // DayManager will send the correct chore reference
    public void SetSweepDustChore(Chore chore)
    {
        sweepDustChore = chore;
    }





    public void StartSweepingTask()
    {
        sweepingCompleted = false;

        SpawnDust();

        Debug.Log("SWEEPING TASK STARTED!");
    }





    private void SpawnDust()
    {
        activeDust.Clear();


        int difficulty = 0;


        if(dayManager != null)
        {
            difficulty =
                dayManager.CurrentDifficulty;
        }



        int dustAmount =
            startingDust +
            (difficulty * dustIncreasePerDay);



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
            new List<Transform>(dustSpawnPoints);



        for(int i = 0; i < dustAmount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    availablePoints.Count
                );


            Transform point =
                availablePoints[randomIndex];


            availablePoints.RemoveAt(randomIndex);



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


                activeDust.Add(dustSpot);
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
    }





    public void CompleteDust(DustSpot cleanedDust)
    {
        if(activeDust.Contains(cleanedDust))
        {
            activeDust.Remove(cleanedDust);
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


    if(choreManager != null)
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