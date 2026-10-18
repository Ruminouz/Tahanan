using System.Collections.Generic;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class DayManager : MonoBehaviour
{
    private const int FinalDay = 7;
    private const float EarliestChoreDeadlineFraction = 0.25f;
    private const float LatestChoreDeadlineFraction = 0.9f;

    public static DayManager Instance;

    [SerializeField] private int currentDay = 1;

    [Header("Debug")]
    [Tooltip("Doubles the speed of gameplay while enabled.")]
    [SerializeField] private bool doubleGameSpeed;

    [Header("Daily Chores")]
    [SerializeField] private Chore washDishes;
    [SerializeField] private Chore mopFloor;
    [SerializeField] private Chore sweepDust;

    [Header("Additional Chores")]
    [SerializeField] private Chore feedDog;
    [SerializeField] private Chore cleanLeaves;
    [SerializeField] private Chore throwTrash;

    [Header("Daily Additional Quest")]
    [SerializeField, Min(0)] private int dailyAdditionalQuestRewardCoins = 5;
    [SerializeField, Min(0.1f)] private float dailyQuestSweepTimeLimit = 45f;
    [SerializeField, Min(0.1f)] private float dailyQuestMopTimeLimit = 45f;
    [SerializeField, Min(0.1f)] private float dailyQuestToyPickupTimeLimit = 30f;
    [SerializeField, Min(0f)] private float dailyQuestDishwashingTimeRemaining = 30f;

    [Header("Garbage System")]
    [SerializeField] private GarbageChore garbageChore;

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

    [Header("Day 2 Introduction")]
    [SerializeField, Min(0.01f)] private float shopCameraMoveDuration = 1.5f;
    [SerializeField, Min(0f)] private float shopDialogueDuration = 4f;
    [SerializeField, Min(0.01f)] private float catCameraMoveDuration = 1f;
    [SerializeField, Min(0f)] private float catCameraHoldDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float catCameraOrthographicSize = 2f;

    [Header("Day 1 Introduction")]
    [SerializeField, Min(0.01f)] private float dayOneCameraMoveDuration = 1f;
    [SerializeField, Min(0.1f)] private float dayOneCameraOrthographicSize = 2.5f;
    [SerializeField, Min(0.1f)] private float dayOneKitchenCameraOrthographicSize = 4.25f;
    [SerializeField] private TMP_FontAsset dayOneIntroFont;

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
    private readonly Dictionary<GameObject, bool> npcActiveStates =
        new Dictionary<GameObject, bool>();
    private bool dayFinished;
    private bool gameOver;
    private bool weekComplete;
    private int dailyCoinsEarned;
    private Chore dailyAdditionalQuestChore;
    private string previousDailyAdditionalQuestName;
    private bool dailyAdditionalQuestCompleted;
    private bool dailyAdditionalQuestResolved;
    private bool dailyAdditionalQuestIsMopping;
    private Coroutine summaryCoroutine;
    private bool summaryRevealComplete;
    private bool transitionInProgress;
    private bool dayStartIntroInProgress;
    private bool dayOneFollowupIntroStarted;
    private bool dayOneWaterTutorialComplete;
    private bool dayOneKitchenIntroStarted;
    private GameObject dayOneIntroOverlay;
    private CinemachineCamera[] dayOneIntroCameras;
    private CinemachineBrain[] dayOneIntroBrains;
    private bool[] dayOneIntroBrainPreviousIgnoreTimeScale;
    private bool dayOneIntroPausedTime;
    private float dayOneIntroPreviousTimeScale = 1f;
    private float normalTimeScale = 1f;
    private bool doubleGameSpeedApplied;

    public int CurrentDay => currentDay;
    public string DailyAdditionalQuestName => dailyAdditionalQuestIsMopping
        ? "Mop Floor"
        : dailyAdditionalQuestChore != null
            ? dailyAdditionalQuestChore.ChoreName
            : string.Empty;
    public string DailyAdditionalQuestGoal => GetDailyAdditionalQuestGoal();
    public bool IsDailyAdditionalQuestCompleted => dailyAdditionalQuestCompleted;
    public bool IsDailyAdditionalQuestMissed => dailyAdditionalQuestIsMopping
        ? waterSpawner != null && waterSpawner.IsMoppingMissed
        : dailyAdditionalQuestChore != null && dailyAdditionalQuestChore.IsMissed;
    public bool IsDailyAdditionalQuestFailed =>
        dailyAdditionalQuestResolved && !dailyAdditionalQuestCompleted;
    public int DailyAdditionalQuestRewardCoins => dailyAdditionalQuestRewardCoins;
    public float DailyQuestSweepTimeLimit => dailyQuestSweepTimeLimit;
    public float DailyQuestMopTimeLimit => dailyQuestMopTimeLimit;
    public float DailyQuestToyPickupTimeLimit => dailyQuestToyPickupTimeLimit;
    public float DailyQuestDishwashingTimeRemaining => dailyQuestDishwashingTimeRemaining;
    // Difficulty is intentionally one-based so every playable day has a
    // distinct level, including Day 1.
    public int CurrentDifficulty => Mathf.Clamp(currentDay, 1, 7);
    public Chore MopFloorChore => mopFloor;
    public Chore SweepDustChore => sweepDust;
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
        currentDay = Mathf.Clamp(currentDay, 1, FinalDay);
        normalTimeScale = Time.timeScale;
        CacheSceneManagers();
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        economyManager = EconomyManager.Instance != null
            ? EconomyManager.Instance
            : FindFirstObjectByType<EconomyManager>();
        SubscribeToEconomy();
        FindSweepDustChore();
        ResolveCleanLeavesChore();

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

    private void ResolveCleanLeavesChore()
    {
        if (cleanLeaves == null)
        {
            cleanLeaves = FindFirstObjectByType<CleanLeavesChore>(
                FindObjectsInactive.Include);
        }
    }

    private void HideSceneNpcs()
    {
        HashSet<GameObject> sceneNpcs = new HashSet<GameObject>(
            GameObject.FindGameObjectsWithTag("NPCS"));
        AddNpcComponents<NPC>(sceneNpcs);
        AddNpcComponents<NPCSocialParticipant>(sceneNpcs);
        AddNpcComponents<MomAI>(sceneNpcs);
        AddNpcComponents<BunsoAI>(sceneNpcs);
        AddNpcComponents<HouseholdHelperAI>(sceneNpcs);
        AddNpcComponents<CatInteraction>(sceneNpcs);

        foreach (GameObject npc in sceneNpcs)
        {
            if (npc == null || npcActiveStates.ContainsKey(npc))
                continue;

            npcActiveStates.Add(npc, npc.activeSelf);
            npc.SetActive(false);
        }
    }

    private static void AddNpcComponents<T>(HashSet<GameObject> sceneNpcs)
        where T : Component
    {
        T[] components = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (T component in components)
        {
            if (component == null)
                continue;

            Transform current = component.transform;
            while (current != null && !current.CompareTag("NPCS"))
                current = current.parent;

            sceneNpcs.Add(current != null ? current.gameObject : component.gameObject);
        }
    }

    private void RestoreSceneNpcs()
    {
        foreach (KeyValuePair<GameObject, bool> npcState in npcActiveStates)
        {
            if (npcState.Key != null)
                npcState.Key.SetActive(npcState.Value);
        }

        npcActiveStates.Clear();
    }

    private void Start()
    {
        if (currentDay == 1)
        {
            HideSceneNpcs();
            dayStartIntroInProgress = true;
            StartCoroutine(StartDayOneWithIntroduction());
        }
        else if (currentDay == 2)
        {
            dayStartIntroInProgress = true;
            StartCoroutine(StartDayTwoWithIntroduction());
        }
        else
        {
            StartDay();
        }
    }

    private void OnDestroy()
    {
        RestoreTimeScale();

        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        ResumeDayOneIntro();

        if (dayOneIntroOverlay != null)
            Destroy(dayOneIntroOverlay);

        if (economyManager != null)
            economyManager.CoinsEarned -= HandleCoinsEarned;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        UpdateGameSpeed();

        if (dayFinished || transitionInProgress || dayStartIntroInProgress)
            return;

        if (currentDay == 1 &&
            dayOneWaterTutorialComplete &&
            !dayOneKitchenIntroStarted &&
            waterSpawner != null &&
            waterSpawner.IsMoppingCompleted)
        {
            dayOneKitchenIntroStarted = true;
            dayStartIntroInProgress = true;
            StartCoroutine(StartDayOneKitchenIntroduction());
            return;
        }

        ExpireChoresAtDeadline();

        if (HasReachedMissedChoreGameOverThreshold)
        {
            TriggerGameOver();
            return;
        }

        if (timeManager != null && timeManager.GetRemainingTime() <= 0f)
        {
            FinishDay();
            return;
        }

        if (HasPendingHudChores())
            return;

        FinishDay();
    }

    public void NotifyDayOneSweepingCompleted()
    {
        if (currentDay != 1 ||
            dayFinished ||
            dayStartIntroInProgress ||
            dayOneFollowupIntroStarted)
            return;

        dayOneFollowupIntroStarted = true;
        dayStartIntroInProgress = true;
        StartCoroutine(StartDayOneFollowupIntroduction());
    }

    private void UpdateGameSpeed()
    {
        if (dayOneIntroPausedTime)
            return;

        if (doubleGameSpeed == doubleGameSpeedApplied)
            return;

        if (doubleGameSpeed)
        {
            normalTimeScale = Time.timeScale;
            Time.timeScale = normalTimeScale * 2f;
            doubleGameSpeedApplied = true;
        }
        else
        {
            RestoreTimeScale();
        }
    }

    private void RestoreTimeScale()
    {
        if (!doubleGameSpeedApplied)
            return;

        Time.timeScale = normalTimeScale;
        doubleGameSpeedApplied = false;
    }

    private void OnDisable()
    {
        RestoreTimeScale();
    }

    private void StartDay()
    {
        ResolveCleanLeavesChore();
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

        AssignDailyAdditionalQuest();
        AssignDailyChoreDeadlines();

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
        EnableChore(cleanLeaves);

        DisableChore(mopFloor);
        DisableChore(throwTrash);

        List<Chore> chores = new List<Chore>();
        AddActiveChore(chores, washDishes);
        AddActiveChore(chores, sweepDust);
        AddActiveChore(chores, feedDog);
        AddActiveChore(chores, cleanLeaves);

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

        activeChores = chores.ToArray();
        StartSweeping();
        Debug.Log("Day " + currentDay + " Loaded");
    }

    private void AddActiveChore(List<Chore> chores, Chore chore)
    {
        if (chore != null && !chores.Contains(chore))
            chores.Add(chore);
    }

    private void AssignDailyChoreDeadlines()
    {
        if (activeChores == null || activeChores.Length == 0)
            return;

        if (!TryGetDailyDeadlineBounds(out float earliestDeadline, out float latestDeadline))
            return;

        List<Chore> chores = new List<Chore>();
        foreach (Chore chore in activeChores)
        {
            if (chore != null && !(chore is GarbageChore))
                chores.Add(chore);
        }

        if (chores.Count == 0)
            return;

        float slotWidth = (latestDeadline - earliestDeadline) / chores.Count;
        List<float> shuffledDeadlines = new List<float>(chores.Count);
        for (int i = 0; i < chores.Count; i++)
        {
            float slotStart = earliestDeadline + slotWidth * i;
            float slotEnd = i == chores.Count - 1
                ? latestDeadline
                : slotStart + slotWidth;
            shuffledDeadlines.Add(Random.Range(slotStart, slotEnd));
        }

        for (int i = shuffledDeadlines.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            float deadline = shuffledDeadlines[i];
            shuffledDeadlines[i] = shuffledDeadlines[swapIndex];
            shuffledDeadlines[swapIndex] = deadline;
        }

        for (int i = 0; i < chores.Count; i++)
            chores[i].SetDailyDeadline(shuffledDeadlines[i]);
    }

    private void AssignDailyAdditionalQuest()
    {
        dailyAdditionalQuestChore = null;
        dailyAdditionalQuestCompleted = false;
        dailyAdditionalQuestResolved = false;
        dailyAdditionalQuestIsMopping = false;

        List<Chore> candidates = new List<Chore>();
        if (activeChores != null)
        {
            foreach (Chore chore in activeChores)
            {
                if (chore != null && !candidates.Contains(chore))
                    candidates.Add(chore);
            }
        }

        if (currentDay >= 2 && cleanLeaves != null && !candidates.Contains(cleanLeaves))
            candidates.Add(cleanLeaves);

        bool moppingIsAvailable = waterSpawner != null &&
            waterSpawner.MaxWaterSpawnsToday > 0;
        if (candidates.Count == 0 && !moppingIsAvailable)
        {
            Debug.LogWarning(
                "Daily additional quest could not be assigned because there are no active chores.",
                this);
            return;
        }

        if (candidates.Count + (moppingIsAvailable ? 1 : 0) > 1 &&
            !string.IsNullOrEmpty(previousDailyAdditionalQuestName))
        {
            List<Chore> differentFromPreviousQuest = candidates.FindAll(chore =>
                !string.Equals(
                    chore.ChoreName,
                    previousDailyAdditionalQuestName,
                    System.StringComparison.OrdinalIgnoreCase));
            bool differentMoppingQuest = moppingIsAvailable &&
                !string.Equals(
                    "Mop Floor",
                    previousDailyAdditionalQuestName,
                    System.StringComparison.OrdinalIgnoreCase);

            if (differentFromPreviousQuest.Count + (differentMoppingQuest ? 1 : 0) > 0)
            {
                candidates = differentFromPreviousQuest;
                moppingIsAvailable = differentMoppingQuest;
            }
        }

        int selectedOption = Random.Range(
            0,
            candidates.Count + (moppingIsAvailable ? 1 : 0));
        if (selectedOption < candidates.Count)
        {
            dailyAdditionalQuestChore = candidates[selectedOption];
            if (dailyAdditionalQuestChore == cleanLeaves)
                EnableChore(cleanLeaves);
        }
        else
        {
            dailyAdditionalQuestIsMopping = true;
        }

        previousDailyAdditionalQuestName = DailyAdditionalQuestName;
        Debug.Log(
            "Daily additional quest: complete " +
            DailyAdditionalQuestName +
            " (" + DailyAdditionalQuestGoal + ")" +
            " for " + dailyAdditionalQuestRewardCoins + " bonus coins.",
            this);
    }

    public bool IsDailyAdditionalQuestFor(Chore chore)
    {
        return chore != null &&
            !dailyAdditionalQuestIsMopping &&
            chore == dailyAdditionalQuestChore;
    }

    public void ResolveDailyAdditionalQuest(Chore chore, bool metChallenge)
    {
        if (!IsDailyAdditionalQuestFor(chore) || dailyAdditionalQuestResolved)
            return;

        dailyAdditionalQuestResolved = true;
        dailyAdditionalQuestCompleted = metChallenge;
        if (metChallenge)
            AwardDailyAdditionalQuestReward();
    }

    public void ResolveDailyMopAdditionalQuest(float elapsedSeconds)
    {
        if (!dailyAdditionalQuestIsMopping || dailyAdditionalQuestResolved)
            return;

        dailyAdditionalQuestResolved = true;
        dailyAdditionalQuestCompleted = elapsedSeconds <= dailyQuestMopTimeLimit;
        if (dailyAdditionalQuestCompleted)
            AwardDailyAdditionalQuestReward();
    }

    private void AwardDailyAdditionalQuestReward()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance != null
                ? EconomyManager.Instance
                : FindFirstObjectByType<EconomyManager>();

        if (economyManager == null)
        {
            Debug.LogError(
                "Daily additional quest was completed, but EconomyManager is unavailable.",
                this);
            return;
        }

        economyManager.AddCoins(dailyAdditionalQuestRewardCoins);
        Debug.Log(
            "Daily additional quest completed! +" +
            dailyAdditionalQuestRewardCoins + " coins.",
            this);
    }

    private string GetDailyAdditionalQuestGoal()
    {
        string questName = DailyAdditionalQuestName;
        if (string.IsNullOrEmpty(questName))
            return string.Empty;

        if (dailyAdditionalQuestIsMopping)
            return "Clean all water from one spill within " +
                dailyQuestMopTimeLimit.ToString("0") + " seconds.";

        if (questName.IndexOf("garbage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("trash", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("segregat", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("basura", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Sort all trash correctly with no mistakes.";

        if (questName.IndexOf("dog", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("feed", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Feed the dog 3 times without a miss.";

        if (questName.IndexOf("sweep", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Sweep all dust within " +
                dailyQuestSweepTimeLimit.ToString("0") + " seconds.";

        if (questName.IndexOf("toy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("leaves", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Put every toy in the box within " +
                dailyQuestToyPickupTimeLimit.ToString("0") + " seconds.";

        if (questName.IndexOf("dish", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Finish with at least " +
                dailyQuestDishwashingTimeRemaining.ToString("0") +
                " seconds left.";

        return "Complete " + questName + " without mistakes.";
    }

    private void AssignRandomDeadline(Chore chore)
    {
        if (chore == null ||
            !TryGetDailyDeadlineBounds(out float earliestDeadline, out float latestDeadline))
            return;

        float earliestAvailableDeadline = Mathf.Max(
            earliestDeadline,
            timeManager.GetTime() + 1f);
        chore.SetDailyDeadline(Random.Range(
            earliestAvailableDeadline,
            Mathf.Max(earliestAvailableDeadline, latestDeadline)));
    }

    public bool StartGarbageBagDeadline(GarbageChore chore)
    {
        if (chore == null ||
            !TryGetDailyDeadlineBounds(out float earliestDeadline, out float latestDeadline))
            return false;

        float deadlineDuration = Random.Range(earliestDeadline, latestDeadline);
        chore.SetDailyDeadline(timeManager.GetTime() + deadlineDuration);
        return true;
    }

    private bool TryGetDailyDeadlineBounds(out float earliestDeadline, out float latestDeadline)
    {
        earliestDeadline = 0f;
        latestDeadline = 0f;
        if (timeManager == null)
        {
            Debug.LogWarning("Daily chore deadlines cannot be randomized because the TimeManager is missing.", this);
            return false;
        }

        float dayLength = timeManager.DayLength;
        earliestDeadline = Mathf.Max(1f, dayLength * EarliestChoreDeadlineFraction);
        latestDeadline = Mathf.Max(
            earliestDeadline + 1f,
            dayLength * LatestChoreDeadlineFraction);
        return true;
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
        AssignRandomDeadline(chore);
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
        weekComplete = currentDay >= FinalDay;
        Debug.Log("DAY " + currentDay + " COMPLETE!");
        MarkUnfinishedChoresAsMissed();
        if (waterSpawner != null)
        {
            if (waterSpawner.MopTaskStarted && !waterSpawner.IsMoppingCompleted)
                waterSpawner.MarkMoppingMissed();

            waterSpawner.StopSpawning();
        }

        gameOver |= HasReachedMissedChoreGameOverThreshold;
        ShowDaySummary();
    }

    private bool HasPendingHudChores()
    {
        if (sweepDust != null &&
            sweepingManager != null &&
            sweepingManager.RemainingDust > 0)
            return true;

        if (garbageChore != null &&
            garbageChore.RemainingBagCount > 0)
            return true;

        if (waterSpawner != null &&
            (waterSpawner.IsSpawning || waterSpawner.RemainingWetAreas > 0))
            return true;

        if (!AllActiveChoresResolved())
            return true;

        if (sweepDust != null &&
            !sweepDust.IsCompleted &&
            !sweepDust.IsMissed &&
            sweepingManager != null &&
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

                if (chore is GarbageChore garbageChore &&
                    (!garbageChore.HasSpawnedBagToday ||
                     garbageChore.RemainingBagCount > 0))
                    continue;

                if (chore == sweepDust &&
                    sweepingManager != null &&
                    sweepingManager.RemainingDust > 0)
                    continue;

                if (chore.RemainingDeadline <= 0f)
                {
                    if (choreManager != null)
                        choreManager.MissChore(chore);
                    else
                        chore.MarkAsMissed();
                }
            }
        }

        if (timeManager != null && timeManager.GetRemainingTime() <= 0f)
        {
            if (waterSpawner != null && waterSpawner.IsSpawning)
            {
                waterSpawner.StopSpawning();
            }

            if (waterSpawner != null && waterSpawner.MopTaskStarted &&
                !waterSpawner.IsMoppingCompleted)
            {
                waterSpawner.MarkMoppingMissed();
                if (suddenTaskManager != null)
                    suddenTaskManager.ResetMopTask();
            }
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
            continueButton.gameObject.SetActive(!gameOver && !weekComplete);
            continueButton.interactable = false;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(gameOver || weekComplete);
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

                    bool garbageWasNotSpawned = chore is GarbageChore garbageChore &&
                        !garbageChore.HasSpawnedBagToday;
                    string choreStatus = chore.IsCompleted
                        ? "COMPLETED"
                        : garbageWasNotSpawned
                            ? "NOT SPAWNED"
                            : chore.IsMissed
                                ? "MISSED"
                                : "INCOMPLETE";
                    string choreMarker = chore.IsCompleted ? "✓ " : "- ";
                    summary.AppendLine(choreMarker + chore.ChoreName + " - " + choreStatus);
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
            else if (weekComplete)
            {
                summary.AppendLine();
                summary.AppendLine("WEEK COMPLETE");
                summary.AppendLine("You completed all 7 days.");
                summaryChoresText.text = summary.ToString();
            }
        }

        summaryRevealComplete = true;
        if (continueButton != null)
            continueButton.interactable = !gameOver && !weekComplete;

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(gameOver || weekComplete);
            mainMenuButton.interactable = gameOver || weekComplete;
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
        if (!dayFinished || gameOver || weekComplete || currentDay >= FinalDay ||
            !summaryRevealComplete || transitionInProgress)
            return;

        transitionInProgress = true;
        StartCoroutine(TransitionToNextDay());
    }

    private IEnumerator TransitionToNextDay()
    {
        yield return Fade(1f);
        ResetAllChores();
        yield return new WaitForSecondsRealtime(1f);
        StartNextDay();
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(false);
        yield return Fade(0f);
        if (currentDay == 2)
        {
            RestoreSceneNpcs();
            yield return PlayDayTwoIntroduction();
        }
        StartDay();
        transitionInProgress = false;
    }

    private IEnumerator StartDayOneWithIntroduction()
    {
        yield return null;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Camera mainCamera = Camera.main;
        if (player == null || mainCamera == null)
        {
            Debug.LogWarning(
                "The Day 1 introduction was skipped because the player or MainCamera is missing.",
                this);
            StartDay();
            dayStartIntroInProgress = false;
            yield break;
        }

        player.ResetToDailySpawn();
        player.SetMovementInputEnabled(false);

        dayOneIntroCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        float originalOrthographicSize = dayOneIntroCameras.Length > 0
            ? dayOneIntroCameras[0].Lens.OrthographicSize
            : mainCamera.orthographicSize;

        SetDayOneCameraFollowTarget(null);

        SetupDay1();
        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        PauseDayOneIntro();

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "WELCOME SA TAHANAN!",
            "Welcome sa Tahanan game! Sa game na ito matututunan mo ang mga pangkaraniwang araw-araw na buhay sa loob ng bahay :D");
        GameHUD hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
            hud.PulseChoreListForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "CHORE LIST",
            "Sa papel na ito nakasulat ang mga task na dapat mong gawin daily. Huwag mong kalimutang gawin kahit isa, magagalit si Nanay!");
        if (hud != null)
            hud.PulseMoodSliderForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "MOOD",
            "Ito naman ang mood ng mga magiging kasama mo sa bahay. Huwag mong hayaang bumaba ito, baka mahirapan ka.");

        DustSpot dustSpot = FindFirstObjectByType<DustSpot>();
        if (dustSpot != null)
        {
            yield return MoveDayOneCameraTo(
                mainCamera,
                dustSpot.transform.position,
                dayOneCameraOrthographicSize,
                dayOneCameraMoveDuration);
        }
        else
        {
            Debug.LogWarning(
                "The Day 1 introduction could not focus on a dust spot because none is available.",
                this);
        }

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "DUST SPOTS",
            "Nako, may mga dumi! Mag-umpisa na tayo maglinis.");

        Component broom = FindFirstObjectByType<BroomChore>();
        if (broom == null)
            broom = FindFirstObjectByType<BroomPickup>();

        if (broom != null)
        {
            yield return MoveDayOneCameraTo(
                mainCamera,
                broom.transform.position,
                dayOneCameraOrthographicSize,
                dayOneCameraMoveDuration);
        }
        else
        {
            Debug.LogWarning(
                "The Day 1 introduction could not focus on the broom because no broom pickup is available.",
                this);
        }

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "WALIS TAMBO",
            "Kunin mo muna ito at i-equip para ma-walisan mo ang dumi.");

        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;
        yield return MoveDayOneCameraTo(
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            dayOneCameraMoveDuration);

        SetDayOneCameraFollowTarget(player.transform);

        StartDay();
        player.SetMovementInputEnabled(true);
        ResumeDayOneIntro();
        dayStartIntroInProgress = false;
    }

    private IEnumerator StartDayOneFollowupIntroduction()
    {
        ResolveWaterSpawner();
        if (dayFinished)
        {
            dayStartIntroInProgress = false;
            yield break;
        }

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Camera mainCamera = Camera.main;
        if (player == null || mainCamera == null)
        {
            Debug.LogWarning(
                "The Day 1 follow-up introduction was skipped because the player or MainCamera is missing.",
                this);
            dayStartIntroInProgress = false;
            yield break;
        }

        if (waterSpawner == null || !waterSpawner.SpawnWaterForTutorial())
        {
            Debug.LogWarning(
                "The Day 1 follow-up introduction was skipped because no tutorial water spot could be spawned.",
                this);
            dayOneWaterTutorialComplete = true;
            dayStartIntroInProgress = false;
            yield break;
        }

        dayOneIntroCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        float originalOrthographicSize = dayOneIntroCameras.Length > 0
            ? dayOneIntroCameras[0].Lens.OrthographicSize
            : mainCamera.orthographicSize;

        player.SetMovementInputEnabled(false);
        SetDayOneCameraFollowTarget(null);

        PauseDayOneIntro();
        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();

        yield return MoveDayOneCameraTo(
            mainCamera,
            waterSpawner.LastWaterSpawnPosition,
            dayOneCameraOrthographicSize,
            dayOneCameraMoveDuration);

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "WATER SPOT",
            "Nako, may natapon na tubig! Puntahan natin at linisin bago may madulas.");

        MopChore mopEquipment = FindFirstObjectByType<MopChore>(
            FindObjectsInactive.Include);
        if (mopEquipment != null)
        {
            yield return MoveDayOneCameraTo(
                mainCamera,
                mopEquipment.transform.position,
                dayOneCameraOrthographicSize,
                dayOneCameraMoveDuration);
        }
        else
        {
            Debug.LogWarning(
                "The Day 1 follow-up introduction could not find the mop equipment.",
                this);
        }

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "MOP",
            "Kunin mo ang mop at i-equip ito para malinis ang natapong tubig.");

        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;
        yield return MoveDayOneCameraTo(
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            dayOneCameraMoveDuration);
        SetDayOneCameraFollowTarget(player.transform);

        player.SetMovementInputEnabled(true);
        ResumeDayOneIntro();
        dayOneWaterTutorialComplete = true;
        dayStartIntroInProgress = false;
    }

    private IEnumerator StartDayOneKitchenIntroduction()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Camera mainCamera = Camera.main;
        Chore dishes = washDishes != null
            ? washDishes
            : FindFirstObjectByType<DishwashingChore>(FindObjectsInactive.Include);
        if (player == null || mainCamera == null || dishes == null)
        {
            Debug.LogWarning(
                "The Day 1 kitchen introduction was skipped because the player, MainCamera, or dishwashing chore is missing.",
                this);
            dayStartIntroInProgress = false;
            yield break;
        }

        dayOneIntroCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        float originalOrthographicSize = dayOneIntroCameras.Length > 0
            ? dayOneIntroCameras[0].Lens.OrthographicSize
            : mainCamera.orthographicSize;

        player.SetMovementInputEnabled(false);
        SetDayOneCameraFollowTarget(null);
        PauseDayOneIntro();
        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        Collider2D kitchenTrigger = dishes.GetComponent<Collider2D>();
        Vector3 kitchenFocusPosition = kitchenTrigger != null
            ? kitchenTrigger.bounds.center
            : dishes.transform.position;

        yield return MoveDayOneCameraTo(
            mainCamera,
            kitchenFocusPosition,
            dayOneKitchenCameraOrthographicSize,
            dayOneCameraMoveDuration);
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "KUSINA",
            "Pagkatapos mag-mop, dito sa kusina ka naman maghuhugas ng mga plato!");

        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;
        yield return MoveDayOneCameraTo(
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            dayOneCameraMoveDuration);
        SetDayOneCameraFollowTarget(player.transform);

        player.SetMovementInputEnabled(true);
        ResumeDayOneIntro();
        dayStartIntroInProgress = false;
    }

    private void PauseDayOneIntro()
    {
        dayOneIntroPreviousTimeScale = Time.timeScale;
        dayOneIntroBrains = FindObjectsByType<CinemachineBrain>(
            FindObjectsSortMode.None);
        dayOneIntroBrainPreviousIgnoreTimeScale =
            new bool[dayOneIntroBrains.Length];
        for (int i = 0; i < dayOneIntroBrains.Length; i++)
        {
            dayOneIntroBrainPreviousIgnoreTimeScale[i] =
                dayOneIntroBrains[i].IgnoreTimeScale;
            dayOneIntroBrains[i].IgnoreTimeScale = true;
        }

        dayOneIntroPausedTime = true;
        Time.timeScale = 0f;
    }

    private void ResumeDayOneIntro()
    {
        if (!dayOneIntroPausedTime)
            return;

        if (dayOneIntroBrains != null)
        {
            for (int i = 0; i < dayOneIntroBrains.Length; i++)
            {
                if (dayOneIntroBrains[i] != null)
                    dayOneIntroBrains[i].IgnoreTimeScale =
                        dayOneIntroBrainPreviousIgnoreTimeScale[i];
            }
        }

        Time.timeScale = dayOneIntroPreviousTimeScale;
        dayOneIntroPausedTime = false;
        dayOneIntroBrains = null;
        dayOneIntroBrainPreviousIgnoreTimeScale = null;
        dayOneIntroCameras = null;
    }

    private void SetDayOneCameraFollowTarget(Transform target)
    {
        if (dayOneIntroCameras == null)
            return;

        foreach (CinemachineCamera cameraRig in dayOneIntroCameras)
        {
            if (cameraRig != null)
                cameraRig.Follow = target;
        }
    }

    private IEnumerator MoveDayOneCameraTo(
        Camera mainCamera,
        Vector3 focusPosition,
        float targetOrthographicSize,
        float duration)
    {
        if (dayOneIntroCameras == null || dayOneIntroCameras.Length == 0)
        {
            yield return MoveCameraTo(
                mainCamera.transform,
                null,
                mainCamera,
                focusPosition,
                targetOrthographicSize,
                duration);
            yield break;
        }

        Vector3[] startPositions = new Vector3[dayOneIntroCameras.Length];
        Vector3[] targetPositions = new Vector3[dayOneIntroCameras.Length];
        float[] startSizes = new float[dayOneIntroCameras.Length];
        for (int i = 0; i < dayOneIntroCameras.Length; i++)
        {
            CinemachineCamera cameraRig = dayOneIntroCameras[i];
            startPositions[i] = cameraRig.transform.position;
            targetPositions[i] = new Vector3(
                focusPosition.x,
                focusPosition.y,
                startPositions[i].z);
            startSizes[i] = cameraRig.Lens.OrthographicSize;
        }

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < dayOneIntroCameras.Length; i++)
            {
                CinemachineCamera cameraRig = dayOneIntroCameras[i];
                if (cameraRig == null)
                    continue;

                cameraRig.transform.position = Vector3.Lerp(
                    startPositions[i],
                    targetPositions[i],
                    progress);
                SetCameraOrthographicSize(
                    cameraRig,
                    mainCamera,
                    Mathf.Lerp(startSizes[i], targetOrthographicSize, progress));
            }

            yield return null;
        }

        for (int i = 0; i < dayOneIntroCameras.Length; i++)
        {
            CinemachineCamera cameraRig = dayOneIntroCameras[i];
            if (cameraRig == null)
                continue;

            cameraRig.transform.position = targetPositions[i];
            SetCameraOrthographicSize(cameraRig, mainCamera, targetOrthographicSize);
        }
    }

    private static GameObject CreateDayOneIntroOverlay(out TMP_Text continuePrompt)
    {
        GameObject canvasObject = new GameObject(
            "Day 1 Introduction Overlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject(
            "Introduction Message Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(canvasObject.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 180f);
        panelRect.sizeDelta = new Vector2(1200f, 440f);
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.04f, 0.04f, 0f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Introduction Message",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(48f, 100f);
        textRect.offsetMax = new Vector2(-48f, -36f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 40f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 26f;
        text.fontSizeMax = 40f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        GameObject promptObject = new GameObject(
            "Continue Prompt",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform promptRect = promptObject.GetComponent<RectTransform>();
        promptRect.SetParent(panelRect, false);
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(1f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.offsetMin = new Vector2(48f, 24f);
        promptRect.offsetMax = new Vector2(-48f, 86f);

        continuePrompt = promptObject.GetComponent<TextMeshProUGUI>();
        continuePrompt.font = TMP_Settings.defaultFontAsset;
        continuePrompt.fontSize = 26f;
        continuePrompt.enableAutoSizing = true;
        continuePrompt.fontSizeMin = 20f;
        continuePrompt.fontSizeMax = 26f;
        continuePrompt.color = Color.white;
        continuePrompt.alignment = TextAlignmentOptions.Center;
        continuePrompt.raycastTarget = false;

        return canvasObject;
    }

    private IEnumerator ShowDayOneIntroText(
        TMP_Text text,
        TMP_Text continuePrompt,
        string title,
        string message)
    {
        text.font = dayOneIntroFont;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        text.text = "<b>" + title + "</b>\n\n" + message;
        text.ForceMeshUpdate();
        text.maxVisibleCharacters = 0;

        continuePrompt.font = dayOneIntroFont;
        continuePrompt.outlineWidth = 0.2f;
        continuePrompt.outlineColor = Color.black;
        continuePrompt.text = "Press Space or Esc to continue";
        Vector3 originalScale = continuePrompt.rectTransform.localScale;
        float revealElapsed = 0f;
        float pulseElapsed = 0f;
        int wordIndex = 0;
        bool skippedTyping = false;
        const float wordRevealInterval = 0.16f;

        while (wordIndex < text.textInfo.wordCount)
        {
            if (WasDayOneIntroContinuePressed())
            {
                text.maxVisibleCharacters = text.textInfo.characterCount;
                skippedTyping = true;
                break;
            }

            float deltaTime = Time.unscaledDeltaTime;
            revealElapsed += deltaTime;
            pulseElapsed += deltaTime;
            if (revealElapsed >= wordRevealInterval)
            {
                revealElapsed -= wordRevealInterval;
                text.maxVisibleCharacters =
                    text.textInfo.wordInfo[wordIndex].lastCharacterIndex + 1;
                wordIndex++;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(pulseElapsed * Mathf.PI * 2f);
            continuePrompt.rectTransform.localScale = originalScale * (1f + pulse * 0.12f);
            yield return null;
        }

        text.maxVisibleCharacters = text.textInfo.characterCount;
        if (skippedTyping)
            yield return null;

        while (!WasDayOneIntroContinuePressed())
        {
            pulseElapsed += Time.unscaledDeltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(pulseElapsed * Mathf.PI * 2f);
            continuePrompt.rectTransform.localScale = originalScale * (1f + pulse * 0.12f);
            yield return null;
        }

        continuePrompt.rectTransform.localScale = originalScale;
    }

    private static bool WasDayOneIntroContinuePressed()
    {
        return (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.escapeKey.wasPressedThisFrame));
    }

    private IEnumerator StartDayTwoWithIntroduction()
    {
        yield return PlayDayTwoIntroduction();
        StartDay();
        dayStartIntroInProgress = false;
    }

    private IEnumerator PlayDayTwoIntroduction()
    {
        ShopTrigger shop = FindFirstObjectByType<ShopTrigger>();
        if (shop == null)
        {
            Debug.LogWarning("The Day 2 introduction was skipped because no ShopTrigger exists in the scene.", this);
            yield break;
        }

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Camera mainCamera = Camera.main;
        CinemachineCamera cinematicCamera = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        if (player == null || mainCamera == null)
        {
            Debug.LogWarning(
                "The Day 2 introduction was skipped because the player or MainCamera is missing.",
                this);
            yield break;
        }

        player.ResetToDailySpawn();
        Transform cameraRig = cinematicCamera != null
            ? cinematicCamera.transform
            : mainCamera.transform;
        float originalOrthographicSize = cinematicCamera != null
            ? cinematicCamera.Lens.OrthographicSize
            : mainCamera.orthographicSize;

        player.SetMovementInputEnabled(false);
        if (cinematicCamera != null)
            cinematicCamera.Follow = null;

        yield return MoveCameraTo(
            cameraRig,
            cinematicCamera,
            mainCamera,
            shop.transform.position,
            originalOrthographicSize,
            shopCameraMoveDuration);

        SpeechBubbleCanvas.Show(
            shop.transform,
            "sa tindahan na ito pwede ka makabili ng mas magandang mga gamit if mayroon kang sapat na pera",
            shopDialogueDuration,
            new Vector2(480f, 104f),
            18);
        yield return WaitForRealtime(shopDialogueDuration);

        if (economyManager != null && economyManager.HasPurchasedCat)
        {
            CatInteraction cat = FindFirstObjectByType<CatInteraction>(
                FindObjectsInactive.Include);
            if (cat != null && cat.gameObject.activeInHierarchy)
            {
                yield return MoveCameraTo(
                    cameraRig,
                    cinematicCamera,
                    mainCamera,
                    cat.transform.position,
                    catCameraOrthographicSize,
                    catCameraMoveDuration);
                yield return WaitForRealtime(catCameraHoldDuration);
            }
            else
            {
                Debug.LogWarning(
                    "The cat was purchased, but no active cat was available for the Day 2 camera focus.",
                    this);
            }
        }

        yield return MoveCameraTo(
            cameraRig,
            cinematicCamera,
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            shopCameraMoveDuration);

        if (cinematicCamera != null)
            cinematicCamera.Follow = player.transform;
        player.SetMovementInputEnabled(true);
    }

    private IEnumerator MoveCameraTo(
        Transform cameraRig,
        CinemachineCamera cinematicCamera,
        Camera mainCamera,
        Vector3 focusPosition,
        float targetOrthographicSize,
        float duration)
    {
        Vector3 startPosition = cameraRig.position;
        Vector3 targetPosition = new Vector3(
            focusPosition.x,
            focusPosition.y,
            startPosition.z);
        float startOrthographicSize = cinematicCamera != null
            ? cinematicCamera.Lens.OrthographicSize
            : mainCamera.orthographicSize;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cameraRig.position = Vector3.Lerp(startPosition, targetPosition, progress);
            SetCameraOrthographicSize(
                cinematicCamera,
                mainCamera,
                Mathf.Lerp(startOrthographicSize, targetOrthographicSize, progress));
            yield return null;
        }

        cameraRig.position = targetPosition;
        SetCameraOrthographicSize(cinematicCamera, mainCamera, targetOrthographicSize);
    }

    private static void SetCameraOrthographicSize(
        CinemachineCamera cinematicCamera,
        Camera mainCamera,
        float orthographicSize)
    {
        if (cinematicCamera != null)
        {
            LensSettings lens = cinematicCamera.Lens;
            lens.OrthographicSize = orthographicSize;
            cinematicCamera.Lens = lens;
        }
        else
        {
            mainCamera.orthographicSize = orthographicSize;
        }
    }

    private static IEnumerator WaitForRealtime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public void ReturnToMainMenu()
    {
        if (!gameOver && !weekComplete)
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
            elapsed += Time.unscaledDeltaTime;
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

            if (chore is GarbageChore garbageChore &&
                !garbageChore.HasSpawnedBagToday)
                continue;

            if (choreManager != null)
                choreManager.MissChore(chore);
            else
                chore.MarkAsMissed();
        }
    }

    private void StartNextDay()
    {
        if (currentDay >= FinalDay)
        {
            weekComplete = true;
            return;
        }

        CancelInvoke();
        currentDay++;

        if (choreManager != null)
            choreManager.ResetDailyProgress();

        if (moodManager != null)
            moodManager.ResetDailyMood();

        gameOver = false;
        dayFinished = false;
        summaryRevealComplete = false;
        Debug.Log("Starting Next Day: " + currentDay);
        if (currentDay != 2)
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
