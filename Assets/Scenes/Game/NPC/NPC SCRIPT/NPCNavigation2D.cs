using System.Collections.Generic;
using UnityEngine;

public static class NPCNavigation2D
{
    private struct Node
    {
        public float cost;
        public float priority;
        public int parent;
    }

    private static readonly Collider2D[] OverlapResults = new Collider2D[32];
    private static readonly RaycastHit2D[] PathCastResults = new RaycastHit2D[32];

    public static List<Vector2> FindPath(
        Vector2 start,
        Vector2 target,
        Collider2D mover,
        LayerMask obstacleLayers,
        float cellSize,
        float boundsPadding,
        int maxNodes)
    {
        var path = new List<Vector2>();
        if (mover == null || cellSize <= 0f || maxNodes < 16)
        {
            path.Add(target);
            return path;
        }

        Bounds bounds = mover.bounds;
        bounds.Encapsulate(start);
        bounds.Encapsulate(target);
        bounds.Expand(boundsPadding * 2f + cellSize * 2f);

        ContactFilter2D obstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false
        };
        obstacleFilter.SetLayerMask(obstacleLayers);

        int width = Mathf.Clamp(Mathf.CeilToInt(bounds.size.x / cellSize), 2, 128);
        int height = Mathf.Clamp(Mathf.CeilToInt(bounds.size.y / cellSize), 2, 128);
        Vector2 origin = bounds.min;
        var walkabilityCache = new byte[width * height];
        int startIndex = FindNearestWalkable(start);
        int targetIndex = FindNearestWalkable(target);

        if (startIndex < 0 || targetIndex < 0 || startIndex == targetIndex)
        {
            path.Add(target);
            return path;
        }

        var nodes = new Dictionary<int, Node>();
        var open = new List<int> { startIndex };
        nodes[startIndex] = CreateNode(startIndex, 0f, targetIndex, -1, width, origin, cellSize);
        int visited = 0;

        while (open.Count > 0 && visited++ < maxNodes)
        {
            int currentIndex = open[0];
            for (int i = 1; i < open.Count; i++)
            {
                if (nodes[open[i]].priority < nodes[currentIndex].priority)
                    currentIndex = open[i];
            }

            if (currentIndex == targetIndex)
                return ReconstructPath(
                    nodes,
                    currentIndex,
                    width,
                    origin,
                    cellSize,
                    target,
                    mover,
                    obstacleFilter);

            open.Remove(currentIndex);
            Node current = nodes[currentIndex];
            int currentX = currentIndex % width;
            int currentY = currentIndex / width;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int nextX = currentX + x;
                    int nextY = currentY + y;
                    if (nextX < 0 || nextX >= width || nextY < 0 || nextY >= height)
                        continue;

                    int nextIndex = nextY * width + nextX;
                    if (!IsCellWalkable(nextIndex))
                        continue;
                    if (x != 0 && y != 0 &&
                        (!IsCellWalkable(currentY * width + nextX) ||
                         !IsCellWalkable(nextY * width + currentX)))
                        continue;

                    float moveCost = x != 0 && y != 0 ? Mathf.Sqrt(2f) : 1f;
                    float nextCost = current.cost + moveCost;
                    if (nodes.TryGetValue(nextIndex, out Node existing) && existing.cost <= nextCost)
                        continue;

                    nodes[nextIndex] = CreateNode(
                        nextIndex,
                        nextCost,
                        targetIndex,
                        currentIndex,
                        width,
                        origin,
                        cellSize);
                    if (!open.Contains(nextIndex))
                        open.Add(nextIndex);
                }
            }
        }

        path.Add(target);
        return path;

        bool IsCellWalkable(int index)
        {
            if (walkabilityCache[index] == 0)
            {
                walkabilityCache[index] = IsWalkable(
                    index,
                    width,
                    origin,
                    cellSize,
                    mover,
                    obstacleFilter)
                    ? (byte)1
                    : (byte)2;
            }

            return walkabilityCache[index] == 1;
        }

        int FindNearestWalkable(Vector2 position)
        {
            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!IsCellWalkable(index))
                        continue;

                    float distance = (GridPosition(index, width, origin, cellSize) - position).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearest = index;
                        nearestDistance = distance;
                    }
                }
            }

            return nearest;
        }
    }

    private static Node CreateNode(
        int index,
        float cost,
        int targetIndex,
        int parent,
        int width,
        Vector2 origin,
        float cellSize)
    {
        int x = index % width;
        int y = index / width;
        int targetX = targetIndex % width;
        int targetY = targetIndex / width;
        int deltaX = Mathf.Abs(targetX - x);
        int deltaY = Mathf.Abs(targetY - y);
        int diagonalSteps = Mathf.Min(deltaX, deltaY);
        int straightSteps = Mathf.Max(deltaX, deltaY) - diagonalSteps;
        float heuristic = diagonalSteps * Mathf.Sqrt(2f) + straightSteps;
        return new Node
        {
            cost = cost,
            priority = cost + heuristic,
            parent = parent
        };
    }

    private static bool IsWalkable(
        int index,
        int width,
        Vector2 origin,
        float cellSize,
        Collider2D mover,
        ContactFilter2D obstacleFilter)
    {
        Vector2 position = GridPosition(index, width, origin, cellSize);
        int count;
        if (mover is BoxCollider2D boxCollider)
        {
            Vector3 scale = boxCollider.transform.lossyScale;
            Vector2 size = Vector2.Scale(
                boxCollider.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y))) +
                Vector2.one * cellSize * 0.9f;
            Vector2 colliderOffset = (Vector2)mover.bounds.center - (Vector2)mover.transform.position;
            count = Physics2D.OverlapBox(
                position + colliderOffset,
                size,
                boxCollider.transform.eulerAngles.z,
                obstacleFilter,
                OverlapResults);
        }
        else
        {
            count = Physics2D.OverlapCircle(
                position,
                Mathf.Max(mover.bounds.extents.x, mover.bounds.extents.y) + cellSize * 0.45f,
                obstacleFilter,
                OverlapResults);
        }

        for (int i = 0; i < count; i++)
        {
            Collider2D hitCollider = OverlapResults[i];
            if (hitCollider != null &&
                hitCollider != mover &&
                (mover.attachedRigidbody == null ||
                 hitCollider.attachedRigidbody != mover.attachedRigidbody))
                return false;
        }

        return true;
    }

    private static Vector2 GridPosition(int index, int width, Vector2 origin, float cellSize)
    {
        return origin + new Vector2(index % width + 0.5f, index / width + 0.5f) * cellSize;
    }

    private static List<Vector2> ReconstructPath(
        Dictionary<int, Node> nodes,
        int currentIndex,
        int width,
        Vector2 origin,
        float cellSize,
        Vector2 target,
        Collider2D mover,
        ContactFilter2D obstacleFilter)
    {
        var path = new List<Vector2>();
        while (currentIndex >= 0 && nodes.TryGetValue(currentIndex, out Node node))
        {
            path.Add(GridPosition(currentIndex, width, origin, cellSize));
            currentIndex = node.parent;
        }

        path.Reverse();
        path = SmoothPath(path, mover, obstacleFilter, cellSize * 0.45f);
        path.Add(target);
        return path;
    }

    private static List<Vector2> SmoothPath(
        List<Vector2> path,
        Collider2D mover,
        ContactFilter2D obstacleFilter,
        float clearancePadding)
    {
        if (path.Count < 3 || mover == null)
            return path;

        var smoothedPath = new List<Vector2> { path[0] };
        int currentIndex = 0;
        while (currentIndex < path.Count - 1)
        {
            int furthestVisibleIndex = path.Count - 1;
            while (furthestVisibleIndex > currentIndex + 1 &&
                   !HasClearPath(
                       path[currentIndex],
                       path[furthestVisibleIndex],
                       mover,
                       obstacleFilter,
                       clearancePadding))
            {
                furthestVisibleIndex--;
            }

            smoothedPath.Add(path[furthestVisibleIndex]);
            currentIndex = furthestVisibleIndex;
        }

        return smoothedPath;
    }

    private static bool HasClearPath(
        Vector2 start,
        Vector2 target,
        Collider2D mover,
        ContactFilter2D obstacleFilter,
        float clearancePadding)
    {
        Vector2 direction = target - start;
        float distance = direction.magnitude;
        if (distance <= 0.0001f)
            return true;

        int hitCount;
        if (mover is BoxCollider2D boxCollider)
        {
            Vector3 scale = boxCollider.transform.lossyScale;
            Vector2 size = Vector2.Scale(
                boxCollider.size,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y))) +
                Vector2.one * clearancePadding * 2f;
            Vector2 colliderOffset = (Vector2)mover.bounds.center - (Vector2)mover.transform.position;
            hitCount = Physics2D.BoxCast(
                start + colliderOffset,
                size,
                boxCollider.transform.eulerAngles.z,
                direction / distance,
                obstacleFilter,
                PathCastResults,
                distance);
        }
        else
        {
            float radius = Mathf.Max(mover.bounds.extents.x, mover.bounds.extents.y) + clearancePadding;
            hitCount = Physics2D.CircleCast(
                start,
                radius,
                direction / distance,
                obstacleFilter,
                PathCastResults,
                distance);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = PathCastResults[i].collider;
            if (hitCollider != null &&
                hitCollider != mover &&
                (mover.attachedRigidbody == null ||
                 hitCollider.attachedRigidbody != mover.attachedRigidbody))
                return false;
        }

        return true;
    }
}
