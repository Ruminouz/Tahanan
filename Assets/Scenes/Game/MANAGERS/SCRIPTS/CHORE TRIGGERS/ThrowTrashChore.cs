using UnityEngine;

public class ThrowTrashChore : Chore
{
    [SerializeField] private ThrowTrashMiniGame miniGame;

    public override void Interact()
    {
        if (IsCompleted)
        {
            Debug.Log("Throw Trash is already completed.");
            return;
        }

        Debug.Log("Starting Throw Trash Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Throw Trash Mini-Game is not assigned!");
        }
    }
}