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
    [SerializeField, Min(0f)] private float summaryLineDelay = 0.25f;

    [Header("Game Over")]
    [Tooltip("Maximum chores that may be missed before game over. Element 0 is Day 1 and element 6 is Day 7.")]
    [Min(0)]
    [SerializeField] private int[] missedChoresGameOverThresholdByDay = { 5, 5, 5, 5, 5, 5, 5 };

    private TimeManager timeManager;
    private ChoreManager choreManager;
    private SuddenTaskManager suddenTaskManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;
    private MoodManager moodManager;
    private EconomyManager economyManager;

    private Chore[] activeChores;
    private readonly List<Chore> dynamicChores = new List<Chore>();
    private bool dayFinished;
    private bool gameOver;
    private int dailyCoinsEarned;
    private Coroutine summaryCoroutine;
    private bool summaryRevealComplete;

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
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        economyManager = EconomyManager.Instance != null
            ? EconomyManager.Instance
            : FindFirstObjectByType<EconomyManager>();
        SubscribeToEconomy();
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

        if (economyManager != null)
            economyManager.CoinsEarned -= HandleCoinsEarned;

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

        if (economyManager == null)
            economyManager = EconomyManager.Instance != null
                ? EconomyManager.Instance
                : FindFirstObjectByType<EconomyManager>();
        SubscribeToEconomy();

        if (moodManager == null)
            moodManager = MoodManager.Instance != null
                ? MoodManager.Instance
                : FindFirstObjectByType<MoodManager>();

        dailyCoinsEarned = 0;
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

        BunsoBoostInventory boostInventory = FindFirstObjectByType<BunsoBoostInventory>();
        if (boostInventory != null)
            boostInventory.ResetForDay();

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
        for (int i = dynamicChores.Count - 1; i >= 0; i--)
        {
            Chore dynamicChore = dynamicChores[i];
            if (dynamicChore != null)
                Destroy(dynamicChore.gameObject);
        }
        dynamicChores.Clear();

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

    public void RegisterDynamicChore(Chore chore)
    {
        if (chore == null || dynamicChores.Contains(chore))
            return;

        dynamicChores.Add(chore);
        List<Chore> chores = activeChores != null
            ? new List<Chore>(activeChores)
            : new List<Chore>();
        AddActiveChore(chores, chore);
        activeChores = chores.ToArray();
    }

    public void UnregisterDynamicChore(Chore chore)
    {
        if (chore == null)
            return;

        dynamicChores.Remove(chore);
        if (activeChores == null)
            return;

        List<Chore> chores = new List<Chore>(activeChores);
        chores.Remove(chore);
        activeChores = chores.ToArray();
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

        float successRate = choreManager != null ? choreManager.SuccessRate : 0f;
        int totalPoints = choreManager != null ? choreManager.totalPoints : 0;
        HouseholdMood endingMood = moodManager != null
            ? moodManager.CurrentMood
            : HouseholdMood.Concerned;

        if (summarySuccessRateText != null)
            summarySuccessRateText.text = "Success Rate: " + successRate.ToString("0") + "%";

        if (summaryPointsText != null)
        {
            summaryPointsText.text = "Points Earned: " + totalPoints
                + "\nCoins Earned: " + dailyCoinsEarned
                + "\nFamily Mood: " + FormatMood(endingMood);
        }

        summaryRevealComplete = false;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(!gameOver);
            continueButton.interactable = false;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(false);
            mainMenuButton.interactable = false;
        }

        if (summaryCoroutine != null)
            StopCoroutine(summaryCoroutine);

        summaryCoroutine = StartCoroutine(RevealDaySummary(endingMood));
    }

    private IEnumerator RevealDaySummary(HouseholdMood endingMood)
    {
        if (summaryChoresText != null)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("DAY " + currentDay + " COMPLETE");
            summary.AppendLine();
            summary.AppendLine("Chores finished:");
            summaryChoresText.text = summary.ToString();

            if (activeChores != null)
            {
                foreach (Chore chore in activeChores)
                {
                    if (chore == null)
                        continue;

                    summary.AppendLine((chore.IsCompleted ? "✓ " : "- ")
                        + chore.ChoreName
                        + (chore.IsCompleted ? " - COMPLETED" : " - MISSED"));
                    summaryChoresText.text = summary.ToString();
                    yield return new WaitForSecondsRealtime(summaryLineDelay);
                }
            }

            if (waterSpawner != null && waterSpawner.MopTaskStarted)
            {
                summary.AppendLine((waterSpawner.IsMoppingCompleted ? "✓ " : "- ")
                    + "Mop Floor"
                    + (waterSpawner.IsMoppingCompleted ? " - COMPLETED" : " - MISSED"));
                summaryChoresText.text = summary.ToString();
                yield return new WaitForSecondsRealtime(summaryLineDelay);
            }

            if (sweepingManager != null)
            {
                summary.AppendLine("Dust swept: "
                    + sweepingManager.CleanedDustCount + "/"
                    + sweepingManager.TotalDustSpawned);
                summaryChoresText.text = summary.ToString();
                yield return new WaitForSecondsRealtime(summaryLineDelay);
            }

            if (waterSpawner != null)
            {
                summary.AppendLine("Water mopped: "
                    + waterSpawner.CleanedWaterCount + "/"
                    + waterSpawner.TotalWaterSpawned);
                summaryChoresText.text = summary.ToString();
                yield return new WaitForSecondsRealtime(summaryLineDelay);
            }

            summary.AppendLine();
            summary.AppendLine("Family mood: " + FormatMood(endingMood));
            summaryChoresText.text = summary.ToString();
            yield return new WaitForSecondsRealtime(summaryLineDelay);

            if (gameOver)
            {
                summary.AppendLine();
                summary.AppendLine("GAME OVER");
                summary.AppendLine("Too many chores were missed.");
                summaryChoresText.text = summary.ToString();
            }
        }

        summaryRevealComplete = true;
        if (continueButton != null)
            continueButton.interactable = !gameOver;

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(gameOver);
            mainMenuButton.interactable = gameOver;
        }
    }

    private string FormatMood(HouseholdMood mood)
    {
        switch (mood)
        {
            case HouseholdMood.Calm:
                return "HAPPY";
            case HouseholdMood.Angry:
                return "ANGRY";
            default:
                return "CONCERNED";
        }
    }

    private void SubscribeToEconomy()
    {
        if (economyManager == null)
            return;

        economyManager.CoinsEarned -= HandleCoinsEarned;
        economyManager.CoinsEarned += HandleCoinsEarned;
    }

    private void HandleCoinsEarned(int amount)
    {
        dailyCoinsEarned += Mathf.Max(0, amount);
    }

    public void ContinueToNextDay()
    {
        if (!dayFinished || gameOver || !summaryRevealComplete)
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

        if (moodManager != null)
            moodManager.ResetDailyMood();

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
