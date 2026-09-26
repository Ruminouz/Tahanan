using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BunsoAI : MonoBehaviour
{
    [Header("Mood")]
    [SerializeField, Min(0f)] private float actionInterval = 12f;
    [SerializeField, Range(0f, 100f)] private float actionChance = 60f;
    [SerializeField] private string badMoodLine = "Pick up the trash!";

    [Header("Good Mood Rewards")]
    [SerializeField] private BunsoBoostType[] possibleBoosts =
        { BunsoBoostType.Chocolate, BunsoBoostType.ExtraTime };
    [SerializeField] private GameObject chocolatePrefab;
    [SerializeField] private GameObject extraTimePrefab;

    [Header("Bad Mood Chore")]
    [SerializeField] private SegregateWasteChore segregateWastePrefab;
    [SerializeField, Min(0f)] private float choreLifetime = 120f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.25f;
    [SerializeField, Min(0.1f)] private float followTriggerDistance = 4.5f;
    [SerializeField, Min(0.1f)] private float followStopDistance = 2.25f;
    [SerializeField, Min(0.1f)] private float wanderRadius = 1.75f;
    [SerializeField, Min(0.05f)] private float arriveDistance = 0.2f;
    [SerializeField, Min(0f)] private float minimumWanderWait = 1f;
    [SerializeField, Min(0f)] private float maximumWanderWait = 3f;

    [Header("Mood Bubble")]
    [SerializeField] private Transform topAnchor;
    [SerializeField] private GameObject moodBubblePrefab;
    [SerializeField, Min(0f)] private float bubbleDuration = 3f;

    private MoodManager moodManager;
    private DayManager dayManager;
    private WaypointMover waypointMover;
    private Rigidbody2D body;
    private Transform player;
    private Vector2 homePosition;
    private Vector2 movementTarget;
    private bool hasMovementTarget;
    private float nextWanderTime;
    private float nextActionTime;
    private readonly List<SegregateWasteChore> spawnedChores = new();

    private void Start()
    {
        waypointMover = GetComponent<WaypointMover>();
        body = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        homePosition = transform.position;

        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        if (segregateWastePrefab == null)
            segregateWastePrefab = FindFirstObjectByType<SegregateWasteChore>(
                FindObjectsInactive.Include);
        nextActionTime = Time.time + actionInterval;
    }

    private void Update()
    {
        UpdateMovement();

        if (moodManager == null || dayManager == null || Time.time < nextActionTime)
            return;

        nextActionTime = Time.time + actionInterval;
        if (Random.Range(0f, 100f) > actionChance)
            return;

        switch (moodManager.CurrentMood)
        {
            case HouseholdMood.Calm:
                DropReward();
                break;
            case HouseholdMood.Angry:
            case HouseholdMood.Concerned:
                SpawnBadMoodChore();
                ShowBadMoodBubble();
                break;
        }
    }

    private void UpdateMovement()
    {
        if (PauseController.IsGamePaused ||
            (waypointMover != null && waypointMover.waypointParent != null))
            return;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 playerPosition = player != null ? player.position : homePosition;
        float playerDistance = Vector2.Distance(currentPosition, playerPosition);

        if (player != null && playerDistance > followTriggerDistance)
        {
            Vector2 awayFromPlayer = currentPosition - playerPosition;
            if (awayFromPlayer.sqrMagnitude <= 0.0001f)
                awayFromPlayer = Vector2.right;

            movementTarget = playerPosition + awayFromPlayer.normalized * followStopDistance;
            hasMovementTarget = true;
        }
        else if (!hasMovementTarget ||
                 Vector2.Distance(currentPosition, movementTarget) <= arriveDistance)
        {
            if (Time.time < nextWanderTime)
                return;

            Vector2 wanderCenter = player != null ? playerPosition : homePosition;
            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            float radius = Random.Range(
                Mathf.Min(followStopDistance, wanderRadius),
                wanderRadius);
            movementTarget = wanderCenter + direction.normalized * radius;
            hasMovementTarget = true;
        }

        if (waypointMover != null)
        {
            waypointMover.MoveTowards(movementTarget, moveSpeed, arriveDistance);
        }
        else
        {
            Vector2 nextPosition = Vector2.MoveTowards(
                currentPosition,
                movementTarget,
                moveSpeed * Time.deltaTime);
            if (body != null)
                body.MovePosition(nextPosition);
            else
                transform.position = nextPosition;
        }

        if (Vector2.Distance(currentPosition, movementTarget) <= arriveDistance)
        {
            hasMovementTarget = false;
            nextWanderTime = Time.time + Random.Range(
                minimumWanderWait,
                Mathf.Max(minimumWanderWait, maximumWanderWait));
        }
    }

    private void DropReward()
    {
        if (possibleBoosts == null || possibleBoosts.Length == 0)
            return;

        BunsoBoostType boost = possibleBoosts[Random.Range(0, possibleBoosts.Length)];
        GameObject prefab = boost == BunsoBoostType.Chocolate
            ? chocolatePrefab
            : extraTimePrefab;
        if (prefab == null)
        {
            Debug.LogWarning("Bunso reward prefab is not assigned for " + boost + ".", this);
            return;
        }

        GameObject droppedReward = Instantiate(prefab, transform.position, Quaternion.identity);
        BunsoBoostPickup pickup = droppedReward.GetComponent<BunsoBoostPickup>();
        if (pickup == null)
            pickup = droppedReward.AddComponent<BunsoBoostPickup>();
        if (droppedReward.GetComponent<Collider2D>() == null)
            droppedReward.AddComponent<BoxCollider2D>();
        pickup.Configure(boost);
    }

    private void SpawnBadMoodChore()
    {
        if (segregateWastePrefab == null || dayManager.CurrentDay < 2)
            return;

        SegregateWasteChore chore = Instantiate(
            segregateWastePrefab,
            transform.position,
            Quaternion.identity);
        chore.ConfigureSpawnedChore("PickUp");
        dayManager.RegisterDynamicChore(chore);
        spawnedChores.Add(chore);

        if (choreLifetime > 0f)
            Destroy(chore.gameObject, choreLifetime);
    }

    private void ShowBadMoodBubble()
    {
        Transform anchor = topAnchor != null ? topAnchor : transform;
        SpeechBubbleCanvas.Show(anchor, badMoodLine, bubbleDuration);
    }

    private void OnDestroy()
    {
        if (dayManager == null)
            return;

        foreach (SegregateWasteChore chore in spawnedChores)
        {
            if (chore != null)
                dayManager.UnregisterDynamicChore(chore);
        }
    }
}
