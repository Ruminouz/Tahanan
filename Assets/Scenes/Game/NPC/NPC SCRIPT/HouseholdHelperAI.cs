using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;

public class HouseholdHelperAI : MonoBehaviour
{
    [Header("Mood Requirement")]
    [Tooltip("The helper can assist when the household mood is at or above this value.")]
    [SerializeField, Range(0f, 100f)] private float minimumMood = 65f;
    [Tooltip("Chance to help when the check interval is reached.")]
    [SerializeField, Range(0f, 100f)] private float helpChance = 50f;

    [Header("Daily Help")]
    [SerializeField, Min(0f)] private float checkInterval = 15f;
    [Tooltip("Maximum chores the helper can complete for each day. Element 0 is Day 1.")]
    [SerializeField, Min(0)] private int[] maximumChoresPerDay = { 1, 2, 2, 2, 2, 2, 2 };
    [Tooltip("Maximum chores used for days beyond the list above.")]
    [SerializeField, Min(0)] private int defaultMaximumChoresPerDay = 2;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float arriveDistance = 0.2f;
    [SerializeField, Min(0.1f)] private float helpTimeout = 20f;
    [SerializeField, Min(0.05f)] private float choreStuckTimeout = 0.5f;
    [SerializeField, Min(1f)] private float choreSpeedMultiplier = 2f;
    [SerializeField, Min(0f)] private float playerChoreDistance = 2f;

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
    private float nextCheckTime;
    private float nextBadMoodBubbleTime;
    private int lastCheckedDay = -1;
    private int choresHelpedToday;
    private bool helping;
    private Rigidbody2D body;
    private WaypointMover waypointMover;
    private Transform player;
    private float choreStuckTimer;

    private void Start()
    {
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        body = GetComponent<Rigidbody2D>();
        waypointMover = GetComponent<WaypointMover>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        ResetForDay();
    }

    private void Update()
    {
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

        if (choresHelpedToday >= GetMaximumChoresForCurrentDay() || helping || Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;

        if (Random.Range(0f, 100f) > helpChance)
            return;

        Chore chore = GetRandomAvailableChore();
        if (chore != null)
            StartCoroutine(HelpWithChore(chore));
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

    private Chore GetRandomAvailableChore()
    {
        Chore[] activeChores = dayManager.GetActiveChores();
        if (activeChores == null)
            return null;

        List<Chore> availableChores = new List<Chore>();
        foreach (Chore chore in activeChores)
        {
            if (IsAvailableForHelper(chore))
                availableChores.Add(chore);
        }

        if (availableChores.Count == 0)
            return null;

        return availableChores[Random.Range(0, availableChores.Count)];
    }

    private bool IsAvailableForHelper(Chore chore)
    {
        if (chore == null || chore.IsCompleted || chore.IsMissed)
            return false;

        if (chore.ChoreName == "Sweep Dust")
            return false;

        return player == null ||
            Vector2.Distance(player.position, chore.transform.position) > playerChoreDistance;
    }

    private IEnumerator HelpWithChore(Chore chore)
    {
        if (waypointMover == null)
        {
            Debug.LogWarning(name + " cannot help with chores because it has no WaypointMover.", this);
            yield break;
        }

        helping = true;
        bool restoreWaypointMovement = waypointMover.MovementEnabled;
        waypointMover.SetMovementEnabled(false);
        waypointMover.ResetWaypointRoute();
        ShowChoreAnnouncement(chore);

        float elapsed = 0f;
        choreStuckTimer = 0f;
        Vector2 previousPosition = GetCurrentPosition();
        while (chore != null && !chore.IsCompleted && !chore.IsMissed &&
               Vector2.Distance(GetCurrentPosition(), chore.transform.position) > arriveDistance &&
               elapsed < helpTimeout)
        {
            if (!PauseController.IsGamePaused)
            {
                Vector2 currentPosition = GetCurrentPosition();
                Vector2 movement = waypointMover.GetMovementAlongWaypointRoute(
                    chore.transform.position,
                    moveSpeed * choreSpeedMultiplier,
                    arriveDistance,
                    true,
                    player);
                Vector2 nextPosition = currentPosition + movement;

                if (body != null)
                    body.MovePosition(nextPosition);
                else
                    transform.position = nextPosition;

                elapsed += Time.deltaTime;
                yield return null;

                if (PauseController.IsGamePaused)
                    continue;

                Vector2 actualPosition = GetCurrentPosition();
                if ((actualPosition - previousPosition).sqrMagnitude <= 0.000001f)
                    choreStuckTimer += Time.deltaTime;
                else
                    choreStuckTimer = 0f;

                if (choreStuckTimer >= choreStuckTimeout)
                {
                    waypointMover.ResetWaypointRoute();
                    choreStuckTimer = 0f;
                }

                previousPosition = actualPosition;
                continue;
            }

            yield return null;
        }

        if (chore != null && !chore.IsCompleted && !chore.IsMissed &&
            Vector2.Distance(GetCurrentPosition(), chore.transform.position) <= arriveDistance &&
            IsAvailableForHelper(chore))
        {
            chore.Complete();
            choreManager.MarkHelperCompleted(chore);
            choresHelpedToday++;
            Debug.Log(name + " helped complete " + chore.ChoreName + ".");
        }

        waypointMover.ResetWaypointRoute();
        waypointMover.SetMovementEnabled(restoreWaypointMovement);

        helping = false;
    }

    private void ShowChoreAnnouncement(Chore chore)
    {
        string message = null;
        if (chore is DishwashingChore)
            message = "Ako na mag huhugas";
        else if (chore is GarbageChore ||
                 string.Equals(chore.ChoreName, "Garbage", System.StringComparison.OrdinalIgnoreCase))
            message = "Ako na magtatapon ng basura";
        else if (string.Equals(chore.ChoreName, "PickUp", System.StringComparison.OrdinalIgnoreCase))
            message = "Nagkalat nanaman si bunso";

        if (!string.IsNullOrEmpty(message))
        {
            Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
            SpeechBubbleCanvas.Show(anchor, message, bubbleDuration);
        }
    }

    private Vector2 GetCurrentPosition()
    {
        return body != null ? body.position : (Vector2)transform.position;
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
