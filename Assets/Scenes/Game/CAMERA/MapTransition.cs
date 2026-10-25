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

    public bool LeadsToAreaContaining(Vector2 position)
    {
        return mapBoundary != null &&
            mapBoundary.enabled &&
            mapBoundary.gameObject.activeInHierarchy &&
            mapBoundary.OverlapPoint(position);
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

    public static void MoveCameraToPosition(Vector3 destination)
    {
        Physics2D.SyncTransforms();

        SetConfinerBoundaryAtPosition(destination);

        CinemachineCamera cameraRig = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        if (cameraRig == null)
            return;

        cameraRig.Follow = null;
        Vector3 cameraPosition = cameraRig.transform.position;
        cameraPosition.x = destination.x;
        cameraPosition.y = destination.y;
        cameraRig.transform.position = cameraPosition;
        cameraRig.ForceCameraPosition(cameraPosition, cameraRig.transform.rotation);
    }

    public static void SetConfinerBoundaryAtPosition(Vector3 destination)
    {
        Physics2D.SyncTransforms();

        CinemachineCamera cameraRig = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        if (cameraRig == null)
            return;

        CinemachineConfiner2D cameraConfiner = cameraRig.GetComponent<CinemachineConfiner2D>();
        if (cameraConfiner != null)
        {
            MapTransition[] transitions = FindObjectsByType<MapTransition>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            PolygonCollider2D destinationBoundary = null;
            float boundaryArea = float.PositiveInfinity;

            foreach (MapTransition transition in transitions)
            {
                PolygonCollider2D boundary = transition.mapBoundary;
                if (boundary == null ||
                    !boundary.enabled ||
                    !boundary.gameObject.activeInHierarchy ||
                    !boundary.OverlapPoint(destination))
                {
                    continue;
                }

                float area = boundary.bounds.size.x * boundary.bounds.size.y;
                if (area < boundaryArea)
                {
                    destinationBoundary = boundary;
                    boundaryArea = area;
                }
            }

            if (destinationBoundary != null)
            {
                cameraConfiner.BoundingShape2D = destinationBoundary;
                cameraConfiner.InvalidateBoundingShapeCache();
            }
            else
            {
                Debug.LogWarning(
                    "No map boundary contains the requested camera position; the camera may not be confined correctly.",
                    cameraRig);
            }
        }
    }

    public static void ReturnCameraToPlayer(PlayerMovement player, Vector3 previousPosition)
    {
        if (player == null)
            return;

        Vector3 destination = player.transform.position;
        CinemachineCamera cameraRig = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        if (cameraRig == null)
            return;

        MoveCameraToPosition(destination);
        cameraRig.Follow = player.transform;
        cameraRig.OnTargetObjectWarped(player.transform, destination - previousPosition);
        Vector3 cameraPosition = cameraRig.transform.position;
        cameraPosition.x = destination.x;
        cameraPosition.y = destination.y;
        cameraRig.transform.position = cameraPosition;
        cameraRig.ForceCameraPosition(cameraPosition, cameraRig.transform.rotation);
    }

    public static void PanCamera(Vector3 worldOffset)
    {
        CinemachineCamera cameraRig = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        if (cameraRig == null)
            return;

        Vector3 cameraPosition = cameraRig.transform.position + worldOffset;
        cameraRig.transform.position = cameraPosition;
        cameraRig.ForceCameraPosition(cameraPosition, cameraRig.transform.rotation);
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