using System.Collections.Generic;
using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    public Transform waypointParent;
    public float moveSpeed = 2f;

    public float waitTime = 2f;
    public bool loopWaypoints = true;
    [SerializeField] private int[] waypointOrderOverride;

    [Header("Collision Avoidance")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0f)] private float collisionPadding = 0.02f;
    [SerializeField, Min(0.05f)] private float stuckTimeout = 0.2f;
    [SerializeField, Min(0f)] private float minimumProgress = 0.001f;
    [SerializeField, Min(0.1f)] private float waypointTimeout = 2f;
    [SerializeField, Min(0.1f)] private float navigationCellSize = 0.35f;
    [SerializeField, Min(0f)] private float navigationBoundsPadding = 2f;
    [SerializeField, Min(64)] private int navigationMaxNodes = 5000;
    [SerializeField, Min(0.1f)] private float personalSpace = 0.65f;

    private static readonly RaycastHit2D[] PathCastResults = new RaycastHit2D[32];
    private Transform[] waypoints;
    private Transform[] activeWaypoints;
    private Transform cachedWaypointParent;
    private int currentWaypointIndex;
    private bool isWaiting;
    private bool movementEnabled = true;
    private Collider2D bodyCollider;
    private Rigidbody2D body;
    private float stuckTimer;
    private float waypointTravelTimer;
    private Transform timedWaypoint;
    private float waitTimer;
    private List<Vector2> currentPath;
    private int currentPathIndex;
    private Vector2 pathTarget;
    private List<int> waypointRoute;
    private int waypointRouteIndex;
    private Vector2 waypointRouteTarget;
    private static readonly RaycastHit2D[] ChorePathCastResults = new RaycastHit2D[32];

    public bool MovementEnabled => movementEnabled;
    internal Transform CurrentWaypointParent => waypointParent;
    internal Transform CurrentWaypointTarget =>
        waypoints != null &&
        currentWaypointIndex >= 0 &&
        currentWaypointIndex < waypoints.Length
            ? waypoints[currentWaypointIndex]
            : null;

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled)
            StopMovement();
    }

    public void ResetWaypointRoute()
    {
        waypointRoute = null;
        waypointRouteIndex = 0;
        waypointRouteTarget = Vector2.zero;
        currentPath = null;
        currentPathIndex = 0;
    }

    public Vector2 GetMovementAlongWaypointRoute(Vector2 target, float speed, float waypointArrivalDistance)
    {
        return GetMovementAlongWaypointRoute(
            target,
            speed,
            waypointArrivalDistance,
            false,
            null);
    }

    public Vector2 GetMovementAlongWaypointRoute(
        Vector2 target,
        float speed,
        float waypointArrivalDistance,
        bool stayOnWaypointsWhenRouteUnavailable)
    {
        return GetMovementAlongWaypointRoute(
            target,
            speed,
            waypointArrivalDistance,
            stayOnWaypointsWhenRouteUnavailable,
            null);
    }

    public Vector2 GetMovementAlongWaypointRoute(
        Vector2 target,
        float speed,
        float waypointArrivalDistance,
        bool stayOnWaypointsWhenRouteUnavailable,
        Transform taggedPlayerObstacle)
    {
        if (waypoints == null && waypointParent != null)
            CacheWaypoints();

        if (waypoints == null || waypoints.Length == 0)
        {
            return GetMovementTowards(target, speed);
        }

        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        LayerMask routeObstacleLayers = stayOnWaypointsWhenRouteUnavailable
            ? GetChoreObstacleLayers(taggedPlayerObstacle)
            : obstacleLayers;
        if (stayOnWaypointsWhenRouteUnavailable &&
            HasClearChorePath(currentPosition, target, routeObstacleLayers, taggedPlayerObstacle))
        {
            waypointRoute = null;
            waypointRouteIndex = 0;
            waypointRouteTarget = target;
            currentPath = null;
            currentPathIndex = 0;
            return GetMovementAlongChoreSegment(
                target,
                speed,
                routeObstacleLayers,
                taggedPlayerObstacle);
        }

        if (waypointRoute == null ||
            (!stayOnWaypointsWhenRouteUnavailable &&
             waypointRoute.Count > 0 &&
             waypointRouteIndex >= waypointRoute.Count) ||
            (target - waypointRouteTarget).sqrMagnitude >
            waypointArrivalDistance * waypointArrivalDistance)
        {
            waypointRoute = stayOnWaypointsWhenRouteUnavailable
                ? FindWaypointRouteThroughGraph(currentPosition, target, routeObstacleLayers, taggedPlayerObstacle)
                : FindWaypointRoute(target);
            waypointRouteIndex = 0;
            waypointRouteTarget = target;
            currentPath = null;
            currentPathIndex = 0;
        }

        while (waypointRouteIndex < waypointRoute.Count &&
               Vector2.Distance(currentPosition, waypoints[waypointRoute[waypointRouteIndex]].position) <=
               waypointArrivalDistance)
        {
            waypointRouteIndex++;
        }

        if (waypointRouteIndex >= waypointRoute.Count)
        {
            if (!stayOnWaypointsWhenRouteUnavailable)
                return GetMovementTowards(target, speed);

            if (waypointRoute.Count == 0 ||
                !HasClearChorePath(currentPosition, target, routeObstacleLayers, taggedPlayerObstacle))
                return GetMovementTowards(target, speed);

            return GetMovementAlongChoreSegment(
                target,
                speed,
                routeObstacleLayers,
                taggedPlayerObstacle);
        }

        if (stayOnWaypointsWhenRouteUnavailable)
        {
            Vector2 nextWaypoint = waypoints[waypointRoute[waypointRouteIndex]].position;
            if (!HasClearChorePath(currentPosition, nextWaypoint, routeObstacleLayers, taggedPlayerObstacle))
                return GetMovementTowards(nextWaypoint, speed);

            return GetMovementAlongChoreSegment(
                nextWaypoint,
                speed,
                routeObstacleLayers,
                taggedPlayerObstacle);
        }

        return GetMovementTowards(
            waypoints[waypointRoute[waypointRouteIndex]].position,
            speed);
    }

    private Vector2 GetMovementAlongChoreSegment(
        Vector2 target,
        float speed,
        LayerMask routeObstacleLayers,
        Transform taggedPlayerObstacle)
    {
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 movement = Vector2.ClampMagnitude(
            target - currentPosition,
            speed * Time.deltaTime);
        movement = NPCMovement2D.GetSeparationMovement(this, currentPosition, movement, personalSpace);
        return NPCMovement2D.GetCollisionSafeMovement(
            bodyCollider,
            movement,
            target - currentPosition,
            routeObstacleLayers,
            collisionPadding,
            taggedPlayerObstacle);
    }

    private LayerMask GetChoreObstacleLayers(Transform taggedPlayerObstacle)
    {
        int layers = obstacleLayers.value;
        AddLayerToMask(ref layers, "NPCS");
        AddLayerToMask(ref layers, "WALL COLLIDERS");

        if (taggedPlayerObstacle != null)
        {
            layers |= 1 << taggedPlayerObstacle.gameObject.layer;
            foreach (Collider2D playerCollider in taggedPlayerObstacle.GetComponentsInChildren<Collider2D>(true))
            {
                if (playerCollider != null)
                    layers |= 1 << playerCollider.gameObject.layer;
            }
        }

        return layers;
    }

    private static void AddLayerToMask(ref int layers, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            layers |= 1 << layer;
    }

    void Start()
    {
        bodyCollider = GetComponent<Collider2D>();
        body = GetComponent<Rigidbody2D>();

        if (waypointParent != null && waypoints == null)
            CacheWaypoints();
    }

    private void CacheWaypoints()
    {
        cachedWaypointParent = waypointParent;
        timedWaypoint = null;
        waypointTravelTimer = 0f;
        if (waypointParent == null)
        {
            waypoints = null;
            activeWaypoints = null;
            currentWaypointIndex = 0;
            return;
        }

        var uniqueWaypoints = new List<Transform>(waypointParent.childCount);
        for (int i = 0; i < waypointParent.childCount; i++)
        {
            Transform waypoint = waypointParent.GetChild(i);
            bool duplicatesExistingPoint = false;
            foreach (Transform existingWaypoint in uniqueWaypoints)
            {
                if (((Vector2)waypoint.position - (Vector2)existingWaypoint.position).sqrMagnitude <= 0.0001f)
                {
                    duplicatesExistingPoint = true;
                    break;
                }
            }

            if (!duplicatesExistingPoint)
                uniqueWaypoints.Add(waypoint);
        }

        if (waypointOrderOverride != null && waypointOrderOverride.Length > 0)
        {
            var orderedWaypoints = new List<Transform>(uniqueWaypoints.Count);
            var addedIndices = new bool[uniqueWaypoints.Count];
            foreach (int waypointIndex in waypointOrderOverride)
            {
                if (waypointIndex < 0 ||
                    waypointIndex >= uniqueWaypoints.Count ||
                    addedIndices[waypointIndex])
                    continue;

                orderedWaypoints.Add(uniqueWaypoints[waypointIndex]);
                addedIndices[waypointIndex] = true;
            }

            for (int waypointIndex = 0; waypointIndex < uniqueWaypoints.Count; waypointIndex++)
            {
                if (!addedIndices[waypointIndex])
                    orderedWaypoints.Add(uniqueWaypoints[waypointIndex]);
            }

            if (orderedWaypoints.Count > 0)
                uniqueWaypoints = orderedWaypoints;
        }

        waypoints = uniqueWaypoints.ToArray();
        activeWaypoints = waypoints;
        currentWaypointIndex = FindNearestUnclaimedWaypointIndex(
            body != null ? body.position : (Vector2)transform.position);
    }

    private void OnEnable()
    {
        NPCMovement2D.Register(this);
        if (waypointParent != null)
            CacheWaypoints();
    }

    private void OnDisable()
    {
        NPCMovement2D.Unregister(this);
    }

    void Update()
    {
        if (cachedWaypointParent != waypointParent)
            CacheWaypoints();

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

        if (currentWaypointIndex < 0 || currentWaypointIndex >= activeWaypoints.Length)
        {
            currentWaypointIndex = FindNearestUnclaimedWaypointIndex(
                body != null ? body.position : (Vector2)transform.position);
            if (currentWaypointIndex < 0)
                return;
        }

        Transform target = activeWaypoints[currentWaypointIndex];
        if (NPCMovement2D.IsWaypointClaimed(this, target))
        {
            currentWaypointIndex = FindNearestUnclaimedWaypointIndex(
                body != null ? body.position : (Vector2)transform.position);
            if (currentWaypointIndex < 0)
                return;

            target = activeWaypoints[currentWaypointIndex];
        }

        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        if (timedWaypoint != target)
        {
            timedWaypoint = target;
            waypointTravelTimer = 0f;
        }

        if (Vector2.Distance(currentPosition, target.position) >= 0.1f)
        {
            waypointTravelTimer += Time.deltaTime;
            if (waypointTravelTimer >= waypointTimeout)
            {
                waypointTravelTimer = 0f;
                SelectAlternativeWaypoint(currentPosition);
                return;
            }
        }

        Vector2 navigationTarget = GetPathTarget(currentPosition, target.position);
        float step = moveSpeed * Time.deltaTime;
        Vector2 movement = Vector2.ClampMagnitude(navigationTarget - currentPosition, step);
        movement = NPCMovement2D.GetSeparationMovement(this, currentPosition, movement, personalSpace);
        Vector2 safeMovement = NPCMovement2D.GetCollisionSafeMovement(
            bodyCollider,
            movement,
            navigationTarget - currentPosition,
            obstacleLayers,
            collisionPadding);

        float previousDistance = Vector2.Distance(currentPosition, navigationTarget);
        float nextDistance = Vector2.Distance(currentPosition + safeMovement, navigationTarget);
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
            timedWaypoint = null;
            waypointTravelTimer = 0f;
            waitTimer = waitTime;
            if (loopWaypoints)
                currentWaypointIndex = FindNextUnclaimedWaypointIndex(currentWaypointIndex);
            else
                currentWaypointIndex = FindNextUnclaimedWaypointIndex(
                    currentWaypointIndex,
                    false);
        }
    }

    private int FindNearestUnclaimedWaypointIndex(Vector2 position)
    {
        int nearestIndex = -1;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];
            if (waypoint == null || NPCMovement2D.IsWaypointClaimed(this, waypoint))
                continue;

            float distance = ((Vector2)waypoint.position - position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestIndex = i;
                nearestDistance = distance;
            }
        }

        return nearestIndex;
    }

    private int FindNextUnclaimedWaypointIndex(int previousIndex, bool loop = true)
    {
        int searchCount = loop ? waypoints.Length : waypoints.Length - previousIndex - 1;
        for (int offset = 1; offset <= searchCount; offset++)
        {
            int index = loop
                ? (previousIndex + offset) % waypoints.Length
                : previousIndex + offset;
            Transform waypoint = waypoints[index];
            if (waypoint != null && !NPCMovement2D.IsWaypointClaimed(this, waypoint))
                return index;
        }

        return loop ? -1 : previousIndex;
    }

    public Vector2 GetMovementTowards(Vector2 target, float speed)
    {
        return GetMovementTowards(target, speed, true);
    }

    public Vector2 GetMovementTowards(Vector2 target, float speed, bool useNpcSeparation)
    {
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 navigationTarget = GetPathTarget(currentPosition, target);
        Vector2 movement = Vector2.ClampMagnitude(navigationTarget - currentPosition, speed * Time.deltaTime);
        if (useNpcSeparation)
            movement = NPCMovement2D.GetSeparationMovement(this, currentPosition, movement, personalSpace);

        return NPCMovement2D.GetCollisionSafeMovement(
            bodyCollider,
            movement,
            navigationTarget - currentPosition,
            obstacleLayers,
            collisionPadding);
    }

    public bool MoveTowards(Vector2 target, float speed, float arriveDistance)
    {
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        Vector2 movement = GetMovementTowards(target, speed);

        if (body != null)
            body.MovePosition(currentPosition + movement);
        else
            transform.position = currentPosition + movement;

        return Vector2.Distance(currentPosition, target) <= arriveDistance;
    }

    private Vector2 GetPathTarget(Vector2 currentPosition, Vector2 target)
    {
        if (HasClearPathBetween(currentPosition, target))
        {
            currentPath = null;
            currentPathIndex = 0;
            pathTarget = target;
            return target;
        }

        if (currentPath == null ||
            currentPathIndex >= currentPath.Count ||
            (target - pathTarget).sqrMagnitude > navigationCellSize * navigationCellSize)
        {
            currentPath = NPCNavigation2D.FindPath(
                currentPosition,
                target,
                bodyCollider,
                obstacleLayers,
                navigationCellSize,
                navigationBoundsPadding,
                navigationMaxNodes);
            currentPathIndex = 0;
            pathTarget = target;
        }

        while (currentPathIndex < currentPath.Count - 1 &&
               (currentPath[currentPathIndex] - currentPosition).sqrMagnitude < navigationCellSize * navigationCellSize)
        {
            currentPathIndex++;
        }

        return currentPath.Count == 0 ? target : currentPath[currentPathIndex];
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
            if (activeWaypoints[nextIndex] != null &&
                !NPCMovement2D.IsWaypointClaimed(this, activeWaypoints[nextIndex]) &&
                HasClearPath(currentPosition, activeWaypoints[nextIndex].position))
            {
                currentWaypointIndex = nextIndex;
                timedWaypoint = null;
                waypointTravelTimer = 0f;
                return;
            }
        }

        nextIndex = currentWaypointIndex;
        for (int i = 0; i < activeWaypoints.Length - 1; i++)
        {
            nextIndex = (nextIndex + 1) % activeWaypoints.Length;
            if (activeWaypoints[nextIndex] == null ||
                NPCMovement2D.IsWaypointClaimed(this, activeWaypoints[nextIndex]))
                continue;

            currentWaypointIndex = nextIndex;
            timedWaypoint = null;
            waypointTravelTimer = 0f;
            return;
        }
    }

    private bool HasClearPath(Vector2 currentPosition, Vector2 targetPosition)
    {
        return HasClearPathBetween(currentPosition, targetPosition);
    }

    private List<int> FindWaypointRoute(Vector2 target)
    {
        Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
        int startIndex = FindNearestWaypoint(currentPosition);
        int targetIndex = FindNearestWaypoint(target);
        if (startIndex < 0 || targetIndex < 0 || startIndex == targetIndex)
            return new List<int>();

        int count = waypoints.Length;
        float[] costs = new float[count];
        int[] previous = new int[count];
        bool[] visited = new bool[count];
        for (int i = 0; i < count; i++)
        {
            costs[i] = float.PositiveInfinity;
            previous[i] = -1;
        }

        costs[startIndex] = 0f;
        for (int step = 0; step < count; step++)
        {
            int current = -1;
            float bestCost = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (!visited[i] && costs[i] < bestCost)
                {
                    current = i;
                    bestCost = costs[i];
                }
            }

            if (current < 0 || current == targetIndex)
                break;

            visited[current] = true;
            for (int neighbor = 0; neighbor < count; neighbor++)
            {
                if (visited[neighbor] || neighbor == current || waypoints[neighbor] == null)
                    continue;

                Vector2 neighborPosition = waypoints[neighbor].position;
                if (!HasClearPathBetween(waypoints[current].position, neighborPosition))
                    continue;

                float routeCost = costs[current] +
                    Vector2.Distance(waypoints[current].position, neighborPosition);
                if (routeCost < costs[neighbor])
                {
                    costs[neighbor] = routeCost;
                    previous[neighbor] = current;
                }
            }
        }

        if (previous[targetIndex] < 0)
            return new List<int>();

        var route = new List<int>();
        for (int current = targetIndex; current >= 0; current = previous[current])
            route.Add(current);

        route.Reverse();
        return route;
    }

    private bool HasClearChorePath(
        Vector2 start,
        Vector2 target,
        LayerMask routeObstacleLayers,
        Transform taggedPlayerObstacle)
    {
        return HasClearPathBetween(
            start,
            target,
            routeObstacleLayers,
            taggedPlayerObstacle,
            true);
    }

    private static bool IsChoreBlockingCollider(Collider2D collider, Transform taggedPlayerObstacle)
    {
        if (collider == null)
            return false;

        if (taggedPlayerObstacle != null &&
            (collider.transform == taggedPlayerObstacle ||
             collider.transform.IsChildOf(taggedPlayerObstacle)))
            return true;

        for (Transform parent = collider.transform; parent != null; parent = parent.parent)
        {
            if (parent.CompareTag("Player"))
                return true;
        }

        int layer = collider.gameObject.layer;
        if (layer == LayerMask.NameToLayer("NPCS") ||
            layer == LayerMask.NameToLayer("WALL COLLIDERS"))
            return true;

        return !collider.isTrigger;
    }

    private List<int> FindWaypointRouteThroughGraph(
        Vector2 start,
        Vector2 target,
        LayerMask routeObstacleLayers,
        Transform taggedPlayerObstacle)
    {
        int count = waypoints.Length;
        float[] costs = new float[count];
        int[] previous = new int[count];
        bool[] visited = new bool[count];

        for (int i = 0; i < count; i++)
        {
            costs[i] = float.PositiveInfinity;
            previous[i] = -1;

            if (waypoints[i] != null &&
                HasClearPathBetween(
                    start,
                    waypoints[i].position,
                    routeObstacleLayers,
                    taggedPlayerObstacle,
                    true))
            {
                costs[i] = Vector2.Distance(start, waypoints[i].position);
            }
        }

        int targetIndex = -1;
        float bestTotalCost = float.PositiveInfinity;
        for (int step = 0; step < count; step++)
        {
            int current = -1;
            float bestCost = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (!visited[i] && costs[i] < bestCost)
                {
                    current = i;
                    bestCost = costs[i];
                }
            }

            if (current < 0 || bestCost >= bestTotalCost)
                break;

            visited[current] = true;
            Vector2 currentWaypoint = waypoints[current].position;
            if (HasClearPathBetween(
                    currentWaypoint,
                    target,
                    routeObstacleLayers,
                    taggedPlayerObstacle,
                    true))
            {
                float totalCost = bestCost + Vector2.Distance(currentWaypoint, target);
                if (totalCost < bestTotalCost)
                {
                    bestTotalCost = totalCost;
                    targetIndex = current;
                }
            }

            for (int neighbor = 0; neighbor < count; neighbor++)
            {
                if (visited[neighbor] || neighbor == current || waypoints[neighbor] == null)
                    continue;

                Vector2 neighborPosition = waypoints[neighbor].position;
                if (!HasClearPathBetween(
                        currentWaypoint,
                        neighborPosition,
                        routeObstacleLayers,
                        taggedPlayerObstacle,
                        true))
                    continue;

                float routeCost = bestCost + Vector2.Distance(currentWaypoint, neighborPosition);
                if (routeCost < costs[neighbor])
                {
                    costs[neighbor] = routeCost;
                    previous[neighbor] = current;
                }
            }
        }

        if (targetIndex < 0)
            return new List<int>();

        var route = new List<int>();
        for (int current = targetIndex; current >= 0; current = previous[current])
            route.Add(current);

        route.Reverse();
        return route;
    }

    private bool HasClearPathBetween(Vector2 start, Vector2 target)
    {
        return HasClearPathBetween(start, target, obstacleLayers, null, false);
    }

    private bool HasClearPathBetween(
        Vector2 start,
        Vector2 target,
        LayerMask layers,
        Transform taggedPlayerObstacle,
        bool includeTaggedTriggers)
    {
        Vector2 direction = target - start;
        float distance = direction.magnitude;
        if (distance <= 0.0001f)
            return true;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = includeTaggedTriggers
        };
        filter.SetLayerMask(layers);

        int hitCount;
        if (bodyCollider is BoxCollider2D boxCollider)
        {
            Vector3 scale = boxCollider.transform.lossyScale;
            Vector2 size = Vector2.Scale(
                boxCollider.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            Vector2 colliderOffset = (Vector2)bodyCollider.bounds.center - currentPosition;
            hitCount = Physics2D.BoxCast(
                start + colliderOffset,
                size,
                boxCollider.transform.eulerAngles.z,
                direction / distance,
                filter,
                PathCastResults,
                distance + collisionPadding);
        }
        else
        {
            float radius = bodyCollider != null
                ? Mathf.Max(bodyCollider.bounds.extents.x, bodyCollider.bounds.extents.y)
                : 0f;
            hitCount = Physics2D.CircleCast(
                start,
                radius + collisionPadding,
                direction / distance,
                filter,
                PathCastResults,
                distance);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = PathCastResults[i].collider;
            if (hitCollider == null || hitCollider == bodyCollider)
                continue;

            if (!includeTaggedTriggers || IsChoreBlockingCollider(hitCollider, taggedPlayerObstacle))
                return false;
        }

        return true;
    }

    private int FindNearestWaypoint(Vector2 position)
    {
        int nearestIndex = -1;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            float distance = ((Vector2)waypoints[i].position - position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestIndex = i;
                nearestDistance = distance;
            }
        }

        return nearestIndex;
    }

}