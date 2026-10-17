using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MomAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private GameObject moodBubblePrefab;
    [SerializeField] private GameObject slipperPrefab;
    [SerializeField] private WaypointMover waypointMover;

    [Header("Reprimand")]
    [SerializeField, Min(0f)] private float slowMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float slowDuration = 3f;
    [SerializeField, Min(0f)] private float reprimandCooldown = 2f;
    [SerializeField, Min(0f)] private float bubbleDuration = 3f;
    [SerializeField, Min(0f)] private float angryBubbleInterval = 8f;
    [SerializeField, Min(0.1f)] private float approachSpeed = 2f;
    [SerializeField, Min(0.1f)] private float spankApproachSpeed = 4f;
    [SerializeField, Min(0.1f)] private float spankDistance = 0.8f;
    [SerializeField, Min(0.1f)] private float approachTimeout = 5f;
    [SerializeField, Min(0.1f)] private float spankApproachTimeout = 15f;
    [SerializeField, Min(0f)] private float reprimandDialogueDuration = 2f;
    [SerializeField, Min(0.01f)] private float waypointArrivalDistance = 0.15f;
    [SerializeField, Min(0.05f)] private float chaseStuckTimeout = 0.5f;
    [SerializeField] private string spankTrigger = "Spank";
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0f)] private float collisionPadding = 0.02f;

    [Header("Mood Dialogue")]
    [SerializeField] private string calmLine = "Good job helping around the house!";
    [SerializeField] private string concernedLine = "Please try to finish your chores.";
    [SerializeField] private string angryLine = "You missed another chore! Get moving!";
    [SerializeField] private Color angrySpriteColor = new Color(1f, 0.45f, 0.45f, 1f);

    private MoodManager moodManager;
    private ChoreManager choreManager;
    private ChoreManager subscribedChoreManager;
    private DayManager dayManager;
    private WaterSpawner waterSpawner;
    private float nextAngryBubbleTime;
    private bool reprimanding;
    private bool gameOverRequested;
    private readonly Queue<Chore> pendingMissedChores = new Queue<Chore>();
    private Collider2D bodyCollider;
    private Collider2D playerCollider;
    private Rigidbody2D body;
    private bool restoreBodyColliderAfterReprimand;
    private bool bodyColliderWasEnabled;
    private bool restoreWaypointMovementAfterReprimand;
    private bool waypointMovementSuspendedForReprimand;
    private float chaseStuckTimer;
    private Color normalSpriteColor = Color.white;
    private bool hasNormalSpriteColor;

    private void Awake()
    {
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        waterSpawner = FindFirstObjectByType<WaterSpawner>();

        ResolvePlayerReferences();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalSpriteColor = spriteRenderer.color;
            hasNormalSpriteColor = true;
        }

        bodyCollider = GetComponent<Collider2D>();
        body = GetComponent<Rigidbody2D>();

        if (waypointMover == null)
            waypointMover = GetComponent<WaypointMover>();

    }

    private void OnEnable()
    {
        SubscribeToChoreManager();
    }

    private void Start()
    {
        SubscribeToChoreManager();
    }

    private void Update()
    {
        if (MiniGameInteractionBlocker.IsAnyMiniGameActive)
            return;

        if (choreManager == null)
            SubscribeToChoreManager();

        if (moodManager == null || moodManager.CurrentMood != HouseholdMood.Angry)
        {
            RestoreSpriteColor();
            return;
        }

        if (reprimanding)
            return;

        if (Time.time >= nextAngryBubbleTime)
        {
            ShowMoodDialogue();
            nextAngryBubbleTime = Time.time + angryBubbleInterval;
        }
    }

    private void OnDisable()
    {
        if (subscribedChoreManager != null)
            subscribedChoreManager.ChoreMissed -= ReprimandPlayer;

        subscribedChoreManager = null;
        RestoreBodyCollider();
        if (waypointMovementSuspendedForReprimand && waypointMover != null)
            waypointMover.SetMovementEnabled(restoreWaypointMovementAfterReprimand);
        waypointMover?.ResetWaypointRoute();
        restoreWaypointMovementAfterReprimand = false;
        waypointMovementSuspendedForReprimand = false;
        reprimanding = false;
    }

    private void SubscribeToChoreManager()
    {
        if (choreManager == null)
            choreManager = FindFirstObjectByType<ChoreManager>();

        if (subscribedChoreManager == choreManager)
            return;

        if (subscribedChoreManager != null)
            subscribedChoreManager.ChoreMissed -= ReprimandPlayer;

        subscribedChoreManager = choreManager;
        if (subscribedChoreManager != null)
            subscribedChoreManager.ChoreMissed += ReprimandPlayer;
    }

    private void ReprimandPlayer(Chore chore)
    {
        if (chore != null && (!chore.IsMissed || chore.IsCompleted))
            return;

        if (gameOverRequested)
            return;

        pendingMissedChores.Enqueue(chore);

        if (!gameOverRequested && dayManager != null &&
            dayManager.HasReachedMissedChoreGameOverThreshold)
        {
            gameOverRequested = true;
        }

        if (!reprimanding)
            StartCoroutine(ProcessReprimands());
    }

    private IEnumerator ProcessReprimands()
    {
        reprimanding = true;

        while (MiniGameInteractionBlocker.IsAnyMiniGameActive)
            yield return null;

        while (pendingMissedChores.Count > 0)
        {
            Chore missedChore = pendingMissedChores.Dequeue();
            yield return ApproachAndReprimand(missedChore);

            if (gameOverRequested)
            {
                pendingMissedChores.Clear();
                break;
            }

            if (reprimandCooldown > 0f)
                yield return new WaitForSeconds(reprimandCooldown);
        }

        reprimanding = false;

        if (gameOverRequested && dayManager != null)
            dayManager.TriggerGameOver();
    }

    private IEnumerator ApproachAndReprimand(Chore missedChore)
    {
        ResolvePlayerReferences();

        restoreWaypointMovementAfterReprimand =
            waypointMover != null && waypointMover.MovementEnabled;
        if (waypointMover != null)
        {
            waypointMovementSuspendedForReprimand = true;
            waypointMover.SetMovementEnabled(false);
        }

        try
        {
            SetAngrySpriteColor();
            if (waypointMover != null)
                waypointMover.ResetWaypointRoute();

            float elapsed = 0f;
            float maxApproachTime = Mathf.Max(approachTimeout, spankApproachTimeout);
            chaseStuckTimer = 0f;
            Vector2 previousPosition = body != null ? body.position : (Vector2)transform.position;
            while (player != null && !IsPlayerWithinSpankRange() && elapsed < maxApproachTime)
            {
                if (MiniGameInteractionBlocker.IsAnyMiniGameActive)
                {
                    yield return null;
                    continue;
                }

                Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
                Vector2 movement = waypointMover != null
                    ? waypointMover.GetMovementTowards(player.position, spankApproachSpeed, false)
                    : GetUnblockedPursuitMovement(currentPosition);
                if (movement.sqrMagnitude <= 0.000001f)
                    movement = GetUnblockedPursuitMovement(currentPosition);

                Vector2 nextPosition = currentPosition + movement;

                UpdateMovementAnimation(nextPosition - currentPosition);
                if (body != null)
                    body.MovePosition(nextPosition);
                else
                    transform.position = nextPosition;

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();

                Vector2 actualPosition = body != null ? body.position : (Vector2)transform.position;
                if ((actualPosition - previousPosition).sqrMagnitude <= 0.000001f)
                    chaseStuckTimer += Time.fixedDeltaTime;
                else
                    chaseStuckTimer = 0f;

                if (chaseStuckTimer >= chaseStuckTimeout)
                {
                    waypointMover?.ResetWaypointRoute();
                    chaseStuckTimer = 0f;
                }

                previousPosition = actualPosition;
            }

            if (player != null && !IsPlayerWithinSpankRange())
                Debug.LogWarning(name + " could not reach the player; performing the missed-chore reprimand anyway.", this);

            StopMovementAnimation();

            NPC dialogue = FindFirstObjectByType<NPC>();
            if (dialogue != null)
            {
                bool dialogueClosed = false;
                bool dialogueStarted = false;
                string line = GetMissedChoreLine(missedChore);
                while (!dialogueStarted)
                {
                    dialogueStarted = dialogue.StartAutomaticDialogue(
                        "Nanay",
                        line,
                        spriteRenderer != null ? spriteRenderer.sprite : null,
                        reprimandDialogueDuration,
                        () => dialogueClosed = true);

                    if (!dialogueStarted)
                        yield return null;
                }

                while (!dialogueClosed)
                    yield return null;
            }
            else
            {
                Debug.LogWarning(name + " cannot display reprimand dialogue because no NPC dialogue component was found.", this);
                ShowMoodDialogue();
            }

            if (animator != null && !string.IsNullOrEmpty(spankTrigger))
                animator.SetTrigger(spankTrigger);

            if (slipperPrefab != null)
            {
                Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
                Vector3 spawnPosition = player != null ? player.position : anchor.position;
                Destroy(Instantiate(slipperPrefab, spawnPosition, anchor.rotation), 1.5f);
            }

            if (playerMovement != null)
                StartCoroutine(SlowPlayer());

            yield return new WaitForSeconds(0.75f);
        }
        finally
        {
            RestoreSpriteColor();
            RestoreBodyCollider();
            if (waypointMover != null)
            {
                waypointMover.ResetWaypointRoute();
                if (waypointMovementSuspendedForReprimand)
                    waypointMover.SetMovementEnabled(restoreWaypointMovementAfterReprimand);
            }

            restoreWaypointMovementAfterReprimand = false;
            waypointMovementSuspendedForReprimand = false;
        }
    }

    private void RestoreBodyCollider()
    {
        if (!restoreBodyColliderAfterReprimand)
            return;

        if (bodyCollider != null)
            bodyCollider.enabled = bodyColliderWasEnabled;
        restoreBodyColliderAfterReprimand = false;
    }

    private Vector2 GetUnblockedPursuitMovement(Vector2 currentPosition)
    {
        Vector2 toPlayer = (Vector2)player.position - currentPosition;
        Vector2 movement = Vector2.ClampMagnitude(toPlayer, spankApproachSpeed * Time.fixedDeltaTime);
        return NPCMovement2D.GetCollisionSafeMovement(
            bodyCollider,
            movement,
            toPlayer,
            obstacleLayers,
            collisionPadding);
    }

    private bool IsPlayerWithinSpankRange()
    {
        if (player == null)
            return false;

        if (Vector2.Distance(transform.position, player.position) <= spankDistance)
            return true;

        return bodyCollider != null && bodyCollider.enabled &&
            playerCollider != null && playerCollider.enabled &&
            bodyCollider.Distance(playerCollider).distance <=
                collisionPadding + Physics2D.defaultContactOffset;
    }

    private void SetAngrySpriteColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = angrySpriteColor;
    }

    private void ResolvePlayerReferences()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            return;

        if (playerMovement == null)
            playerMovement = player.GetComponent<PlayerMovement>();

        if (playerCollider == null)
        {
            playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null)
                playerCollider = player.GetComponentInChildren<Collider2D>();
        }
    }

    private void RestoreSpriteColor()
    {
        if (spriteRenderer != null && hasNormalSpriteColor)
            spriteRenderer.color = normalSpriteColor;
    }

    private void UpdateMovementAnimation(Vector2 direction)
    {
        if (animator == null)
            return;

        animator.SetBool("isWalking", direction.sqrMagnitude > 0.001f);
        if (direction.sqrMagnitude > 0.001f)
        {
            animator.SetFloat("InputX", direction.x);
            animator.SetFloat("InputY", direction.y);
        }
    }

    private void StopMovementAnimation()
    {
        if (animator != null)
            animator.SetBool("isWalking", false);
    }

    private IEnumerator SlowPlayer()
    {
        playerMovement.SetSpeedMultiplier(slowMultiplier);
        yield return new WaitForSeconds(slowDuration);
        playerMovement.SetSpeedMultiplier(1f);
    }

    private void ShowMoodDialogue()
    {
        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        string line = GetMoodLine();
        SpeechBubbleCanvas.Show(anchor, line, bubbleDuration);
    }

    private string GetMissedChoreLine(Chore chore)
    {
        if (chore == null && waterSpawner != null && waterSpawner.IsMoppingMissed)
            return "Yung basa pag may nadulas dito";

        if (chore is DishwashingChore)
            return "Yung hugasin!";

        if (chore is FeedDogChore)
            return "Di mo napakain yung aso!";

        if (chore is GarbageChore)
            return "Di mo na segregate ng tama!";

        string choreName = chore != null ? chore.ChoreName ?? string.Empty : string.Empty;
        if (choreName.IndexOf("mop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            choreName.IndexOf("wet", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "Yung basa pag may nadulas dito";

        if (choreName.IndexOf("sweep", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            (dayManager != null && chore == dayManager.SweepDustChore))
            return "Yung pinapawalis ko sayo!";

        if (chore is CleanLeavesChore ||
            chore is SegregateWasteChore ||
            choreName.IndexOf("toy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            choreName.IndexOf("pickup", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "yung laruan ng kapatid mo di mo niligpit!";

        return GetMoodLine();
    }

    private string GetMoodLine()
    {
        HouseholdMood mood = moodManager != null
            ? moodManager.CurrentMood
            : HouseholdMood.Concerned;

        switch (mood)
        {
            case HouseholdMood.Calm:
                return calmLine;
            case HouseholdMood.Angry:
                return angryLine;
            default:
                return concernedLine;
        }
    }
}