using UnityEngine;

public class SegregateWasteChore : Chore
{
    [SerializeField] private SegregateWasteMiniGame miniGame;

    private DayManager dayManager;

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

    private bool IsAvailableToday()
    {
        DayManager resolvedDayManager = ResolveDayManager();
        int day = resolvedDayManager != null ? resolvedDayManager.CurrentDay : 1;
        return day >= 2 && day <= 7;
    }

    public override bool CanInteract()
    {
        return base.CanInteract() && IsAvailableToday();
    }

    public override void Complete()
    {
        if (IsCompleted || IsMissed)
            return;

        base.Complete();
        gameObject.SetActive(false);
    }

    public override void Interact()
    {
        if (!IsAvailableToday())
        {
            Debug.Log("Segregate Waste is available from Day 2 to Day 7 only.");
            return;
        }

        Debug.Log("Starting Segregate Waste Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Segregate Waste Mini-Game is not assigned!");
        }
    }

    public void ConfigureSpawnedChore(string runtimeName)
    {
        SetRuntimeChoreName(runtimeName);
        ResetChore();
    }
}
