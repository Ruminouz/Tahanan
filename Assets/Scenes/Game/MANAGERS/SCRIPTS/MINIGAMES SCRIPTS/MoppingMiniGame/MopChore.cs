using UnityEngine;

public class MopChore : Interactable
{
    [SerializeField] private GameObject mopVisual;
    [SerializeField] private GameObject upgradedMopVisual;

    private bool hasBeenPickedUp = false;

    private void OnEnable()
    {
        SubscribeToEconomy();
        ApplyMopVisual(GetCurrentMopLevel());
    }

    private void Start()
    {
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

        Debug.Log("MOP PICKED UP!");
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
        if (selectedVisual != null)
            selectedVisual.SetActive(true);
    }

    private void SetAllMopVisualsActive(bool isActive)
    {
        if (mopVisual != null)
            mopVisual.SetActive(isActive);

        if (upgradedMopVisual != null)
            upgradedMopVisual.SetActive(isActive);
    }
}