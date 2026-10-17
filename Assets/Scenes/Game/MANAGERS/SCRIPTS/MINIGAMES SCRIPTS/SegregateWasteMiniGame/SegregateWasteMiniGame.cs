using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class SegregateWasteMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform spawnArea;
    [SerializeField] private DropZone boxDropZone;
    [SerializeField] private DropZone[] dropZones;
    [FormerlySerializedAs("mistakesText")]
    [SerializeField] private TextMeshProUGUI toysRemainingText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private WasteSpawner spawner;
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private WasteObject[] spawnedWaste;
    private readonly HashSet<WasteObject> collectedToys = new HashSet<WasteObject>();
    private int correctlySegregatedCount;
    private int totalItemsNeeded;
    private float elapsedTime;
    private bool gameStarted;

    public bool IsGameStarted => gameStarted;
    public int RemainingToyCount => gameStarted && spawnedWaste != null
        ? Mathf.Max(0, spawnedWaste.Length - collectedToys.Count)
        : 0;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;
        CleanupWaste();
        collectedToys.Clear();
        correctlySegregatedCount = 0;
        elapsedTime = 0f;
        gameStarted = false;

        MiniGameInteractionBlocker.RegisterPanel(panel);
        panel.SetActive(true);
        int currentDay = FindFirstObjectByType<DayManager>()?.CurrentDifficulty ?? 1;
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
        switch (Mathf.Clamp(day, 1, 7))
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
                totalItemsNeeded = 3;
                break;
        }
    }

    private void ShowTutorial()
    {
        if (tutorial == null)
        {
            StartSegregation();
            return;
        }

        tutorial.ShowTutorial(
            CleanLeavesChore.ToyPickupChoreName.ToUpperInvariant(),
            "Drag each toy into the box.\n" +
            $"Collect all {totalItemsNeeded} toys as quickly as you can!",
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
        if (!SpawnWaste())
        {
            panel.SetActive(false);
            currentChore = null;
            return;
        }

        gameStarted = true;
        elapsedTime = 0f;
        UpdateUI();

        Debug.Log("Toy collection game started!");
    }

    private bool SpawnWaste()
    {
        if (spawner == null)
        {
            Debug.LogError("WasteSpawner not assigned!");
            return false;
        }

        if (spawnArea == null)
        {
            Debug.LogError("Toy spawn area not assigned!");
            return false;
        }

        DropZone boxDropZone = GetBoxDropZone();
        if (boxDropZone == null)
        {
            Debug.LogError("Toy box drop zone not assigned!");
            return false;
        }

        spawnedWaste = spawner.SpawnWaste(totalItemsNeeded, spawnArea, boxDropZone.transform);

        int spawnedCount = 0;
        foreach (WasteObject waste in spawnedWaste)
        {
            if (waste != null)
            {
                waste.SetMiniGame(this);
                spawnedCount++;
            }
        }

        if (spawnedCount != totalItemsNeeded)
        {
            Debug.LogError($"Expected {totalItemsNeeded} toys, but only spawned {spawnedCount}.");
            CleanupWaste();
            return false;
        }

        return true;
    }

    private void Update()
    {
        if (!gameStarted)
            return;

        elapsedTime += Time.deltaTime;
        ReconcileCollectedToys();
        UpdateUI();
        CheckCompletion();
    }

    public bool TryCollectToy(WasteObject waste, DropZone dropZone)
    {
        if (!gameStarted ||
            waste == null ||
            waste.IsCollected ||
            collectedToys.Contains(waste) ||
            dropZone != GetBoxDropZone())
        {
            return false;
        }

        dropZone.StackToy(waste, correctlySegregatedCount);
        collectedToys.Add(waste);
        correctlySegregatedCount = collectedToys.Count;

        SoundEffectManager.Play("Toys");
        Debug.Log($"Toy collected! Progress: {correctlySegregatedCount}/{totalItemsNeeded}");

        UpdateUI();
        CheckCompletion();
        return true;
    }

    public bool TryCollectToyAtScreenPosition(
        WasteObject waste,
        Vector2 screenPosition,
        Camera eventCamera)
    {
        DropZone box = GetBoxDropZone();
        RectTransform boxRect = box != null ? box.transform as RectTransform : null;

        if (boxRect == null ||
            !RectTransformUtility.RectangleContainsScreenPoint(boxRect, screenPosition, eventCamera))
        {
            return false;
        }

        return TryCollectToy(waste, box);
    }

    private DropZone GetBoxDropZone()
    {
        if (boxDropZone != null &&
            boxDropZone.isActiveAndEnabled &&
            boxDropZone.gameObject.activeInHierarchy)
        {
            return boxDropZone;
        }

        if (dropZones != null)
        {
            foreach (DropZone zone in dropZones)
            {
                if (zone != null && zone.isActiveAndEnabled && zone.gameObject.activeInHierarchy)
                    return zone;
            }
        }

        return null;
    }

    private void UpdateUI()
    {
        if (toysRemainingText != null)
            toysRemainingText.text = $"Toys left: {totalItemsNeeded - correctlySegregatedCount}";

        if (progressText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            float seconds = elapsedTime - minutes * 60f;
            progressText.text =
                $"Progress: {correctlySegregatedCount}/{totalItemsNeeded}  |  Time: {minutes:00}:{seconds:00.0}";
        }
    }

    private void CheckCompletion()
    {
        ReconcileCollectedToys();

        if (gameStarted &&
            spawnedWaste != null &&
            collectedToys.Count >= spawnedWaste.Length)
        {
            CompleteGame();
        }
    }

    private void ReconcileCollectedToys()
    {
        if (spawnedWaste == null)
            return;

        DropZone box = GetBoxDropZone();
        if (box == null)
            return;

        foreach (WasteObject toy in spawnedWaste)
        {
            if (toy != null &&
                toy.IsCollected &&
                toy.transform.IsChildOf(box.transform))
            {
                collectedToys.Add(toy);
            }
        }

        correctlySegregatedCount = collectedToys.Count;
    }

    private void CompleteGame()
    {
        if (!gameStarted)
            return;

        gameStarted = false;

        Debug.Log($"Toy collection complete in {elapsedTime:0.0} seconds!");

        panel.SetActive(false);
        CleanupWaste();

        if (currentChore != null)
        {
            DayManager dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
            if (dayManager != null)
            {
                dayManager.ResolveDailyAdditionalQuest(
                    currentChore,
                    elapsedTime <= dayManager.DailyQuestToyPickupTimeLimit);
            }

            Debug.Log($"Completing chore: {currentChore.ChoreName}");
            currentChore.Complete();
            SoundEffectManager.Play("ChoreFinished");
        }

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
