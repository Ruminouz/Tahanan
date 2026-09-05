using UnityEngine;

public class FeedDogChore : Chore
{
    [SerializeField] private FeedDogMiniGame miniGame;

    public override void Interact()
    {
        if (IsCompleted)
        {
            Debug.Log("Feed Dog is already completed.");
            return;
        }

        Debug.Log("Starting Feed Dog Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Feed Dog Mini-Game is not assigned!");
        }
    }
}