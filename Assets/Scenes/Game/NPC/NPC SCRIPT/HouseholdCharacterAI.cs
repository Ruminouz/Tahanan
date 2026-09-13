using UnityEngine;

public enum HouseholdCharacterState
{
    Idle,
    Busy,
    Helping,
    Resting
}

[System.Serializable]
public class HouseholdStateScheduleEntry
{
    public float startTimeSeconds;
    public HouseholdCharacterState state;
}

public class HouseholdCharacterAI : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private string characterName;
    [SerializeField] private HouseholdCharacterState startingState = HouseholdCharacterState.Idle;
    [SerializeField] private bool useTimeSchedule = true;
    [SerializeField] private HouseholdStateScheduleEntry[] dailySchedule;

    [Header("2D Top-Down Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private Transform[] idlePoints;
    [SerializeField] private Transform[] busyPoints;
    [SerializeField] private Transform[] restingPoints;
    [SerializeField] private float arriveDistance = 0.08f;
    [SerializeField] private float waitAtPoint = 2f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0f)] private float collisionPadding = 0.02f;

    private HouseholdCharacterState currentState;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Animator animator;
    private Transform[] currentPoints;
    private int pointIndex;
    private float waitTimer;
    private TimeManager timeManager;

    public string CharacterName => characterName;
    public HouseholdCharacterState CurrentState => currentState;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        timeManager = FindFirstObjectByType<TimeManager>();
        SetState(startingState);
    }

    private void FixedUpdate()
    {
        UpdateScheduledState();

        if (PauseController.IsGamePaused || currentState == HouseholdCharacterState.Helping)
        {
            StopMoving();
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            StopMoving();
            return;
        }

        MoveAlongSchedule();
    }

    private void UpdateScheduledState()
    {
        if (!useTimeSchedule || timeManager == null || dailySchedule == null || dailySchedule.Length == 0)
            return;

        HouseholdStateScheduleEntry selectedEntry = null;
        foreach (HouseholdStateScheduleEntry entry in dailySchedule)
        {
            if (entry != null && entry.startTimeSeconds <= timeManager.GetTime() &&
                (selectedEntry == null || entry.startTimeSeconds > selectedEntry.startTimeSeconds))
            {
                selectedEntry = entry;
            }
        }

        if (selectedEntry != null && selectedEntry.state != currentState)
            SetState(selectedEntry.state);
    }

    public void SetState(HouseholdCharacterState state)
    {
        currentState = state;
        pointIndex = 0;
        waitTimer = 0f;
        currentPoints = GetPointsForState(state);
    }

    public void StartHelping()
    {
        SetState(HouseholdCharacterState.Helping);
    }

    public void StopHelping()
    {
        SetState(HouseholdCharacterState.Idle);
    }

    private Transform[] GetPointsForState(HouseholdCharacterState state)
    {
        switch (state)
        {
            case HouseholdCharacterState.Busy:
                return busyPoints;
            case HouseholdCharacterState.Resting:
                return restingPoints;
            default:
                return idlePoints;
        }
    }

    private void MoveAlongSchedule()
    {
        if (currentPoints == null || currentPoints.Length == 0)
        {
            StopMoving();
            return;
        }

        Transform target = currentPoints[pointIndex];
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, target.position, moveSpeed * Time.fixedDeltaTime);
        Vector2 movement = nextPosition - currentPosition;
        Vector2 safeMovement = NPCMovement2D.GetCollisionSafeMovement(
            bodyCollider, movement, (Vector2)target.position - currentPosition, obstacleLayers, collisionPadding);
        nextPosition = currentPosition + safeMovement;
        Vector2 direction = safeMovement;

        if (body != null)
            body.MovePosition(nextPosition);
        else
            transform.position = nextPosition;

        UpdateAnimation(direction);

        if (Vector2.Distance(nextPosition, target.position) <= arriveDistance)
        {
            pointIndex = (pointIndex + 1) % currentPoints.Length;
            waitTimer = waitAtPoint;
        }
    }

    private void StopMoving()
    {
        if (body != null)
            body.linearVelocity = Vector2.zero;

        UpdateAnimation(Vector2.zero);
    }

    private void UpdateAnimation(Vector2 direction)
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
}