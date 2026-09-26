using UnityEngine;
using System.Collections;

public class CatInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float checkInterval = 8f;
    [SerializeField, Range(0f, 100f)] private float interactionChance = 45f;
    [SerializeField, Min(0.1f)] private float approachSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float approachDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float approachTimeout = 20f;
    [SerializeField, Min(5f)] private float pettingDuration = 5f;
    [SerializeField, Min(0f)] private float moodGain = 10f;

    [Header("Heart Bubble")]
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private GameObject heartBubblePrefab;
    [SerializeField, Min(0f)] private float bubbleDuration = 2f;

    private float nextCheckTime;
    private WaypointMover currentNpc;

    private void Start()
    {
        nextCheckTime = Time.time + checkInterval;
    }

    private void Update()
    {
        if (currentNpc != null || Time.time < nextCheckTime || PauseController.IsGamePaused)
            return;

        nextCheckTime = Time.time + checkInterval;
        if (Random.Range(0f, 100f) > interactionChance)
            return;

        WaypointMover[] npcs = FindObjectsByType<WaypointMover>(FindObjectsSortMode.None);
        Shuffle(npcs);
        foreach (WaypointMover npc in npcs)
        {
            if (npc != null && npc.MovementEnabled)
            {
                StartCoroutine(PetCat(npc));
                return;
            }
        }
    }

    private IEnumerator PetCat(WaypointMover npc)
    {
        currentNpc = npc;
        bool wasMoving = npc.MovementEnabled;
        npc.SetMovementEnabled(false);
        npc.ResetWaypointRoute();
        SpeechBubbleCanvas.Show(npc.transform, "Asan si ming ming", bubbleDuration);

        Collider2D catCollider = GetPettingCollider();
        float elapsed = 0f;
        bool reachedCat = false;
        while (npc != null && elapsed < approachTimeout)
        {
            Vector2 currentPosition = GetNpcPosition(npc);
            Vector2 targetPosition = GetPettingPosition(npc, currentPosition, catCollider);
            if (Vector2.Distance(currentPosition, targetPosition) <= 0.1f)
            {
                reachedCat = true;
                break;
            }

            if (!PauseController.IsGamePaused)
            {
                Vector2 movement = npc.GetMovementAlongWaypointRoute(
                    targetPosition,
                    approachSpeed,
                    Mathf.Max(0.1f, approachDistance),
                    true);
                Vector2 nextPosition = currentPosition + movement;
                Rigidbody2D body = npc.GetComponent<Rigidbody2D>();
                if (body != null)
                    body.MovePosition(nextPosition);
                else
                    npc.transform.position = nextPosition;

                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        if (npc == null)
        {
            currentNpc = null;
            yield break;
        }

        if (reachedCat)
        {
            ShowHeartBubble();
            yield return new WaitForSeconds(Mathf.Max(5f, pettingDuration));

            if (npc != null)
            {
                MoodManager moodManager = MoodManager.Instance != null
                    ? MoodManager.Instance
                    : FindFirstObjectByType<MoodManager>();
                if (moodManager != null)
                    moodManager.AddMood(moodGain);
            }
        }
        else
        {
            Debug.LogWarning(name + " could not find a clear waypoint route to the cat.", this);
        }

        if (npc != null)
        {
            npc.ResetWaypointRoute();
            npc.SetMovementEnabled(wasMoving);
        }

        currentNpc = null;
    }

    private Collider2D GetPettingCollider()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null && candidate.enabled && !candidate.isTrigger)
                return candidate;
        }

        return null;
    }

    private Vector2 GetPettingPosition(WaypointMover npc, Vector2 npcPosition, Collider2D catCollider)
    {
        if (catCollider == null)
            return Vector2.MoveTowards(transform.position, npcPosition, approachDistance);

        Vector2 closestPoint = catCollider.ClosestPoint(npcPosition);
        Vector2 direction = npcPosition - closestPoint;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = npcPosition - (Vector2)transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;

        Collider2D npcCollider = npc.GetComponent<Collider2D>();
        float npcClearance = npcCollider != null
            ? Mathf.Max(npcCollider.bounds.extents.x, npcCollider.bounds.extents.y) + 0.1f
            : approachDistance;
        float gap = Mathf.Max(approachDistance, npcClearance);
        return closestPoint + direction.normalized * gap;
    }

    private static Vector2 GetNpcPosition(WaypointMover npc)
    {
        Rigidbody2D body = npc.GetComponent<Rigidbody2D>();
        return body != null ? body.position : (Vector2)npc.transform.position;
    }

    private void ShowHeartBubble()
    {
        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        SpeechBubbleCanvas.Show(anchor, "♥", bubbleDuration);
    }

    private static void Shuffle(WaypointMover[] npcs)
    {
        for (int index = npcs.Length - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            WaypointMover temporary = npcs[index];
            npcs[index] = npcs[swapIndex];
            npcs[swapIndex] = temporary;
        }
    }
}