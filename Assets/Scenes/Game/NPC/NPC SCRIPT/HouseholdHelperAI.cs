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
    private Transform player;

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
        helping = true;
        float elapsed = 0f;
        while (chore != null && !chore.IsCompleted && !chore.IsMissed &&
               Vector2.Distance(transform.position, chore.transform.position) > arriveDistance &&
               elapsed < helpTimeout)
        {
            if (!PauseController.IsGamePaused)
            {
                Vector2 nextPosition = Vector2.MoveTowards(
                    transform.position,
                    chore.transform.position,
                    moveSpeed * Time.deltaTime);

                if (body != null)
                    body.MovePosition(nextPosition);
                else
                    transform.position = nextPosition;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (chore != null && !chore.IsCompleted && !chore.IsMissed &&
            Vector2.Distance(transform.position, chore.transform.position) <= arriveDistance &&
            IsAvailableForHelper(chore))
        {
            chore.Complete();
            choreManager.MarkHelperCompleted(chore);
            choresHelpedToday++;
            Debug.Log(name + " helped complete " + chore.ChoreName + ".");
        }

        helping = false;
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
        if (moodBubblePrefab == null)
            return;

        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        GameObject bubble = Instantiate(moodBubblePrefab, anchor.position, Quaternion.identity, anchor);
        TMP_Text tmpText = bubble.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
            tmpText.text = badMoodLine;

        Destroy(bubble, bubbleDuration);
    }
}
