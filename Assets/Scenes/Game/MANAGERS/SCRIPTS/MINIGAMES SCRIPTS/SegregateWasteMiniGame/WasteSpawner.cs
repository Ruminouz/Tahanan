using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [SerializeField] private GameObject leafPrefab;
    [SerializeField] private GameObject bottlePrefab;
    [SerializeField] private GameObject wrapperPrefab;
    [SerializeField] private GameObject wormPrefab;

    public WasteObject[] SpawnWaste(int totalCount, Transform spawnArea, int currentDay)
    {
        WasteObject[] spawnedWaste = new WasteObject[totalCount];
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

        SpawnItems(spawnedWaste, ref spawnIndex, leafCount, leafPrefab, WasteType.Leaves, spawnArea);
        SpawnItems(spawnedWaste, ref spawnIndex, bottleCount, bottlePrefab, WasteType.Bottle, spawnArea);
        SpawnItems(spawnedWaste, ref spawnIndex, wrapperCount, wrapperPrefab, WasteType.Wrapper, spawnArea);
        SpawnItems(spawnedWaste, ref spawnIndex, wormCount, wormPrefab, WasteType.Worm, spawnArea);

        return spawnedWaste;
    }

    private void SpawnItems(
        WasteObject[] spawnedWaste,
        ref int spawnIndex,
        int itemCount,
        GameObject prefab,
        WasteType wasteType,
        Transform spawnArea)
    {
        for (int i = 0; i < itemCount && spawnIndex < spawnedWaste.Length; i++)
        {
            spawnedWaste[spawnIndex] = SpawnWasteItem(prefab, spawnArea, wasteType);
            spawnIndex++;
        }
    }

    private WasteObject SpawnWasteItem(GameObject prefab, Transform spawnArea, WasteType wasteType)
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

        Vector3 spawnPosition = GetRandomSpawnPosition(spawnArea);
        GameObject spawnedItem = Instantiate(prefab, spawnPosition, Quaternion.identity, spawnArea);
        WasteObject wasteObject = spawnedItem.GetComponent<WasteObject>();

        if (wasteObject == null)
        {
            Debug.LogError($"Spawned prefab for {wasteType} does not have a WasteObject component!");
        }

        return wasteObject;
    }

    private Vector3 GetRandomSpawnPosition(Transform spawnArea)
    {
        RectTransform rectTransform = spawnArea as RectTransform;

        if (rectTransform == null)
        {
            return spawnArea.position + new Vector3(
                Random.Range(-100f, 100f),
                Random.Range(-100f, 100f),
                0f);
        }

        float x = Random.Range(-rectTransform.rect.width / 2f, rectTransform.rect.width / 2f);
        float y = Random.Range(-rectTransform.rect.height / 2f, rectTransform.rect.height / 2f);

        return rectTransform.TransformPoint(new Vector3(x, y, 0f));
    }
}
