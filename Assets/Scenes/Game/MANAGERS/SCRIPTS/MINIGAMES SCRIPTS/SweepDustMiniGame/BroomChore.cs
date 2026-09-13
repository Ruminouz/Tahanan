using UnityEngine;

public class BroomChore : Interactable
{
    [SerializeField] private GameObject broomVisual;

    private bool hasBeenPickedUp = false;


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


        if (broomVisual != null)
        {
            broomVisual.SetActive(false);
        }


        Debug.Log("BROOM PICKED UP!");
    }
}