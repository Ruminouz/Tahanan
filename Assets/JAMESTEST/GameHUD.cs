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

    private void Start()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        dayManager = FindFirstObjectByType<DayManager>();
    }

    private void Update()
    {
        if (timeManager == null || dayManager == null)
            return;

        UpdateDay();
        UpdateTime();
        UpdateChoreList();
    }

    private void UpdateDay()
    {
        dayText.text = "Day " + dayManager.CurrentDay;
    }

    private void UpdateTime()
    {
        float percentage = timeManager.GetTimePercentage();

        timeBar.value = percentage;

        float remainingSeconds = timeManager.GetRemainingTime();

        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);

        timeText.text = string.Format(
            "Time: {0:00}:{1:00}",
            minutes,
            seconds
        );
    }

    private void UpdateChoreList()
    {
        Chore[] chores = dayManager.GetActiveChores();

        if (chores == null)
            return;

        choreListText.text = "CHORES\n";

        foreach (Chore chore in chores)
        {
            if (chore == null)
                continue;

            if (chore.IsCompleted)
            {
                choreListText.text += "✓ " + chore.ChoreName + "\n";
            }
            else
            {
                choreListText.text += "○ " + chore.ChoreName + "\n";
            }
        }
    }
}