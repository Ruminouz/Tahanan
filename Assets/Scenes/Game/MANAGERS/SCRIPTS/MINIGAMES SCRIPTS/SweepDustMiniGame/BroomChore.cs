using UnityEngine;

public class BroomChore : Interactable
{
    [SerializeField] private GameObject broomVisual;

    private bool hasBeenPickedUp = false;

    public void ResetForDay()
    {
        hasBeenPickedUp = false;

        if (broomVisual != null)
            broomVisual.SetActive(true);
    }

    public override void Interact()
    {
        if (hasBeenPickedUp)
        {
            Debug.Log("Broom already picked up.");
            return;
        }


        GameObject player = GameObject.FindGameObjectWithTag("Player");


        if (player == null)
        {
            Debug.LogWarning("Player not found.");
            return;
        }


        SweepingPlayerState playerState =
            player.GetComponent<SweepingPlayerState>();


        if (playerState == null)
        {
            Debug.LogWarning("SweepingPlayerState missing.");
            return;
        }


        playerState.PickUpBroom();

        hasBeenPickedUp = true;

        PlayerEquipmentInventory inventory = player.GetComponent<PlayerEquipmentInventory>();
        if (inventory == null)
            inventory = player.AddComponent<PlayerEquipmentInventory>();

        inventory.Add(EquipmentType.Broom, broomVisual != null ? broomVisual : gameObject);

        Debug.Log("BROOM PICKED UP!");
    }
}