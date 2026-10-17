using System.Collections.Generic;
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
    [SerializeField] private string chocolateRewardLine = "May chocolate ako para sa'yo!";
    [SerializeField] private string chocolateConsumedLine = "Mama! kinain ni kuya chocolate ko!";
    [SerializeField] private string timeFreezerRewardLine = "Eto, pampahinto ng oras!";

    [Header("Good Mood Rewards")]
    [SerializeField] private BunsoBoostType[] possibleBoosts =
        { BunsoBoostType.Chocolate, BunsoBoostType.TimeFreezer };
    [SerializeField] private GameObject chocolatePrefab;
    [FormerlySerializedAs("extraTimePrefab")]
    [SerializeField] private GameObject timeFreezerPrefab;
    [SerializeField, Min(0f)] private float rewardDropGap = 0.15f;

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
    [SerializeField, Min(0.1f)] private float dialogueApproachDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float dialogueApproachTimeout = 30f;
    [SerializeField, Min(0f)] private float minimumWanderWait = 1f;
    [SerializeField, Min(0f)] private float maximumWanderWait = 3f;
    [SerializeField, Min(0f)] private float dialogueDisplayDuration = 2f;

    [Header("Mood Bubble")]
    [SerializeField] private Transform topAnchor;
    [SerializeField] private GameObject moodBubblePrefab;
    [SerializeField, Min(0f)] private float bubbleDuration = 3f;

    private MoodManager moodManager;
    private DayManager dayManager;
    private WaypointMover waypointMover;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Vector2 homePosition;
    private Vector2 movementTarget;
    private bool hasMovementTarget;
    private float nextWanderTime;
    private float nextActionTime;
    private bool actionInProgress;
    private readonly List<Chore> spawnedChores = new();
    private readonly List<GameObject> spawnedBadMoodObjects = new();

    private void Start()
    {
        waypointMover = GetComponent<WaypointMover>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        if (MiniGameInteractionBlocker.IsAnyMiniGameActive)
            return;

        UpdateMovement();

        if (actionInProgress ||
            moodManager == null ||
            dayManager == null ||
            Time.time < nextActionTime)
            return;

        nextActionTime = Time.time + actionInterval;
        if (Random.Range(0f, 100f) > actionChance)
            return;

        switch (moodManager.CurrentMood)
        {
            case HouseholdMood.Calm:
                BeginRewardDrop();
                break;
            case HouseholdMood.Angry:
            case HouseholdMood.Concerned:
                BeginBadMoodChore();
                break;
        }
    }

    private void UpdateMovement()
    {
        if (actionInProgress ||
            MiniGameInteractionBlocker.IsAnyMiniGameActive ||
            PauseController.IsGamePaused ||
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

    private void BeginRewardDrop()
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

        string dialogue = boost == BunsoBoostType.Chocolate
            ? chocolateRewardLine
            : timeFreezerRewardLine;
        StartCoroutine(ApproachAndAct(dialogue, () => DropReward(boost)));
    }

    public void ReactToChocolateConsumed()
    {
        Transform anchor = topAnchor != null ? topAnchor : transform;
        SpeechBubbleCanvas.Show(anchor, chocolateConsumedLine, bubbleDuration);
    }

    private void DropReward(BunsoBoostType boost)
    {
        GameObject prefab = boost == BunsoBoostType.Chocolate
            ? chocolatePrefab
            : timeFreezerPrefab;
        GameObject droppedReward = Instantiate(prefab, transform.position, Quaternion.identity);
        BunsoBoostPickup pickup = droppedReward.GetComponent<BunsoBoostPickup>();
        if (pickup == null)
            pickup = droppedReward.AddComponent<BunsoBoostPickup>();
        if (droppedReward.GetComponent<Collider2D>() == null)
            droppedReward.AddComponent<BoxCollider2D>();
        PlaceRewardBesideBunso(droppedReward);
        pickup.Configure(boost);
    }

    private void PlaceRewardBesideBunso(GameObject reward)
    {
        Collider2D rewardCollider = reward.GetComponent<Collider2D>();
        SpriteRenderer rewardRenderer = reward.GetComponentInChildren<SpriteRenderer>();

        float bunsoHalfWidth = bodyCollider != null && bodyCollider.enabled
            ? bodyCollider.bounds.extents.x
            : 0f;
        if (spriteRenderer != null && spriteRenderer.enabled)
            bunsoHalfWidth = Mathf.Max(bunsoHalfWidth, spriteRenderer.bounds.extents.x);

        float rewardHalfWidth = rewardCollider != null && rewardCollider.enabled
            ? rewardCollider.bounds.extents.x
            : 0f;
        if (rewardRenderer != null && rewardRenderer.enabled)
            rewardHalfWidth = Mathf.Max(rewardHalfWidth, rewardRenderer.bounds.extents.x);

        float side = player != null && player.position.x > transform.position.x ? -1f : 1f;
        float bunsoCenterX = bodyCollider != null && bodyCollider.enabled
            ? bodyCollider.bounds.center.x
            : transform.position.x;
        float rewardCenterX = rewardCollider != null && rewardCollider.enabled
            ? rewardCollider.bounds.center.x
            : reward.transform.position.x;
        float targetCenterX = bunsoCenterX +
            side * (bunsoHalfWidth + rewardHalfWidth + rewardDropGap);

        Vector3 rewardPosition = reward.transform.position;
        rewardPosition.x += targetCenterX - rewardCenterX;
        reward.transform.position = rewardPosition;
    }

    private void BeginBadMoodChore()
    {
        if (dayManager.CurrentDay < 2)
            return;

        if (segregateWastePrefab == null)
            segregateWastePrefab = FindFirstObjectByType<SegregateWasteChore>(
                FindObjectsInactive.Include);

        BadMoodChoreType choreType = (BadMoodChoreType)Random.Range(0, 4);
        string dialogue;
        System.Func<bool> spawnChore;

        switch (choreType)
        {
            case BadMoodChoreType.Dust:
                dialogue = dustBadMoodLine;
                spawnChore = SpawnDustSpot;
                break;
            case BadMoodChoreType.WaterArea:
                dialogue = waterBadMoodLine;
                spawnChore = SpawnWetArea;
                break;
            case BadMoodChoreType.Segregate:
                dialogue = segregateBadMoodLine;
                spawnChore = SpawnSegregateChore;
                break;
            default:
                dialogue = garbageBagBadMoodLine;
                spawnChore = SpawnGarbageBag;
                break;
        }

        StartCoroutine(ApproachAndAct(dialogue, () =>
        {
            if (!spawnChore())
                Debug.LogWarning("Bunso's selected bad-mood chore could not be spawned.", this);
        }));
    }

    private System.Collections.IEnumerator ApproachAndAct(
        string dialogueLine,
        System.Action performAction)
    {
        actionInProgress = true;
        hasMovementTarget = false;

        try
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (player == null)
            {
                Debug.LogWarning("Bunso cannot approach because the Player object was not found.", this);
                yield break;
            }

            float elapsed = 0f;
            while (Vector2.Distance(transform.position, player.position) > dialogueApproachDistance &&
                   elapsed < dialogueApproachTimeout)
            {
                if (PauseController.IsGamePaused ||
                    MiniGameInteractionBlocker.IsAnyMiniGameActive)
                {
                    yield return null;
                    continue;
                }

                Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
                Vector2 movement = waypointMover != null
                    ? waypointMover.GetMovementTowards(player.position, moveSpeed, false)
                    : Vector2.ClampMagnitude(
                        (Vector2)player.position - currentPosition,
                        moveSpeed * Time.deltaTime);

                if (movement.sqrMagnitude <= 0.000001f)
                {
                    Vector2 toPlayer = (Vector2)player.position - currentPosition;
                    movement = NPCMovement2D.GetCollisionSafeMovement(
                        bodyCollider,
                        Vector2.ClampMagnitude(toPlayer, moveSpeed * Time.deltaTime),
                        toPlayer,
                        Physics2D.AllLayers,
                        0.02f);
                }

                Vector2 nextPosition = currentPosition + movement;
                if (body != null)
                    body.MovePosition(nextPosition);
                else
                    transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (Vector2.Distance(transform.position, player.position) > dialogueApproachDistance)
            {
                Debug.LogWarning(name + " could not reach the player before the dialogue approach timed out.", this);
                yield break;
            }

            NPC dialogueHost = null;
            bool dialogueClosed = false;
            while (!dialogueClosed)
            {
                if (MiniGameInteractionBlocker.IsAnyMiniGameActive)
                {
                    yield return null;
                    continue;
                }

                if (dialogueHost == null || !dialogueHost.CanStartAutomaticDialogue)
                {
                    if (PauseController.IsGamePaused)
                    {
                        yield return null;
                        continue;
                    }

                    dialogueHost = NPC.FindDialogueHost(GetComponent<NPC>());
                    if (dialogueHost == null)
                    {
                        Debug.LogWarning("Bunso could not find an NPC dialogue panel; using a speech bubble instead.", this);
                        Transform anchor = topAnchor != null ? topAnchor : transform;
                        SpeechBubbleCanvas.Show(anchor, dialogueLine, bubbleDuration);
                        yield return new WaitForSecondsRealtime(bubbleDuration);
                        break;
                    }
                }

                bool started = dialogueHost.StartAutomaticDialogue(
                    "Bunso",
                    dialogueLine,
                    spriteRenderer != null ? spriteRenderer.sprite : null,
                    dialogueDisplayDuration,
                    () => dialogueClosed = true);
                if (!started)
                {
                    dialogueHost = null;
                    yield return null;
                    continue;
                }

                while (!dialogueClosed)
                    yield return null;
            }

            performAction?.Invoke();
        }
        finally
        {
            actionInProgress = false;
            hasMovementTarget = false;
        }
    }

    private bool SpawnDustSpot()
    {
        SweepingMinigame minigame = FindFirstObjectByType<SweepingMinigame>(
            FindObjectsInactive.Include);
        SweepingManager manager = FindFirstObjectByType<SweepingManager>(
            FindObjectsInactive.Include);
        if (dustSpotPrefab == null || minigame == null)
        {
            Debug.LogWarning("Bunso cannot spawn a dust spot because its prefab or sweeping minigame is missing.", this);
            return false;
        }

        DustSpot dustSpot = Instantiate(dustSpotPrefab, transform.position, Quaternion.identity);
        if (manager != null)
            manager.RegisterExternalDust(dustSpot, minigame);
        else
            dustSpot.SetSweepingMinigame(minigame);
        TrackSpawnedObject(dustSpot.gameObject);
        return true;
    }

    private bool SpawnWetArea()
    {
        MoppingMinigame minigame = FindFirstObjectByType<MoppingMinigame>(
            FindObjectsInactive.Include);
        WaterSpawner spawner = FindFirstObjectByType<WaterSpawner>(
            FindObjectsInactive.Include);
        if (wetAreaPrefab == null || minigame == null)
        {
            Debug.LogWarning("Bunso cannot spawn a wet area because its prefab or mopping minigame is missing.", this);
            return false;
        }

        WetArea wetArea = Instantiate(wetAreaPrefab, transform.position, Quaternion.identity);
        if (spawner != null)
            spawner.RegisterExternalWetArea(wetArea, minigame);
        else
            wetArea.SetMoppingMinigame(minigame);
        return true;
    }

    private bool SpawnSegregateChore()
    {
        Chore choreTemplate = segregateWastePrefab;
        if (choreTemplate == null)
        {
            choreTemplate = FindFirstObjectByType<SegregateWasteChore>(
                FindObjectsInactive.Include);
        }

        if (choreTemplate == null)
        {
            choreTemplate = FindFirstObjectByType<CleanLeavesChore>(
                FindObjectsInactive.Include);
        }

        if (choreTemplate == null)
        {
            Debug.LogWarning("Bunso cannot spawn a segregation chore because its prefab or scene chore is missing.", this);
            return false;
        }

        Chore chore = Instantiate(
            choreTemplate,
            transform.position,
            Quaternion.identity);
        chore.ResetChore();
        chore.SetRuntimeChoreName(CleanLeavesChore.ToyPickupChoreName);
        dayManager.RegisterDynamicChore(chore);
        spawnedChores.Add(chore);
        TrackSpawnedObject(chore.gameObject);

        SpawnGarbageBag();
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

    private void OnDestroy()
    {
        if (dayManager != null)
        {
            foreach (Chore chore in spawnedChores)
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
