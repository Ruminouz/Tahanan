using UnityEngine;
using UnityEngine.InputSystem;

public class MopFloorMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] dirtySpots;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private bool moppingStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        moppingStarted = false;

        panel.SetActive(true);

        ResetSpots();

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartMopping();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorial.ShowTutorial(
            "MOPPING",
            "1. Hold Left Mouse Button.\n" +
            "2. Move your mouse over the dirty spots.\n" +
            "3. Clean all the dirty spots.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartMopping();
    }

    private void StartMopping()
    {
        moppingStarted = true;

        Debug.Log("Mopping started!");
    }

    public void CleanSpot(GameObject spot)
    {
        if (!moppingStarted)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.isPressed)
            return;

        if (!spot.activeSelf)
            return;

        spot.SetActive(false);

        Debug.Log("Dirty spot cleaned!");

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        foreach (GameObject spot in dirtySpots)
        {
            if (spot != null && spot.activeSelf)
                return;
        }

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Mopping complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);

        currentChore = null;
        moppingStarted = false;
    }

    private void ResetSpots()
    {
        foreach (GameObject spot in dirtySpots)
        {
            if (spot != null)
            {
                spot.SetActive(true);
            }
        }
    }
}