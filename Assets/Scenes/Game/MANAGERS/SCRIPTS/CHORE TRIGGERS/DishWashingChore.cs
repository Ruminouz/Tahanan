using UnityEngine;

public class DishwashingChore : Chore
{
    [SerializeField] private DishwashingMiniGame miniGame;

    public override void Interact()
    {
        Debug.Log("Starting Dishwashing Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Dishwashing Mini-Game is not assigned!");
        }
    }

    public override void Complete()
    {
        if (IsCompleted || IsMissed)
            return;

        base.Complete();

        DayManager resolvedDayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();

        if (resolvedDayManager == null || resolvedDayManager.CurrentDay < 2)
            return;

        GarbageChore garbageChore = GarbageChore.Instance != null
            ? GarbageChore.Instance
            : FindFirstObjectByType<GarbageChore>(FindObjectsInactive.Include);

        if (garbageChore == null)
        {
            Debug.LogWarning("Dishwashing completed, but no GarbageChore is available to spawn the bag.", this);
            return;
        }

        garbageChore.SpawnGarbageBag();
    }
}