using UnityEngine;

public class MopFloorChore : Chore
{
    [SerializeField] private MopFloorMiniGame miniGame;

    public override void Interact()
    {
        if (IsCompleted)
        {
            Debug.Log("Mop Floor is already completed.");
            return;
        }

        Debug.Log("Starting Mopping Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Mop Floor Mini-Game is not assigned!");
        }
    }
}