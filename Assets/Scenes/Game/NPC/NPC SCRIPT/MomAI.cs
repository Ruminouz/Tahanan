using System.Collections;
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
    private DayManager dayManager;
    private float nextAngryBubbleTime;
    private bool reprimanding;
    private bool gameOverRequested;
    private int pendingReprimands;
    private Collider2D bodyCollider;
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

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerMovement == null && player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

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
        if (moodManager == null || moodManager.CurrentMood != HouseholdMood.Angry)
        {
            RestoreSpriteColor();
            return;
        }

        if (Time.time >= nextAngryBubbleTime)
        {
            ShowMoodDialogue();
            nextAngryBubbleTime = Time.time + angryBubbleInterval;
        }
    }

    private void OnDisable()
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
        if (choreManager != null)
            choreManager.ChoreMissed -= ReprimandPlayer;
    }

    private void SubscribeToChoreManager()
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
        if (choreManager != null)
        {
            choreManager.ChoreMissed -= ReprimandPlayer;
            choreManager.ChoreMissed += ReprimandPlayer;
        }
    }

    private void ReprimandPlayer(Chore chore)
    {
        pendingReprimands++;

        if (!gameOverRequested && dayManager != null &&
            dayManager.HasReachedMissedChoreGameOverThreshold)
        {
            gameOverRequested = true;
            StartCoroutine(TriggerGameOverAfterMissEvent());
        }

        if (!reprimanding)
            StartCoroutine(ProcessReprimands());
    }

    private IEnumerator TriggerGameOverAfterMissEvent()
    {
        yield return null;

        if (dayManager != null)
            dayManager.TriggerGameOver();
    }

    private IEnumerator ProcessReprimands()
    {
        reprimanding = true;

        while (pendingReprimands > 0)
        {
            pendingReprimands--;
            yield return ApproachAndReprimand();

            if (reprimandCooldown > 0f)
                yield return new WaitForSeconds(reprimandCooldown);
        }

        reprimanding = false;
    }

    private IEnumerator ApproachAndReprimand()
    {
        if (waypointMover != null)
            waypointMover.SetMovementEnabled(false);

        SetAngrySpriteColor();
        if (waypointMover != null)
            waypointMover.ResetWaypointRoute();

        float elapsed = 0f;
        float maxApproachTime = Mathf.Max(approachTimeout, spankApproachTimeout);
        Vector2 previousPosition = transform.position;
        while (player != null && Vector2.Distance(transform.position, player.position) > spankDistance &&
               elapsed < maxApproachTime)
        {
            Vector2 movement = waypointMover != null
                ? waypointMover.GetMovementAlongWaypointRoute(
                    player.position,
                    spankApproachSpeed,
                    waypointArrivalDistance)
                : waypointMover != null
                    ? waypointMover.GetMovementTowards(player.position, spankApproachSpeed)
                : Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    spankApproachSpeed * Time.deltaTime) - (Vector2)transform.position;
            Vector2 nextPosition = (Vector2)transform.position + movement;

            UpdateMovementAnimation(nextPosition - (Vector2)transform.position);
            transform.position = nextPosition;

            if ((nextPosition - previousPosition).sqrMagnitude <= 0.000001f)
                chaseStuckTimer += Time.deltaTime;
            else
                chaseStuckTimer = 0f;

            if (chaseStuckTimer >= chaseStuckTimeout)
            {
                if (waypointMover != null)
                    waypointMover.ResetWaypointRoute();
                chaseStuckTimer = 0f;
            }

            previousPosition = transform.position;
            elapsed += Time.deltaTime;
            yield return null;
        }

        bool isInSpankRange = player != null &&
            Vector2.Distance(transform.position, player.position) <= spankDistance;
        if (!isInSpankRange)
        {
            StopMovementAnimation();
            RestoreSpriteColor();
            if (waypointMover != null)
                waypointMover.ResetWaypointRoute();
            if (waypointMover != null)
                waypointMover.SetMovementEnabled(true);
            yield break;
        }

        StopMovementAnimation();
        ShowMoodDialogue();

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

        RestoreSpriteColor();
        if (waypointMover != null)
            waypointMover.ResetWaypointRoute();
        yield return new WaitForSeconds(0.75f);
        if (waypointMover != null)
            waypointMover.SetMovementEnabled(true);
    }

    private void SetAngrySpriteColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = angrySpriteColor;
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