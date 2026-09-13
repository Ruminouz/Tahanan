using UnityEngine;

public static class NPCMovement2D
{
    private static readonly RaycastHit2D[] CastResults = new RaycastHit2D[8];

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
