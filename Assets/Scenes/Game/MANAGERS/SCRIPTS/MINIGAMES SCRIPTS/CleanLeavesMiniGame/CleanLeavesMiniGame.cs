using UnityEngine;
using UnityEngine.InputSystem;

public class CleanLeavesMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] leafSpots;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private bool cleaningStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        cleaningStarted = false;

        panel.SetActive(true);

        ResetLeaves();

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartCleaning();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorial.ShowTutorial(
            "CLEAN LEAVES",
            "1. Hold Left Mouse Button.\n" +
            "2. Move your mouse over the leaves.\n" +
            "3. Clear all the leaves.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartCleaning();
    }

    private void StartCleaning()
    {
        cleaningStarted = true;

        Debug.Log("Cleaning leaves started!");
    }

    public void CleanLeaves(GameObject leaf)
    {
        if (!cleaningStarted)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.isPressed)
            return;

        if (!leaf.activeSelf)
            return;

        leaf.SetActive(false);

        Debug.Log("Leaves cleaned!");

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        foreach (GameObject leaf in leafSpots)
        {
            if (leaf != null && leaf.activeSelf)
                return;
        }

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Clean Leaves complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);

        currentChore = null;
        cleaningStarted = false;
    }

    private void ResetLeaves()
    {
        foreach (GameObject leaf in leafSpots)
        {
            if (leaf != null)
            {
                leaf.SetActive(true);
            }
        }
    }
}