using UnityEngine;

public class CleanLeavesChore : Chore
{
    [SerializeField] private CleanLeavesMiniGame miniGame;

    public override void Interact()
    {
        if (IsCompleted)
        {
            Debug.Log("Clean Leaves is already completed.");
            return;
        }

        Debug.Log("Starting Clean Leaves Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Clean Leaves Mini-Game is not assigned!");
        }
    }
}