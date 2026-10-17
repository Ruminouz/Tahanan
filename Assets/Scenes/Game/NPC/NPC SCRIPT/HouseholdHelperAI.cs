using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HouseholdHelperAI : MonoBehaviour
{
    [Header("Mood Requirement")]
    [Tooltip("The helper can assist when the household mood is at or above this value.")]
    [SerializeField, Range(0f, 100f)] private float minimumMood = 65f;

    [Header("Daily Help")]
    [SerializeField, Min(0f)] private float checkInterval = 15f;
    [Tooltip("Maximum chores the helper can complete for each day. Element 0 is Day 1.")]
    [SerializeField, Min(0)] private int[] maximumChoresPerDay = { 1, 2, 2, 2, 2, 2, 2 };
    [Tooltip("Maximum chores used for days beyond the list above.")]
    [SerializeField, Min(0)] private int defaultMaximumChoresPerDay = 2;

    [Header("Player Requests")]
    [SerializeField, Min(0.1f)] private float movementSpeed = 4f;
    [SerializeField, Min(0f)] private float playerApproachDistance = 1.2f;
    [SerializeField, Min(0.05f)] private float choreArrivalDistance = 0.35f;

    [Header("Chore Work")]
    [SerializeField, Min(0f)] private float defaultChoreDuration = 15f;
    [SerializeField, Min(0f)] private float dishwashingDuration = 20f;
    [SerializeField, Min(0f)] private float garbageSortingDuration = 20f;
    [SerializeField, Min(0f)] private float cleanLeavesDuration = 18f;
    [SerializeField, Min(0f)] private float feedDogDuration = 15f;
    [SerializeField] private Sprite dishwashingIcon;
    [SerializeField] private Sprite garbageSortingIcon;
    [SerializeField] private Sprite cleanLeavesIcon;
    [SerializeField] private Sprite feedDogIcon;
    [SerializeField] private Sprite sweepingIcon;
    [SerializeField] private Sprite moppingIcon;
    [SerializeField, Min(0.1f)] private float choreIconSize = 0.45f;
    [SerializeField, Min(0f)] private float choreIconOffset = 0.55f;

    [Header("Bad Mood")]
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private GameObject moodBubblePrefab;
    [SerializeField, Min(0f)] private float badMoodBubbleInterval = 8f;
    [SerializeField, Range(0f, 100f)] private float badMoodBubbleChance = 50f;
    [SerializeField, Min(0f)] private float bubbleDuration = 3f;
    [SerializeField] private string badMoodLine = "I do not feel like helping right now.";

    private MoodManager moodManager;
    private DayManager dayManager;
    private ChoreManager choreManager;
    private WaterSpawner waterSpawner;
    private SweepingManager sweepingManager;
    private float nextCheckTime;
    private float nextBadMoodBubbleTime;
    private int lastCheckedDay = -1;
    private int choresHelpedToday;
    private bool helping;
    private bool restoreBodyColliderAfterHelp;
    private bool bodyColliderWasEnabled;
    private bool restoreWaypointMovementAfterHelp;
    private bool waypointMovementSuspendedForHelp;
    private WaypointMover waypointMover;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Transform player;
    private NPC npcDialogue;
    private GameObject choreIconObject;
    private SpriteRenderer choreIconRenderer;
    private GameObject choreProgressBarObject;
    private Slider choreProgressSlider;
    private int playerChoice;
    private bool dialogueChoiceReceived;

    public bool IsHelping => helping;

    private sealed class HelperTask
    {
        public Chore Chore;
        public bool MopWetAreas;
        public bool SweepDust;
        public string DisplayName;
        public int AreasCleanedByHelper;

        public HelperTask(Chore chore, bool mopWetAreas, bool sweepDust, string displayName)
        {
            Chore = chore;
            MopWetAreas = mopWetAreas;
            SweepDust = sweepDust;
            DisplayName = displayName;
        }
    }

    private void Start()
    {
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        waterSpawner = FindFirstObjectByType<WaterSpawner>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
        waypointMover = GetComponent<WaypointMover>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        npcDialogue = GetComponent<NPC>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        ResetForDay();
    }

    private void Update()
    {
        if (moodManager == null)
            moodManager = MoodManager.Instance != null
                ? MoodManager.Instance
                : FindFirstObjectByType<MoodManager>();

        if (dayManager == null)
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();

        if (choreManager == null)
            choreManager = FindFirstObjectByType<ChoreManager>();

        if (waterSpawner == null)
            waterSpawner = FindFirstObjectByType<WaterSpawner>();

        if (sweepingManager == null)
            sweepingManager = FindFirstObjectByType<SweepingManager>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (dayManager == null || moodManager == null || choreManager == null)
            return;

        if (lastCheckedDay != dayManager.CurrentDay)
            ResetForDay();

        if (moodManager.Mood < minimumMood)
        {
            TryShowBadMoodBubble();
            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + checkInterval;
            return;
        }

        if (helping || PauseController.IsGamePaused || Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;
        if (choresHelpedToday >= GetMaximumChoresForCurrentDay())
            return;

        List<HelperTask> availableTasks = GetAvailableTasks();
        if (availableTasks.Count == 0)
            return;

        if (npcDialogue == null)
        {
            Debug.LogWarning(name + " cannot offer help because its NPC dialogue is missing.", this);
            return;
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("Helper cannot approach because the Player object was not found.", this);
            return;
        }

        StartCoroutine(OfferHelp(availableTasks));
    }

    public void Interact()
    {
        if (helping || PauseController.IsGamePaused)
            return;

        if (moodManager == null)
            moodManager = MoodManager.Instance != null
                ? MoodManager.Instance
                : FindFirstObjectByType<MoodManager>();

        if (moodManager == null || npcDialogue == null)
        {
            Debug.LogWarning(name + " cannot offer help because its mood manager or NPC dialogue is missing.", this);
            return;
        }

        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        }

        if (dayManager == null)
        {
            Debug.LogWarning(name + " cannot offer help because the DayManager is missing.", this);
            return;
        }

        if (moodManager.Mood < minimumMood)
        {
            npcDialogue.StartHelperDialogue(
                "Medyo hindi maganda ang mood ko ngayon.",
                "Baka makakatulong ako kapag mas maganda na ang mood natin.",
                null,
                null);
            return;
        }

        if (choresHelpedToday >= GetMaximumChoresForCurrentDay())
        {
            npcDialogue.StartHelperDialogue(
                "Dahil good mood ako ngayon!",
                "Nakatulong na ako sa maximum na chores ko ngayong araw.",
                null,
                null);
            return;
        }

        List<HelperTask> availableTasks = GetAvailableTasks();
        if (availableTasks.Count == 0)
        {
            npcDialogue.StartHelperDialogue(
                "Dahil good mood ako ngayon!",
                "Wala pang chore na kailangan ng tulong.",
                null,
                null);
            return;
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("Helper cannot approach because the Player object was not found.", this);
            return;
        }

        StartCoroutine(OfferHelp(availableTasks));
    }

    private void OnDisable()
    {
        HideChoreIcon();
        RestoreBodyCollider();
        if (waypointMovementSuspendedForHelp && waypointMover != null)
            waypointMover.SetMovementEnabled(restoreWaypointMovementAfterHelp);
        waypointMover?.ResetWaypointRoute();
        restoreWaypointMovementAfterHelp = false;
        waypointMovementSuspendedForHelp = false;
        helping = false;
    }

    private void ResetForDay()
    {
        lastCheckedDay = dayManager != null ? dayManager.CurrentDay : -1;
        choresHelpedToday = 0;
        nextCheckTime = Time.time + checkInterval;
        nextBadMoodBubbleTime = Time.time + badMoodBubbleInterval;
    }

    private int GetMaximumChoresForCurrentDay()
    {
        int dayIndex = dayManager.CurrentDay - 1;
        if (maximumChoresPerDay != null && dayIndex >= 0 && dayIndex < maximumChoresPerDay.Length)
            return maximumChoresPerDay[dayIndex];

        return defaultMaximumChoresPerDay;
    }

    private List<HelperTask> GetAvailableTasks()
    {
        List<HelperTask> availableTasks = new List<HelperTask>();
        Chore[] activeChores = dayManager.GetActiveChores();
        if (activeChores == null)
            activeChores = new Chore[0];

        foreach (Chore chore in activeChores)
        {
            if (!IsAvailableForHelper(chore))
                continue;

            bool isSweepTask = IsSweepDustChore(chore);
            if (isSweepTask && (sweepingManager == null || sweepingManager.RemainingDust == 0))
                continue;

            bool isMopTask = IsMopChore(chore) &&
                waterSpawner != null &&
                waterSpawner.HasActiveWater;
            string displayName = GetChoreDisplayName(chore);
            AddTaskIfSupported(
                availableTasks,
                new HelperTask(chore, isMopTask, isSweepTask, displayName));
        }

        bool hasMopTask = false;
        bool hasSweepTask = false;
        foreach (HelperTask task in availableTasks)
        {
            if (task.MopWetAreas)
                hasMopTask = true;
            if (task.SweepDust)
                hasSweepTask = true;
        }

        if (!hasMopTask && waterSpawner != null && waterSpawner.HasActiveWater)
            AddTaskIfSupported(availableTasks, new HelperTask(null, true, false, "Mop Wet Floor"));

        if (!hasSweepTask && sweepingManager != null && sweepingManager.RemainingDust > 0)
            AddTaskIfSupported(availableTasks, new HelperTask(null, false, true, "Sweep Dust"));

        return availableTasks;
    }

    private bool IsAvailableForHelper(Chore chore)
    {
        return chore != null && !chore.IsCompleted && !chore.IsMissed;
    }

    private void AddTaskIfSupported(List<HelperTask> tasks, HelperTask task)
    {
        if (tasks.Count < 9)
            tasks.Add(task);
    }

    private IEnumerator OfferHelp(List<HelperTask> availableTasks)
    {
        if (waypointMover == null)
        {
            Debug.LogWarning(name + " cannot help with chores because it has no WaypointMover.", this);
            yield break;
        }

        helping = true;
        restoreWaypointMovementAfterHelp = waypointMover.MovementEnabled;
        waypointMovementSuspendedForHelp = true;
        waypointMover.SetMovementEnabled(false);
        waypointMover.ResetWaypointRoute();
        restoreBodyColliderAfterHelp = bodyCollider != null;
        bodyColliderWasEnabled = restoreBodyColliderAfterHelp && bodyCollider.enabled;
        if (bodyColliderWasEnabled)
            bodyCollider.enabled = false;

        try
        {
            while (PauseController.IsGamePaused)
                yield return null;

            if (player != null)
            {
                DisableBodyColliderForTravel();
                while (player != null &&
                       Vector2.Distance(transform.position, player.position) > playerApproachDistance)
                {
                    if (PauseController.IsGamePaused)
                    {
                        yield return null;
                        continue;
                    }

                    Vector2 directionFromPlayer = (Vector2)transform.position - (Vector2)player.position;
                    if (directionFromPlayer.sqrMagnitude < 0.01f)
                        directionFromPlayer = Vector2.right;
                    Vector2 target = (Vector2)player.position +
                        directionFromPlayer.normalized * playerApproachDistance;
                    MoveHelperTowards(target, 0.1f);

                    yield return null;
                }

                RestoreBodyCollider();
            }

            List<string> choices = new List<string>(availableTasks.Count);
            foreach (HelperTask task in availableTasks)
                choices.Add(task.DisplayName);

            dialogueChoiceReceived = false;
            playerChoice = -1;
            bool dialogueStarted = npcDialogue != null &&
                npcDialogue.StartHelperDialogue(
                    "Dahil good mood ako ngayon!",
                    "Saan mo kailangan ng tulong?",
                    choices,
                    choice =>
                    {
                        playerChoice = choice;
                        dialogueChoiceReceived = true;
                    });
            if (!dialogueStarted)
                yield break;

            while (!dialogueChoiceReceived)
                yield return null;

            int selectedChoice = playerChoice + 1;

            if (selectedChoice <= 0 || selectedChoice > availableTasks.Count)
                yield break;

            HelperTask selectedTask = availableTasks[selectedChoice - 1];
            if (!IsTaskStillAvailable(selectedTask))
            {
                SpeechBubbleCanvas.Show(
                    bubbleAnchor != null ? bubbleAnchor : transform,
                    "That chore is already taken care of!",
                    bubbleDuration);
                yield break;
            }

            float workDuration = selectedTask.Chore != null
                ? GetChoreDuration(selectedTask.Chore)
                : defaultChoreDuration;

            if (selectedTask.MopWetAreas || selectedTask.SweepDust)
            {
                ShowChoreAnnouncement(selectedTask);
                ShowChoreIcon(selectedTask);
                yield return StartCoroutine(HelpCleanAreas(selectedTask, workDuration));

                bool completedAllAreas = selectedTask.MopWetAreas
                    ? waterSpawner != null && !waterSpawner.HasActiveWater
                    : sweepingManager != null && sweepingManager.RemainingDust == 0;
                if (completedAllAreas)
                    CompleteHelperTask(selectedTask);
            }
            else
            {
                Vector2 taskPosition;
                if (!TryGetTaskPosition(selectedTask, out taskPosition))
                    yield break;

                DisableBodyColliderForTravel();
                bool arrivedAtChore = false;
                while (!arrivedAtChore)
                {
                    if (!IsTaskStillAvailable(selectedTask))
                        yield break;

                    if (!PauseController.IsGamePaused)
                        arrivedAtChore = MoveHelperTowards(taskPosition, choreArrivalDistance);

                    yield return null;
                }

                if (!IsTaskStillAvailable(selectedTask))
                    yield break;

                PinAtPosition(taskPosition);
                ShowChoreAnnouncement(selectedTask);
                ShowChoreIcon(selectedTask);
                float workElapsed = 0f;
                SetChoreProgress(workDuration > 0f ? 0f : 1f);
                while (workElapsed < workDuration && IsTaskStillAvailable(selectedTask))
                {
                    if (!PauseController.IsGamePaused)
                    {
                        waypointMover.SetMovementEnabled(false);
                        PinAtPosition(taskPosition);
                        workElapsed += Time.deltaTime;
                        SetChoreProgress(workElapsed / workDuration);
                    }

                    yield return null;
                }

                if (IsTaskStillAvailable(selectedTask))
                    CompleteHelperTask(selectedTask);
            }
        }
        finally
        {
            HideChoreIcon();
            RestoreBodyCollider();
            waypointMover.ResetWaypointRoute();
            waypointMover.SetMovementEnabled(restoreWaypointMovementAfterHelp);
            helping = false;
            restoreWaypointMovementAfterHelp = false;
            waypointMovementSuspendedForHelp = false;
        }
    }

    private IEnumerator HelpCleanAreas(HelperTask task, float workDuration)
    {
        int initialAreaCount = task.MopWetAreas
            ? waterSpawner != null ? waterSpawner.RemainingWetAreas : 0
            : sweepingManager != null ? sweepingManager.RemainingDust : 0;
        float workPerArea = initialAreaCount > 0
            ? workDuration / initialAreaCount
            : 0f;
        float workElapsed = 0f;

        DisableBodyColliderForTravel();
        while (IsTaskStillAvailable(task))
        {
            WetArea wetArea = null;
            DustSpot dustSpot = null;
            bool foundArea = task.MopWetAreas
                ? waterSpawner != null && waterSpawner.TryGetRandomWetArea(out wetArea)
                : sweepingManager != null && sweepingManager.TryGetRandomDustSpot(out dustSpot);
            if (!foundArea)
                break;

            Vector2 targetPosition = task.MopWetAreas
                ? wetArea.transform.position
                : dustSpot.transform.position;
            while (IsAreaActive(task, wetArea, dustSpot) &&
                   Vector2.Distance(transform.position, targetPosition) > choreArrivalDistance)
            {
                if (!PauseController.IsGamePaused)
                    MoveHelperTowards(targetPosition, choreArrivalDistance);

                yield return null;
            }

            if (!IsAreaActive(task, wetArea, dustSpot))
                continue;

            PinAtPosition(targetPosition);
            float areaWorkElapsed = 0f;
            while (areaWorkElapsed < workPerArea &&
                   IsAreaActive(task, wetArea, dustSpot) &&
                   IsTaskStillAvailable(task))
            {
                if (!PauseController.IsGamePaused)
                {
                    PinAtPosition(targetPosition);
                    areaWorkElapsed += Time.deltaTime;
                    workElapsed += Time.deltaTime;
                    SetChoreProgress(workDuration > 0f ? workElapsed / workDuration : 1f);
                }

                yield return null;
            }

            if (!IsAreaActive(task, wetArea, dustSpot))
                continue;

            if (task.MopWetAreas)
                wetArea.Clean();
            else
                dustSpot.Clean();

            task.AreasCleanedByHelper++;
        }

        SetChoreProgress(1f);
    }

    private bool IsAreaActive(HelperTask task, WetArea wetArea, DustSpot dustSpot)
    {
        return task.MopWetAreas
            ? wetArea != null && wetArea.gameObject.activeInHierarchy
            : dustSpot != null && dustSpot.gameObject.activeInHierarchy;
    }

    private bool TryGetTaskPosition(HelperTask task, out Vector2 position)
    {
        if (task.MopWetAreas)
        {
            if (waterSpawner != null)
                return waterSpawner.TryGetNearestWetAreaPosition(transform.position, out position);

            position = Vector2.zero;
            return false;
        }

        if (task.SweepDust)
        {
            if (sweepingManager != null)
                return sweepingManager.TryGetNearestDustSpotPosition(transform.position, out position);

            position = Vector2.zero;
            return false;
        }

        if (task.Chore != null)
        {
            position = task.Chore.transform.position;
            return true;
        }

        position = Vector2.zero;
        return false;
    }

    private bool IsTaskStillAvailable(HelperTask task)
    {
        if (task.MopWetAreas)
            return waterSpawner != null && waterSpawner.HasActiveWater;

        if (task.SweepDust)
            return sweepingManager != null && sweepingManager.RemainingDust > 0;

        return IsAvailableForHelper(task.Chore) &&
            !IsSweepDustChore(task.Chore);
    }

    private void CompleteHelperTask(HelperTask task)
    {
        bool completedWork;
        if (task.MopWetAreas)
        {
            completedWork = waterSpawner != null &&
                task.AreasCleanedByHelper > 0 &&
                !waterSpawner.HasActiveWater;
            if (completedWork && task.Chore != null && !task.Chore.IsCompleted)
                task.Chore.Complete();
        }
        else if (task.SweepDust)
        {
            completedWork = sweepingManager != null &&
                task.AreasCleanedByHelper > 0 &&
                sweepingManager.RemainingDust == 0;
            if (completedWork &&
                task.Chore != null &&
                !task.Chore.IsCompleted)
                task.Chore.Complete();
        }
        else
        {
            completedWork = task.Chore != null;
            if (completedWork)
                task.Chore.Complete();
        }

        if (!completedWork)
            return;

        if (task.Chore == null && choreManager != null)
            choreManager.ApplyHelperMoodCost();

        if (task.Chore != null && task.Chore.IsCompleted && choreManager != null)
            choreManager.MarkHelperCompleted(task.Chore);

        choresHelpedToday++;
        Debug.Log(name + " helped with " + task.DisplayName + ".");
    }

    private bool IsSweepDustChore(Chore chore)
    {
        if (chore == null)
            return false;

        if (dayManager != null && chore == dayManager.SweepDustChore)
            return true;

        string choreName = chore != null ? chore.ChoreName ?? string.Empty : string.Empty;
        return choreName.IndexOf("sweep", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
            (choreName.IndexOf("dust", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             string.Equals(choreName, "Sweep", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(choreName, "Sweeping", System.StringComparison.OrdinalIgnoreCase));
    }

    private bool IsMopChore(Chore chore)
    {
        string choreName = chore != null ? chore.ChoreName ?? string.Empty : string.Empty;
        return choreName.IndexOf("mop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            choreName.IndexOf("wet", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string GetChoreDisplayName(Chore chore)
    {
        string choreName = chore.ChoreName ?? string.Empty;
        if (IsSweepDustChore(chore))
            return "Sweep Dust";

        if (chore is CleanLeavesChore ||
            chore is SegregateWasteChore ||
            choreName.IndexOf("toy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            choreName.IndexOf("pickup", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Pick Up Toys";

        return string.IsNullOrWhiteSpace(choreName) ? "Chore" : choreName;
    }

    private bool MoveHelperTowards(Vector2 target, float arriveDistance)
    {
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            target,
            movementSpeed * Time.deltaTime);

        if (body != null)
            body.position = nextPosition;
        else
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

        return Vector2.Distance(nextPosition, target) <= arriveDistance;
    }

    private void PinAtPosition(Vector2 position)
    {
        if (body != null)
            body.position = position;
        else
            transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void RestoreBodyCollider()
    {
        if (!restoreBodyColliderAfterHelp)
            return;

        if (bodyCollider != null)
            bodyCollider.enabled = bodyColliderWasEnabled;

        restoreBodyColliderAfterHelp = false;
    }

    private void DisableBodyColliderForTravel()
    {
        if (bodyCollider == null)
            return;

        if (!restoreBodyColliderAfterHelp)
        {
            bodyColliderWasEnabled = bodyCollider.enabled;
            restoreBodyColliderAfterHelp = true;
        }

        bodyCollider.enabled = false;
    }

    private float GetChoreDuration(Chore chore)
    {
        if (chore is DishwashingChore)
            return dishwashingDuration;
        if (chore is GarbageChore)
            return garbageSortingDuration;
        if (chore is CleanLeavesChore || chore is SegregateWasteChore)
            return cleanLeavesDuration;
        if (chore is FeedDogChore)
            return feedDogDuration;

        return defaultChoreDuration;
    }

    private void ShowChoreIcon(HelperTask task)
    {
        Sprite icon = GetChoreIcon(task);
        if (icon == null)
        {
            if (choreIconObject != null)
                choreIconObject.SetActive(false);
        }
        else
        {
            if (choreIconObject == null)
            {
                choreIconObject = new GameObject("ChoreIcon");
                choreIconRenderer = choreIconObject.AddComponent<SpriteRenderer>();
            }

            Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
            Transform iconTransform = choreIconObject.transform;
            iconTransform.SetParent(anchor, false);
            iconTransform.localPosition = new Vector3(0f, choreIconOffset, 0f);
            iconTransform.localRotation = Quaternion.identity;

            choreIconRenderer.sprite = icon;
            choreIconRenderer.enabled = true;
            SpriteRenderer npcRenderer = GetComponent<SpriteRenderer>();
            choreIconRenderer.sortingOrder = npcRenderer != null ? npcRenderer.sortingOrder + 1 : 1;
            if (npcRenderer != null)
                choreIconRenderer.sortingLayerID = npcRenderer.sortingLayerID;

            float largestDimension = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            float scale = largestDimension > 0f ? choreIconSize / largestDimension : 1f;
            iconTransform.localScale = Vector3.one * scale;
            choreIconObject.SetActive(true);
        }

        Transform progressAnchor = bubbleAnchor != null ? bubbleAnchor : transform;
        SpriteRenderer helperRenderer = GetComponent<SpriteRenderer>();
        ShowChoreProgressBar(progressAnchor, helperRenderer);
    }

    private void ShowChoreProgressBar(Transform anchor, SpriteRenderer npcRenderer)
    {
        if (choreProgressBarObject == null)
        {
            choreProgressBarObject = new GameObject(
                "ChoreProgressBar",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(Slider),
                typeof(Image));

            Canvas canvas = choreProgressBarObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.pixelPerfect = false;

            RectTransform canvasRect = choreProgressBarObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(140f, 16f);
            choreProgressBarObject.transform.localScale = Vector3.one * 0.0075f;

            Image background = choreProgressBarObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.8f);
            background.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(choreProgressBarObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 1f);
            fillRect.offsetMax = new Vector2(-2f, -1f);

            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color(0.35f, 0.9f, 0.3f, 1f);
            fill.raycastTarget = false;

            choreProgressSlider = choreProgressBarObject.GetComponent<Slider>();
            choreProgressSlider.fillRect = fillRect;
            choreProgressSlider.targetGraphic = background;
            choreProgressSlider.direction = Slider.Direction.LeftToRight;
            choreProgressSlider.minValue = 0f;
            choreProgressSlider.maxValue = 1f;
            choreProgressSlider.interactable = false;
        }

        Transform progressTransform = choreProgressBarObject.transform;
        progressTransform.SetParent(anchor, false);
        progressTransform.localPosition = new Vector3(
            0f,
            choreIconOffset + choreIconSize * 0.5f + 0.08f,
            0f);
        progressTransform.localRotation = Quaternion.identity;

        Canvas progressCanvas = choreProgressBarObject.GetComponent<Canvas>();
        progressCanvas.sortingLayerID = npcRenderer != null ? npcRenderer.sortingLayerID : 0;
        progressCanvas.sortingOrder = npcRenderer != null ? npcRenderer.sortingOrder + 2 : 2;
        choreProgressBarObject.SetActive(true);
    }

    private void SetChoreProgress(float progress)
    {
        if (choreProgressSlider != null)
            choreProgressSlider.value = Mathf.Clamp01(progress);
    }

    private Sprite GetChoreIcon(HelperTask task)
    {
        if (task.MopWetAreas)
            return moppingIcon;
        if (task.SweepDust)
            return sweepingIcon;

        Chore chore = task.Chore;
        if (chore is DishwashingChore)
            return dishwashingIcon;
        if (chore is GarbageChore)
            return garbageSortingIcon;
        if (chore is CleanLeavesChore || chore is SegregateWasteChore)
            return cleanLeavesIcon;
        if (chore is FeedDogChore)
            return feedDogIcon;

        return null;
    }

    private void HideChoreIcon()
    {
        if (choreIconObject != null)
            choreIconObject.SetActive(false);
        if (choreProgressBarObject != null)
            choreProgressBarObject.SetActive(false);
    }

    private void ShowChoreAnnouncement(HelperTask task)
    {
        string message;
        if (task.MopWetAreas)
            message = "Ako na ang magmo-mop!";
        else if (task.SweepDust)
            message = "Ako na ang magwawalis!";
        else if (task.DisplayName == "Pick Up Toys")
            message = "Ako na ang magliligpit ng mga laruan!";
        else if (task.Chore is DishwashingChore)
            message = "Ako na mag huhugas";
        else if (task.Chore is GarbageChore ||
                 (task.Chore != null &&
                  string.Equals(task.Chore.ChoreName, "Garbage", System.StringComparison.OrdinalIgnoreCase)))
            message = "Ako na magtatapon ng basura";
        else
            message = "Ako na ang bahala sa " + task.DisplayName + "!";

        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        SpeechBubbleCanvas.Show(anchor, message, bubbleDuration);
    }

    private void TryShowBadMoodBubble()
    {
        if (Time.time < nextBadMoodBubbleTime ||
            Random.Range(0f, 100f) > badMoodBubbleChance)
            return;

        nextBadMoodBubbleTime = Time.time + badMoodBubbleInterval;
        ShowBadMoodBubble();
    }

    private void ShowBadMoodBubble()
    {
        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        SpeechBubbleCanvas.Show(anchor, badMoodLine, bubbleDuration);
    }
}
