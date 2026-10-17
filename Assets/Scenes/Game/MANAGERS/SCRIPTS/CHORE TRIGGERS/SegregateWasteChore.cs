using UnityEngine;

public class SegregateWasteChore : Chore
{
    [SerializeField] private SegregateWasteMiniGame miniGame;

    private DayManager dayManager;

    private void Awake()
    {
        SetChoreName("Clean Leaves");

        if (miniGame == null)
        {
            miniGame = FindFirstObjectByType<SegregateWasteMiniGame>(
                FindObjectsInactive.Include);
        }
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

    private bool IsAvailableToday()
    {
        DayManager resolvedDayManager = ResolveDayManager();
        int day = resolvedDayManager != null ? resolvedDayManager.CurrentDay : 1;
        return day >= 2;
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
            Debug.Log("Clean Leaves is available from Day 2 onward.");
            return;
        }

        Debug.Log("Starting Clean Leaves toy-pickup mini-game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Toy-pickup mini-game is not assigned!");
        }
    }

    public void ConfigureSpawnedChore(string runtimeName)
    {
        SetRuntimeChoreName(runtimeName);
        ResetChore();
    }
}
