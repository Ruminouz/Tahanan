using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text dayText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text choreListText;
    [SerializeField] private Slider timeBar;
<<<<<<< HEAD
    [SerializeField] private Text pointsText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text successRateText;

    private TimeManager timeManager;
    private DayManager dayManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;
    private ChoreManager choreManager;

    private void Start()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
        waterSpawner = FindFirstObjectByType<WaterSpawner>();
        choreManager = FindFirstObjectByType<ChoreManager>();
    }

    private void Update()
    {
        if (timeManager == null || dayManager == null)
            return;

        UpdateDay();
        UpdateTime();
        UpdateChoreList();
        UpdateDailyStats();
    }

    private void UpdateDay()
    {
        if (dayText != null)
            dayText.text = "Day " + dayManager.CurrentDay;
    }

    private void UpdateTime()
    {
        if (timeBar != null)
            timeBar.value = timeManager.GetTimePercentage();

        if (timeText != null)
            timeText.text = "Time: " + FormatTime(timeManager.GetRemainingTime());
    }

    private void UpdateChoreList()
    {
        if (choreListText == null)
            return;

        choreListText.text = "CHORES\n";
        Chore[] chores = dayManager.GetActiveChores();
        bool sweepListed = false;

        if (chores != null)
        {
            foreach (Chore chore in chores)
            {
                if (chore == null)
                    continue;

                if (chore == dayManager.SweepDustChore)
                    sweepListed = true;

                AppendChore(chore);
            }
        }

        if (!sweepListed && dayManager.SweepDustChore != null)
            AppendChore(dayManager.SweepDustChore);

        if (!sweepListed && dayManager.SweepDustChore == null && sweepingManager != null)
        {
            string marker = sweepingManager.IsSweepingCompleted ? "✓ " : "○ ";
            string status = sweepingManager.IsSweepingCompleted
                ? string.Empty
                : " (" + sweepingManager.RemainingDust + " dust remaining, "
                    + FormatTime(timeManager.GetRemainingTime()) + ")";
            choreListText.text += marker + "Sweep Dust" + status + "\n";
        }

        if (waterSpawner == null || !waterSpawner.MopTaskStarted)
            return;

        if (waterSpawner.IsMoppingCompleted)
        {
            choreListText.text += "✓ Mop Floor\n";
        }
        else if (waterSpawner.IsMoppingMissed)
        {
            choreListText.text += "- Mop Floor (MISSED)\n";
        }
        else
        {
            choreListText.text += "○ Mop Floor ("
                + waterSpawner.RemainingWetAreas
                + " remaining, "
                + FormatTime(timeManager.GetRemainingTime())
                + ")\n";
        }
    }

    private void AppendChore(Chore chore)
    {
        string marker = chore.IsCompleted ? "✓ " : chore.IsMissed ? "- " : "○ ";
        string status = chore.IsCompleted
            ? choreManager != null && choreManager.WasCompletedByHelper(chore)
                ? " (HELPER)"
                : string.Empty
            : chore.IsMissed
                ? " (MISSED)"
                : " (" + FormatTime(chore.RemainingDeadline) + ")";

        choreListText.text += marker + chore.ChoreName + status + "\n";
    }

    private void UpdateDailyStats()
    {
        if (choreManager == null)
            return;

        if (pointsText != null)
            pointsText.text = "Points: " + choreManager.totalPoints;

        EconomyManager economyManager = EconomyManager.Instance != null
            ? EconomyManager.Instance
            : FindFirstObjectByType<EconomyManager>();
        if (coinsText != null && economyManager != null)
            coinsText.text = "Coins: " + economyManager.Coins;

        if (successRateText != null)
            successRateText.text = "Success: " + choreManager.SuccessRate.ToString("0") + "%";
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, remainingSeconds);
    }
}
=======



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
        if(timeManager == null ||
           dayManager == null)
            return;


        UpdateDay();

        UpdateTime();

        UpdateChoreList();
    }





    private void UpdateDay()
    {
        if(dayText != null)
        {
            dayText.text =
                "Day " + dayManager.CurrentDay;
        }
    }





    private void UpdateTime()
    {
        if(timeBar == null ||
           timeText == null)
            return;



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



        if(chores != null)
        {
            foreach(Chore chore in chores)
            {
                if(chore == null)
                    continue;



                if(chore.IsCompleted)
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

        if(sweepingManager != null)
        {
            if(sweepingManager.IsSweepingCompleted)
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
        // DAY 2-7 ONLY
        // ==========================

        if(waterSpawner != null &&
           dayManager.CurrentDay >= 2)
        {
            if(waterSpawner.IsMoppingCompleted)
            {
                choreListText.text +=
                    "✓ Mop Floor\n";
            }
            else
            {
                if(waterSpawner.HasActiveWater)
                {
                    choreListText.text +=
                        "○ Mop Floor ("
                        + waterSpawner.RemainingWetAreas
                        + " remaining)\n";
                }
                else
                {
                    choreListText.text +=
                        "○ Mop Floor (Waiting)\n";
                }
            }
        }
    }
}
>>>>>>> 2ND-MAIN
