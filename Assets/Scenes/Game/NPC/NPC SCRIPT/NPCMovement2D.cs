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
        if (moverCollider == null || movement.sqrMagnitude <= 0.0001f)
            return movement;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = false
        };
        filter.SetLayerMask(obstacleLayers);

        int hitCount = moverCollider.Cast(
            movement.normalized,
            filter,
            CastResults,
            movement.magnitude + collisionPadding);

        if (hitCount == 0)
            return movement;

        RaycastHit2D nearestHit = CastResults[0];
        for (int i = 1; i < hitCount; i++)
        {
            if (CastResults[i].distance < nearestHit.distance)
                nearestHit = CastResults[i];
        }

        Vector2 wallDirection = Vector2.Perpendicular(nearestHit.normal).normalized;
        if (Vector2.Dot(wallDirection, toTarget) < 0f)
            wallDirection = -wallDirection;

        Vector2 slideMovement = wallDirection * movement.magnitude;
        int slideHitCount = moverCollider.Cast(
            slideMovement.normalized,
            filter,
            CastResults,
            slideMovement.magnitude + collisionPadding);

        return slideHitCount == 0 ? slideMovement : Vector2.zero;
    }
}
