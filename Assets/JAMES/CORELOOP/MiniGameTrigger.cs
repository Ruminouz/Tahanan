using UnityEngine;
using HouseChoresGame;

public class MiniGameTrigger : MonoBehaviour
{
    [Header("Chore Reference")]
    public ChoreData choreData;

    [Header("Canvas Reference")]
    public Canvas miniGameCanvas; // assign your MiniGameCanvas in Inspector

    [Header("UI Panel Prefab")]
    public GameObject choreUIPanelPrefab; // drag prefab here

    private GameObject spawnedPanel;
    private bool playerInside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Trigger Enter] Collider: {other.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            playerInside = true;
            Debug.Log($"✅ Player entered {choreData.choreName} zone. Press E to start.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[Trigger Exit] Collider: {other.name}, Tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            playerInside = false;
            Debug.Log($"⛔ Player left {choreData.choreName} zone.");
        }
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"🟢 E pressed inside {choreData.choreName} zone.");

            if (spawnedPanel == null)
            {
                // Instantiate panel under Canvas
                spawnedPanel = Instantiate(choreUIPanelPrefab, miniGameCanvas.transform, false);
                MiniGameOverlayManager.Instance.RegisterMiniGame(choreData, spawnedPanel);
                Debug.Log($"📋 Spawned {choreData.choreName} panel under {miniGameCanvas.name}");
            }
            else if (!spawnedPanel.activeSelf)
            {
                MiniGameOverlayManager.Instance.RestoreMiniGame(choreData);
                Debug.Log($"🔄 Restored {choreData.choreName} panel.");
            }
            else
            {
                Debug.Log($"Panel already active for {choreData.choreName}.");
            }
        }
    }
}
