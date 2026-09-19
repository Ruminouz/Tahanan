using UnityEngine;

public class BroomPickup : Interactable
{
    public override void Interact()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerEquipmentInventory inventory = player.GetComponent<PlayerEquipmentInventory>();
        if (inventory != null && inventory.Owns(EquipmentType.Broom))
        {
            Debug.Log("Player already has broom.");
            return;
        }

        if (inventory == null)
            inventory = player.AddComponent<PlayerEquipmentInventory>();

        inventory.Add(EquipmentType.Broom, gameObject);
        Debug.Log("Broom collected!");
    }

    public void ResetForDay()
    {
        gameObject.SetActive(true);
    }
}