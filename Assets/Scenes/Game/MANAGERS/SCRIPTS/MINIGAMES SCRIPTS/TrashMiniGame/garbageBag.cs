using UnityEngine;


public class GarbageBag : Interactable
{

    [SerializeField] private GarbageChore garbageChore;

    public void ConfigureSpawnedBag(GarbageChore chore)
    {
        if (garbageChore != null && garbageChore != chore)
            garbageChore.UnregisterSpawnedBag(this);

        garbageChore = chore;
        if (garbageChore != null)
            garbageChore.RegisterSpawnedBag(this);
    }

    public void NotifyConsumed()
    {
        if (garbageChore != null)
            garbageChore.UnregisterSpawnedBag(this);

        garbageChore = null;
    }

    private void OnDestroy()
    {
        NotifyConsumed();
    }

    public override void Interact()
    {
        if (GarbageCarry.Instance == null)
        {
            Debug.LogWarning("Cannot pick up garbage bag because GarbageCarry is missing.");
            return;
        }

        if (GarbageCarry.Instance.HasGarbage())
        {
            Debug.Log("Sort the carried garbage bag before picking up another one.");
            return;
        }

        if (garbageChore == null)
        {
            Debug.LogWarning("Cannot pick up garbage bag because its GarbageChore is missing.", this);
            return;
        }

        GarbageCarry.Instance.Pickup(gameObject);
        garbageChore.EnableGarbageBin();
        Debug.Log("Picked up garbage bag.");
    }
}