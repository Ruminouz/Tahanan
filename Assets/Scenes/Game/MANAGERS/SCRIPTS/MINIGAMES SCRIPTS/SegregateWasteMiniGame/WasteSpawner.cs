using System.Collections.Generic;
using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    private const int RandomPositionAttempts = 100;
    private const int FallbackPositionAttempts = 500;

    [SerializeField, Min(0f)]
    [Tooltip("Minimum edge-to-edge spacing between toys, measured in Spawn Area local units.")]
    private float minimumDistanceBetweenToys = 80f;

    [SerializeField] private GameObject leafPrefab;
    [SerializeField] private GameObject bottlePrefab;
    [SerializeField] private GameObject wrapperPrefab;
    [SerializeField] private GameObject wormPrefab;

    private struct SpawnPlacement
    {
        public Vector2 position;
        public Vector2 size;
    }

    public WasteObject[] SpawnWaste(int totalCount, Transform spawnArea, int currentDay)
    {
        WasteObject[] spawnedWaste = new WasteObject[totalCount];
        List<SpawnPlacement> placements = new List<SpawnPlacement>(totalCount);
        int spawnIndex = 0;

        int leafCount;
        int bottleCount;
        int wrapperCount;
        int wormCount;

        switch (currentDay)
        {
            case 2:
                leafCount = 3;
                bottleCount = 2;
                wrapperCount = 0;
                wormCount = 0;
                break;
            case 3:
                leafCount = 3;
                bottleCount = 2;
                wrapperCount = 2;
                wormCount = 0;
                break;
            case 4:
                leafCount = 3;
                bottleCount = 3;
                wrapperCount = 3;
                wormCount = 0;
                break;
            case 5:
                leafCount = 4;
                bottleCount = 3;
                wrapperCount = 3;
                wormCount = 0;
                break;
            case 6:
                leafCount = 4;
                bottleCount = 4;
                wrapperCount = 3;
                wormCount = 1;
                break;
            case 7:
                leafCount = 5;
                bottleCount = 4;
                wrapperCount = 4;
                wormCount = 1;
                break;
            default:
                leafCount = 3;
                bottleCount = 2;
                wrapperCount = 0;
                wormCount = 0;
                break;
        }

        SpawnItems(spawnedWaste, ref spawnIndex, leafCount, leafPrefab, WasteType.Leaves, spawnArea, placements);
        SpawnItems(spawnedWaste, ref spawnIndex, bottleCount, bottlePrefab, WasteType.Bottle, spawnArea, placements);
        SpawnItems(spawnedWaste, ref spawnIndex, wrapperCount, wrapperPrefab, WasteType.Wrapper, spawnArea, placements);
        SpawnItems(spawnedWaste, ref spawnIndex, wormCount, wormPrefab, WasteType.Worm, spawnArea, placements);

        return spawnedWaste;
    }

    private void SpawnItems(
        WasteObject[] spawnedWaste,
        ref int spawnIndex,
        int itemCount,
        GameObject prefab,
        WasteType wasteType,
        Transform spawnArea,
        List<SpawnPlacement> placements)
    {
        for (int i = 0; i < itemCount && spawnIndex < spawnedWaste.Length; i++)
        {
            spawnedWaste[spawnIndex] = SpawnWasteItem(prefab, spawnArea, wasteType, placements);
            spawnIndex++;
        }
    }

    private WasteObject SpawnWasteItem(
        GameObject prefab,
        Transform spawnArea,
        WasteType wasteType,
        List<SpawnPlacement> placements)
    {
        if (prefab == null)
        {
            Debug.LogError($"Prefab for {wasteType} is not assigned!");
            return null;
        }

        if (spawnArea == null)
        {
            Debug.LogError("Spawn area is not assigned!");
            return null;
        }

        RectTransform prefabRect = prefab.GetComponent<RectTransform>();
        Vector2 itemSize = prefabRect != null
            ? Vector2.Scale(prefabRect.rect.size, Abs(prefabRect.localScale))
            : Vector2.Scale(Vector2.one * 64f, Abs(prefab.transform.localScale));

        Vector2 spawnPosition = GetScatteredSpawnPosition(spawnArea, itemSize, placements);
        GameObject spawnedItem = Instantiate(prefab, spawnArea);
        spawnedItem.transform.localPosition = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        placements.Add(new SpawnPlacement { position = spawnPosition, size = itemSize });
        WasteObject wasteObject = spawnedItem.GetComponent<WasteObject>();

        if (wasteObject == null)
        {
            Debug.LogError($"Spawned prefab for {wasteType} does not have a WasteObject component!");
        }

        return wasteObject;
    }

    private Vector2 GetScatteredSpawnPosition(
        Transform spawnArea,
        Vector2 itemSize,
        List<SpawnPlacement> placements)
    {
        RectTransform rectTransform = spawnArea as RectTransform;
        Rect bounds = rectTransform != null
            ? rectTransform.rect
            : new Rect(-100f, -100f, 200f, 200f);
        float halfWidth = Mathf.Max(itemSize.x, 1f) / 2f;
        float halfHeight = Mathf.Max(itemSize.y, 1f) / 2f;
        float marginX = Mathf.Min(halfWidth, bounds.width * 0.1f);
        float marginY = Mathf.Min(halfHeight, bounds.height * 0.1f);
        float minX = bounds.xMin + marginX;
        float maxX = bounds.xMax - marginX;
        float minY = bounds.yMin + marginY;
        float maxY = bounds.yMax - marginY;

        for (int attempt = 0; attempt < RandomPositionAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));

            if (HasEnoughSpace(candidate, itemSize, placements))
            {
                return candidate;
            }
        }

        Vector2 bestPosition = default;
        float bestClearance = float.NegativeInfinity;
        for (int attempt = 0; attempt < FallbackPositionAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));
            float nearestClearance = float.PositiveInfinity;

            foreach (SpawnPlacement placement in placements)
            {
                float clearance = GetPlacementClearance(candidate, itemSize, placement);
                nearestClearance = Mathf.Min(nearestClearance, clearance);
            }

            if (nearestClearance > bestClearance)
            {
                bestClearance = nearestClearance;
                bestPosition = candidate;
            }
        }

        return bestPosition;
    }

    private bool HasEnoughSpace(Vector2 candidate, Vector2 itemSize, List<SpawnPlacement> placements)
    {
        foreach (SpawnPlacement placement in placements)
        {
            if (GetPlacementClearance(candidate, itemSize, placement) < 0f)
            {
                return false;
            }
        }

        return true;
    }

    private float GetPlacementClearance(
        Vector2 candidate,
        Vector2 itemSize,
        SpawnPlacement placement)
    {
        float gapX = Mathf.Abs(candidate.x - placement.position.x)
            - (itemSize.x + placement.size.x) / 2f
            - minimumDistanceBetweenToys;
        float gapY = Mathf.Abs(candidate.y - placement.position.y)
            - (itemSize.y + placement.size.y) / 2f
            - minimumDistanceBetweenToys;

        if (gapX < 0f && gapY < 0f)
        {
            return Mathf.Max(gapX, gapY);
        }

        return new Vector2(Mathf.Max(gapX, 0f), Mathf.Max(gapY, 0f)).magnitude;
    }

    private Vector2 Abs(Vector3 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }
}
