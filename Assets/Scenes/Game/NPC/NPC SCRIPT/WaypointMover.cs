using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    public Transform waypointParent;
    public float moveSpeed = 2f;

    public float waitTime = 2f;
    public bool loopWaypoints = true;

    [Header("Collision Avoidance")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0f)] private float collisionPadding = 0.02f;
    [SerializeField, Min(0.05f)] private float stuckTimeout = 0.2f;
    [SerializeField, Min(0f)] private float minimumProgress = 0.001f;

    private Transform[] waypoints;
    private Transform[] activeWaypoints;
    private int currentWaypointIndex;
    private bool isWaiting;
    private bool movementEnabled = true;
    private Collider2D bodyCollider;
    private Rigidbody2D body;
    private float stuckTimer;
    private float waitTimer;

    public bool MovementEnabled => movementEnabled;

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled)
            StopMovement();
    }

    void Start()
    {
        bodyCollider = GetComponent<Collider2D>();
        body = GetComponent<Rigidbody2D>();

        if (waypointParent == null)
            return;

        waypoints = new Transform[waypointParent.childCount];

        for (int i = 0; i < waypointParent.childCount; i++)
        {
            waypoints[i] = waypointParent.GetChild(i);
        }

        activeWaypoints = waypoints;
    }

    void Update()
    {
        if (PauseController.IsGamePaused || isWaiting || !movementEnabled)
        {
            StopMovement();
            stuckTimer = 0f;
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            StopMovement();
            return;
        }

        MoveToWaypoint();
    }

    private void StopMovement()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    void MoveToWaypoint()
    {
        activeWaypoints = waypoints;
        if (activeWaypoints == null || activeWaypoints.Length == 0) return;

        if (currentWaypointIndex >= activeWaypoints.Length)
            currentWaypointIndex = 0;

        Transform target = activeWaypoints[currentWaypointIndex];
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 toTarget = (Vector2)target.position - currentPosition;
        float step = moveSpeed * Time.deltaTime;
        Vector2 movement = Vector2.ClampMagnitude(toTarget, step);
        Vector2 safeMovement = NPCMovement2D.GetCollisionSafeMovement(
            bodyCollider, movement, toTarget, obstacleLayers, collisionPadding);

        float previousDistance = toTarget.magnitude;
        float nextDistance = Vector2.Distance(currentPosition + safeMovement, target.position);
        bool madeProgress = safeMovement.magnitude > minimumProgress && nextDistance < previousDistance;
        stuckTimer = madeProgress ? 0f : stuckTimer + Time.deltaTime;

        if (stuckTimer >= stuckTimeout)
        {
            SelectAlternativeWaypoint(currentPosition);
            return;
        }

        if (body != null)
            body.MovePosition(currentPosition + safeMovement);
        else
            transform.position = currentPosition + safeMovement;

        if (Vector2.Distance(currentPosition + safeMovement, target.position) < 0.1f)
        {
            waitTimer = waitTime;
            if (loopWaypoints)
                currentWaypointIndex = (currentWaypointIndex + 1) % activeWaypoints.Length;
            else
                currentWaypointIndex = Mathf.Min(currentWaypointIndex + 1, activeWaypoints.Length - 1);
        }
    }

    private void SelectAlternativeWaypoint(Vector2 currentPosition)
    {
        stuckTimer = 0f;

        if (activeWaypoints == null || activeWaypoints.Length <= 1)
            return;

        int nextIndex = currentWaypointIndex;
        for (int i = 0; i < activeWaypoints.Length - 1; i++)
        {
            nextIndex = (nextIndex + 1) % activeWaypoints.Length;
            if (HasClearPath(currentPosition, activeWaypoints[nextIndex].position))
            {
                currentWaypointIndex = nextIndex;
                return;
            }
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % activeWaypoints.Length;
    }

    private bool HasClearPath(Vector2 currentPosition, Vector2 targetPosition)
    {
        if (bodyCollider == null)
            return true;

        Vector2 toTarget = targetPosition - currentPosition;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return true;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false
        };
        filter.SetLayerMask(obstacleLayers);

        RaycastHit2D[] hits = new RaycastHit2D[8];
        return bodyCollider.Cast(toTarget.normalized, filter, hits, toTarget.magnitude + collisionPadding) == 0;
    }

}