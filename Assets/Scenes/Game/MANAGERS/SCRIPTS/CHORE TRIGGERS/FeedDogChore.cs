using UnityEngine;

public class FeedDogChore : Chore
{
    [SerializeField] private FeedDogMiniGame miniGame;

        public override void Interact()
    {
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