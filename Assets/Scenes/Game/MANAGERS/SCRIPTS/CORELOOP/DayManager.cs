using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using Random = UnityEngine.Random;
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
    private static readonly Color WeekCompleteSummaryTextColor =
        new Color32(46, 125, 50, 255);
    private static readonly Color WeekCompleteSummaryOutlineColor =
        new Color32(0, 0, 0, 255);
    private static Sprite shopWaypointArrowSprite;

    public static DayManager Instance;

    [SerializeField] private int currentDay = 1;
    [SerializeField] private GameObject day1Wall;

    [Header("Debug")]
    [Tooltip("Doubles the speed of gameplay while enabled.")]
    [SerializeField] private bool doubleGameSpeed;
    [Tooltip("Opens the day-win summary immediately without changing chore progress or score. One-shot.")]
    [SerializeField] private bool debugInstantWin;

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
    [SerializeField, Min(0.1f)] private float dailyQuestSweepTimeLimit = 30f;
    [SerializeField, Min(0.1f)] private float dailyQuestMopTimeLimit = 30f;
    [SerializeField, Min(0f)] private float dailyQuestDishwashingTimeBeforeDeadline = 60f;

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
    [SerializeField, Min(0f)] private float transitionCardHoldDuration = 1f;

    [Header("Week Ending")]
    [SerializeField] private GameObject day7WinView;
    [SerializeField, Tooltip("Frames used to animate the successful Day 7 summary panel.")]
    private Sprite[] weekCompleteSummaryPanelFrames = new Sprite[5];
    [SerializeField, Min(1f)] private float weekCompleteSummaryPanelFramesPerSecond = 5f;
    [SerializeField, Min(0.01f)] private float weekEndingCameraMoveDuration = 1.5f;
    [SerializeField, Min(0f)] private float weekEndingStoryDuration = 4f;
    [SerializeField, Min(1f)] private float weekEndingCameraPadding = 1.25f;

    [Header("Day Win Celebration")]
    [SerializeField] private Image dayWinCelebrationImage;
    [SerializeField] private Image[] additionalDayWinCelebrationImages = new Image[6];
    [SerializeField] private Sprite[] dayWinCelebrationFrames = new Sprite[8];
    [SerializeField, Min(1f)] private float dayWinCelebrationFramesPerSecond = 12f;

    [Header("Day 2 Introduction")]
    [SerializeField, Min(0.01f)] private float shopCameraMoveDuration = 1.5f;
    [SerializeField, Min(0f)] private float shopDirectionsDuration = 7f;
    [SerializeField, Min(0.25f)] private float shopWaypointArrowSpacing = 1.2f;
    [SerializeField, Min(0.1f)] private float shopWaypointArrowScale = 0.65f;
    [SerializeField, Min(0.1f)] private float shopWaypointReachDistance = 1.5f;
    [SerializeField, Min(0.05f)] private float shopWaypointRefreshInterval = 0.2f;
    [SerializeField, Min(0.01f)] private float catCameraMoveDuration = 1f;
    [SerializeField, Min(0f)] private float catCameraHoldDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float catCameraOrthographicSize = 2f;

    [Header("Day 1 Introduction")]
    [SerializeField, Min(0.01f)] private float dayOneCameraMoveDuration = 1f;
    [SerializeField, Min(0.1f)] private float dayOneCameraOrthographicSize = 2.5f;
    [SerializeField, Min(0.1f)] private float dayOneKitchenCameraOrthographicSize = 4.25f;
    [SerializeField] private TMP_FontAsset dayOneIntroFont;

    [Header("Family Story")]
    [SerializeField] private Transform tricycle;
    [SerializeField] private Transform dayFourNanayArrivalSpawnPoint;
    [SerializeField, Min(0.1f)] private float parentWalkSpeed = 2f;
    [SerializeField, Min(0.05f)] private float parentWalkArrivalDistance = 0.2f;
    [SerializeField, Min(0.1f)] private float parentWalkTimeout = 30f;

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
    private readonly Dictionary<Canvas, bool> temporarilyHiddenCanvasEnabledStates =
        new Dictionary<Canvas, bool>();
    private readonly Dictionary<GraphicRaycaster, bool> temporarilyHiddenRaycasterEnabledStates =
        new Dictionary<GraphicRaycaster, bool>();
    private readonly Dictionary<GameObject, Vector3> parentHomePositions =
        new Dictionary<GameObject, Vector3>();
    private bool dayFinished;
    private bool gameOver;
    private bool weekComplete;
    private int dailyCoinsEarned;
    private int bunsoBadMoodChoresDroppedToday;
    private Chore dailyAdditionalQuestChore;
    private string previousDailyAdditionalQuestName;
    private bool dailyAdditionalQuestCompleted;
    private bool dailyAdditionalQuestResolved;
    private bool dailyAdditionalQuestIsMopping;
    private Coroutine summaryCoroutine;
    private Coroutine dayWinCelebrationCoroutine;
    private bool summaryRevealComplete;
    private bool transitionInProgress;
    private bool dayStartIntroInProgress;
    private bool dayOneFollowupIntroStarted;
    private bool dayOneWaterTutorialComplete;
    private bool dayOneKitchenIntroStarted;
    private bool dayFourIntroInProgress;
    private bool waitingForDayTwoShop;
    private bool dayTwoPikoIntroductionPending;
    private bool dayTwoPikoIntroductionInProgress;
    private bool dayTwoPikoIntroductionStarted;
    private ShopTrigger dayTwoShopTrigger;
    private GameObject dayTwoShopWaypoints;
    private GameObject dayTwoPikoWaypoints;
    private GameObject dayOneIntroOverlay;
    private CinemachineCamera[] dayOneIntroCameras;
    private CinemachineBrain[] dayOneIntroBrains;
    private bool[] dayOneIntroBrainPreviousIgnoreTimeScale;
    private bool dayOneIntroPausedTime;
    private float dayOneIntroPreviousTimeScale = 1f;
    private float normalTimeScale = 1f;
    private bool doubleGameSpeedApplied;
    private readonly List<SummaryChoreIcon> summaryChoreIcons =
        new List<SummaryChoreIcon>();
    private readonly List<Image> summaryChoreIconImages = new List<Image>();

    private sealed class SummaryChoreIcon
    {
        public int CharacterIndex;
        public Sprite Sprite;
    }

    public int CurrentDay => currentDay;
    public string DailyAdditionalQuestName => dailyAdditionalQuestIsMopping
        ? "Mop Floor"
        : dailyAdditionalQuestChore != null
            ? dailyAdditionalQuestChore.ChoreName
            : string.Empty;
    public string DailyAdditionalQuestDisplayName => dailyAdditionalQuestIsMopping
        ? "Mop"
        : dailyAdditionalQuestChore == feedDog
            ? "Feed the Dog"
            : dailyAdditionalQuestChore == sweepDust
                ? "Sweep the Floor"
                : dailyAdditionalQuestChore == washDishes
                    ? "Wash the Dishes"
                    : dailyAdditionalQuestChore == garbageChore
                        ? "Sort the Trash"
                        : dailyAdditionalQuestChore == cleanLeaves
                            ? "Pick Up the Toys"
                            : DailyAdditionalQuestName;
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
    public float DailyQuestDishwashingTimeBeforeDeadline =>
        dailyQuestDishwashingTimeBeforeDeadline;
    // Difficulty is intentionally one-based so every playable day has a
    // distinct level, including Day 1.
    public int CurrentDifficulty => Mathf.Clamp(currentDay, 1, 7);
    public Chore MopFloorChore => mopFloor;
    public Chore SweepDustChore => sweepDust;
    public bool IsGameOver => gameOver;
    public int MissedChoreGameOverThreshold => GetMissedChoreGameOverThreshold();
    public bool HasReachedMissedChoreGameOverThreshold => choreManager != null &&
        choreManager.missedChores >= GetMissedChoreGameOverThreshold();

    public bool CanRegisterBunsoBadMoodChoreDrop(int dailyMaximum)
    {
        return dailyMaximum > 0 &&
            bunsoBadMoodChoresDroppedToday < dailyMaximum;
    }

    public bool TryRegisterBunsoBadMoodChoreDrop(int dailyMaximum)
    {
        if (!CanRegisterBunsoBadMoodChoreDrop(dailyMaximum))
            return false;

        bunsoBadMoodChoresDroppedToday++;
        return true;
    }

    public void UnregisterBunsoBadMoodChoreDrop()
    {
        if (bunsoBadMoodChoresDroppedToday > 0)
            bunsoBadMoodChoresDroppedToday--;
    }

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
        ResolveDaySummaryPanel();
        EnsureDayWinCelebrationImage();
        if (transitionFade != null)
        {
            if (daySummaryPanel != null &&
                transitionFade.transform.IsChildOf(daySummaryPanel.transform) &&
                daySummaryPanel.transform.parent != null)
            {
                transitionFade.transform.SetParent(daySummaryPanel.transform.parent, false);
                transitionFade.transform.SetAsLastSibling();
            }

            transitionFade.alpha = 0f;
            transitionFade.blocksRaycasts = false;
        }

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
        ResolveDay1Wall();

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void EnsureDayWinCelebrationImage()
    {
        if (dayWinCelebrationImage == null && daySummaryPanel != null)
        {
            GameObject celebrationObject = new GameObject(
                "DayWinCelebration",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            celebrationObject.layer = daySummaryPanel.layer;

            RectTransform celebrationRect = celebrationObject.GetComponent<RectTransform>();
            celebrationRect.SetParent(daySummaryPanel.transform, false);
            celebrationRect.anchorMin = new Vector2(0.5f, 1f);
            celebrationRect.anchorMax = new Vector2(0.5f, 1f);
            celebrationRect.pivot = new Vector2(0.5f, 0.5f);
            celebrationRect.anchoredPosition = new Vector2(0f, -110f);
            celebrationRect.sizeDelta = new Vector2(220f, 220f);
            celebrationRect.SetAsFirstSibling();

            dayWinCelebrationImage = celebrationObject.GetComponent<Image>();
        }

        PrepareDayWinCelebrationImage(dayWinCelebrationImage);
        if (additionalDayWinCelebrationImages == null)
            return;

        foreach (Image image in additionalDayWinCelebrationImages)
            PrepareDayWinCelebrationImage(image);
    }

    private void ResolveDaySummaryPanel()
    {
        if (daySummaryPanel != null || summaryChoresText == null)
            return;

        Transform summaryParent = summaryChoresText.transform.parent;
        if (summaryParent != null && summaryParent.GetComponent<Image>() != null)
            daySummaryPanel = summaryParent.gameObject;
    }

    private void PrepareDayWinCelebrationImage(Image image)
    {
        if (image == null)
            return;

        image.raycastTarget = false;
        image.preserveAspect = true;
        image.enabled = false;
        image.sprite = null;
    }

    private bool HasDayWinCelebrationFrames()
    {
        if (dayWinCelebrationFrames == null)
            return false;

        foreach (Sprite frame in dayWinCelebrationFrames)
        {
            if (frame != null)
                return true;
        }

        return false;
    }

    private IEnumerator PlayDayWinCelebration()
    {
        if (!HasDayWinCelebrationImage())
            yield break;

        float frameDelay = 1f / Mathf.Max(1f, dayWinCelebrationFramesPerSecond);
        while (true)
        {
            foreach (Sprite frame in dayWinCelebrationFrames)
            {
                if (frame == null)
                    continue;

                SetDayWinCelebrationFrame(frame);
                yield return new WaitForSecondsRealtime(frameDelay);
            }
        }
    }

    private bool HasDayWinCelebrationImage()
    {
        if (dayWinCelebrationImage != null)
            return true;

        if (additionalDayWinCelebrationImages == null)
            return false;

        foreach (Image image in additionalDayWinCelebrationImages)
        {
            if (image != null)
                return true;
        }

        return false;
    }

    private void SetDayWinCelebrationFrame(Sprite frame)
    {
        if (dayWinCelebrationImage != null)
        {
            dayWinCelebrationImage.sprite = frame;
            dayWinCelebrationImage.enabled = true;
        }

        if (additionalDayWinCelebrationImages == null)
            return;

        foreach (Image image in additionalDayWinCelebrationImages)
        {
            if (image == null)
                continue;

            image.sprite = frame;
            image.enabled = true;
        }
    }

    private void StopDayWinCelebration()
    {
        if (dayWinCelebrationCoroutine != null)
        {
            StopCoroutine(dayWinCelebrationCoroutine);
            dayWinCelebrationCoroutine = null;
        }

        if (dayWinCelebrationImage != null)
        {
            dayWinCelebrationImage.enabled = false;
            dayWinCelebrationImage.sprite = null;
        }

        if (additionalDayWinCelebrationImages == null)
            return;

        foreach (Image image in additionalDayWinCelebrationImages)
        {
            if (image == null)
                continue;

            image.enabled = false;
            image.sprite = null;
        }
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

    private void HideSceneNpcs(Transform excludedRoot = null)
    {
        HashSet<GameObject> sceneNpcs = new HashSet<GameObject>(
            GameObject.FindGameObjectsWithTag("NPCS"));
        if (excludedRoot != null)
        {
            sceneNpcs.RemoveWhere(npc =>
                npc == null ||
                npc.transform == excludedRoot ||
                npc.transform.IsChildOf(excludedRoot));
        }

        AddNpcComponents<NPC>(sceneNpcs, excludedRoot);
        AddNpcComponents<NPCSocialParticipant>(sceneNpcs, excludedRoot);
        AddNpcComponents<MomAI>(sceneNpcs, excludedRoot);
        AddNpcComponents<TatayGameOverAI>(sceneNpcs, excludedRoot);
        AddNpcComponents<BunsoAI>(sceneNpcs, excludedRoot);
        AddNpcComponents<HouseholdHelperAI>(sceneNpcs, excludedRoot);
        AddNpcComponents<CatInteraction>(sceneNpcs, excludedRoot);

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
        AddNpcComponents<T>(sceneNpcs, null);
    }

    private static void AddNpcComponents<T>(
        HashSet<GameObject> sceneNpcs,
        Transform excludedRoot)
        where T : Component
    {
        T[] components = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (T component in components)
        {
            if (component == null)
                continue;

            if (excludedRoot != null &&
                (component.transform == excludedRoot ||
                 component.transform.IsChildOf(excludedRoot)))
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

    private void RestoreParentNpcsForDeparture()
    {
        foreach (KeyValuePair<GameObject, bool> npcState in npcActiveStates)
        {
            if (npcState.Key != null && IsParentNpc(npcState.Key))
                npcState.Key.SetActive(npcState.Value);
        }

        SetParentMoversEnabled(false);
    }

    private void RestoreSceneNpcsForCurrentDay()
    {
        foreach (KeyValuePair<GameObject, bool> npcState in npcActiveStates)
        {
            if (npcState.Key == null)
                continue;

            bool shouldBeActive = npcState.Value;
            if (currentDay < 4 && IsParentNpc(npcState.Key))
                shouldBeActive = false;

            npcState.Key.SetActive(shouldBeActive);
        }

        npcActiveStates.Clear();
        SetParentNpcsActive(currentDay >= 4);
    }

    private static HashSet<GameObject> GetParentNpcs()
    {
        HashSet<GameObject> parentNpcs = new HashSet<GameObject>();
        AddNpcComponents<MomAI>(parentNpcs);
        AddNpcComponents<TatayGameOverAI>(parentNpcs);

        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform == null ||
                !sceneTransform.CompareTag("NPCS") ||
                !IsParentNpc(sceneTransform.gameObject))
                continue;

            parentNpcs.Add(sceneTransform.gameObject);
        }

        NPCSocialParticipant[] participants =
            FindObjectsByType<NPCSocialParticipant>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        foreach (NPCSocialParticipant participant in participants)
        {
            if (participant == null ||
                !IsParentName(participant.DisplayName))
                continue;

            Transform current = participant.transform;
            while (current != null && !current.CompareTag("NPCS"))
                current = current.parent;

            parentNpcs.Add(current != null ? current.gameObject : participant.gameObject);
        }

        List<GameObject> nestedParentNpcs = new List<GameObject>();
        foreach (GameObject parentNpc in parentNpcs)
        {
            for (Transform ancestor = parentNpc.transform.parent;
                 ancestor != null;
                 ancestor = ancestor.parent)
            {
                if (!parentNpcs.Contains(ancestor.gameObject))
                    continue;

                nestedParentNpcs.Add(parentNpc);
                break;
            }
        }

        foreach (GameObject nestedParentNpc in nestedParentNpcs)
            parentNpcs.Remove(nestedParentNpc);

        return parentNpcs;
    }

    private static bool IsParentName(string npcName)
    {
        return npcName.IndexOf("Nanay", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            npcName.IndexOf("Tatay", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsTatayNpc(GameObject npc)
    {
        if (npc == null)
            return false;

        if (npc.name.IndexOf("Tatay", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        NPCSocialParticipant[] participants =
            npc.GetComponentsInChildren<NPCSocialParticipant>(true);
        foreach (NPCSocialParticipant participant in participants)
        {
            if (participant != null &&
                participant.DisplayName.IndexOf(
                    "Tatay",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool IsNanayNpc(GameObject npc)
    {
        if (npc == null)
            return false;

        if (npc.name.IndexOf("Nanay", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        NPCSocialParticipant[] participants =
            npc.GetComponentsInChildren<NPCSocialParticipant>(true);
        foreach (NPCSocialParticipant participant in participants)
        {
            if (participant != null &&
                participant.DisplayName.IndexOf(
                    "Nanay",
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool IsParentNpc(GameObject npc)
    {
        if (npc == null)
            return false;

        if (npc.GetComponentInChildren<MomAI>(true) != null ||
            npc.GetComponentInChildren<TatayGameOverAI>(true) != null)
            return true;

        string npcName = npc.name;
        NPCSocialParticipant[] participants =
            npc.GetComponentsInChildren<NPCSocialParticipant>(true);
        foreach (NPCSocialParticipant participant in participants)
        {
            if (participant == null)
                continue;

            if (IsParentName(participant.DisplayName))
                return true;
        }

        return IsParentName(npcName);
    }

    private static void SetParentNpcsActive(bool active)
    {
        foreach (GameObject parentNpc in GetParentNpcs())
        {
            if (parentNpc != null)
                parentNpc.SetActive(active);
        }
    }

    private void CacheParentHomePositions()
    {
        parentHomePositions.Clear();
        foreach (GameObject parentNpc in GetParentNpcs())
        {
            if (parentNpc != null)
                parentHomePositions[parentNpc] = parentNpc.transform.position;
        }
    }

    private bool ResolveTricycle()
    {
        if (tricycle != null)
            return true;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform != null && sceneTransform.name == "Tricycle")
            {
                tricycle = sceneTransform;
                return true;
            }
        }

        Debug.LogWarning(
            "The family introduction could not move the parents because no GameObject named 'Tricycle' exists in the scene.",
            this);
        return false;
    }

    private void SetTricycleVisibleForCurrentDay()
    {
        if (!ResolveTricycle())
            return;

        bool visibleToday = currentDay == 1;
        if (tricycle.gameObject.activeSelf != visibleToday)
            tricycle.gameObject.SetActive(visibleToday);
    }

    private IEnumerator WalkParentsToPositions(
        Dictionary<GameObject, Vector3> destinations,
        Camera followCamera = null)
    {
        List<GameObject> parents = new List<GameObject>();
        foreach (KeyValuePair<GameObject, Vector3> destination in destinations)
        {
            GameObject parentNpc = destination.Key;
            if (parentNpc == null)
                continue;

            WaypointMover mover =
                parentNpc.GetComponentInChildren<WaypointMover>(true);
            if (mover == null)
            {
                Debug.LogWarning(
                    "The parent NPC '" + parentNpc.name +
                    "' could not walk because it has no WaypointMover.",
                    parentNpc);
                continue;
            }

            mover.SetMovementEnabled(false);
            parents.Add(parentNpc);
        }

        float elapsed = 0f;
        while (parents.Count > 0 && elapsed < parentWalkTimeout)
        {
            elapsed += Time.deltaTime;
            for (int i = parents.Count - 1; i >= 0; i--)
            {
                GameObject parentNpc = parents[i];
                if (parentNpc == null ||
                    !destinations.TryGetValue(parentNpc, out Vector3 destination))
                {
                    parents.RemoveAt(i);
                    continue;
                }

                WaypointMover mover =
                    parentNpc.GetComponentInChildren<WaypointMover>(true);
                if (mover == null ||
                    mover.MoveTowards(
                        destination,
                        parentWalkSpeed,
                        parentWalkArrivalDistance))
                {
                    SetParentWalkingAnimation(parentNpc, Vector2.zero);
                    if (mover != null)
                        mover.SetMovementEnabled(true);
                    parents.RemoveAt(i);
                }
                else
                {
                    SetParentWalkingAnimation(
                        parentNpc,
                        (Vector2)(destination - parentNpc.transform.position));
                }
            }

            if (followCamera != null && destinations.Count > 0)
            {
                Vector3 familyPosition = Vector3.zero;
                int familyMemberCount = 0;
                foreach (GameObject parentNpc in destinations.Keys)
                {
                    if (parentNpc == null)
                        continue;

                    familyPosition += GetParentCharacterPosition(parentNpc);
                    familyMemberCount++;
                }

                if (familyMemberCount > 0)
                {
                    familyPosition /= familyMemberCount;
                    FollowIntroCamera(followCamera, familyPosition);
                }
            }

            yield return null;
        }

        foreach (GameObject parentNpc in parents)
        {
            if (parentNpc == null)
                continue;

            WaypointMover mover =
                parentNpc.GetComponentInChildren<WaypointMover>(true);
            SetParentWalkingAnimation(parentNpc, Vector2.zero);
            if (mover != null)
                mover.SetMovementEnabled(true);

            Debug.LogWarning(
                "The parent NPC '" + parentNpc.name +
                "' did not reach its introduction destination before the walk timed out.",
                parentNpc);
        }
    }

    private void FollowIntroCamera(Camera mainCamera, Vector3 focusPosition)
    {
        float followStrength = 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime);
        if (dayOneIntroCameras == null || dayOneIntroCameras.Length == 0)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            cameraPosition.x = Mathf.Lerp(
                cameraPosition.x,
                focusPosition.x,
                followStrength);
            cameraPosition.y = Mathf.Lerp(
                cameraPosition.y,
                focusPosition.y,
                followStrength);
            mainCamera.transform.position = cameraPosition;
            return;
        }

        foreach (CinemachineCamera cameraRig in dayOneIntroCameras)
        {
            if (cameraRig == null)
                continue;

            Vector3 cameraPosition = cameraRig.transform.position;
            cameraPosition.x = Mathf.Lerp(
                cameraPosition.x,
                focusPosition.x,
                followStrength);
            cameraPosition.y = Mathf.Lerp(
                cameraPosition.y,
                focusPosition.y,
                followStrength);
            cameraRig.transform.position = cameraPosition;
            cameraRig.ForceCameraPosition(cameraPosition, cameraRig.transform.rotation);
        }
    }

    private static void SetParentWalkingAnimation(
        GameObject parentNpc,
        Vector2 movementDirection)
    {
        Animator animator = parentNpc.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        bool isWalking = movementDirection.sqrMagnitude > 0.0001f;
        Vector2 normalizedDirection = isWalking
            ? movementDirection.normalized
            : Vector2.zero;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == "isWalking" &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameter.nameHash, isWalking);
            }
            else if (parameter.name == "InputX" &&
                     parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameter.nameHash, normalizedDirection.x);
            }
            else if (parameter.name == "InputY" &&
                     parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameter.nameHash, normalizedDirection.y);
            }
        }
    }

    private void SetParentMoversEnabled(bool enabled)
    {
        foreach (GameObject parentNpc in GetParentNpcs())
        {
            if (parentNpc == null)
                continue;

            WaypointMover mover =
                parentNpc.GetComponentInChildren<WaypointMover>(true);
            if (mover != null)
                mover.SetMovementEnabled(enabled);
        }
    }

    private Dictionary<GameObject, Vector3> GetParentEntrancePositions()
    {
        List<GameObject> parents = new List<GameObject>(GetParentNpcs());
        parents.Sort((first, second) =>
            string.Compare(first.name, second.name, System.StringComparison.Ordinal));

        Dictionary<GameObject, Vector3> positions =
            new Dictionary<GameObject, Vector3>();
        for (int i = 0; i < parents.Count; i++)
        {
            GameObject parentNpc = parents[i];
            if (parentNpc == null)
                continue;

            float horizontalOffset =
                (i - (parents.Count - 1) * 0.5f) * parentWalkArrivalDistance * 1.5f;
            positions[parentNpc] = tricycle.position +
                Vector3.right * horizontalOffset;
        }

        GameObject tatay = parents.Find(IsTatayNpc);
        if (tatay != null && positions.TryGetValue(tatay, out Vector3 tatayPosition))
        {
            foreach (GameObject parentNpc in parents)
            {
                if (IsNanayNpc(parentNpc))
                    positions[parentNpc] = tatayPosition;
            }
        }

        return positions;
    }

    private static Vector3 GetParentCharacterPosition(GameObject parentNpc)
    {
        if (parentNpc == null)
            return Vector3.zero;

        MomAI nanay = parentNpc.GetComponentInChildren<MomAI>(true);
        return nanay != null ? nanay.transform.position : parentNpc.transform.position;
    }

    private void Start()
    {
        CacheParentHomePositions();
        SetTricycleVisibleForCurrentDay();
        UpdateDay1Wall();

        if (currentDay == 1)
        {
            HideSceneNpcs();
            dayStartIntroInProgress = true;
            StartCoroutine(StartDayOneWithIntroduction());
        }
        else if (currentDay == 2)
        {
            SetParentNpcsActive(false);
            dayStartIntroInProgress = true;
            StartCoroutine(StartDayTwoWithIntroduction());
        }
        else if (currentDay == 4)
        {
            SetParentNpcsActive(true);
            dayStartIntroInProgress = true;
            StartCoroutine(StartDayFourWithIntroduction());
        }
        else
        {
            SetParentNpcsActive(currentDay >= 4);
            StartDay();
        }
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
        if ((waitingForDayTwoShop ||
             dayTwoPikoIntroductionInProgress ||
             dayFourIntroInProgress) &&
            timeManager != null)
            timeManager.SetDayTimePaused(false);
        if (dayTwoShopWaypoints != null)
            Destroy(dayTwoShopWaypoints);
        if (dayTwoPikoWaypoints != null)
            Destroy(dayTwoPikoWaypoints);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToNextDay);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        RestoreSceneNpcs();
        RestoreTemporarilyHiddenCanvasUI();
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

        if (debugInstantWin)
        {
            debugInstantWin = false;
            dayFinished = true;
            weekComplete = currentDay >= FinalDay;
            if (waterSpawner != null)
                waterSpawner.StopSpawning();

            Debug.Log("DEBUG: Instantly opening the Day " + currentDay + " win summary.", this);
            if (weekComplete)
                StartCoroutine(PlayWeekEnding());
            else
                ShowDaySummary();
            return;
        }

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
            return;

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

        SoundEffectManager.PlayLoop("BGM");
        Debug.Log("=== START DAY " + currentDay + " ===");
        bunsoBadMoodChoresDroppedToday = 0;

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
        if (timeManager != null)
            timeManager.SetDayTimePaused(currentDay == 2 && waitingForDayTwoShop);

        if (currentDay == 1)
            GrantDayOneTutorialBoosts();
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
            return "Mop up the spilled water within " +
                dailyQuestMopTimeLimit.ToString("0") + " seconds.";

        if (questName.IndexOf("garbage", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("trash", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("segregat", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("basura", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Sort all the trash correctly without making a mistake.";

        if (questName.IndexOf("dog", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("feed", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Feed the dog without missing a single attempt.";

        if (questName.IndexOf("sweep", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Sweep up one dust pile within " +
                dailyQuestSweepTimeLimit.ToString("0") + " seconds.";

        if (questName.IndexOf("toy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            questName.IndexOf("leaves", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Pick up all the toys without touching any ants.";

        if (questName.IndexOf("dish", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Finish washing the dishes with at least " +
                dailyQuestDishwashingTimeBeforeDeadline.ToString("0") +
                " seconds remaining before the deadline.";

        return "Complete the task without making a mistake.";
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

        MarkUnfinishedChoresAsMissed();
        if (waterSpawner != null)
        {
            if (waterSpawner.MopTaskStarted && !waterSpawner.IsMoppingCompleted)
                waterSpawner.MarkMoppingMissed();

            waterSpawner.StopSpawning();
        }

        if (HasReachedMissedChoreGameOverThreshold && !gameOver)
            return;

        dayFinished = true;
        weekComplete = currentDay >= FinalDay;
        Debug.Log("DAY " + currentDay + " COMPLETE!");
        if (weekComplete && !gameOver)
            StartCoroutine(PlayWeekEnding());
        else
            ShowDaySummary();
    }

    private IEnumerator PlayWeekEnding()
    {
        CanvasGroup sceneFade = CreateWeekEndingSceneFade();
        yield return FadeCanvasGroup(sceneFade, 1f, transitionDuration);

        if (day7WinView == null)
        {
            Transform[] sceneTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform sceneTransform in sceneTransforms)
            {
                if (sceneTransform.name != "Day7winView")
                    continue;

                day7WinView = sceneTransform.gameObject;
                break;
            }
        }

        HideOtherCanvasUI(sceneFade, day7WinView != null ? day7WinView.transform : null);

        if (day7WinView == null)
        {
            Debug.LogWarning(
                "The Day 7 ending view could not be found; showing the ending story without the family scene.",
                this);
        }
        else
        {
            day7WinView.SetActive(true);
            StartCoroutine(PlayFamilySoundAfterDelay());
        }
        HideSceneNpcs(day7WinView != null ? day7WinView.transform : null);

        if (timeManager == null)
            timeManager = FindFirstObjectByType<TimeManager>();
        if (timeManager != null)
            timeManager.SetDayTimePaused(true);

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            player.SetMovementInputEnabled(false);

        if (day7WinView != null)
            yield return FrameWeekEndingCamera(day7WinView.transform);

        yield return FadeCanvasGroup(sceneFade, 0f, transitionDuration);
        if (sceneFade != null)
            Destroy(sceneFade.gameObject);

        yield return WaitForRealtime(3f);

        RestoreTemporarilyHiddenCanvasUI();
        yield return ShowWeekCompleteIntro();

        ShowDaySummary();
    }

    private IEnumerator PlayFamilySoundAfterDelay()
    {
        yield return WaitForRealtime(2f);
        SoundEffectManager.Play("Family");
    }

    private void HideOtherCanvasUI(CanvasGroup overlay, Transform excludedRoot)
    {
        temporarilyHiddenCanvasEnabledStates.Clear();
        temporarilyHiddenRaycasterEnabledStates.Clear();
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null ||
                (overlay != null && canvas.gameObject == overlay.gameObject) ||
                (excludedRoot != null &&
                    (canvas.transform == excludedRoot ||
                     canvas.transform.IsChildOf(excludedRoot))))
                continue;

            temporarilyHiddenCanvasEnabledStates.Add(canvas, canvas.enabled);
            canvas.enabled = false;

            GraphicRaycaster[] raycasters = canvas.GetComponents<GraphicRaycaster>();
            foreach (GraphicRaycaster raycaster in raycasters)
            {
                if (raycaster == null)
                    continue;

                temporarilyHiddenRaycasterEnabledStates.Add(raycaster, raycaster.enabled);
                raycaster.enabled = false;
            }
        }
    }

    private void RestoreTemporarilyHiddenCanvasUI()
    {
        foreach (KeyValuePair<Canvas, bool> canvasState in temporarilyHiddenCanvasEnabledStates)
        {
            if (canvasState.Key != null)
                canvasState.Key.enabled = canvasState.Value;
        }

        foreach (KeyValuePair<GraphicRaycaster, bool> raycasterState in
                 temporarilyHiddenRaycasterEnabledStates)
        {
            if (raycasterState.Key != null)
                raycasterState.Key.enabled = raycasterState.Value;
        }

        temporarilyHiddenCanvasEnabledStates.Clear();
        temporarilyHiddenRaycasterEnabledStates.Clear();
    }

    private IEnumerator ShowWeekCompleteIntro()
    {
        ResolveDaySummaryPanel();
        if (daySummaryPanel == null)
        {
            Debug.LogWarning(
                "The Day 7 summary panel could not be found; skipping the congratulations message.",
                this);
            yield break;
        }

        daySummaryPanel.SetActive(true);
        CanvasGroup panelCanvasGroup = daySummaryPanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = daySummaryPanel.AddComponent<CanvasGroup>();

        if (summarySuccessRateText != null)
            summarySuccessRateText.gameObject.SetActive(false);
        if (summaryPointsText != null)
            summaryPointsText.gameObject.SetActive(false);
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);

        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        StartWeekCompleteSummaryPanelAnimation();

        if (summaryChoresText != null)
        {
            RectTransform textRect = summaryChoresText.rectTransform;
            Vector2 originalAnchorMin = textRect.anchorMin;
            Vector2 originalAnchorMax = textRect.anchorMax;
            Vector2 originalAnchoredPosition = textRect.anchoredPosition;
            Vector2 originalOffsetMin = textRect.offsetMin;
            Vector2 originalOffsetMax = textRect.offsetMax;
            Vector3 originalLocalScale = textRect.localScale;
            TextAlignmentOptions originalAlignment = summaryChoresText.alignment;
            float originalFontSize = summaryChoresText.fontSize;
            bool originalAutoSizing = summaryChoresText.enableAutoSizing;
            float originalFontSizeMin = summaryChoresText.fontSizeMin;
            float originalFontSizeMax = summaryChoresText.fontSizeMax;
            TextOverflowModes originalOverflowMode = summaryChoresText.overflowMode;

            textRect.anchorMin = new Vector2(0.08f, 0.12f);
            textRect.anchorMax = new Vector2(0.92f, 0.88f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.localScale = Vector3.one;
            summaryChoresText.alignment = TextAlignmentOptions.Center;
            summaryChoresText.enableAutoSizing = true;
            summaryChoresText.fontSizeMin = 18f;
            summaryChoresText.fontSizeMax = 42f;
            summaryChoresText.overflowMode = TextOverflowModes.Truncate;
            summaryChoresText.text =
                "<size=155%><b><color=#FFD66B>CONGRATULATIONS!</color></b></size>\n\n" +
                "<color=#2E7D32>You completed the one-week challenge!\n\n" +
                "Your hard work brought the family closer, and now you can " +
                "celebrate a happier home together.</color>";

            summaryChoresText.ForceMeshUpdate();
            yield return FadeCanvasGroup(panelCanvasGroup, 1f, transitionDuration);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
            yield return WaitForRealtime(weekEndingStoryDuration);

            textRect.anchorMin = originalAnchorMin;
            textRect.anchorMax = originalAnchorMax;
            textRect.anchoredPosition = originalAnchoredPosition;
            textRect.offsetMin = originalOffsetMin;
            textRect.offsetMax = originalOffsetMax;
            textRect.localScale = originalLocalScale;
            summaryChoresText.alignment = originalAlignment;
            summaryChoresText.enableAutoSizing = originalAutoSizing;
            summaryChoresText.fontSizeMin = originalFontSizeMin;
            summaryChoresText.fontSizeMax = originalFontSizeMax;
            summaryChoresText.overflowMode = originalOverflowMode;
            summaryChoresText.fontSize = originalFontSize;
            summaryChoresText.ForceMeshUpdate();
        }
        else
        {
            yield return FadeCanvasGroup(panelCanvasGroup, 1f, transitionDuration);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
            yield return WaitForRealtime(weekEndingStoryDuration);
        }
    }

    private CanvasGroup CreateWeekEndingSceneFade()
    {
        GameObject canvasObject = new GameObject(
            "Week Ending Scene Fade",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new GameObject(
            "Fade",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        return canvasGroup;
    }

    private IEnumerator FrameWeekEndingCamera(Transform endingView)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning(
                "The Day 7 ending camera could not be moved because no MainCamera exists.",
                this);
            yield break;
        }

        Bounds viewBounds = new Bounds(endingView.position, Vector3.zero);
        bool hasRendererBounds = false;
        Renderer[] renderers = endingView.GetComponentsInChildren<Renderer>();
        foreach (Renderer sceneRenderer in renderers)
        {
            if (sceneRenderer == null || !sceneRenderer.enabled ||
                !sceneRenderer.gameObject.activeInHierarchy)
                continue;

            if (hasRendererBounds)
                viewBounds.Encapsulate(sceneRenderer.bounds);
            else
            {
                viewBounds = sceneRenderer.bounds;
                hasRendererBounds = true;
            }
        }

        float aspect = Mathf.Max(0.01f, mainCamera.aspect);
        float targetOrthographicSize = hasRendererBounds
            ? Mathf.Max(
                viewBounds.extents.y,
                viewBounds.extents.x / aspect) * weekEndingCameraPadding
            : mainCamera.orthographicSize / weekEndingCameraPadding;
        targetOrthographicSize = Mathf.Max(0.1f, targetOrthographicSize);
        CinemachineConfiner2D[] cameraConfiners = FindObjectsByType<CinemachineConfiner2D>(
            FindObjectsSortMode.None);
        foreach (CinemachineConfiner2D cameraConfiner in cameraConfiners)
            cameraConfiner.enabled = false;

        dayOneIntroCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        SetDayOneCameraFollowTarget(null);
        yield return MoveDayOneCameraTo(
            mainCamera,
            viewBounds.center,
            targetOrthographicSize,
            weekEndingCameraMoveDuration);
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

    private static void SetWeekCompleteSummaryTextStyle(TMP_Text text)
    {
        text.color = WeekCompleteSummaryTextColor;

        Material material = text.fontMaterial;
        if (material.HasProperty("_OutlineColor") && material.HasProperty("_OutlineWidth"))
        {
            material.SetColor("_OutlineColor", WeekCompleteSummaryOutlineColor);
            material.SetFloat("_OutlineWidth", 0.4f);
        }
        else
        {
            Debug.LogWarning(
                "The Day 7 summary font material does not support text outlines.",
                text);
        }
    }

    private void ShowDaySummary()
    {
        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(true);

        if (weekComplete && !gameOver)
        {
            if (summaryChoresText != null)
                SetWeekCompleteSummaryTextStyle(summaryChoresText);
            if (summarySuccessRateText != null)
                SetWeekCompleteSummaryTextStyle(summarySuccessRateText);
            if (summaryPointsText != null)
                SetWeekCompleteSummaryTextStyle(summaryPointsText);
        }

        if (weekComplete && !gameOver && daySummaryPanel != null)
        {
            CanvasGroup panelCanvasGroup = daySummaryPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
        }

        StopDayWinCelebration();
        if (gameOver)
            SoundEffectManager.Play("GameOver");
        else
            SoundEffectManager.Play("Win");

        if (!gameOver && HasDayWinCelebrationFrames())
            dayWinCelebrationCoroutine = StartCoroutine(PlayDayWinCelebration());

        float successRate = choreManager != null ? choreManager.SuccessRate : 0f;
        int totalPoints = choreManager != null ? choreManager.totalPoints : 0;
        HouseholdMood endingMood = moodManager != null
            ? moodManager.CurrentMood
            : HouseholdMood.Concerned;

        if (summarySuccessRateText != null)
        {
            summarySuccessRateText.text = "Success Rate: " + successRate.ToString("0") + "%";
            summarySuccessRateText.gameObject.SetActive(true);
        }

        if (summaryPointsText != null)
        {
            summaryPointsText.text = "Points Earned: " + totalPoints
                + "\nCoins Earned: " + dailyCoinsEarned
                + "\nFamily Mood: " + FormatMood(endingMood);
            summaryPointsText.gameObject.SetActive(true);
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

    private void StartWeekCompleteSummaryPanelAnimation()
    {
        if (weekCompleteSummaryPanelFrames == null)
            return;

        bool hasFrame = false;
        foreach (Sprite frame in weekCompleteSummaryPanelFrames)
        {
            if (frame == null)
                continue;

            hasFrame = true;
            break;
        }

        if (!hasFrame)
            return;

        Image summaryPanelImage = daySummaryPanel != null
            ? daySummaryPanel.GetComponent<Image>()
            : null;
        if (summaryPanelImage == null && summaryChoresText != null)
            summaryPanelImage = summaryChoresText.GetComponentInParent<Image>();

        if (summaryPanelImage == null)
        {
            Debug.LogWarning(
                "Day 7 summary panel frames are assigned, but no summary panel Image could be found.",
                this);
            return;
        }

        StartCoroutine(PlayWeekCompleteSummaryPanel(summaryPanelImage));
    }

    private IEnumerator PlayWeekCompleteSummaryPanel(Image summaryPanelImage)
    {
        float frameDelay = 1f / Mathf.Max(1f, weekCompleteSummaryPanelFramesPerSecond);
        while (summaryPanelImage != null)
        {
            foreach (Sprite frame in weekCompleteSummaryPanelFrames)
            {
                if (frame == null)
                    continue;

                summaryPanelImage.sprite = frame;
                yield return new WaitForSecondsRealtime(frameDelay);
            }
        }
    }

    private IEnumerator RevealDaySummary(HouseholdMood endingMood)
    {
        ClearSummaryChoreIcons();

        if (summaryChoresText != null)
        {
            StringBuilder summary = new StringBuilder();
            HelperNPC helper = FindFirstObjectByType<HelperNPC>(
                FindObjectsInactive.Include);
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
                    bool completedByHelper = chore.IsCompleted &&
                        choreManager != null &&
                        choreManager.WasCompletedByHelper(chore);
                    string choreStatus = chore.IsCompleted
                        ? (completedByHelper ? "COMPLETED BY HELPER" : "COMPLETED")
                        : garbageWasNotSpawned
                            ? "NOT SPAWNED"
                            : chore.IsMissed
                                ? "MISSED"
                                : "INCOMPLETE";
                    string choreMarker = chore.IsCompleted ? "✓ " : "- ";
                    string choreLine = choreMarker + chore.ChoreName + " - " + choreStatus;
                    AddSummaryChoreIcon(
                        summary,
                        choreLine,
                        completedByHelper && helper != null ? helper.SummaryPortrait : null);
                    summary.AppendLine(choreLine);
                    summaryChoresText.text = summary.ToString();
                    UpdateSummaryChoreIcons();
                    yield return new WaitForSecondsRealtime(summaryLineDelay);
                }
            }

            if (waterSpawner != null && waterSpawner.MopTaskStarted)
            {
                bool completedByHelper = waterSpawner.IsMoppingCompleted &&
                    choreManager != null &&
                    choreManager.WasTaskCompletedByHelper("Mop Wet Floor");
                string mopLine = (waterSpawner.IsMoppingCompleted ? "✓ " : "- ")
                    + "Mop Floor"
                    + (waterSpawner.IsMoppingCompleted
                        ? (completedByHelper || WasChoreCompletedByHelper("mop")
                            ? " - COMPLETED BY HELPER"
                            : " - COMPLETED")
                        : " - MISSED");
                completedByHelper |= WasChoreCompletedByHelper("mop");
                AddSummaryChoreIcon(
                    summary,
                    mopLine,
                    completedByHelper && helper != null ? helper.SummaryPortrait : null);
                summary.AppendLine(mopLine);
                summaryChoresText.text = summary.ToString();
                UpdateSummaryChoreIcons();
                yield return new WaitForSecondsRealtime(summaryLineDelay);
            }

            if (sweepingManager != null)
            {
                bool completedByHelper =
                    (choreManager != null &&
                     choreManager.WasTaskCompletedByHelper("Sweep Dust")) ||
                    WasChoreCompletedByHelper("sweep");
                string sweepLine = "Dust swept: "
                    + sweepingManager.CleanedDustCount + "/"
                    + sweepingManager.TotalDustSpawned
                    + (completedByHelper ? " - COMPLETED BY HELPER" : string.Empty);
                AddSummaryChoreIcon(
                    summary,
                    sweepLine,
                    completedByHelper && helper != null ? helper.SummaryPortrait : null);
                summary.AppendLine(sweepLine);
                summaryChoresText.text = summary.ToString();
                UpdateSummaryChoreIcons();
                yield return new WaitForSecondsRealtime(summaryLineDelay);
            }

            if (waterSpawner != null)
            {
                bool completedByHelper = !waterSpawner.MopTaskStarted &&
                    choreManager != null &&
                    (choreManager.WasTaskCompletedByHelper("Mop Wet Floor") ||
                     WasChoreCompletedByHelper("mop"));
                string mopProgressLine = "Water mopped: "
                    + waterSpawner.CleanedWaterCount + "/"
                    + waterSpawner.TotalWaterSpawned
                    + (completedByHelper ? " - COMPLETED BY HELPER" : string.Empty);
                AddSummaryChoreIcon(
                    summary,
                    mopProgressLine,
                    completedByHelper && helper != null ? helper.SummaryPortrait : null);
                summary.AppendLine(mopProgressLine);
                summaryChoresText.text = summary.ToString();
                UpdateSummaryChoreIcons();
                yield return new WaitForSecondsRealtime(summaryLineDelay);
            }

            summary.AppendLine();
            summary.AppendLine();
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

    private void ClearSummaryChoreIcons()
    {
        summaryChoreIcons.Clear();
        foreach (Image icon in summaryChoreIconImages)
        {
            if (icon != null)
                icon.gameObject.SetActive(false);
        }
    }

    private void AddSummaryChoreIcon(StringBuilder summary, string line, Sprite sprite)
    {
        if (sprite == null)
            return;

        summaryChoreIcons.Add(new SummaryChoreIcon
        {
            CharacterIndex = summary.Length + line.Length - 1,
            Sprite = sprite
        });
    }

    private bool WasChoreCompletedByHelper(string nameFragment)
    {
        if (choreManager == null || activeChores == null)
            return false;

        foreach (Chore chore in activeChores)
        {
            if (chore != null &&
                chore.IsCompleted &&
                choreManager.WasCompletedByHelper(chore) &&
                (chore.ChoreName ?? string.Empty).IndexOf(
                    nameFragment,
                    System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private void UpdateSummaryChoreIcons()
    {
        if (summaryChoresText == null)
            return;

        summaryChoresText.ForceMeshUpdate();
        TMP_TextInfo textInfo = summaryChoresText.textInfo;

        for (int i = 0; i < summaryChoreIcons.Count; i++)
        {
            if (i >= summaryChoreIconImages.Count)
                summaryChoreIconImages.Add(CreateSummaryChoreIcon());

            Image icon = summaryChoreIconImages[i];
            SummaryChoreIcon summaryIcon = summaryChoreIcons[i];
            int characterInfoIndex = -1;
            for (int j = 0; j < textInfo.characterCount; j++)
            {
                if (textInfo.characterInfo[j].index == summaryIcon.CharacterIndex)
                {
                    characterInfoIndex = j;
                    break;
                }
            }

            if (characterInfoIndex < 0)
            {
                icon.gameObject.SetActive(false);
                continue;
            }

            TMP_CharacterInfo character = textInfo.characterInfo[characterInfoIndex];
            float iconSize = Mathf.Max(1f, summaryChoresText.fontSize * 1.2f);
            RectTransform iconRect = icon.rectTransform;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = new Vector2(
                character.topRight.x + iconSize * 0.6f,
                (character.topRight.y + character.bottomRight.y) * 0.5f);
            icon.sprite = summaryIcon.Sprite;
            icon.gameObject.SetActive(true);
        }

        for (int i = summaryChoreIcons.Count; i < summaryChoreIconImages.Count; i++)
            summaryChoreIconImages[i].gameObject.SetActive(false);
    }

    private Image CreateSummaryChoreIcon()
    {
        GameObject iconObject = new GameObject(
            "Helper Completed Chore Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.layer = summaryChoresText.gameObject.layer;
        iconObject.transform.SetParent(summaryChoresText.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
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

        StopDayWinCelebration();
        transitionInProgress = true;
        StartCoroutine(TransitionToNextDay());
    }

    private IEnumerator TransitionToNextDay()
    {
        yield return Fade(1f);
        ClearSummaryChoreIcons();
        ResetAllChores();

        bool showTransitionCards = daySummaryPanel != null && summaryChoresText != null;
        GameObject[] summaryDetails =
        {
            summarySuccessRateText != null ? summarySuccessRateText.gameObject : null,
            summaryPointsText != null ? summaryPointsText.gameObject : null,
            continueButton != null ? continueButton.gameObject : null,
            mainMenuButton != null ? mainMenuButton.gameObject : null
        };
        bool[] summaryDetailsWereActive = new bool[summaryDetails.Length];
        RectTransform summaryTextRect = null;
        Transform originalSummaryTextParent = null;
        int originalSummaryTextSiblingIndex = -1;
        Vector3 originalSummaryTextLocalScale = Vector3.one;
        Vector2 originalAnchorMin = Vector2.zero;
        Vector2 originalAnchorMax = Vector2.zero;
        Vector2 originalAnchoredPosition = Vector2.zero;
        Vector2 originalSizeDelta = Vector2.zero;
        Vector2 originalOffsetMin = Vector2.zero;
        Vector2 originalOffsetMax = Vector2.zero;
        TextAlignmentOptions originalAlignment = TextAlignmentOptions.TopLeft;
        float originalFontSize = 0f;

        if (showTransitionCards)
        {
            daySummaryPanel.SetActive(true);
            for (int i = 0; i < summaryDetails.Length; i++)
            {
                GameObject detail = summaryDetails[i];
                if (detail == null)
                    continue;

                summaryDetailsWereActive[i] = detail.activeSelf;
                detail.SetActive(false);
            }

            summaryTextRect = summaryChoresText.rectTransform;
            originalSummaryTextParent = summaryTextRect.parent;
            originalSummaryTextSiblingIndex = summaryTextRect.GetSiblingIndex();
            originalSummaryTextLocalScale = summaryTextRect.localScale;
            originalAnchorMin = summaryTextRect.anchorMin;
            originalAnchorMax = summaryTextRect.anchorMax;
            originalAnchoredPosition = summaryTextRect.anchoredPosition;
            originalSizeDelta = summaryTextRect.sizeDelta;
            originalOffsetMin = summaryTextRect.offsetMin;
            originalOffsetMax = summaryTextRect.offsetMax;
            originalAlignment = summaryChoresText.alignment;
            originalFontSize = summaryChoresText.fontSize;

            if (daySummaryPanel.transform.parent != null)
            {
                summaryTextRect.SetParent(daySummaryPanel.transform.parent, false);
                summaryTextRect.SetAsLastSibling();
            }

            summaryTextRect.anchorMin = Vector2.zero;
            summaryTextRect.anchorMax = Vector2.one;
            summaryTextRect.anchoredPosition = Vector2.zero;
            summaryTextRect.offsetMin = Vector2.zero;
            summaryTextRect.offsetMax = Vector2.zero;
            summaryTextRect.localScale = Vector3.one;
            summaryChoresText.alignment = TextAlignmentOptions.Center;
            summaryChoresText.fontSize = 70f;
            summaryChoresText.text =
                "<size=125%><b>Sleep Time</b></size>\n\nGood Night! :).";
            SoundEffectManager.Play("Good Night");
            yield return Fade(0f);
            yield return WaitForRealtime(transitionCardHoldDuration);

            yield return Fade(1f);
            StartNextDay();
            summaryChoresText.text =
                "<size=125%><b>DAY " + currentDay +
                "</b></size>\n\nA fresh morning begins.";
            SoundEffectManager.Play("Good Morning");
            yield return Fade(0f);
            yield return WaitForRealtime(transitionCardHoldDuration);
            yield return Fade(1f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(1f);
            StartNextDay();
        }

        if (daySummaryPanel != null)
            daySummaryPanel.SetActive(false);

        if (showTransitionCards)
        {
            if (originalSummaryTextParent != null)
            {
                summaryTextRect.SetParent(originalSummaryTextParent, false);
                summaryTextRect.SetSiblingIndex(originalSummaryTextSiblingIndex);
            }

            summaryTextRect.anchorMin = originalAnchorMin;
            summaryTextRect.anchorMax = originalAnchorMax;
            summaryTextRect.anchoredPosition = originalAnchoredPosition;
            summaryTextRect.sizeDelta = originalSizeDelta;
            summaryTextRect.offsetMin = originalOffsetMin;
            summaryTextRect.offsetMax = originalOffsetMax;
            summaryTextRect.localScale = originalSummaryTextLocalScale;
            summaryChoresText.alignment = originalAlignment;
            summaryChoresText.fontSize = originalFontSize;

            for (int i = 0; i < summaryDetails.Length; i++)
            {
                if (summaryDetails[i] != null)
                    summaryDetails[i].SetActive(summaryDetailsWereActive[i]);
            }
        }

        yield return Fade(0f);
        if (currentDay == 2)
        {
            RestoreSceneNpcsForCurrentDay();
            yield return PlayDayTwoIntroduction();
        }
        else if (currentDay == 4)
        {
            SetParentNpcsActive(true);
            dayStartIntroInProgress = true;
            yield return StartCoroutine(StartDayFourWithIntroduction());
            transitionInProgress = false;
            yield break;
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
            RestoreSceneNpcsForCurrentDay();
            StartDay();
            dayStartIntroInProgress = false;
            yield break;
        }

        player.ResetToDailySpawn();
        MapTransition.MoveCameraToPosition(player.transform.position);
        player.SetMovementInputEnabled(false);
        PlayerInventoryUI inventoryUI = FindFirstObjectByType<PlayerInventoryUI>(
            FindObjectsInactive.Include);
        if (inventoryUI != null)
        {
            inventoryUI.SetTutorialInputEnabled(false);
            inventoryUI.SetInventoryPanelEnabled(false);
            inventoryUI.SetHotbarHiddenForTutorial(true);
        }

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
            "A THREE-DAY BUSINESS TRIP",
            "Nanay and Tatay need to leave for a three-day business trip. Ate, Kuya, and Bunso will look after the house until they come home.",
            null,
            1.5f);
        if (ResolveTricycle())
        {
            RestoreParentNpcsForDeparture();
            if (timeManager == null)
                timeManager = FindFirstObjectByType<TimeManager>();
            if (timeManager != null)
                timeManager.SetDayTimePaused(true);

            Vector3 departureFocus = tricycle.position;
            float departureCameraSize = Mathf.Min(
                originalOrthographicSize,
                dayOneCameraOrthographicSize);

            yield return MoveDayOneCameraTo(
                mainCamera,
                departureFocus,
                departureCameraSize,
                dayOneCameraMoveDuration);

            Destroy(dayOneIntroOverlay);
            dayOneIntroOverlay = null;
            Time.timeScale = dayOneIntroPreviousTimeScale;
            yield return WalkParentsToPositions(GetParentEntrancePositions());
            Time.timeScale = 0f;
            RestoreSceneNpcsForCurrentDay();
            dayOneIntroOverlay = CreateDayOneIntroOverlay(out continuePrompt);
            introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        }
        else
        {
            RestoreSceneNpcsForCurrentDay();
        }

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "WELCOME TO TAHANAN!",
            "In this game, you'll experience everyday life at home.");
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "MOVEMENT",
            "Use W, A, S, and D to move up, left, down, and right around the house. Try each direction to continue.",
            player);
        GameHUD hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
            hud.PulseChoreListForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "CHORE LIST",
            "This list shows the chores you need to complete each day. Don't forget any of them, or Mom might get upset!");
        if (hud != null)
            hud.PulseMoodSliderForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "MOOD",
            "This shows your family's mood. Try to keep it from dropping, or things may get harder.");
        if (hud != null)
            hud.PulseMissedChoreCounterForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "MISSED CHORES",
            "If you miss too many chores, the game will be over.");
        if (hud != null)
            hud.PulseTimeBarForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "WATCH THE TIME",
            "When this fills up, the day ends, so watch out.");

        Vector2 nearestDustPosition = Vector2.zero;
        bool hasDustSpot = sweepingManager != null &&
            sweepingManager.TryGetNearestDustSpotPosition(
                player.transform.position,
                out nearestDustPosition);
        if (!hasDustSpot)
        {
            DustSpot fallbackDustSpot = FindFirstObjectByType<DustSpot>();
            if (fallbackDustSpot != null)
            {
                nearestDustPosition = fallbackDustSpot.transform.position;
                hasDustSpot = true;
            }
        }

        if (hasDustSpot)
        {
            yield return MoveDayOneCameraTo(
                mainCamera,
                nearestDustPosition,
                dayOneCameraOrthographicSize,
                dayOneCameraMoveDuration);
        }
        else
        {
            Debug.LogWarning(
                "The Day 1 introduction could not focus on a dust spot because none is available.",
                this);
        }

        GameObject dustSpotArrow = hasDustSpot
            ? CreateDayOneSpotArrow(nearestDustPosition)
            : null;
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "DUST PILES",
            "There are dust piles around the house. Let's start cleaning!");
        if (dustSpotArrow != null)
            Destroy(dustSpotArrow);

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
            "BROOM",
            "Pick up and equip the broom to sweep the dust.");

        StartDay();
        yield return null;

        if (hud != null)
            hud.PulseDailyChallengeCardForTutorial();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "Bonus Task",
            "Check the Daily Challenge card for an extra challenge and bonus coins.");

        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;
        yield return MoveDayOneCameraTo(
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            dayOneCameraMoveDuration);

        SetDayOneCameraFollowTarget(player.transform);

        player.SetMovementInputEnabled(true);
        if (inventoryUI != null)
            inventoryUI.SetTutorialInputEnabled(true);
        ResumeDayOneIntro();
        dayStartIntroInProgress = false;

        PlayerEquipmentInventory equipmentInventory =
            player.GetComponent<PlayerEquipmentInventory>();
        if (broom != null)
        {
            GameObject broomWaypoints = CreateDayOneWaypoints(
                player.transform,
                broom.transform.position);
            yield return GuidePlayerAlongDayOnePath(
                broomWaypoints,
                player.transform,
                () => broom != null ? broom.transform.position : player.transform.position,
                () => dayFinished ||
                    (equipmentInventory != null &&
                     equipmentInventory.Owns(EquipmentType.Broom)));
        }

        bool hasPickedUpBroom = equipmentInventory != null &&
            equipmentInventory.Owns(EquipmentType.Broom);
        if (inventoryUI != null && hasPickedUpBroom)
        {
            inventoryUI.SetHotbarHiddenForTutorial(false);
            inventoryUI.SetInventoryPanelEnabled(true);
        }

        if (!dayFinished && broom != null && hasPickedUpBroom)
            yield return StartCoroutine(PlayDayOneInventoryTutorial(
                player,
                mainCamera,
                originalOrthographicSize,
                inventoryUI));

        if (!dayFinished &&
            hasPickedUpBroom &&
            sweepingManager != null &&
            sweepingManager.RemainingDust > 0)
        {
            GameObject dustWaypoints = CreateDayOneWaypoints(
                player.transform,
                player.transform.position);
            yield return GuidePlayerAlongDayOnePath(
                dustWaypoints,
                player.transform,
                () => sweepingManager != null &&
                    sweepingManager.TryGetNearestDustSpotPosition(
                        player.transform.position,
                        out Vector2 position)
                            ? position
                            : (Vector2)player.transform.position,
                () => dayFinished ||
                    sweepingManager == null ||
                    sweepingManager.RemainingDust <= 0 ||
                    (sweepDust != null &&
                     (sweepDust.IsCompleted || sweepDust.IsMissed)));
        }
    }

    private IEnumerator PlayDayOneInventoryTutorial(
        PlayerMovement player,
        Camera mainCamera,
        float originalOrthographicSize,
        PlayerInventoryUI inventoryUI)
    {
        if (inventoryUI == null ||
            !inventoryUI.HasInventoryPanel ||
            !inventoryUI.SelectEquipmentForTutorial(EquipmentType.Broom))
        {
            Debug.LogWarning(
                "The Day 1 inventory tutorial was skipped because the broom or inventory UI is unavailable.",
                this);
            yield break;
        }

        inventoryUI.CloseInventoryForTutorial();
        inventoryUI.SetTutorialInputEnabled(true);
        player.SetMovementInputEnabled(false);
        dayStartIntroInProgress = true;
        dayOneIntroCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        SetDayOneCameraFollowTarget(null);
        PauseDayOneIntro();

        yield return MoveDayOneCameraTo(
            mainCamera,
            player.transform.position,
            dayOneCameraOrthographicSize,
            dayOneCameraMoveDuration);
        inventoryUI.HighlightSelectedHotbarForTutorial(true);

        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "YOU'VE PICKED UP THE BROOM",
            "The broom is equipped. Check the highlighted hotbar icon to see which item you're holding.");
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "SWITCH ITEMS",
            "Press Q to switch to the next item. Try it now.");

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "OPEN YOUR INVENTORY",
            "Press I to open your inventory and view your items.");

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "USE AN ITEM",
            "Press I again to close your inventory. Equip Chocolate or the Time Freezer, then press E to use it.");

        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "USE YOUR EQUIPPED ITEM",
            "Equip Chocolate and press E to eat it and run faster. Equip the Time Freezer and press E to briefly stop time.");

        inventoryUI.HighlightSelectedHotbarForTutorial(false);
        inventoryUI.SetTutorialInputEnabled(true);
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

    private void GrantDayOneTutorialBoosts()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            Debug.LogError(
                "The Day 1 tutorial Time Freezer could not be granted because the player is missing.",
                this);
            return;
        }

        BunsoBoostInventory inventory = player.GetComponent<BunsoBoostInventory>();
        if (inventory == null)
            inventory = player.gameObject.AddComponent<BunsoBoostInventory>();

        if (!inventory.Add(BunsoBoostType.Chocolate))
        {
            Debug.LogError(
                "The Day 1 tutorial Chocolate could not be added to the player's inventory.",
                this);
        }

        if (!inventory.Add(BunsoBoostType.TimeFreezer))
        {
            Debug.LogError(
                "The Day 1 tutorial Time Freezer could not be added to the player's inventory.",
                this);
        }
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
            "Pick up and equip the mop to clean up the spill.");

        yield return MoveDayOneCameraTo(
            mainCamera,
            waterSpawner.LastWaterSpawnPosition,
            dayOneCameraOrthographicSize,
            dayOneCameraMoveDuration);
        GameObject waterSpotArrow = CreateDayOneSpotArrow(
            waterSpawner.LastWaterSpawnPosition);
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "SPILLED WATER",
            "There's water on the floor! Clean it up before someone slips.");
        if (waterSpotArrow != null)
            Destroy(waterSpotArrow);

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

        MoppingPlayerState moppingPlayerState =
            player.GetComponent<MoppingPlayerState>();
        if (mopEquipment != null &&
            moppingPlayerState != null &&
            !moppingPlayerState.HasMop)
        {
            GameObject mopWaypoints = CreateDayOneWaypoints(
                player.transform,
                mopEquipment.transform.position);
            yield return GuidePlayerAlongDayOnePath(
                mopWaypoints,
                player.transform,
                () => mopEquipment != null
                    ? mopEquipment.transform.position
                    : player.transform.position,
                () => dayFinished ||
                    moppingPlayerState == null ||
                    moppingPlayerState.HasMop);
        }

        if (!dayFinished && !waterSpawner.IsMoppingCompleted)
        {
            GameObject waterWaypoints = CreateDayOneWaypoints(
                player.transform,
                waterSpawner.LastWaterSpawnPosition);
            yield return GuidePlayerAlongDayOnePath(
                waterWaypoints,
                player.transform,
                () => waterSpawner != null
                    ? waterSpawner.LastWaterSpawnPosition
                    : player.transform.position,
                () => dayFinished ||
                    waterSpawner == null ||
                    waterSpawner.IsMoppingCompleted);
        }

        if (!dayFinished)
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
            "KITCHEN",
            "After mopping, head to the kitchen to wash the dishes!");

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
        GameObject kitchenWaypoints = CreateDayOneWaypoints(
            player.transform,
            kitchenFocusPosition);
        yield return GuidePlayerAlongDayOnePath(
            kitchenWaypoints,
            player.transform,
            () => kitchenFocusPosition,
            () => dayFinished ||
                dishes == null ||
                Vector2.Distance(player.transform.position, kitchenFocusPosition) <=
                    shopWaypointReachDistance);
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
        MapTransition.SetConfinerBoundaryAtPosition(focusPosition);

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

    private GameObject CreateDayOneSpotArrow(Vector3 spotPosition)
    {
        GameObject arrowRoot = new GameObject("Day 1 Spot Arrow");
        arrowRoot.transform.position = new Vector3(0f, 0f, spotPosition.z);

        Vector2 arrowPosition = (Vector2)spotPosition +
            Vector2.up * (shopWaypointArrowScale * 0.6f);
        CreateShopWaypointArrow(
            arrowRoot.transform,
            GetShopWaypointArrowSprite(),
            arrowPosition,
            Vector2.down,
            true);
        return arrowRoot;
    }

    private GameObject CreateDayOneWaypoints(
        Transform player,
        Vector2 targetPosition)
    {
        GameObject waypointRoot = new GameObject("Day 1 Tutorial Waypoints");
        waypointRoot.transform.position = new Vector3(
            0f,
            0f,
            player.position.z);
        RebuildWaypointPath(
            waypointRoot.transform,
            player,
            targetPosition,
            false,
            true);
        return waypointRoot;
    }

    private IEnumerator GuidePlayerAlongDayOnePath(
        GameObject waypointRoot,
        Transform player,
        Func<Vector2> getTargetPosition,
        Func<bool> isComplete)
    {
        float refreshInterval = Mathf.Max(0.05f, shopWaypointRefreshInterval);
        float rebuildDistance = Mathf.Max(0.5f, shopWaypointArrowSpacing * 0.75f);
        Vector2 previousPlayerPosition = player.position;
        Vector2 previousTargetPosition = getTargetPosition();
        while (waypointRoot != null &&
               player != null &&
               !dayFinished &&
               !isComplete())
        {
            Vector2 targetPosition = getTargetPosition();
            if (Vector2.Distance(player.position, previousPlayerPosition) >= rebuildDistance ||
                Vector2.Distance(targetPosition, previousTargetPosition) >= 0.5f)
            {
                RebuildWaypointPath(
                    waypointRoot.transform,
                    player,
                    targetPosition,
                    false,
                    true);
                previousPlayerPosition = player.position;
                previousTargetPosition = targetPosition;
            }

            yield return new WaitForSecondsRealtime(refreshInterval);
        }

        if (waypointRoot != null)
            Destroy(waypointRoot);
    }

    private IEnumerator ShowDayOneIntroText(
        TMP_Text text,
        TMP_Text continuePrompt,
        string title,
        string message,
        PlayerMovement movementPracticePlayer = null,
        float autoContinueDelay = -1f)
    {
        text.font = dayOneIntroFont;
        text.outlineWidth = 0.4f;
        text.outlineColor = Color.black;
        text.text = "<b>" + title + "</b>\n\n" + message;
        text.ForceMeshUpdate();
        text.maxVisibleCharacters = 0;

        continuePrompt.font = dayOneIntroFont;
        continuePrompt.outlineWidth = 0.4f;
        continuePrompt.outlineColor = Color.black;
        continuePrompt.text = "Press Space or Esc to continue";
        Vector3 originalScale = continuePrompt.rectTransform.localScale;
        float revealElapsed = 0f;
        float pulseElapsed = 0f;
        int wordIndex = 0;
        bool skippedTyping = false;
        const float wordRevealInterval = 0.16f;

        if (text.textInfo.wordCount > 0)
            SoundEffectManager.PlayLoop("Write");

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

        SoundEffectManager.Stop("Write");
        text.maxVisibleCharacters = text.textInfo.characterCount;
        if (skippedTyping)
            yield return null;

        if (movementPracticePlayer != null)
            yield return TestDayOneMovement(continuePrompt, movementPracticePlayer);

        if (autoContinueDelay >= 0f)
        {
            continuePrompt.text = "Nanay and Tatay are getting ready to leave...";
            yield return WaitForRealtime(autoContinueDelay);
        }
        else
        {
            while (!WasDayOneIntroContinuePressed())
            {
                pulseElapsed += Time.unscaledDeltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(pulseElapsed * Mathf.PI * 2f);
                continuePrompt.rectTransform.localScale = originalScale * (1f + pulse * 0.12f);
                yield return null;
            }
        }

        continuePrompt.rectTransform.localScale = originalScale;
    }

    private IEnumerator TestDayOneMovement(
        TMP_Text continuePrompt,
        PlayerMovement player)
    {
        bool testedUp = false;
        bool testedLeft = false;
        bool testedDown = false;
        bool testedRight = false;
        int directionsTested = -1;

        SetDayOneCameraFollowTarget(player.transform);
        Time.timeScale = dayOneIntroPreviousTimeScale;
        player.SetMovementInputEnabled(true);

        while (!testedUp || !testedLeft || !testedDown || !testedRight)
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 moveInput = player.CurrentMoveInput;
            if (keyboard != null)
            {
                testedUp |= keyboard.wKey.isPressed && moveInput.y > 0.5f;
                testedLeft |= keyboard.aKey.isPressed && moveInput.x < -0.5f;
                testedDown |= keyboard.sKey.isPressed && moveInput.y < -0.5f;
                testedRight |= keyboard.dKey.isPressed && moveInput.x > 0.5f;
            }

            int currentDirectionsTested =
                (testedUp ? 1 : 0) +
                (testedLeft ? 1 : 0) +
                (testedDown ? 1 : 0) +
                (testedRight ? 1 : 0);
            if (currentDirectionsTested != directionsTested)
            {
                directionsTested = currentDirectionsTested;
                continuePrompt.text =
                    "Test W, A, S, and D to move in each direction (" +
                    directionsTested +
                    "/4)";
            }

            yield return null;
        }

        player.SetMovementInputEnabled(false);
        SetDayOneCameraFollowTarget(null);
        Time.timeScale = 0f;
        continuePrompt.text = "Movement tested! Press Space or Esc to continue";
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

    private IEnumerator StartDayFourWithIntroduction()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Camera mainCamera = Camera.main;
        if (player == null || mainCamera == null)
        {
            Debug.LogWarning(
                "The Day 4 introduction was skipped because the player or MainCamera is missing.",
                this);
            StartDay();
            dayStartIntroInProgress = false;
            yield break;
        }

        player.ResetToDailySpawn();
        player.SetMovementInputEnabled(false);
        PlayerInventoryUI inventoryUI = FindFirstObjectByType<PlayerInventoryUI>(
            FindObjectsInactive.Include);
        if (inventoryUI != null)
            inventoryUI.SetTutorialInputEnabled(false);

        dayFourIntroInProgress = true;
        if (timeManager == null)
            timeManager = FindFirstObjectByType<TimeManager>();
        if (timeManager != null)
            timeManager.SetDayTimePaused(true);
        else
            Debug.LogWarning(
                "The Day 4 introduction could not pause the day clock because no TimeManager exists.",
                this);

        HashSet<GameObject> parentNpcs = GetParentNpcs();
        SetParentNpcsActive(true);

        if (parentNpcs.Count == 0)
        {
            Debug.LogWarning(
                "The Day 4 introduction could not show the returning parents because no Nanay or Tatay NPC was found.",
                this);
        }

        bool hasTricycle = ResolveTricycle();
        Dictionary<GameObject, Vector3> returnDestinations =
            new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Vector3> entrancePositions = hasTricycle
            ? GetParentEntrancePositions()
            : new Dictionary<GameObject, Vector3>();
        if (dayFourNanayArrivalSpawnPoint != null)
        {
            foreach (GameObject parentNpc in parentNpcs)
            {
                if (IsNanayNpc(parentNpc))
                    entrancePositions[parentNpc] =
                        dayFourNanayArrivalSpawnPoint.position;
            }
        }

        foreach (GameObject parentNpc in parentNpcs)
        {
            if (parentNpc == null ||
                !entrancePositions.TryGetValue(parentNpc, out Vector3 entrancePosition))
                continue;

            WaypointMover mover =
                parentNpc.GetComponentInChildren<WaypointMover>(true);
            if (mover != null)
                mover.SetMovementEnabled(false);

            if (IsNanayNpc(parentNpc) &&
                dayFourNanayArrivalSpawnPoint != null)
            {
                Vector3 characterOffset =
                    dayFourNanayArrivalSpawnPoint.position -
                    GetParentCharacterPosition(parentNpc);
                parentNpc.transform.position += characterOffset;
            }
            else
            {
                parentNpc.transform.position = entrancePosition;
            }
            if (parentHomePositions.TryGetValue(
                    parentNpc,
                    out Vector3 homePosition))
            {
                returnDestinations[parentNpc] = homePosition;
            }
            else
            {
                Debug.LogWarning(
                    "The parent NPC '" + parentNpc.name +
                    "' has no remembered home position for the Day 4 arrival.",
                    parentNpc);
            }
        }
        Physics2D.SyncTransforms();

        dayOneIntroCameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        float originalOrthographicSize = dayOneIntroCameras.Length > 0
            ? dayOneIntroCameras[0].Lens.OrthographicSize
            : mainCamera.orthographicSize;
        SetDayOneCameraFollowTarget(null);

        Vector3 arrivalFocus = tricycle != null
            ? tricycle.position
            : player.transform.position;
        float arrivalCameraSize = hasTricycle
            ? Mathf.Min(originalOrthographicSize, dayOneCameraOrthographicSize)
            : originalOrthographicSize;
        if (parentNpcs.Count > 0)
        {
            Vector3 familyPosition = Vector3.zero;
            int familyMemberCount = 0;
            foreach (GameObject parentNpc in parentNpcs)
            {
                if (parentNpc == null)
                    continue;

                familyPosition += GetParentCharacterPosition(parentNpc);
                familyMemberCount++;
            }

            if (familyMemberCount > 0)
                arrivalFocus = familyPosition / familyMemberCount;
        }

        yield return MoveDayOneCameraTo(
            mainCamera,
            arrivalFocus,
            arrivalCameraSize,
            dayOneCameraMoveDuration);

        if (returnDestinations.Count > 0)
        {
            Vector3 familyHomePosition = Vector3.zero;
            int familyMemberCount = 0;
            foreach (KeyValuePair<GameObject, Vector3> destination in returnDestinations)
            {
                GameObject parentNpc = destination.Key;
                if (parentNpc == null)
                    continue;

                familyHomePosition += destination.Value;
                familyMemberCount++;

                WaypointMover mover =
                    parentNpc.GetComponentInChildren<WaypointMover>(true);
                if (mover != null)
                    mover.SetMovementEnabled(true);
            }

            if (familyMemberCount > 0)
            {
                familyHomePosition /= familyMemberCount;
                yield return WalkParentsToPositions(returnDestinations, mainCamera);
                yield return MoveDayOneCameraTo(
                    mainCamera,
                    familyHomePosition,
                    Mathf.Min(originalOrthographicSize, dayOneCameraOrthographicSize),
                    dayOneCameraMoveDuration);
            }
        }

        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "NANAY AND TATAY ARE HOME!",
            "After three days away, Nanay and Tatay have returned. Finish your chores efficiently and keep the house tidy, or Nanay might get upset.");

        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;
        yield return MoveDayOneCameraTo(
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            dayOneCameraMoveDuration);

        SetDayOneCameraFollowTarget(player.transform);
        player.SetMovementInputEnabled(true);
        if (inventoryUI != null)
            inventoryUI.SetTutorialInputEnabled(true);
        if (timeManager != null)
            timeManager.SetDayTimePaused(false);

        dayFourIntroInProgress = false;
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

        HideOtherCanvasUI(null, null);
        MapTransition.SetConfinerBoundaryAtPosition(shop.transform.position);
        yield return MoveCameraTo(
            cameraRig,
            cinematicCamera,
            mainCamera,
            shop.transform.position,
            originalOrthographicSize,
            shopCameraMoveDuration);

        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "THE SHOP",
            "Visit the shop to buy or upgrade equipment and furniture using the coins you earn.");
        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;

        if (economyManager != null && economyManager.HasPurchasedCat)
        {
            CatInteraction cat = FindFirstObjectByType<CatInteraction>(
                FindObjectsInactive.Include);
            if (cat != null && cat.gameObject.activeInHierarchy)
            {
                MapTransition.SetConfinerBoundaryAtPosition(cat.transform.position);
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

        MapTransition.SetConfinerBoundaryAtPosition(player.transform.position);
        yield return MoveCameraTo(
            cameraRig,
            cinematicCamera,
            mainCamera,
            player.transform.position,
            originalOrthographicSize,
            shopCameraMoveDuration);

        RestoreTemporarilyHiddenCanvasUI();

        if (cinematicCamera != null)
            cinematicCamera.Follow = player.transform;

        MapTransition shopAreaTransition = FindShopAreaTransition(
            player.transform.position,
            shop.transform.position);
        Vector2 initialWaypoint = shopAreaTransition != null
            ? (Vector2)shopAreaTransition.transform.position
            : (Vector2)shop.transform.position;
        dayTwoShopTrigger = shop;
        waitingForDayTwoShop = true;
        dayTwoShopWaypoints = CreateShopWaypoints(player.transform, initialWaypoint);
        if (dayTwoShopWaypoints != null)
        {
            StartCoroutine(GuidePlayerToShop(
                dayTwoShopWaypoints,
                player.transform,
                shop,
                shopAreaTransition));
        }

        string directionToShop = GetDirectionToShop(player.transform.position, initialWaypoint);
        SpeechBubbleCanvas.Show(
            player.transform,
            "Follow the arrows " + directionToShop + " to the next area, then keep following them to the store. Move with WASD or the arrow keys, then press E at the shop.",
            shopDirectionsDuration,
            new Vector2(480f, 104f),
            18);
        player.SetMovementInputEnabled(true);
    }

    public void OnDayTwoShopOpened(ShopTrigger shop)
    {
        if (currentDay != 2 ||
            dayTwoPikoIntroductionPending ||
            dayTwoPikoIntroductionStarted)
            return;

        if (waitingForDayTwoShop)
        {
            waitingForDayTwoShop = false;
            dayTwoShopTrigger = null;
        }

        dayTwoPikoIntroductionPending = true;
        StartCoroutine(WaitForDayTwoShopToClose(shop));
        if (timeManager != null)
            timeManager.SetDayTimePaused(false);

        if (dayTwoShopWaypoints != null)
        {
            Destroy(dayTwoShopWaypoints);
            dayTwoShopWaypoints = null;
        }
    }

    public void OnDayTwoShopClosed()
    {
        if (currentDay != 2 || dayTwoPikoIntroductionStarted)
            return;

        dayTwoPikoIntroductionPending = false;
        dayTwoPikoIntroductionStarted = true;
        StartCoroutine(PlayDayTwoPikoIntroduction());
    }

    public void OnDayTwoPikoInteracted()
    {
        if (dayTwoPikoWaypoints == null)
            return;

        Destroy(dayTwoPikoWaypoints);
        dayTwoPikoWaypoints = null;
    }

    private IEnumerator WaitForDayTwoShopToClose(ShopTrigger shop)
    {
        yield return null;
        while (dayTwoPikoIntroductionPending &&
               shop != null &&
               shop.IsShopOpen)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (dayTwoPikoIntroductionPending)
            OnDayTwoShopClosed();
    }

    private IEnumerator PlayDayTwoPikoIntroduction()
    {
        PikoMiniGame piko = FindFirstObjectByType<PikoMiniGame>(
            FindObjectsInactive.Include);
        Transform pikoBoard = piko != null
            ? piko.transform
            : FindPikoBoardTransform();
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (pikoBoard == null)
        {
            Debug.LogWarning(
                "The Piko introduction was skipped because no Piko board exists in the scene.",
                this);
            yield break;
        }

        if (piko == null)
        {
            Debug.LogWarning(
                "The Piko board was found, but it has no PikoMiniGame component.",
                this);
        }

        if (player == null)
        {
            Debug.LogWarning(
                "The Piko introduction was skipped because the player is missing.",
                this);
            yield break;
        }

        if (timeManager == null)
            timeManager = FindFirstObjectByType<TimeManager>();
        if (timeManager == null)
        {
            Debug.LogWarning(
                "The Piko introduction could not pause the day clock because no TimeManager exists.",
                this);
        }
        else
        {
            timeManager.SetDayTimePaused(true);
        }

        dayTwoPikoIntroductionInProgress = true;
        player.SetMovementInputEnabled(false);
        HideOtherCanvasUI(null, null);
        yield return MapTransition.MoveCameraToPositionAndWait(pikoBoard.position);

        dayOneIntroOverlay = CreateDayOneIntroOverlay(out TMP_Text continuePrompt);
        TMP_Text introText = dayOneIntroOverlay.GetComponentInChildren<TMP_Text>();
        yield return ShowDayOneIntroText(
            introText,
            continuePrompt,
            "PIKO",
            "Take turns tossing the pamato and hopping from square 1 to 10. Don't miss a square; the first to finish wins. Your opponent is Ate, and you can play twice each day. Press E at the board to start.");
        Destroy(dayOneIntroOverlay);
        dayOneIntroOverlay = null;

        MapTransition pikoAreaTransition = FindShopAreaTransition(
            player.transform.position,
            pikoBoard.position);
        Vector2 initialWaypoint = pikoAreaTransition != null
            ? (Vector2)pikoAreaTransition.transform.position
            : (Vector2)pikoBoard.position;
        dayTwoPikoWaypoints = CreateShopWaypoints(
            player.transform,
            initialWaypoint,
            true);

        yield return MapTransition.MoveCameraToPositionAndWait(
            player.transform.position,
            player.transform);

        RestoreTemporarilyHiddenCanvasUI();
        player.SetMovementInputEnabled(true);
        if (dayTwoPikoWaypoints != null)
        {
            StartCoroutine(GuidePlayerToPiko(
                dayTwoPikoWaypoints,
                player.transform,
                pikoBoard,
                pikoAreaTransition));
        }
        if (timeManager != null)
            timeManager.SetDayTimePaused(false);

        dayTwoPikoIntroductionInProgress = false;
    }

    private static Transform FindPikoBoardTransform()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer != null &&
                string.Equals(
                    spriteRenderer.gameObject.name,
                    "piko_0",
                    StringComparison.OrdinalIgnoreCase))
            {
                return spriteRenderer.transform;
            }
        }

        return null;
    }

    private static MapTransition FindShopAreaTransition(
        Vector2 playerPosition,
        Vector2 shopPosition)
    {
        MapTransition[] transitions = FindObjectsByType<MapTransition>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        MapTransition nearestTransition = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (MapTransition transition in transitions)
        {
            if (transition == null ||
                !transition.isActiveAndEnabled ||
                !transition.LeadsToAreaContaining(shopPosition) ||
                transition.LeadsToAreaContaining(playerPosition))
            {
                continue;
            }

            float distance = ((Vector2)transition.transform.position - playerPosition).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestTransition = transition;
                nearestDistance = distance;
            }
        }

        return nearestTransition;
    }

    private GameObject CreateShopWaypoints(
        Transform player,
        Vector2 targetPosition,
        bool allowDirectFallback = false)
    {
        var waypointRoot = new GameObject("Day 2 Shop Waypoints");
        waypointRoot.transform.position = new Vector3(0f, 0f, player.position.z);
        RebuildWaypointPath(
            waypointRoot.transform,
            player,
            targetPosition,
            true,
            false,
            allowDirectFallback);
        return waypointRoot;
    }

    private void RebuildWaypointPath(
        Transform waypointRoot,
        Transform player,
        Vector2 targetPosition,
        bool warnIfNoRoute = true,
        bool dayOneStyle = false,
        bool allowDirectFallback = false)
    {
        for (int i = waypointRoot.childCount - 1; i >= 0; i--)
            Destroy(waypointRoot.GetChild(i).gameObject);

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            playerCollider = player.GetComponentInChildren<Collider2D>();

        List<Vector2> route = NPCNavigation2D.FindPath(
            player.position,
            targetPosition,
            playerCollider,
            ~0,
            0.35f,
            2f,
            5000);
        if (route.Count == 0)
        {
            if (allowDirectFallback)
            {
                route.Add(player.position);
                route.Add(targetPosition);
            }
            else
            {
                if (warnIfNoRoute)
                    Debug.LogWarning("No waypoint route was found.", this);
                return;
            }
        }

        if (route.Count == 1)
            route.Insert(0, player.position);
        else
            route[0] = player.position;

        float routeLength = 0f;
        for (int i = 1; i < route.Count; i++)
            routeLength += Vector2.Distance(route[i - 1], route[i]);

        if (routeLength <= Mathf.Epsilon)
            return;

        Sprite arrowSprite = GetShopWaypointArrowSprite();
        float desiredArrowSpacing = dayOneStyle
            ? Mathf.Min(shopWaypointArrowSpacing, 0.65f)
            : shopWaypointArrowSpacing;
        float arrowSpacing = Mathf.Max(
            0.25f,
            Mathf.Min(desiredArrowSpacing, routeLength / 4f));
        float distanceUntilNextArrow = Mathf.Min(arrowSpacing, 0.6f);

        for (int i = 1; i < route.Count; i++)
        {
            Vector2 segmentStart = route[i - 1];
            Vector2 segment = route[i] - segmentStart;
            float segmentLength = segment.magnitude;
            if (segmentLength <= Mathf.Epsilon)
                continue;

            Vector2 direction = segment / segmentLength;
            while (distanceUntilNextArrow <= segmentLength)
            {
                Vector2 arrowPosition = segmentStart + direction * distanceUntilNextArrow;
                CreateShopWaypointArrow(
                    waypointRoot,
                    arrowSprite,
                    arrowPosition,
                    direction,
                    dayOneStyle);
                distanceUntilNextArrow += arrowSpacing;
            }

            distanceUntilNextArrow -= segmentLength;
        }
    }

    private void CreateShopWaypointArrow(
        Transform parent,
        Sprite arrowSprite,
        Vector2 position,
        Vector2 direction,
        bool dayOneStyle = false)
    {
        GameObject arrowObject = new GameObject("Waypoint Arrow");
        arrowObject.transform.SetParent(parent, false);
        arrowObject.transform.localPosition = new Vector3(position.x, position.y, 0f);
        arrowObject.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Vector2.SignedAngle(Vector2.right, direction));

        SpriteRenderer outline = arrowObject.AddComponent<SpriteRenderer>();
        outline.sprite = arrowSprite;
        outline.color = dayOneStyle
            ? new Color(0.48f, 0.78f, 0.56f, 0.95f)
            : new Color(0.45f, 0f, 0f, 0.9f);
        outline.sortingLayerName = "Player";
        outline.sortingOrder = 50;
        outline.transform.localScale = Vector3.one * (shopWaypointArrowScale * 1.2f);

        GameObject arrowFace = new GameObject("Arrow Face");
        arrowFace.transform.SetParent(arrowObject.transform, false);
        SpriteRenderer fill = arrowFace.AddComponent<SpriteRenderer>();
        fill.sprite = arrowSprite;
        fill.color = dayOneStyle
            ? new Color(0.94f, 1f, 0.95f, 1f)
            : new Color(1f, 0.08f, 0.08f, 1f);
        fill.sortingLayerName = "Player";
        fill.sortingOrder = 51;
        fill.transform.localScale = Vector3.one * 0.82f;
        if (dayOneStyle)
            StartCoroutine(AnimateDayOneWaypointArrow(
                arrowObject.transform,
                fill.transform));
        else
            StartCoroutine(PulseShopWaypointArrow(arrowObject.transform));
    }

    private IEnumerator AnimateDayOneWaypointArrow(
        Transform arrow,
        Transform arrowFace)
    {
        Vector3 baseScale = arrow.localScale;
        Vector3 basePosition = arrow.localPosition;
        Vector3 baseFaceScale = arrowFace.localScale;
        float phase = Mathf.Repeat(arrow.GetInstanceID() * 0.173f, Mathf.PI * 2f);
        while (arrow != null && arrowFace != null)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f + phase);
            float smoothWave = wave * wave * (3f - 2f * wave);
            arrow.localScale = baseScale * (1f + smoothWave * 0.07f);
            arrow.localPosition = basePosition +
                Vector3.up * (smoothWave * 0.04f);
            arrowFace.localScale = baseFaceScale *
                (0.94f + smoothWave * 0.04f);
            yield return null;
        }
    }

    private IEnumerator PulseShopWaypointArrow(Transform arrow)
    {
        Vector3 baseScale = arrow.localScale;
        while (arrow != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            arrow.localScale = baseScale * (1f + pulse * 0.2f);
            yield return null;
        }
    }

    private static Sprite GetShopWaypointArrowSprite()
    {
        if (shopWaypointArrowSprite != null)
            return shopWaypointArrowSprite;

        const int textureSize = 32;
        var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "Waypoint Arrow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[textureSize * textureSize];
        int centerY = textureSize / 2;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool shaft = x >= 3 && x <= 20 && Mathf.Abs(y - centerY) <= 3;
                bool arrowHead = x >= 16 &&
                    Mathf.Abs(y - centerY) <= textureSize - 1 - x;
                if (shaft || arrowHead)
                    pixels[y * textureSize + x] = new Color32(255, 255, 255, 255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        shopWaypointArrowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        return shopWaypointArrowSprite;
    }

    private IEnumerator GuidePlayerToShop(
        GameObject waypointRoot,
        Transform player,
        ShopTrigger shop,
        MapTransition shopAreaTransition)
    {
        bool reachedShopArea = shopAreaTransition == null ||
            shopAreaTransition.LeadsToAreaContaining(player.position);
        while (waypointRoot != null &&
               player != null &&
               shop != null &&
               Vector2.Distance(player.position, shop.transform.position) > shopWaypointReachDistance)
        {
            if (!reachedShopArea &&
                shopAreaTransition.LeadsToAreaContaining(player.position))
            {
                reachedShopArea = true;
                RebuildWaypointPath(
                    waypointRoot.transform,
                    player,
                    shop.transform.position);
            }

            yield return new WaitForSecondsRealtime(shopWaypointRefreshInterval);
        }

        if (waypointRoot != null)
        {
            Destroy(waypointRoot);
            if (dayTwoShopWaypoints == waypointRoot)
                dayTwoShopWaypoints = null;
        }
    }

    private IEnumerator GuidePlayerToPiko(
        GameObject waypointRoot,
        Transform player,
        Transform pikoBoard,
        MapTransition pikoAreaTransition)
    {
        bool reachedPikoArea = pikoAreaTransition == null ||
            pikoAreaTransition.LeadsToAreaContaining(player.position);
        while (waypointRoot != null &&
               player != null &&
               pikoBoard != null &&
               Vector2.Distance(player.position, pikoBoard.position) >
                   shopWaypointReachDistance)
        {
            if (!reachedPikoArea &&
                pikoAreaTransition.LeadsToAreaContaining(player.position))
            {
                reachedPikoArea = true;
                RebuildWaypointPath(
                    waypointRoot.transform,
                    player,
                    pikoBoard.position,
                    true,
                    false,
                    true);
            }

            yield return new WaitForSecondsRealtime(shopWaypointRefreshInterval);
        }

        if (waypointRoot != null)
        {
            Destroy(waypointRoot);
            if (dayTwoPikoWaypoints == waypointRoot)
                dayTwoPikoWaypoints = null;
        }
    }

    private static string GetDirectionToShop(Vector3 playerPosition, Vector2 targetPosition)
    {
        Vector2 offset = targetPosition - (Vector2)playerPosition;
        if (offset.sqrMagnitude < 0.01f)
            return "nearby";

        if (Mathf.Abs(offset.x) >= Mathf.Abs(offset.y))
            return offset.x >= 0f ? "right" : "left";

        return offset.y >= 0f ? "up" : "down";
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

    private static IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float targetAlpha,
        float duration)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (canvasGroup == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = targetAlpha;
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
        PlayerInventoryUI inventoryUI = FindFirstObjectByType<PlayerInventoryUI>(
            FindObjectsInactive.Include);
        if (inventoryUI != null)
        {
            inventoryUI.SetHotbarHiddenForTutorial(false);
            inventoryUI.SetInventoryPanelEnabled(true);
        }
        SetTricycleVisibleForCurrentDay();
        SetParentNpcsActive(currentDay >= 4);
        UpdateDay1Wall();

        if (choreManager != null)
            choreManager.ResetDailyProgress();

        if (moodManager != null)
            moodManager.ResetDailyMood();

        gameOver = false;
        dayFinished = false;
        summaryRevealComplete = false;
        Debug.Log("Starting Next Day: " + currentDay);
    }

    private void ResolveDay1Wall()
    {
        if (day1Wall != null)
            return;

        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform.name == "Day1Wall")
            {
                day1Wall = sceneTransform.gameObject;
                return;
            }
        }

        Debug.LogWarning("Day1Wall was not found; day-based wall gating is unavailable.", this);
    }

    private void UpdateDay1Wall()
    {
        if (day1Wall != null)
            day1Wall.SetActive(currentDay == 1);
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
