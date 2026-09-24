using UnityEngine;

public class MopChore : Interactable
{
    [SerializeField] private GameObject mopVisual;
    [SerializeField] private GameObject upgradedMopVisual;
    [SerializeField] private ChoreTutorial tutorial;

    private bool hasBeenPickedUp = false;
    private TutorialManager tutorialManager;

    private void OnEnable()
    {
        SubscribeToEconomy();
        ApplyMopVisual(GetCurrentMopLevel());
    }

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        SubscribeToEconomy();
        ApplyMopVisual(GetCurrentMopLevel());
    }

    private void OnDisable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.MopUpgradeChanged -= ApplyMopVisual;
    }

    private void SubscribeToEconomy()
    {
        if (EconomyManager.Instance == null)
            return;

        EconomyManager.Instance.MopUpgradeChanged -= ApplyMopVisual;
        EconomyManager.Instance.MopUpgradeChanged += ApplyMopVisual;
    }

    public void ResetForDay()
    {
        hasBeenPickedUp = false;

        // The default mop visual can be this same GameObject. Inventory reset
        // disables it, so reactivate the pickup object before applying visuals.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        ApplyMopVisual(GetCurrentMopLevel());
    }

    public override void Interact()
    {
        if (hasBeenPickedUp)
        {
            Debug.Log("Mop has already been picked up.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("Player object with 'Player' tag was not found.");
            return;
        }

        MoppingPlayerState playerState = player.GetComponent<MoppingPlayerState>();

        if (playerState == null)
        {
            Debug.LogWarning("MoppingPlayerState is missing from the Player.");
            return;
        }

        playerState.PickUpMop();

        hasBeenPickedUp = true;

        SetAllMopVisualsActive(false);
        PlayerEquipmentInventory inventory = player.GetComponent<PlayerEquipmentInventory>();
        if (inventory == null)
            inventory = player.AddComponent<PlayerEquipmentInventory>();

        inventory.Add(EquipmentType.Mop, mopVisual != null ? mopVisual : gameObject);

        ShowFirstPickupTutorial();
        Debug.Log("MOP PICKED UP!");
    }

    private void ShowFirstPickupTutorial()
    {
        const string tutorialKey = "Mop";
        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<TutorialManager>();

        if (tutorialManager != null && tutorialManager.HasLearned(tutorialKey))
            return;

        if (tutorial == null)
            tutorial = FindFirstObjectByType<ChoreTutorial>();

        if (tutorial == null)
        {
            tutorialManager?.MarkAsLearned(tutorialKey);
            return;
        }

        tutorial.ShowTutorial(
            "MOPPING",
            "1. Equip the mop.\n" +
            "2. Hold Left Mouse Button.\n" +
            "3. Move the mop over the wet area.\n" +
            "4. Scrub quickly until the water fades.",
            () => tutorialManager?.MarkAsLearned(tutorialKey));
    }

    private int GetCurrentMopLevel()
    {
        return EconomyManager.Instance != null && EconomyManager.Instance.HasUpgradedMop
            ? 1
            : 0;
    }

    private void ApplyMopVisual(int level)
    {
        if (hasBeenPickedUp)
            return;

        SetAllMopVisualsActive(false);
        GameObject selectedVisual = level == 1 ? upgradedMopVisual : mopVisual;
        if (selectedVisual != null && selectedVisual != gameObject)
            selectedVisual.SetActive(true);
    }

    private void SetAllMopVisualsActive(bool isActive)
    {
        // The legacy prefab can reference this trigger itself as mopVisual.
        // Toggling it here would invoke OnEnable again indefinitely.
        if (mopVisual != null && mopVisual != gameObject)
            mopVisual.SetActive(isActive);

        if (upgradedMopVisual != null && upgradedMopVisual != gameObject)
            upgradedMopVisual.SetActive(isActive);
    }
}