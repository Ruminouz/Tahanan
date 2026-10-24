using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class MapTransition : MonoBehaviour
{
    [Header("Confiner Settings")]
    [SerializeField] private PolygonCollider2D mapBoundary;
    [SerializeField] private CinemachineConfiner2D confiner;

    [Header("Transition Settings")]
    [SerializeField] private Direction direction;
    [SerializeField] private float positionOffset = 2.5f;

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private void Awake()
    {
        if (confiner == null)
        {
            confiner = FindFirstObjectByType<CinemachineConfiner2D>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PerformMapTransition(collision.gameObject);
        }
    }

    private void PerformMapTransition(GameObject player)
    {
        // 1. Move player position
        Vector3 offset = GetOffsetVector();
        player.transform.position += offset;
        Physics2D.SyncTransforms();

        // 2. Assign target boundary and clear shape cache
        if (confiner != null && mapBoundary != null)
        {
            confiner.BoundingShape2D = mapBoundary;
            confiner.InvalidateBoundingShapeCache();
        }

        // 3. Force Cinemachine camera tracking state reset
        if (confiner != null && confiner.TryGetComponent<CinemachineCamera>(out var vcam))
        {
            // Inform Cinemachine of the target warp so damping/positioning resets cleanly
            vcam.OnTargetObjectWarped(player.transform, offset);
            
            // Force the camera GameObject itself to move immediately to the player's new position
            vcam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, vcam.transform.position.z);
            vcam.ForceCameraPosition(vcam.transform.position, vcam.transform.rotation);
        }
    }

    private Vector3 GetOffsetVector()
    {
        switch (direction)
        {
            case Direction.Up:    return new Vector3(0, positionOffset, 0);
            case Direction.Down:  return new Vector3(0, -positionOffset, 0);
            case Direction.Left:  return new Vector3(-positionOffset, 0, 0);
            case Direction.Right: return new Vector3(positionOffset, 0, 0);
            default:             return Vector3.zero;
        }
    }
}