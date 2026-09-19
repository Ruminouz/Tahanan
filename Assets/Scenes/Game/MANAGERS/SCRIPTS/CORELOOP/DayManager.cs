using System.Collections.Generic;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    [SerializeField] private int currentDay = 1;

    [Header("Daily Chores")]
    [SerializeField] private Chore washDishes;
    [SerializeField] private Chore mopFloor;
    [SerializeField] private Chore sweepDust;

    [Header("Additional Chores")]
    [SerializeField] private Chore feedDog;
    [SerializeField] private Chore cleanLeaves;
    [SerializeField] private Chore throwTrash;

    [Header("Garbage System")]
    [SerializeField] private GarbageChore garbageChore;

    [Header("Segregation System")]
    [SerializeField] private Chore segregateWasteChore;

    [Header("Day Transition UI")]
    [SerializeField] private CanvasGroup transitionFade;
    [SerializeField] private GameObject daySummaryPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TMP_Text summaryChoresText;
    [SerializeField] private TMP_Text summarySuccessRateText;
    [SerializeField] private TMP_Text summaryPointsText;
    [SerializeField] private float transitionDuration = 0.5f;

    [Header("Game Over")]
    [Tooltip("Maximum chores that may be missed before game over. Element 0 is Day 1 and element 6 is Day 7.")]
    [Min(0)]
    [SerializeField] private int[] missedChoresGameOverThresholdByDay = { 5, 5, 5, 5, 5, 5, 5 };

    private TimeManager timeManager;
    private ChoreManager choreManager;
    private SuddenTaskManager suddenTaskManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;

    private Chore[] activeChores;
    private bool dayFinished;
    private bool gameOver;

    public int CurrentDay => currentDay;
    // Difficulty is intentionally one-based so every playable day has a
    // distinct level, including Day 1.
    public int CurrentDifficulty => Mathf.Clamp(currentDay, 1, 7);
    public Chore MopFloorChore => mopFloor;
    public Chore SweepDustChore => sweepDust;
    public Chore SegregateWasteChore => segregateWasteChore;
    public bool IsGameOver => gameOver;
    public bool HasReachedMissedChoreGameOverThreshold => choreManager != null &&
        choreManager.missedChores >= GetMissedChoreGameOverThreshold();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheSceneManagers();
        FindSweepDustChore();
        ResolveSegregateWasteChore();

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void CacheSceneManagers()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        suddenTaskManager = FindFirstObjectByType<SuddenTaskManager>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
    }

    private void ResolveSegregateWasteChore()
    {
        if (segregateWasteChore == null)
        {
            segregateWasteChore = FindFirstObjectByType<SegregateWasteChore>(
                FindObjectsInactive.Include);

            if (segregateWasteChore == null)
            {
                // Older scenes stored the segregation entry point as the
                // legacy CleanLeavesChore component.
                segregateWasteChore = FindFirstObjectByType<CleanLeavesChore>(
                    FindObjectsInactive.Include);
            }
        }
    }

    private void Start()
    {
        StartDay();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (dayFinished || choreManager == null)
            return;

        ExpireChoresAtDeadline();

        if (HasPendingHudChores())
            return;

        FinishDay();
    }

    private void StartDay()
    {
        ResolveSegregateWasteChore();
        ResolveWaterSpawner();

        Debug.Log("=== START DAY " + currentDay + " ===");

        ResetDayDependencies();
        ResetAllChores();
        SetupGarbage();

        if (currentDay == 1)
            SetupDay1();
        else
            SetupDay2To7();

        if (waterSpawner != null)
        {
            waterSpawner.StartSpawning();
            Debug.Log("Water Event Started Day " + currentDay);
        }

        Debug.Log("=== DAY START COMPLETE ===");
    }

    private void ResolveWaterSpawner()
    {
        if (waterSpawner == null)
            waterSpawner = FindFirstObjectByType<WaterSpawner>();
    }

    private void ResetDayDependencies()
    {
        ResetPlayerAndTools();

        if (timeManager != null)
        {
            timeManager.ResetDayTimer();
            Debug.Log("Timer Reset");
        }

        if (waterSpawner != null)
        {
            waterSpawner.ResetDailyMop();
            Debug.Log("Water Reset");
        }

        ResetMoppingMinigames();

        if (suddenTaskManager != null)
        {
            suddenTaskManager.ResetMopTask();
            Debug.Log("Mop Task Reset");
        }
    }

    private void ResetPlayerAndTools()
    {
        PlayerEquipmentInventory inventory = FindFirstObjectByType<PlayerEquipmentInventory>();
        if (inventory != null)
            inventory.ResetForDay();

        GarbageCarry garbageCarry = FindFirstObjectByType<GarbageCarry>();
        if (garbageCarry != null)
            garbageCarry.ResetForDay();

        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.ResetToDailySpawn();

        MoppingPlayerState moppingState = FindFirstObjectByType<MoppingPlayerState>();
        if (moppingState != null)
            moppingState.ResetMop();

        SweepingPlayerState sweepingState = FindFirstObjectByType<SweepingPlayerState>();
        if (sweepingState != null)
            sweepingState.ResetBroom();

        PlayerTool playerTool = FindFirstObjectByType<PlayerTool>();
        if (playerTool != null)
            playerTool.ResetTool();

        MopChore[] mopChores = FindObjectsByType<MopChore>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (MopChore mopChore in mopChores)
        {
            if (mopChore != null)
                mopChore.ResetForDay();
        }

        BroomChore[] broomChores = FindObjectsByType<BroomChore>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (BroomChore broomChore in broomChores)
        {
            if (broomChore != null)
                broomChore.ResetForDay();
        }

        BroomPickup[] broomPickups = FindObjectsByType<BroomPickup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (BroomPickup broomPickup in broomPickups)
        {
            if (broomPickup != null)
                broomPickup.ResetForDay();
        }
    }

    private void SetupGarbage()
    {
        if (garbageChore == null)
        {
            Debug.LogWarning("GarbageChore reference missing!");
            return;
        }

        if (currentDay >= 2)
        {
            garbageChore.ResetChore();
            Debug.Log("Garbage Sorting ENABLED Day " + currentDay);
        }
        else
        {
            garbageChore.DisableChore();
            Debug.Log("Garbage Sorting LOCKED Day " + currentDay);
        }
    }

    private void SetupDay1()
    {
        Debug.Log("Loading Day 1");

        EnableChore(washDishes);
        EnableChore(sweepDust);

        DisableChore(mopFloor);
        DisableChore(feedDog);
        DisableChore(cleanLeaves);
        DisableChore(throwTrash);
        DisableChore(segregateWasteChore);

        List<Chore> chores = new List<Chore>();
        AddActiveChore(chores, washDishes);
        AddActiveChore(chores, sweepDust);
        activeChores = chores.ToArray();
        StartSweeping();
        Debug.Log("Day 1 Loaded");
    }

    private void SetupDay2To7()
    {
        Debug.Log("Loading Day " + currentDay);

        EnableChore(washDishes);
        EnableChore(sweepDust);
        EnableChore(feedDog);
        // Leaves are handled by the segregation mini-game. Keep the legacy
        // clean-leaves reference for scene compatibility, but never register
        // it as a second daily chore.
        DisableChore(cleanLeaves);

        DisableChore(mopFloor);
        DisableChore(throwTrash);
        DisableChore(segregateWasteChore);

        List<Chore> chores = new List<Chore>();
        AddActiveChore(chores, washDishes);
        AddActiveChore(chores, sweepDust);
        AddActiveChore(chores, feedDog);

        if (garbageChore != null && currentDay >= 2)
        {
            EnableChore(garbageChore);
            AddActiveChore(chores, garbageChore);
            Debug.Log("Garbage Chore Enabled Day " + currentDay);
        }
        else
        {
            DisableChore(garbageChore);
        }

        if (segregateWasteChore != null && currentDay >= 2 && currentDay <= 7)
        {
            EnableChore(segregateWasteChore);
            AddActiveChore(chores, segregateWasteChore);
            Debug.Log("Segregate Waste Enabled Day " + currentDay);
        }
        else
        {
            DisableChore(segregateWasteChore);
        }

        activeChores = chores.ToArray();
        StartSweeping();
        Debug.Log("Day " + currentDay + " Loaded");
    }

    private void AddActiveChore(List<Chore> chores, Chore chore)
    {
        if (chore != null && !chores.Contains(chore))
            chores.Add(chore);
    }

    private void StartSweeping()
    {
        if (sweepingManager == null)
            return;

        if (sweepDust != null)
            sweepingManager.SetSweepDustChore(sweepDust);

        sweepingManager.StartSweepingTask();
    }

    private void EnableChore(Chore chore)
    {
        if (chore != null)
            chore.ResetChore();
    }

    private void DisableChore(Chore chore)
    {
        if (chore != null)
            chore.DisableChore();
    }

    private void ResetAllChores()
    {
        DisableChore(washDishes);
        DisableChore(mopFloor);
        DisableChore(sweepDust);
        DisableChore(feedDog);
        DisableChore(cleanLeaves);
        DisableChore(throwTrash);
        DisableChore(garbageChore);
        DisableChore(segregateWasteChore);
    }

    public Chore[] GetActiveChores()
    {
        return activeChores;
    }

    private int GetMissedChoreGameOverThreshold()
    {
        if (missedChoresGameOverThresholdByDay == null ||
            missedChoresGameOverThresholdByDay.Length == 0)
            return 5;

        int dayIndex = Mathf.Clamp(currentDay - 1, 0, missedChoresGameOverThresholdByDay.Length - 1);
        return missedChoresGameOverThresholdByDay[dayIndex];
    }

    public void TriggerGameOver()
    {
        if (dayFinished || gameOver)
            return;

        gameOver = true;
        FinishDay();
    }

    private bool AllActiveChoresResolved()
    {
        if (activeChores == null || activeChores.Length == 0)
            return true;

        foreach (Chore chore in activeChores)
        {
            if (chore == null)
                continue;

            if (!chore.IsCompleted && !chore.IsMissed)
                return false;
        }

        return true;
    }

    private void FinishDay()
    {
        if (dayFinished)
            return;

        dayFinished = true;
        Debug.Log("DAY " + currentDay + " COMPLETE!");
        MarkUnfinishedChoresAsMissed();
        if (waterSpawner != null)
            waterSpawner.StopSpawning();

        gameOver = HasReachedMissedChoreGameOverThreshold;
        ShowDaySummary();
    }

    private bool HasPendingHudChores()
    {
        if (!AllActiveChoresResolved())
            return true;

        if (sweepingManager != null &&
            !sweepingManager.IsSweepingCompleted)
            return true;

        return waterSpawner != null &&
            (waterSpawner.IsSpawning ||
             (waterSpawner.MopTaskStarted &&
              !waterSpawner.IsMoppingCompleted && !waterSpawner.IsMoppingMissed));
    }

    private void ExpireChoresAtDeadline()
    {
        if (activeChores != null)
        {
            foreach (Chore chore in activeChores)
            {
                if (chore == null || chore.IsCompleted || chore.IsMissed)
                    continue;

                if (chore.RemainingDeadline <= 0f)
                    choreManager.MissChore(chore);
            }
        }

        if (timeManager != null && timeManager.GetRemainingTime() <= 0f)
        {
            if (waterSpawner != null && waterSpawner.MopTaskStarted &&
                !waterSpawner.IsMoppingCompleted)
            {
                waterSpawner.MarkMoppingMissed();
                if (suddenTaskManager != null)
                    suddenTaskManager.ResetMopTask();
            }

            if (waterSpawner != null)
                waterSpawner.StopSpawning();
        }
    }

    private void ShowDaySummary()
    {
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(true);

        if (summaryChoresText != null)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Chores");

            if (activeChores != null)
            {
                foreach (Chore chore in activeChores)
                {
                    if (chore == null)
                        continue;

                    string marker = chore.IsCompleted ? "✓ " : "- ";
                    string status = chore.IsCompleted ? "COMPLETED" : "MISSED";
                    summary.AppendLine(marker + chore.ChoreName + " - " + status);
                }
            }

            if (waterSpawner != null && waterSpawner.MopTaskStarted)
            {
                string marker = waterSpawner.IsMoppingCompleted ? "✓ " : "- ";
                string status = waterSpawner.IsMoppingCompleted ? "COMPLETED" : "MISSED";
                summary.AppendLine(marker + "Mop Floor - " + status);
            }

            if (sweepingManager != null)
            {
                summary.AppendLine(
                    "Dust swept: " + sweepingManager.CleanedDustCount + "/" +
                    sweepingManager.TotalDustSpawned);
            }

            if (waterSpawner != null)
            {
                summary.AppendLine(
                    "Water mopped: " + waterSpawner.CleanedWaterCount + "/" +
                    waterSpawner.TotalWaterSpawned);
            }

            summaryChoresText.text = summary.ToString();
        }

        float successRate = choreManager != null ? choreManager.SuccessRate : 0f;
        int totalPoints = choreManager != null ? choreManager.totalPoints : 0;

        if (summarySuccessRateText != null)
            summarySuccessRateText.text = "Success Rate: " + successRate.ToString("0") + "%";

        if (summaryPointsText != null)
            summaryPointsText.text = "Points Earned: " + totalPoints;

        if (continueButton != null)
            continueButton.gameObject.SetActive(!gameOver);

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(gameOver);

        if (gameOver && summaryChoresText != null)
            summaryChoresText.text += "\nGAME OVER\nToo many chores were missed.";
    }

    public void ContinueToNextDay()
    {
        if (!dayFinished || gameOver)
            return;

        StartCoroutine(TransitionToNextDay());
    }

    private IEnumerator TransitionToNextDay()
    {
        yield return Fade(1f);
        ResetAllChores();
        yield return new WaitForSeconds(1f);
        StartNextDay();
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(false);
        yield return Fade(0f);
    }

    public void ReturnToMainMenu()
    {
        if (!gameOver)
            return;

        SceneManager.LoadScene(0);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (transitionFade == null)
            yield break;

        float startAlpha = transitionFade.alpha;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            transitionFade.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / transitionDuration);
            yield return null;
        }

        transitionFade.alpha = targetAlpha;
        transitionFade.blocksRaycasts = targetAlpha > 0f;
    }

    private void MarkUnfinishedChoresAsMissed()
    {
        if (activeChores == null)
            return;

        foreach (Chore chore in activeChores)
        {
            if (chore == null || chore.IsCompleted || chore.IsMissed)
                continue;

            if (choreManager != null)
                choreManager.MissChore(chore);
            else
                chore.MarkAsMissed();
        }
    }

    private void StartNextDay()
    {
        CancelInvoke();
        currentDay++;

        if (choreManager != null)
            choreManager.ResetDailyProgress();

        gameOver = false;
        dayFinished = false;
        Debug.Log("Starting Next Day: " + currentDay);
        StartDay();
    }

    private void ResetMoppingMinigames()
    {
        MoppingMinigame[] minigames = FindObjectsByType<MoppingMinigame>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (MoppingMinigame minigame in minigames)
        {
            if (minigame != null)
                minigame.ResetMopping();
        }
    }

    private void FindSweepDustChore()
    {
        if (sweepDust != null)
            return;

        Chore[] chores = FindObjectsByType<Chore>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Chore chore in chores)
        {
            if (chore.ChoreName == "Sweep Dust")
            {
                sweepDust = chore;
                return;
            }
        }
    }
}
