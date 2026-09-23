using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractionManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float checkInterval = 5f;
    [SerializeField, Min(0.1f)] private float conversationDuration = 4f;
    [SerializeField, Min(0f)] private float interactionCooldown = 12f;
    [SerializeField, Min(0.1f)] private float approachSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float arriveDistance = 0.25f;

    [Header("Positioning")]
    [SerializeField, Min(0.1f)] private float conversationDistance = 0.8f;
    [SerializeField, Min(0f)] private float bubbleDuration = 2.5f;
    [SerializeField] private List<NPCSocialParticipant> participants = new();

    private MoodManager moodManager;
    private float nextCheckTime;
    private float nextInteractionTime;
    private bool interactionActive;

    private void Start()
    {
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();

        if (participants.Count == 0)
            participants.AddRange(FindObjectsByType<NPCSocialParticipant>(FindObjectsSortMode.None));

        if (moodManager != null)
            moodManager.MoodChanged += HandleMoodChanged;
        nextCheckTime = Time.time + checkInterval;
    }

    private void OnDestroy()
    {
        if (moodManager != null)
            moodManager.MoodChanged -= HandleMoodChanged;
    }

    private void HandleMoodChanged(HouseholdMood previousMood, HouseholdMood newMood)
    {
        if (!interactionActive)
            nextCheckTime = Time.time;
    }

    private void Update()
    {
        if (interactionActive || moodManager == null ||
            PauseController.IsGamePaused || Time.time < nextCheckTime ||
            Time.time < nextInteractionTime)
            return;

        nextCheckTime = Time.time + checkInterval;
        NPCSocialParticipant first = FindAvailableParticipant();
        NPCSocialParticipant second = FindAvailableParticipant(first);
        if (first != null && second != null)
            StartCoroutine(RunConversation(first, second, moodManager.CurrentMood));
    }

    private NPCSocialParticipant FindAvailableParticipant(
        NPCSocialParticipant excluded = null)
    {
        NPCSocialParticipant selected = null;
        float nearestDistance = float.MaxValue;

        foreach (NPCSocialParticipant candidate in participants)
        {
            if (candidate == null || candidate == excluded || !candidate.CanSocialize)
                continue;

            float distance = excluded == null
                ? 0f
                : Vector2.Distance(candidate.transform.position, excluded.transform.position);
            if (distance < nearestDistance)
            {
                selected = candidate;
                nearestDistance = distance;
            }
        }

        return selected;
    }

    private IEnumerator RunConversation(
        NPCSocialParticipant first,
        NPCSocialParticipant second,
        HouseholdMood mood)
    {
        interactionActive = true;
        first.WaypointMover.SetMovementEnabled(false);
        second.WaypointMover.SetMovementEnabled(false);

        Vector2 center = (first.transform.position + second.transform.position) * 0.5f;
        Vector2 direction = (second.transform.position - first.transform.position).normalized;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        Vector2 firstTarget = center - direction * conversationDistance * 0.5f;
        Vector2 secondTarget = center + direction * conversationDistance * 0.5f;
        float elapsed = 0f;
        bool linesShown = false;

        while (elapsed < conversationDuration &&
               first != null && second != null &&
               !PauseController.IsGamePaused)
        {
            first.WaypointMover.MoveTowards(firstTarget, approachSpeed, arriveDistance);
            second.WaypointMover.MoveTowards(secondTarget, approachSpeed, arriveDistance);

            if (!linesShown && elapsed > 0.5f)
            {
                ShowLine(first, first.GetLine(mood));
                ShowLine(second, second.GetLine(mood));
                linesShown = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (first != null)
            first.WaypointMover.SetMovementEnabled(true);
        if (second != null)
            second.WaypointMover.SetMovementEnabled(true);

        nextInteractionTime = Time.time + interactionCooldown;
        interactionActive = false;
    }

    private void ShowLine(NPCSocialParticipant participant, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        SpeechBubbleCanvas.Show(participant.SpeechAnchor, line, bubbleDuration);
    }
}
