using UnityEngine;
using UnityEngine.Serialization;

public class CleanLeavesChore : Chore
{
    [FormerlySerializedAs("segregationMiniGame")]
    [SerializeField] private SegregateWasteMiniGame toyPickupMiniGame;

    private DayManager dayManager;

    private void Awake()
    {
        SetChoreName("Clean Leaves");

        if (toyPickupMiniGame == null)
        {
            toyPickupMiniGame = FindFirstObjectByType<SegregateWasteMiniGame>(
                FindObjectsInactive.Include);
        }
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
        return day >= 2;
    }

    public override bool CanInteract()
    {
        return base.CanInteract() && IsAvailableToday();
    }

    public override void Interact()
    {
        if (!IsAvailableToday())
        {
            Debug.Log("Clean Leaves is available from Day 2 onward.");
            return;
        }

        if (toyPickupMiniGame != null)
        {
            Debug.Log("Starting Clean Leaves toy-pickup mini-game!");
            toyPickupMiniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Toy-pickup mini-game is not assigned!");
        }
    }
}