using UnityEngine;

public class DishwashingChore : Chore
{
    [SerializeField] private DishwashingMiniGame miniGame;

        public override void Interact()
    {
        Debug.Log("Starting Dishwashing Mini-Game!");

        if (miniGame != null)
        {
            miniGame.StartGame(this);
        }
        else
        {
            Debug.LogWarning("Dishwashing Mini-Game is not assigned!");
        }
    }
}