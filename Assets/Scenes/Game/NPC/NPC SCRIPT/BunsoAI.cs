using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class BunsoAI : MonoBehaviour
{
    private enum BadMoodChoreType
    {
        Dust,
        WaterArea,
        Segregate,
        GarbageBag
    }

    [Header("Mood")]
    [SerializeField, Min(0f)] private float actionInterval = 12f;
    [SerializeField, Range(0f, 100f)] private float actionChance = 60f;
    [SerializeField] private string dustBadMoodLine = "Pakiwalisan to kuya! bwhahaah!";
    [SerializeField] private string waterBadMoodLine = "paki mop to kuya! bwhaahah!";
    [SerializeField] private string segregateBadMoodLine = "pakiligpit to kuya! bwahahah!";
    [SerializeField] private string garbageBagBadMoodLine = "pakitapon to sa basura kuya! bwaahha!";

    [Header("Good Mood Rewards")]
    [SerializeField] private BunsoBoostType[] possibleBoosts =
        { BunsoBoostType.Chocolate, BunsoBoostType.TimeFreezer };
    [SerializeField] private GameObject chocolatePrefab;
    [FormerlySerializedAs("extraTimePrefab")]
    [SerializeField] private GameObject timeFreezerPrefab;

    [Header("Bad Mood Chore")]
    [SerializeField] private SegregateWasteChore segregateWastePrefab;
    [SerializeField] private DustSpot dustSpotPrefab;
    [SerializeField] private WetArea wetAreaPrefab;
    [SerializeField] private GarbageBag garbageBagPrefab;
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
    private readonly List<GameObject> spawnedBadMoodObjects = new();

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
            : timeFreezerPrefab;
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
        if (dayManager.CurrentDay < 2)
            return;

        if (segregateWastePrefab == null)
            segregateWastePrefab = FindFirstObjectByType<SegregateWasteChore>(
                FindObjectsInactive.Include);

        BadMoodChoreType choreType = (BadMoodChoreType)Random.Range(0, 4);
        string dialogue;
        bool spawned;

        switch (choreType)
        {
            case BadMoodChoreType.Dust:
                dialogue = dustBadMoodLine;
                spawned = SpawnDustSpot();
                break;
            case BadMoodChoreType.WaterArea:
                dialogue = waterBadMoodLine;
                spawned = SpawnWetArea();
                break;
            case BadMoodChoreType.Segregate:
                dialogue = segregateBadMoodLine;
                spawned = SpawnSegregateChore();
                break;
            default:
                dialogue = garbageBagBadMoodLine;
                spawned = SpawnGarbageBag();
                break;
        }

        if (spawned)
            ShowBadMoodBubble(dialogue);
    }

    private bool SpawnDustSpot()
    {
        SweepingMinigame minigame = FindFirstObjectByType<SweepingMinigame>();
        if (dustSpotPrefab == null || minigame == null)
        {
            Debug.LogWarning("Bunso cannot spawn a dust spot because its prefab or sweeping minigame is missing.", this);
            return false;
        }

        DustSpot dustSpot = Instantiate(dustSpotPrefab, transform.position, Quaternion.identity);
        dustSpot.SetSweepingMinigame(minigame);
        TrackSpawnedObject(dustSpot.gameObject);
        return true;
    }

    private bool SpawnWetArea()
    {
        MoppingMinigame minigame = FindFirstObjectByType<MoppingMinigame>();
        if (wetAreaPrefab == null || minigame == null)
        {
            Debug.LogWarning("Bunso cannot spawn a wet area because its prefab or mopping minigame is missing.", this);
            return false;
        }

        WetArea wetArea = Instantiate(wetAreaPrefab, transform.position, Quaternion.identity);
        wetArea.SetMoppingMinigame(minigame);
        TrackSpawnedObject(wetArea.gameObject);
        return true;
    }

    private bool SpawnSegregateChore()
    {
        if (segregateWastePrefab == null)
        {
            Debug.LogWarning("Bunso cannot spawn a segregation chore because its prefab or scene chore is missing.", this);
            return false;
        }

        SegregateWasteChore chore = Instantiate(
            segregateWastePrefab,
            transform.position,
            Quaternion.identity);
        chore.ConfigureSpawnedChore("PickUp");
        dayManager.RegisterDynamicChore(chore);
        spawnedChores.Add(chore);
        TrackSpawnedObject(chore.gameObject);

        return true;
    }

    private bool SpawnGarbageBag()
    {
        GarbageChore garbageChore = GarbageChore.Instance != null
            ? GarbageChore.Instance
            : FindFirstObjectByType<GarbageChore>();
        if (garbageBagPrefab == null || garbageChore == null)
        {
            Debug.LogWarning("Bunso cannot spawn a garbage bag because its prefab or garbage chore is missing.", this);
            return false;
        }

        GarbageBag garbageBag = Instantiate(
            garbageBagPrefab,
            transform.position,
            Quaternion.identity);
        garbageBag.ConfigureSpawnedBag(garbageChore);
        garbageBag.gameObject.SetActive(true);
        TrackSpawnedObject(garbageBag.gameObject);
        return true;
    }

    private void TrackSpawnedObject(GameObject spawnedObject)
    {
        spawnedBadMoodObjects.Add(spawnedObject);
        if (choreLifetime > 0f)
            Destroy(spawnedObject, choreLifetime);
    }

    private void ShowBadMoodBubble(string dialogue)
    {
        Transform anchor = topAnchor != null ? topAnchor : transform;
        SpeechBubbleCanvas.Show(anchor, dialogue, bubbleDuration);
    }

    private void OnDestroy()
    {
        if (dayManager != null)
        {
            foreach (SegregateWasteChore chore in spawnedChores)
            {
                if (chore != null)
                    dayManager.UnregisterDynamicChore(chore);
            }
        }

        foreach (GameObject spawnedObject in spawnedBadMoodObjects)
        {
            if (spawnedObject != null)
                Destroy(spawnedObject);
        }
    }
}
