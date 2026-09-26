using System.Collections.Generic;
using UnityEngine;

public static class NPCNavigation2D
{
    private struct Node
    {
        public int x;
        public int y;
        public float cost;
        public float priority;
        public int parent;
    }

    private static readonly Collider2D[] OverlapResults = new Collider2D[32];

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
        int startIndex = ToIndex(FindNearestWalkable(start), width);
        int targetIndex = ToIndex(FindNearestWalkable(target), width);

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
                return ReconstructPath(nodes, currentIndex, width, origin, cellSize, target);

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
                    if (!IsWalkable(nextIndex, width, origin, cellSize, mover, obstacleFilter))
                        continue;
                    if (x != 0 && y != 0 &&
                        (!IsWalkable(currentY * width + nextX, width, origin, cellSize, mover, obstacleFilter) ||
                         !IsWalkable(nextY * width + currentX, width, origin, cellSize, mover, obstacleFilter)))
                        continue;

                    float moveCost = x != 0 && y != 0 ? 1.4142f : 1f;
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

        int FindNearestWalkable(Vector2 position)
        {
            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!IsWalkable(index, width, origin, cellSize, mover, obstacleFilter))
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
        float heuristic = Mathf.Abs(targetX - x) + Mathf.Abs(targetY - y);
        return new Node
        {
            x = x,
            y = y,
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
        int count = Physics2D.OverlapCircle(
            position,
            Mathf.Max(mover.bounds.extents.x, mover.bounds.extents.y) + cellSize * 0.45f,
            obstacleFilter,
            OverlapResults);

        for (int i = 0; i < count; i++)
        {
            if (OverlapResults[i] != null && OverlapResults[i] != mover)
                return false;
        }

        return true;
    }

    private static Vector2 GridPosition(int index, int width, Vector2 origin, float cellSize)
    {
        return origin + new Vector2(index % width + 0.5f, index / width + 0.5f) * cellSize;
    }

    private static int ToIndex(int index, int width)
    {
        return index < 0 ? -1 : index;
    }

    private static List<Vector2> ReconstructPath(
        Dictionary<int, Node> nodes,
        int currentIndex,
        int width,
        Vector2 origin,
        float cellSize,
        Vector2 target)
    {
        var path = new List<Vector2>();
        while (currentIndex >= 0 && nodes.TryGetValue(currentIndex, out Node node))
        {
            path.Add(GridPosition(currentIndex, width, origin, cellSize));
            currentIndex = node.parent;
        }

        path.Reverse();
        path.Add(target);
        return path;
    }
}
