using UnityEngine;

public class SweepDustChore : Chore
{
    [SerializeField] private SweepDustMiniGame miniGame;

    public override void Interact()
    {
        if (IsCompleted)
        {
            Debug.Log("Sweep Dust is already completed.");
            return;
        }

        Debug.Log("Starting Sweeping Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Sweep Dust Mini-Game is not assigned!");
        }
    }
}