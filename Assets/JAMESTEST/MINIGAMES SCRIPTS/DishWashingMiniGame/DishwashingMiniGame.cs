using UnityEngine;

public class DishwashingMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private WashableDish[] dishes;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;

    public void StartGame(Chore chore)
    {
        currentChore = chore;

        panel.SetActive(true);

        ResetDishes();

        if (tutorial != null)
        {
            tutorial.ShowTutorial(
                "WASHING DISHES",
                "1. Hold Left Mouse Button.\n" +
                "2. Move your mouse across the dirty dish.\n" +
                "3. Keep scrubbing until the dish is clean.",
                StartWashing
            );
        }
        else
        {
            StartWashing();
        }
    }

    private void StartWashing()
    {
        Debug.Log("Dishwashing started!");
    }

    private void Update()
    {
        if (!panel.activeSelf)
            return;

        if (tutorial != null)
            return;

        CheckDishes();
    }

    private void CheckDishes()
    {
        foreach (WashableDish dish in dishes)
        {
            if (!dish.IsClean)
                return;
        }

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Dishwashing complete!");

        currentChore.Complete();

        panel.SetActive(false);
    }

   private void ResetDishes()
{
    foreach (WashableDish dish in dishes)
    {
        dish.ResetDish();
    }
}
}