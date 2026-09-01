using UnityEngine;

public class WetArea : Interactable
{
    private MoppingMinigame moppingMinigame;

    private WaterSpawner waterSpawner;
    private Transform spawnPoint;

    private bool isCleaned = false;



    public void SetSpawner(
        WaterSpawner spawner,
        Transform point
    )
    {
        waterSpawner = spawner;
        spawnPoint = point;
    }



    public void SetMoppingMinigame(
        MoppingMinigame minigame
    )
    {
        moppingMinigame = minigame;
    }



    public override void Interact()
    {
        if (isCleaned)
            return;



        GameObject player =
            GameObject.FindGameObjectWithTag("Player");



        if (player == null)
        {
            Debug.LogWarning(
                "Player object with 'Player' tag was not found."
            );

            return;
        }



        MoppingPlayerState playerState =
            player.GetComponent<MoppingPlayerState>();



        if (playerState == null)
        {
            Debug.LogWarning(
                "MoppingPlayerState is missing from Player."
            );

            return;
        }



        if (!playerState.HasMop)
        {
            Debug.Log(
                "You need to pick up the mop first!"
            );

            return;
        }



        if (moppingMinigame == null)
        {
            Debug.LogWarning(
                "Mopping Minigame reference is missing."
            );

            return;
        }



        Debug.Log(
            "MOP FOUND! Opening mopping minigame."
        );



        moppingMinigame.StartMopping(this);
    }





    public void Clean()
    {
        if (isCleaned)
            return;



        isCleaned = true;



        // Remove from HUD tracking
        if (waterSpawner != null)
        {
            waterSpawner.RemoveWetArea(this);

            waterSpawner.FreeSpawnPoint(
                spawnPoint
            );
        }



        gameObject.SetActive(false);



        Debug.Log(
            "WET AREA CLEANED!"
        );
    }
}