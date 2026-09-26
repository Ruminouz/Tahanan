using UnityEngine;
using System.Collections.Generic;

public static class NPCMovement2D
{
    private static readonly RaycastHit2D[] CastResults = new RaycastHit2D[8];
    private static readonly List<WaypointMover> Movers = new();

    public static void Register(WaypointMover mover)
    {
        if (mover != null && !Movers.Contains(mover))
            Movers.Add(mover);
    }

    public static void Unregister(WaypointMover mover)
    {
        Movers.Remove(mover);
    }

    public static Vector2 GetSeparationMovement(
        WaypointMover mover,
        Vector2 position,
        Vector2 movement,
        float personalSpace)
    {
        if (movement.sqrMagnitude <= 0.0001f || personalSpace <= 0f)
            return movement;

        Vector2 result = movement;
        float minimumDistance = personalSpace;
        foreach (WaypointMover other in Movers)
        {
            if (other == null || other == mover || !other.MovementEnabled)
                continue;

            Vector2 offset = position - (Vector2)other.transform.position;
            float distance = offset.magnitude;
            if (distance <= 0.001f || distance >= minimumDistance)
                continue;

            float strength = (minimumDistance - distance) / minimumDistance;
            Vector2 sideStep = Vector2.Perpendicular(offset / distance) * strength * movement.magnitude;
            if (Vector2.Dot(sideStep, movement) < 0f)
                sideStep = -sideStep;
            result += sideStep + offset.normalized * strength * movement.magnitude;
        }

        return Vector2.ClampMagnitude(result, movement.magnitude);
    }

    public static Vector2 GetCollisionSafeMovement(
        Collider2D moverCollider,
        Vector2 movement,
        Vector2 toTarget,
        LayerMask obstacleLayers,
        float collisionPadding)
    {
        return GetCollisionSafeMovementInternal(
            moverCollider,
            movement,
            toTarget,
            obstacleLayers,
            collisionPadding,
            null,
            false);
    }

    public static Vector2 GetCollisionSafeMovement(
        Collider2D moverCollider,
        Vector2 movement,
        Vector2 toTarget,
        LayerMask obstacleLayers,
        float collisionPadding,
        Transform taggedPlayerObstacle)
    {
        return GetCollisionSafeMovementInternal(
            moverCollider,
            movement,
            toTarget,
            obstacleLayers,
            collisionPadding,
            taggedPlayerObstacle,
            true);
    }

    private static Vector2 GetCollisionSafeMovementInternal(
        Collider2D moverCollider,
        Vector2 movement,
        Vector2 toTarget,
        LayerMask obstacleLayers,
        float collisionPadding,
        Transform taggedPlayerObstacle,
        bool includeTaggedPlayerTriggers)
    {
        if (moverCollider == null || movement.sqrMagnitude <= 0.0001f)
            return movement;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = includeTaggedPlayerTriggers
        };
        filter.SetLayerMask(obstacleLayers);

        int hitCount = moverCollider.Cast(
            movement.normalized,
            filter,
            CastResults,
            movement.magnitude + collisionPadding);

        RaycastHit2D nearestHit = default;
        bool foundBlockingHit = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = CastResults[i].collider;
            if (!IsBlockingCollider(hitCollider, taggedPlayerObstacle, includeTaggedPlayerTriggers))
                continue;

            if (!foundBlockingHit || CastResults[i].distance < nearestHit.distance)
            {
                nearestHit = CastResults[i];
                foundBlockingHit = true;
            }
        }

        if (!foundBlockingHit)
            return movement;

        Vector2 wallDirection = Vector2.Perpendicular(nearestHit.normal).normalized;
        if (Vector2.Dot(wallDirection, toTarget) < 0f)
            wallDirection = -wallDirection;

        Vector2 slideMovement = wallDirection * movement.magnitude;
        int slideHitCount = moverCollider.Cast(
            slideMovement.normalized,
            filter,
            CastResults,
            slideMovement.magnitude + collisionPadding);

        for (int i = 0; i < slideHitCount; i++)
        {
            if (IsBlockingCollider(
                    CastResults[i].collider,
                    taggedPlayerObstacle,
                    includeTaggedPlayerTriggers))
                return Vector2.zero;
        }

        return slideMovement;
    }

    private static bool IsBlockingCollider(
        Collider2D collider,
        Transform taggedPlayerObstacle,
        bool includeTaggedPlayerTriggers)
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

        if (includeTaggedPlayerTriggers)
        {
            int layer = collider.gameObject.layer;
            if (layer == LayerMask.NameToLayer("NPCS") ||
                layer == LayerMask.NameToLayer("WALL COLLIDERS"))
                return true;
        }

        return !collider.isTrigger;
    }
}
