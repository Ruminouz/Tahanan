using UnityEngine;

public class DustSpot : Interactable
{
    private SweepingMinigame sweepingMinigame;
<<<<<<< HEAD
    private SweepingManager sweepingManager;
=======
>>>>>>> 2ND-MAIN

    private bool isCleaned = false;


    public void SetSweepingMinigame(SweepingMinigame minigame)
    {
        sweepingMinigame = minigame;
    }

<<<<<<< HEAD
    public void SetSweepingManager(SweepingManager manager)
    {
        sweepingManager = manager;
    }

=======
>>>>>>> 2ND-MAIN

    public override void Interact()
    {
        if (isCleaned)
            return;


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


        if (!playerState.HasBroom)
        {
            Debug.Log("You need to pick up the broom first!");
            return;
        }


        if (sweepingMinigame == null)
        {
            Debug.LogWarning("Sweeping Minigame missing.");
            return;
        }


        Debug.Log("Opening Sweeping Minigame");


        sweepingMinigame.StartSweeping(this);
    }


    public void Clean()
    {
        if (isCleaned)
            return;


        isCleaned = true;


<<<<<<< HEAD
        if (sweepingManager != null)
        {
            sweepingManager.CompleteDust(this);
=======
        SweepingManager manager =
            FindFirstObjectByType<SweepingManager>();


        if (manager != null)
        {
            manager.CompleteDust(this);
>>>>>>> 2ND-MAIN
        }


        gameObject.SetActive(false);


        Debug.Log("DUST CLEANED!");
    }
}