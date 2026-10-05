using UnityEngine;


public class GarbageBag : Interactable
{

    [SerializeField] private GarbageChore garbageChore;

    public void ConfigureSpawnedBag(GarbageChore chore)
    {
        garbageChore = chore;
    }

    public override void Interact()
    {
        if (GarbageCarry.Instance == null)
        {
            Debug.LogWarning("Cannot pick up garbage bag because GarbageCarry is missing.");
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