using System.Collections.Generic;
using UnityEngine;

public class WaterSpawner : MonoBehaviour
{
    [Header("Water Setup")]
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private Transform[] waterSpawnPoints;

    [Header("Mopping Minigame")]
    [SerializeField] private MoppingMinigame moppingMinigame;

    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnDelay = 20f;
    [SerializeField] private float maxSpawnDelay = 60f;

    private List<Transform> availableSpawnPoints = new List<Transform>();

    private bool spawning = true;
    private float spawnTimer;

    private void Start()
    {
        ResetSpawnPoints();
        SetNextSpawnTime();
    }

    private void Update()
    {
        if (!spawning)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnWater();
            SetNextSpawnTime();
        }
    }

    private void ResetSpawnPoints()
    {
        availableSpawnPoints.Clear();

        foreach (Transform spawnPoint in waterSpawnPoints)
        {
            if (spawnPoint != null)
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }
    }

    private void SetNextSpawnTime()
    {
        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void SpawnWater()
    {
        if (waterPrefab == null)
        {
            Debug.LogWarning("Water Prefab is not assigned.");
            return;
        }

        if (moppingMinigame == null)
        {
            Debug.LogWarning("Mopping Minigame is not assigned to WaterSpawner.");
            return;
        }

        if (availableSpawnPoints.Count == 0)
        {
            Debug.Log("No available water spots.");
            return;
        }

        int randomIndex = Random.Range(0, availableSpawnPoints.Count);

        Transform selectedSpot = availableSpawnPoints[randomIndex];

        GameObject newWater = Instantiate(
            waterPrefab,
            selectedSpot.position,
            selectedSpot.rotation
        );

        WetArea wetArea = newWater.GetComponent<WetArea>();

        if (wetArea != null)
        {
            wetArea.SetSpawner(this, selectedSpot);
            wetArea.SetMoppingMinigame(moppingMinigame);
        }
        else
        {
            Debug.LogWarning("Water prefab does not have a WetArea component.");
        }

        availableSpawnPoints.RemoveAt(randomIndex);

        Debug.Log("Water spawned at: " + selectedSpot.name);
    }

    public void FreeSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint == null)
            return;

        if (!availableSpawnPoints.Contains(spawnPoint))
        {
            availableSpawnPoints.Add(spawnPoint);
        }

        Debug.Log("Water spot is available again: " + spawnPoint.name);
    }

    public void StopSpawning()
    {
        spawning = false;
    }

    public void StartSpawning()
    {
        spawning = true;
        SetNextSpawnTime();
    }
}