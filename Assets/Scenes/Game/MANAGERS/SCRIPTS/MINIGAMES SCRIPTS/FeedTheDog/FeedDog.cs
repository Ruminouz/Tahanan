using UnityEngine;

public class FeedDogMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private bool feedingStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        feedingStarted = false;

        panel.SetActive(true);

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartFeeding();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorial.ShowTutorial(
            "FEED THE DOG",
            "1. Click the food.\n" +
            "2. Give the food to the dog.\n" +
            "3. Finish feeding the dog.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartFeeding();
    }

    private void StartFeeding()
    {
        feedingStarted = true;

        Debug.Log("Feeding started!");
    }

    public void FeedDog()
    {
        if (!feedingStarted)
            return;

        Debug.Log("Dog fed!");

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Feed Dog complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);

        currentChore = null;
        feedingStarted = false;
    }
}