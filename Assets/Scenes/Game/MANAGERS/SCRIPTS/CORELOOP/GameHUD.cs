using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text dayText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text choreListText;
    [SerializeField] private Slider timeBar;
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
        HashSet<Chore> listedChores = new HashSet<Chore>();

        if (chores != null)
        {
            foreach (Chore chore in chores)
            {
                if (chore == null)
                    continue;

                if (chore == dayManager.SweepDustChore)
                    sweepListed = true;

                if (listedChores.Add(chore))
                    AppendChore(chore);
            }
        }

        if (!sweepListed && dayManager.SweepDustChore != null &&
            listedChores.Add(dayManager.SweepDustChore))
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
