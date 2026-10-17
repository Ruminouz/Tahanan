using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WasteSpawner : MonoBehaviour
{
    private const int RandomPositionAttempts = 100;
    private const int FallbackPositionAttempts = 500;

    [SerializeField, Min(0f)]
    [Tooltip("Minimum edge-to-edge spacing between toys, measured in Spawn Area local units.")]
    private float minimumDistanceBetweenToys = 80f;

    [SerializeField] private Sprite[] toySprites = new Sprite[8];
    [SerializeField, HideInInspector] private GameObject leafPrefab;
    [SerializeField, HideInInspector] private GameObject bottlePrefab;
    [SerializeField, HideInInspector] private GameObject wrapperPrefab;
    [SerializeField, HideInInspector] private GameObject wormPrefab;

    private struct SpawnPlacement
    {
        public Vector2 position;
        public Vector2 size;
    }

    public WasteObject[] SpawnWaste(int totalCount, Transform spawnArea, Transform reservedArea = null)
    {
        WasteObject[] spawnedWaste = new WasteObject[totalCount];

        if (spawnArea == null)
        {
            Debug.LogError("Toy spawn area is not assigned!");
            return spawnedWaste;
        }

        List<SpawnPlacement> placements = new List<SpawnPlacement>(totalCount);
        bool hasReservedArea = TryGetReservedAreaRect(spawnArea, reservedArea, out Rect reservedRect);
        List<Sprite> availableSprites = new List<Sprite>(toySprites != null ? toySprites.Length : 0);

        if (toySprites != null)
        {
            foreach (Sprite toySprite in toySprites)
            {
                if (toySprite != null)
                    availableSprites.Add(toySprite);
            }
        }

        if (availableSprites.Count > 0)
        {
            Shuffle(availableSprites);

            for (int i = 0; i < totalCount; i++)
            {
                spawnedWaste[i] = SpawnToy(
                    availableSprites[i % availableSprites.Count],
                    spawnArea,
                    placements,
                    reservedRect,
                    hasReservedArea);
            }

            return spawnedWaste;
        }

        List<GameObject> legacyPrefabs = new List<GameObject>(4);
        AddPrefabIfAssigned(legacyPrefabs, leafPrefab);
        AddPrefabIfAssigned(legacyPrefabs, bottlePrefab);
        AddPrefabIfAssigned(legacyPrefabs, wrapperPrefab);
        AddPrefabIfAssigned(legacyPrefabs, wormPrefab);

        if (legacyPrefabs.Count == 0)
        {
            Debug.LogError($"Assign at least {totalCount} toy sprites to the WasteSpawner.");
            return spawnedWaste;
        }

        for (int i = 0; i < totalCount; i++)
        {
            GameObject prefab = legacyPrefabs[Random.Range(0, legacyPrefabs.Count)];
            spawnedWaste[i] = SpawnLegacyToy(prefab, spawnArea, placements, reservedRect, hasReservedArea);
        }

        return spawnedWaste;
    }

    private void Shuffle(List<Sprite> sprites)
    {
        for (int i = sprites.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Sprite sprite = sprites[i];
            sprites[i] = sprites[swapIndex];
            sprites[swapIndex] = sprite;
        }
    }

    private void AddPrefabIfAssigned(List<GameObject> prefabs, GameObject prefab)
    {
        if (prefab != null)
            prefabs.Add(prefab);
    }

    private WasteObject SpawnToy(
        Sprite sprite,
        Transform spawnArea,
        List<SpawnPlacement> placements,
        Rect reservedRect,
        bool hasReservedArea)
    {
        Vector2 itemSize = Vector2.one * 120f;
        if (!TryGetScatteredSpawnPosition(
            spawnArea,
            itemSize,
            placements,
            reservedRect,
            hasReservedArea,
            out Vector2 spawnPosition))
        {
            return null;
        }

        GameObject spawnedItem = new GameObject(
            sprite.name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        WasteObject wasteObject = spawnedItem.AddComponent<WasteObject>();
        RectTransform toyRect = spawnedItem.GetComponent<RectTransform>();
        toyRect.SetParent(spawnArea, false);
        toyRect.sizeDelta = itemSize;
        toyRect.localPosition = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        Image image = spawnedItem.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;
        placements.Add(new SpawnPlacement { position = spawnPosition, size = itemSize });
        return wasteObject;
    }

    private WasteObject SpawnLegacyToy(
        GameObject prefab,
        Transform spawnArea,
        List<SpawnPlacement> placements,
        Rect reservedRect,
        bool hasReservedArea)
    {
        RectTransform prefabRect = prefab.GetComponent<RectTransform>();
        Vector2 itemSize = prefabRect != null
            ? Vector2.Scale(prefabRect.rect.size, Abs(prefabRect.localScale))
            : Vector2.Scale(Vector2.one * 64f, Abs(prefab.transform.localScale));
        itemSize *= 1.2f;
        if (!TryGetScatteredSpawnPosition(
            spawnArea,
            itemSize,
            placements,
            reservedRect,
            hasReservedArea,
            out Vector2 spawnPosition))
        {
            return null;
        }

        GameObject spawnedItem = Instantiate(prefab, spawnArea);
        if (spawnedItem.transform is RectTransform spawnedRect)
            spawnedRect.sizeDelta *= 1.2f;
        else
            spawnedItem.transform.localScale *= 1.2f;
        spawnedItem.transform.localPosition = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        placements.Add(new SpawnPlacement { position = spawnPosition, size = itemSize });
        WasteObject wasteObject = spawnedItem.GetComponent<WasteObject>();

        if (wasteObject == null)
            Debug.LogError($"Spawned prefab {prefab.name} does not have a WasteObject component!");

        return wasteObject;
    }

    private bool TryGetScatteredSpawnPosition(
        Transform spawnArea,
        Vector2 itemSize,
        List<SpawnPlacement> placements,
        Rect reservedRect,
        bool hasReservedArea,
        out Vector2 spawnPosition)
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

            if (HasEnoughSpace(candidate, itemSize, placements, reservedRect, hasReservedArea))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        Vector2 bestPosition = default;
        float bestClearance = float.NegativeInfinity;
        bool foundPosition = false;
        for (int attempt = 0; attempt < FallbackPositionAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));
            if (OverlapsReservedArea(candidate, itemSize, reservedRect, hasReservedArea))
                continue;

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
                foundPosition = true;
            }
        }

        spawnPosition = bestPosition;
        if (!foundPosition)
            Debug.LogError("Could not find a toy spawn position outside the toy box.");

        return foundPosition;
    }

    private bool HasEnoughSpace(
        Vector2 candidate,
        Vector2 itemSize,
        List<SpawnPlacement> placements,
        Rect reservedRect,
        bool hasReservedArea)
    {
        if (OverlapsReservedArea(candidate, itemSize, reservedRect, hasReservedArea))
            return false;

        foreach (SpawnPlacement placement in placements)
        {
            if (GetPlacementClearance(candidate, itemSize, placement) < 0f)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetReservedAreaRect(
        Transform spawnArea,
        Transform reservedArea,
        out Rect reservedRect)
    {
        RectTransform reservedRectTransform = reservedArea as RectTransform;
        if (reservedRectTransform == null)
        {
            reservedRect = default;
            return false;
        }

        Vector3[] corners = new Vector3[4];
        reservedRectTransform.GetWorldCorners(corners);
        Vector2 firstCorner = spawnArea.InverseTransformPoint(corners[0]);
        float minX = firstCorner.x;
        float maxX = firstCorner.x;
        float minY = firstCorner.y;
        float maxY = firstCorner.y;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 corner = spawnArea.InverseTransformPoint(corners[i]);
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minY = Mathf.Min(minY, corner.y);
            maxY = Mathf.Max(maxY, corner.y);
        }

        reservedRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private bool OverlapsReservedArea(
        Vector2 position,
        Vector2 size,
        Rect reservedRect,
        bool hasReservedArea)
    {
        if (!hasReservedArea)
            return false;

        Rect itemRect = Rect.MinMaxRect(
            position.x - size.x / 2f,
            position.y - size.y / 2f,
            position.x + size.x / 2f,
            position.y + size.y / 2f);
        return itemRect.Overlaps(reservedRect);
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
