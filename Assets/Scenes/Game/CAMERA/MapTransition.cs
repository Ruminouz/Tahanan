using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class MapTransition : MonoBehaviour
{
    private const float CameraTransitionDuration = 0.65f;

    [Header("Confiner Settings")]
    [SerializeField] private PolygonCollider2D mapBoundary;
    [SerializeField] private CinemachineConfiner2D confiner;

    [Header("Transition Settings")]
    [SerializeField] private Direction direction;
    [SerializeField] private float positionOffset = 2.5f;

    private static CinemachineCamera transitioningCamera;
    private static Coroutine cameraTransition;

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

        // 3. Smoothly move the camera to the player's new position
        if (confiner != null && confiner.TryGetComponent<CinemachineCamera>(out var vcam))
        {
            Vector3 destination = vcam.transform.position;
            destination.x = player.transform.position.x;
            destination.y = player.transform.position.y;
            vcam.Follow = null;
            StartCameraTransition(vcam, destination, player.transform);
        }
    }

    public static void MoveCameraToPosition(Vector3 destination)
    {
        Physics2D.SyncTransforms();

        SetConfinerBoundaryAtPosition(destination);

        CinemachineCamera cameraRig = FindCameraRig();
        if (cameraRig == null)
            return;

        Vector3 cameraPosition = cameraRig.transform.position;
        cameraPosition.x = destination.x;
        cameraPosition.y = destination.y;
        cameraRig.Follow = null;
        StartCameraTransition(cameraRig, cameraPosition);
    }

    public static IEnumerator MoveCameraToPositionAndWait(
        Vector3 destination,
        Transform followTarget = null)
    {
        Physics2D.SyncTransforms();
        SetConfinerBoundaryAtPosition(destination);

        CinemachineCamera cameraRig = FindCameraRig();
        if (cameraRig == null)
        {
            Debug.LogWarning(
                "The camera could not move because no active Cinemachine camera was found.");
            yield break;
        }

        Vector3 cameraPosition = cameraRig.transform.position;
        cameraPosition.x = destination.x;
        cameraPosition.y = destination.y;
        cameraRig.Follow = null;
        StartCameraTransition(cameraRig, cameraPosition, followTarget);

        while (cameraTransition != null && transitioningCamera == cameraRig)
            yield return null;
    }

    public static void SetConfinerBoundaryAtPosition(Vector3 destination)
    {
        Physics2D.SyncTransforms();

        CinemachineCamera cameraRig = FindCameraRig();
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

    public static void ReturnCameraToPlayer(PlayerMovement player)
    {
        if (player == null)
            return;

        Vector3 destination = player.transform.position;
        CinemachineCamera cameraRig = FindCameraRig();
        if (cameraRig == null)
            return;

        SetConfinerBoundaryAtPosition(destination);
        Vector3 cameraPosition = cameraRig.transform.position;
        cameraPosition.x = destination.x;
        cameraPosition.y = destination.y;
        cameraRig.Follow = null;
        StartCameraTransition(cameraRig, cameraPosition, player.transform);
    }

    public static void PanCamera(Vector3 worldOffset)
    {
        CinemachineCamera cameraRig = FindCameraRig();
        if (cameraRig == null)
            return;

        CancelCameraTransition();
        Vector3 cameraPosition = cameraRig.transform.position + worldOffset;
        cameraRig.transform.position = cameraPosition;
        cameraRig.ForceCameraPosition(cameraPosition, cameraRig.transform.rotation);
    }

    private static CinemachineCamera FindCameraRig()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(
            FindObjectsSortMode.None);
        foreach (CinemachineCamera cameraRig in cameras)
        {
            if (cameraRig != null && cameraRig.isActiveAndEnabled)
                return cameraRig;
        }

        return null;
    }

    private static void StartCameraTransition(
        CinemachineCamera cameraRig,
        Vector3 destination,
        Transform followTarget = null)
    {
        CancelCameraTransition();
        transitioningCamera = cameraRig;
        cameraTransition = cameraRig.StartCoroutine(
            SmoothCameraTransition(cameraRig, destination, followTarget));
    }

    private static void CancelCameraTransition()
    {
        if (cameraTransition == null || transitioningCamera == null)
            return;

        transitioningCamera.StopCoroutine(cameraTransition);

        cameraTransition = null;
        transitioningCamera = null;
    }

    private static IEnumerator SmoothCameraTransition(
        CinemachineCamera cameraRig,
        Vector3 destination,
        Transform followTarget)
    {
        Vector3 startPosition = cameraRig.transform.position;
        Vector3 targetStartPosition = followTarget != null
            ? followTarget.position
            : Vector3.zero;
        float elapsed = 0f;

        while (elapsed < CameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / CameraTransitionDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            Vector3 currentDestination = GetCurrentDestination(
                destination,
                followTarget,
                targetStartPosition);
            Vector3 position = Vector3.LerpUnclamped(
                startPosition,
                currentDestination,
                easedProgress);
            cameraRig.transform.position = position;
            cameraRig.ForceCameraPosition(position, cameraRig.transform.rotation);
            yield return null;
        }

        Vector3 finalPosition = GetCurrentDestination(
            destination,
            followTarget,
            targetStartPosition);
        cameraRig.transform.position = finalPosition;
        cameraRig.ForceCameraPosition(finalPosition, cameraRig.transform.rotation);
        if (followTarget != null)
            cameraRig.Follow = followTarget;

        cameraTransition = null;
        transitioningCamera = null;
    }

    private static Vector3 GetCurrentDestination(
        Vector3 destination,
        Transform followTarget,
        Vector3 targetStartPosition)
    {
        if (followTarget != null)
            destination += followTarget.position - targetStartPosition;

        return destination;
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