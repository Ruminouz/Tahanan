using UnityEngine;

public class BunsoBoostPickup : Interactable
{
    [SerializeField] private BunsoBoostType boostType;

    public void Configure(BunsoBoostType type)
    {
        boostType = type;
    }

    public override bool CanInteract()
    {
        return BunsoBoostInventory.Instance != null;
    }

    public override void Interact()
    {
        BunsoBoostInventory inventory = BunsoBoostInventory.Instance;
        if (inventory == null)
            return;

        inventory.Add(boostType);
        Destroy(gameObject);
    }
}
