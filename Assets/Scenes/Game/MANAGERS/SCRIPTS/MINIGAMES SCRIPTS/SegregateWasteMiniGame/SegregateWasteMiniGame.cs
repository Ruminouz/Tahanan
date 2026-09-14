using UnityEngine;
using TMPro;

public class SegregateWasteMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform spawnArea;
    [SerializeField] private DropZone[] dropZones;
    [SerializeField] private TextMeshProUGUI mistakesText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private WasteSpawner spawner;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;
    private DayManager dayManager;

    private WasteObject[] spawnedWaste;
    private int correctlySegregatedCount = 0;
    private int mistakeCount = 0;
    private int maxMistakes = 3;
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
        mistakeCount = 0;
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
                maxMistakes = 3;
                break;
            case 2:
                totalItemsNeeded = 5;
                maxMistakes = 3;
                break;
            case 3:
                totalItemsNeeded = 7;
                maxMistakes = 3;
                break;
            case 4:
                totalItemsNeeded = 9;
                maxMistakes = 3;
                break;
            case 5:
                totalItemsNeeded = 10;
                maxMistakes = 2;
                break;
            case 6:
                totalItemsNeeded = 12;
                maxMistakes = 2;
                break;
            case 7:
                totalItemsNeeded = 14;
                maxMistakes = 2;
                break;
            default:
                totalItemsNeeded = 5;
                maxMistakes = 3;
                break;
        }

        Debug.Log($"Day {day} - Total Items: {totalItemsNeeded}, Max Mistakes: {maxMistakes}");
    }

    private void ShowTutorial()
    {
        if (tutorial == null)
        {
            StartSegregation();
            return;
        }

        tutorial.ShowTutorial(
            "SEGREGATE WASTE",
            "1. Drag waste items to correct bins:\n" +
            "   - Leaves → Leaves Bin\n" +
            "   - Bottles → Bottles Bin\n" +
            "   - Wrappers → Wrappers Bin\n" +
            "2. Avoid wrong bins!\n" +
            "3. Complete before mistakes run out.",
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

        Debug.Log("Segregate Waste game started!");
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

    public void OnWasteSegregated(WasteObject waste)
    {
        if (!gameStarted)
            return;

        correctlySegregatedCount++;
        Destroy(waste.gameObject);

        Debug.Log($"Waste segregated! Progress: {correctlySegregatedCount}/{totalItemsNeeded}");

        UpdateUI();
        CheckCompletion();
    }

    public void OnMistake()
    {
        if (!gameStarted)
            return;

        mistakeCount++;

        Debug.Log($"Mistake! {mistakeCount}/{maxMistakes}");

        UpdateUI();

        if (mistakeCount >= maxMistakes)
        {
            FailGame();
        }
    }

    private void UpdateUI()
    {
        if (mistakesText != null)
            mistakesText.text = $"Mistakes: {mistakeCount}/{maxMistakes}";

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

        Debug.Log("Segregate Waste complete!");

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        panel.SetActive(false);
        CleanupWaste();

        currentChore = null;
    }

    private void FailGame()
    {
        gameStarted = false;

        Debug.Log("Segregate Waste failed - too many mistakes!");

        if (currentChore != null)
        {
            ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();

            if (choreManager != null)
            {
                choreManager.MissChore(currentChore);
            }
            else
            {
                currentChore.MarkAsMissed();
            }
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
