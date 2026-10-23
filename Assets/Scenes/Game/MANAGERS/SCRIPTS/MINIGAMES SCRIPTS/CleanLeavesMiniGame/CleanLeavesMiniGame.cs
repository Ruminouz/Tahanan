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
        if (chore == null || panel == null)
            return;

        if (currentChore == chore)
        {
            panel.SetActive(true);
            if (tutorial != null)
                tutorial.ResumeTutorial();
            return;
        }

        currentChore = chore;
        cleaningStarted = false;

        MiniGamePanelMinimizer.Attach(panel, MinimizeGame);
        if (tutorial != null)
            tutorial.AttachMinimizeButton(MinimizeGame);
        MiniGameInteractionBlocker.RegisterPanel(panel);
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
        if (tutorial == null)
        {
            StartCleaning();
            return;
        }

        tutorial.ShowTutorial(
            CleanLeavesChore.ToyPickupChoreName.ToUpperInvariant(),
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

    private void MinimizeGame()
    {
        if (tutorial != null)
            tutorial.HideTutorial();

        panel.SetActive(false);
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
        SoundEffectManager.Play("Sweep");

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
        Debug.Log(CleanLeavesChore.ToyPickupChoreName + " complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        SoundEffectManager.Play("ChoreFinished");
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