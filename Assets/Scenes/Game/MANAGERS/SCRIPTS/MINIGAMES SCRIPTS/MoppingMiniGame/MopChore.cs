using UnityEngine;

public class MopChore : Interactable
{
    [SerializeField] private GameObject mopVisual;
    [SerializeField] private Sprite classicMopSprite;
    [SerializeField] private Sprite upgradedMopSprite;

    private bool hasBeenPickedUp = false;
    private SpriteRenderer mopSpriteRenderer;
    private Sprite originalMopSprite;

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
            EconomyManager.Instance.MopUpgradeChanged -= OnMopUpgradeChanged;
    }

    private void SubscribeToEconomy()
    {
        if (EconomyManager.Instance == null)
            return;

        EconomyManager.Instance.MopUpgradeChanged -= OnMopUpgradeChanged;
        EconomyManager.Instance.MopUpgradeChanged += OnMopUpgradeChanged;
    }

    private void OnMopUpgradeChanged(int level)
    {
        ApplyMopVisual(level);
    }

    private void CacheMopSprite()
    {
        if (mopVisual == null)
            return;

        mopSpriteRenderer = mopVisual.GetComponent<SpriteRenderer>();
        if (mopSpriteRenderer == null)
            mopSpriteRenderer = mopVisual.GetComponentInChildren<SpriteRenderer>(true);

        if (mopSpriteRenderer != null)
            originalMopSprite = mopSpriteRenderer.sprite;
    }

    public void ResetForDay()
    {
        hasBeenPickedUp = false;

        // The default mop visual can be this same GameObject. Inventory reset
        // disables it, so reactivate the pickup object before applying visuals.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (mopVisual != null && mopVisual != gameObject)
            mopVisual.SetActive(true);

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

        if (mopVisual != null && mopVisual != gameObject)
            mopVisual.SetActive(false);
        PlayerEquipmentInventory inventory = player.GetComponent<PlayerEquipmentInventory>();
        if (inventory == null)
            inventory = player.AddComponent<PlayerEquipmentInventory>();

        inventory.Add(EquipmentType.Mop, mopVisual != null ? mopVisual : gameObject);

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

        if (mopSpriteRenderer == null)
            CacheMopSprite();

        if (mopSpriteRenderer == null)
            return;

        Sprite selectedSprite = level == 1 ? upgradedMopSprite : classicMopSprite;
        mopSpriteRenderer.sprite = selectedSprite != null ? selectedSprite : originalMopSprite;
    }
}