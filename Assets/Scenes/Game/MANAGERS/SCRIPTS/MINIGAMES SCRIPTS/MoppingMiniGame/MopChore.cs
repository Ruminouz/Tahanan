using UnityEngine;

public class MopChore : Interactable
{
    [SerializeField] private GameObject mopVisual;

    private bool hasBeenPickedUp = false;

    public override void Interact()
    {
        if (hasBeenPickedUp)
        {
            Debug.Log("Mop has already been picked up.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("Player object with 'Player' tag was not found.");
            return;
        }

        MoppingPlayerState playerState = player.GetComponent<MoppingPlayerState>();

        if (playerState == null)
        {
            Debug.LogWarning("MoppingPlayerState is missing from the Player.");
            return;
        }

        playerState.PickUpMop();

        hasBeenPickedUp = true;

        if (mopVisual != null)
        {
            mopVisual.SetActive(false);
        }

        Debug.Log("MOP PICKED UP!");
    }
}