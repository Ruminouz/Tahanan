using UnityEngine;

public class CleanLeavesChore : Chore
{
    [SerializeField] private CleanLeavesMiniGame miniGame;
    [SerializeField] private SegregateWasteMiniGame segregationMiniGame;

    private DayManager dayManager;

    private void Awake()
    {
        SetChoreName("Segregate Waste");

        if (segregationMiniGame == null)
            segregationMiniGame = FindFirstObjectByType<SegregateWasteMiniGame>();
    }

    private bool IsAvailableToday()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        }

        int day = dayManager != null ? dayManager.CurrentDay : 1;
        return day >= 2 && day <= 7;
    }

    public override bool CanInteract()
    {
        return base.CanInteract() && IsAvailableToday();
    }

    public override void Interact()
    {
        if (!IsAvailableToday())
        {
            Debug.Log("Segregate Waste is available from Day 2 to Day 7 only.");
            return;
        }

        if (segregationMiniGame != null)
        {
            Debug.Log("Starting Segregate Waste Mini-Game!");
            segregationMiniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Segregate Waste Mini-Game is not assigned!");
        }
    }
}