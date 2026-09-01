using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text dayText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text choreListText;
    [SerializeField] private Slider timeBar;


    private TimeManager timeManager;
    private DayManager dayManager;

    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;



    private void Start()
    {
        timeManager =
            FindFirstObjectByType<TimeManager>();

        dayManager =
            FindFirstObjectByType<DayManager>();

        sweepingManager =
            FindFirstObjectByType<SweepingManager>();

        waterSpawner =
            FindFirstObjectByType<WaterSpawner>();
    }




    private void Update()
    {
        if (timeManager == null ||
            dayManager == null)
            return;


        UpdateDay();

        UpdateTime();

        UpdateChoreList();
    }





    private void UpdateDay()
    {
        dayText.text =
            "Day " + dayManager.CurrentDay;
    }





    private void UpdateTime()
    {
        float percentage =
            timeManager.GetTimePercentage();


        timeBar.value =
            percentage;



        float remainingSeconds =
            timeManager.GetRemainingTime();



        int minutes =
            Mathf.FloorToInt(
                remainingSeconds / 60f
            );


        int seconds =
            Mathf.FloorToInt(
                remainingSeconds % 60f
            );



        timeText.text =
            string.Format(
                "Time: {0:00}:{1:00}",
                minutes,
                seconds
            );
    }





    private void UpdateChoreList()
    {
        choreListText.text =
            "CHORES\n";



        // ==========================
        // NORMAL CHORES
        // ==========================

        Chore[] chores =
            dayManager.GetActiveChores();



        if (chores != null)
        {
            foreach (Chore chore in chores)
            {
                if (chore == null)
                    continue;



                if (chore.IsCompleted)
                {
                    choreListText.text +=
                        "✓ "
                        + chore.ChoreName
                        + "\n";
                }
                else
                {
                    choreListText.text +=
                        "○ "
                        + chore.ChoreName
                        + "\n";
                }
            }
        }





        // ==========================
        // SWEEPING TASK
        // ==========================

        if (sweepingManager != null)
        {
            if (sweepingManager.IsSweepingCompleted)
            {
                choreListText.text +=
                    "✓ Sweep Dust\n";
            }
            else
            {
                choreListText.text +=
                    "○ Sweep Dust ("
                    + sweepingManager.RemainingDust
                    + " remaining)\n";
            }
        }





        // ==========================
        // MOPPING TASK
        // ==========================

        if (waterSpawner != null &&
            waterSpawner.MopTaskStarted)
        {
            if (waterSpawner.IsMoppingCompleted)
            {
                choreListText.text +=
                    "✓ Mop Floor\n";
            }
            else
            {
                choreListText.text +=
                    "○ Mop Floor ("
                    + waterSpawner.RemainingWetAreas
                    + " remaining)\n";
            }
        }
    }
}