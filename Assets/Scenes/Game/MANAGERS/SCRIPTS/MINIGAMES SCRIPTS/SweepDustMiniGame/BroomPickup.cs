using UnityEngine;

public class BroomPickup : Interactable
{
    private PlayerTool playerTool;

    private void Start()
    {
        playerTool = FindFirstObjectByType<PlayerTool>();
    }

    public override void Interact()
    {
        if (playerTool.hasBroom)
        {
            Debug.Log("Player already has broom.");
            return;
        }

        playerTool.PickupBroom();

        gameObject.SetActive(false);

        Debug.Log("Broom collected!");
    }
}