using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class SegregateWasteMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform spawnArea;
    [SerializeField] private DropZone[] dropZones;
    [FormerlySerializedAs("mistakesText")]
    [SerializeField] private TextMeshProUGUI toysRemainingText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private WasteSpawner spawner;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;
    private DayManager dayManager;

    private WasteObject[] spawnedWaste;
    private int correctlySegregatedCount = 0;
    private int totalItemsNeeded = 0;
    private bool gameStarted = false;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        dayManager = FindFirstObjectByType<DayManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        correctlySegregatedCount = 0;
        gameStarted = false;

        panel.SetActive(true);

        int currentDay = dayManager != null ? dayManager.CurrentDay : 1;
        ConfigureDifficulty(currentDay);

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartSegregation();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ConfigureDifficulty(int day)
    {
        day = Mathf.Clamp(day, 1, 7);

        switch (day)
        {
            case 1:
                totalItemsNeeded = 3;
                break;
            case 2:
                totalItemsNeeded = 5;
                break;
            case 3:
                totalItemsNeeded = 7;
                break;
            case 4:
                totalItemsNeeded = 9;
                break;
            case 5:
                totalItemsNeeded = 10;
                break;
            case 6:
                totalItemsNeeded = 12;
                break;
            case 7:
                totalItemsNeeded = 14;
                break;
            default:
                totalItemsNeeded = 5;
                break;
        }

        Debug.Log($"Day {day} - Total Toys: {totalItemsNeeded}");
    }

    private void ShowTutorial()
    {
        if (tutorial == null)
        {
            StartSegregation();
            return;
        }

        tutorial.ShowTutorial(
            "TOY COLLECTION",
            "1. Pick up each toy and drag it into the box.\n" +
            "2. Move quickly to collect all the toys.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

        StartSegregation();
    }

    private void StartSegregation()
    {
        gameStarted = true;
        SpawnWaste();
        UpdateUI();

        Debug.Log("Toy collection game started!");
    }

    private void SpawnWaste()
    {
        if (spawner == null)
        {
            Debug.LogError("WasteSpawner not assigned!");
            return;
        }

        int currentDay = dayManager != null ? dayManager.CurrentDay : 1;
        spawnedWaste = spawner.SpawnWaste(totalItemsNeeded, spawnArea, currentDay);

        foreach (WasteObject waste in spawnedWaste)
        {
            if (waste != null)
            {
                waste.SetMiniGame(this);
            }
        }
    }

    public void OnWasteSegregated(WasteObject waste, DropZone dropZone)
    {
        if (!gameStarted)
            return;

        SoundEffectManager.Play("Toys");
        dropZone.StackToy(waste, correctlySegregatedCount);
        correctlySegregatedCount++;

        Debug.Log($"Toy collected! Progress: {correctlySegregatedCount}/{totalItemsNeeded}");

        UpdateUI();
        CheckCompletion();
    }

    private void UpdateUI()
    {
        if (toysRemainingText != null)
            toysRemainingText.text = $"Toys left: {totalItemsNeeded - correctlySegregatedCount}";

        if (progressText != null)
            progressText.text = $"Progress: {correctlySegregatedCount}/{totalItemsNeeded}";
    }

    private void CheckCompletion()
    {
        if (correctlySegregatedCount >= totalItemsNeeded)
        {
            CompleteGame();
        }
    }

    private void CompleteGame()
    {
        gameStarted = false;

        Debug.Log("Toy collection complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
            SoundEffectManager.Play("ChoreFinished");
        }

        panel.SetActive(false);
        CleanupWaste();

        currentChore = null;
    }

    private void CleanupWaste()
    {
        if (spawnedWaste == null)
            return;

        foreach (WasteObject waste in spawnedWaste)
        {
            if (waste != null)
            {
                Destroy(waste.gameObject);
            }
        }
    }
}
