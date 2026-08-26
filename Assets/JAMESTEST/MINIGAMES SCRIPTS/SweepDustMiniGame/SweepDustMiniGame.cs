using UnityEngine;
using UnityEngine.InputSystem;

public class SweepDustMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] dustSpots;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private bool sweepingStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        sweepingStarted = false;

        panel.SetActive(true);

        ResetDust();

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartSweeping();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorial.ShowTutorial(
            "SWEEPING",
            "1. Hold Left Mouse Button.\n" +
            "2. Move your mouse over the dust.\n" +
            "3. Clear all the dust.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartSweeping();
    }

    private void StartSweeping()
    {
        sweepingStarted = true;

        Debug.Log("Sweeping started!");
    }

    public void CleanDust(GameObject dust)
    {
        if (!sweepingStarted)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.isPressed)
            return;

        if (!dust.activeSelf)
            return;

        dust.SetActive(false);

        Debug.Log("Dust swept!");

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        foreach (GameObject dust in dustSpots)
        {
            if (dust != null && dust.activeSelf)
                return;
        }

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Sweeping complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);

        currentChore = null;
        sweepingStarted = false;
    }

    private void ResetDust()
    {
        foreach (GameObject dust in dustSpots)
        {
            if (dust != null)
            {
                dust.SetActive(true);
            }
        }
    }
}