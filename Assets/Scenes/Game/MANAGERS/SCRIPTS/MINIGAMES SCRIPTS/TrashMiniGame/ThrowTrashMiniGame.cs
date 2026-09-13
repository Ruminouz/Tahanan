using UnityEngine;

public class ThrowTrashMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] trashItems;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private bool throwingStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        throwingStarted = false;

        panel.SetActive(true);

        ResetTrash();

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartThrowing();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorial.ShowTutorial(
            "THROW TRASH",
            "1. Click the trash.\n" +
            "2. Put all the trash in the bin.\n" +
            "3. Clear all the trash.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartThrowing();
    }

    private void StartThrowing()
    {
        throwingStarted = true;

        Debug.Log("Throwing trash started!");
    }

    public void ThrowTrash(GameObject trash)
    {
        if (!throwingStarted)
            return;

        if (!trash.activeSelf)
            return;

        trash.SetActive(false);

        Debug.Log("Trash thrown away!");

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        foreach (GameObject trash in trashItems)
        {
            if (trash != null && trash.activeSelf)
                return;
        }

        CompleteGame();
    }

    private void CompleteGame()
    {
        Debug.Log("Throw Trash complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);

        currentChore = null;
        throwingStarted = false;
    }

    private void ResetTrash()
    {
        foreach (GameObject trash in trashItems)
        {
            if (trash != null)
            {
                trash.SetActive(true);
            }
        }
    }
}