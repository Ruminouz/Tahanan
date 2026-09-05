using UnityEngine;

public class FeedDogMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private ChoreTutorial tutorial;
    [SerializeField] private FeedDogSlider slider;


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


        if(tutorialManager != null)
        {
            alreadyLearned =
                tutorialManager.HasLearned(chore.ChoreName);
        }



        if(alreadyLearned)
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
            "1. Watch the slider.\n" +
            "2. Press SPACE when the food is inside the marked area.\n" +
            "3. Feed the dog successfully.",
            FinishTutorial
        );
    }




    private void FinishTutorial()
    {
        if(tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(
                currentChore.ChoreName
            );
        }


        StartFeeding();
    }




    private void StartFeeding()
    {
        feedingStarted = true;


        Debug.Log("Feed Dog Mini Game Started");


        if(slider != null)
        {
            slider.StartSlider();
        }
    }




    public void CheckResult(bool success)
    {
        if(!feedingStarted)
            return;



        if(success)
        {
            CompleteGame();
        }
        else
        {
            MissGame();
        }
    }





    private void CompleteGame()
    {
        Debug.Log("DOG FED SUCCESS!");

        if(currentChore != null)
        {
            currentChore.Complete();
        }


        ClosePanel();
    }





    private void MissGame()
    {
        Debug.Log("DOG FOOD DROPPED!");

        ChoreManager manager =
            FindFirstObjectByType<ChoreManager>();


        if(manager != null)
        {
            manager.MissChore(currentChore);
        }


        ClosePanel();
    }





    private void ClosePanel()
    {
        panel.SetActive(false);

        currentChore = null;
        feedingStarted = false;
    }
}