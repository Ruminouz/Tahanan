using UnityEngine;

public class GarbageBin : Interactable
{
    [SerializeField] private GarbageChore garbageChore;

    public override bool CanInteract()
    {
        GarbageChore chore = ResolveGarbageChore();
        return chore != null && chore.CanInteract();
    }

    public override void Interact()
    {
        if (GarbageCarry.Instance == null || !GarbageCarry.Instance.HasGarbage())
        {
            Debug.Log("Need garbage bag first!");
            return;
        }

        GarbageChore chore = ResolveGarbageChore();
        if (chore == null)
        {
            Debug.LogWarning("Cannot start garbage sorting because GarbageChore is missing.", this);
            return;
        }

        chore.Interact();
    }

    private GarbageChore ResolveGarbageChore()
    {
        if (garbageChore == null)
            garbageChore = GarbageChore.Instance != null
                ? GarbageChore.Instance
                : FindFirstObjectByType<GarbageChore>();

        return garbageChore;
    }
}