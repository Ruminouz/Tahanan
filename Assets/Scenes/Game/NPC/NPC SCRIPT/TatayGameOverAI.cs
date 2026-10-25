using System.Collections;
using UnityEngine;

public class TatayGameOverAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private WaypointMover waypointMover;

    [Header("Game Over")]
    [SerializeField, Min(0.1f)] private float approachSpeed = 4f;
    [SerializeField, Min(0.1f)] private float approachDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float approachTimeout = 30f;
    [SerializeField, Min(0f)] private float dialogueDuration = 3f;
    [SerializeField, Min(0f)] private float fallbackBubbleDuration = 3f;
    [SerializeField] private string dialogueLine = "You're not behaving well. It's time to end the day.";

    private DayManager dayManager;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private bool gameOverSequenceStarted;
    private bool restoreWaypointMovement;
    private bool bodyColliderWasEnabled;
    private bool restoreBodyColliderAfterApproach;

    private void Awake()
    {
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (waypointMover == null)
            waypointMover = GetComponent<WaypointMover>();

        ResolvePlayer();
    }

    private void Update()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        }

        if (!gameOverSequenceStarted &&
            dayManager != null &&
            dayManager.HasReachedMissedChoreGameOverThreshold)
        {
            gameOverSequenceStarted = true;
            StartCoroutine(ApproachAndEndDay());
        }
    }

    private void OnDisable()
    {
        RestoreBodyCollider();
    }

    private IEnumerator ApproachAndEndDay()
    {
        while (MiniGameInteractionBlocker.IsAnyMiniGameActive || PauseController.IsGamePaused)
            yield return null;

        ResolvePlayer();
        restoreWaypointMovement = waypointMover != null && waypointMover.MovementEnabled;
        if (waypointMover != null)
        {
            waypointMover.SetMovementEnabled(false);
            waypointMover.ResetWaypointRoute();
        }

        try
        {
            if (player != null)
            {
                DisableBodyColliderForApproach();
                yield return ApproachPlayer();
                RestoreBodyCollider();
            }
            else
                Debug.LogWarning("Tatay cannot approach because the Player object was not found.", this);

            yield return ShowGameOverDialogue();
            dayManager.TriggerGameOver();
        }
        finally
        {
            RestoreBodyCollider();
            StopMovementAnimation();
            if (waypointMover != null)
            {
                waypointMover.ResetWaypointRoute();
                waypointMover.SetMovementEnabled(restoreWaypointMovement);
            }
        }
    }

    private void DisableBodyColliderForApproach()
    {
        if (bodyCollider == null || restoreBodyColliderAfterApproach)
            return;

        bodyColliderWasEnabled = bodyCollider.enabled;
        restoreBodyColliderAfterApproach = true;
        bodyCollider.enabled = false;
    }

    private void RestoreBodyCollider()
    {
        if (!restoreBodyColliderAfterApproach)
            return;

        if (bodyCollider != null)
            bodyCollider.enabled = bodyColliderWasEnabled;
        restoreBodyColliderAfterApproach = false;
    }

    private IEnumerator ApproachPlayer()
    {
        float elapsed = 0f;
        while (player != null &&
               Vector2.Distance(transform.position, player.position) > approachDistance &&
               elapsed < approachTimeout)
        {
            if (MiniGameInteractionBlocker.IsAnyMiniGameActive || PauseController.IsGamePaused)
            {
                yield return null;
                continue;
            }

            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 movement = waypointMover != null
                ? waypointMover.GetMovementTowards(player.position, approachSpeed, false)
                : Vector2.ClampMagnitude(
                    (Vector2)player.position - currentPosition,
                    approachSpeed * Time.deltaTime);
            Vector2 nextPosition = currentPosition + movement;

            UpdateMovementAnimation(movement);
            if (body != null)
                body.MovePosition(nextPosition);
            else
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMovementAnimation();
        if (player != null && Vector2.Distance(transform.position, player.position) > approachDistance)
            Debug.LogWarning("Tatay could not reach the player before the game-over approach timed out.", this);
    }

    private IEnumerator ShowGameOverDialogue()
    {
        NPC dialogueHost = null;
        bool dialogueClosed = false;

        while (true)
        {
            if (MiniGameInteractionBlocker.IsAnyMiniGameActive || PauseController.IsGamePaused)
            {
                yield return null;
                continue;
            }

            dialogueHost = NPC.FindDialogueHost(GetComponent<NPC>());
            if (dialogueHost == null)
            {
                Debug.LogWarning("Tatay could not find an NPC dialogue panel; showing the game-over line in a speech bubble.", this);
                SpeechBubbleCanvas.Show(transform, dialogueLine, fallbackBubbleDuration);
                yield return new WaitForSecondsRealtime(fallbackBubbleDuration);
                yield break;
            }

            if (dialogueHost.StartAutomaticDialogue(
                "Tatay",
                dialogueLine,
                spriteRenderer != null ? spriteRenderer.sprite : null,
                dialogueDuration,
                () => dialogueClosed = true))
            {
                break;
            }

            yield return null;
        }

        while (!dialogueClosed)
            yield return null;
    }

    private void ResolvePlayer()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
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
}
