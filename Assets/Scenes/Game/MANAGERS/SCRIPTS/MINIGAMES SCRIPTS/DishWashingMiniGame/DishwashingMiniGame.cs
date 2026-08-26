using UnityEngine;

public class DishwashingMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private WashableDish[] dishes;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private bool washingStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        washingStarted = false;

        panel.SetActive(true);

        ResetDishes();

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartWashing();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorial.ShowTutorial(
            "WASHING DISHES",
            "1. Hold Left Mouse Button.\n" +
            "2. Move your mouse across the dirty dish.\n" +
            "3. Keep scrubbing until the dish is clean.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartWashing();
    }

    private void StartWashing()
    {
        washingStarted = true;

        Debug.Log("Dishwashing started!");
    }

    private void Update()
    {
        if (!panel.activeSelf)
            return;

        if (!washingStarted)
            return;

        CheckDishes();
    }

    private void CheckDishes()
    {
        foreach (WashableDish dish in dishes)
        {
            if (dish == null || !dish.IsClean)
                return;
        }

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Dishwashing complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);

        currentChore = null;
        washingStarted = false;
    }

    private void ResetDishes()
    {
        foreach (WashableDish dish in dishes)
        {
            if (dish != null)
            {
                dish.ResetDish();
            }
        }
    }
}