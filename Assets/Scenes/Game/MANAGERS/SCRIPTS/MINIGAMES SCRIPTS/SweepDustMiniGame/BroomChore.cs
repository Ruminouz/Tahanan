using UnityEngine;

public class BroomChore : Interactable
{
    [SerializeField] private GameObject broomVisual;
    [SerializeField] private Sprite vacuumSprite;

    private bool hasBeenPickedUp = false;
    private SpriteRenderer broomSpriteRenderer;
    private Sprite originalBroomSprite;

    private void Start()
    {
        CacheBroomSprite();
        SubscribeToBroomUpgrade();
        UpdateBroomVisual();
    }

    private void OnEnable()
    {
        SubscribeToBroomUpgrade();
    }

    private void OnDisable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.BroomUpgradeChanged -= OnBroomUpgradeChanged;
    }

    private void CacheBroomSprite()
    {
        if (broomVisual == null)
            return;

        broomSpriteRenderer = broomVisual.GetComponent<SpriteRenderer>();
        if (broomSpriteRenderer == null)
            broomSpriteRenderer = broomVisual.GetComponentInChildren<SpriteRenderer>(true);

        if (broomSpriteRenderer != null)
            originalBroomSprite = broomSpriteRenderer.sprite;
    }

    private void SubscribeToBroomUpgrade()
    {
        if (EconomyManager.Instance == null)
            return;

        EconomyManager.Instance.BroomUpgradeChanged -= OnBroomUpgradeChanged;
        EconomyManager.Instance.BroomUpgradeChanged += OnBroomUpgradeChanged;
    }

    private void OnBroomUpgradeChanged(int level)
    {
        UpdateBroomVisual();
    }

    private void UpdateBroomVisual()
    {
        if (broomSpriteRenderer == null)
            CacheBroomSprite();

        if (broomSpriteRenderer == null || originalBroomSprite == null)
            return;

        bool hasVacuumUpgrade = EconomyManager.Instance != null
            && EconomyManager.Instance.HasUpgradedBroom;
        broomSpriteRenderer.sprite = hasVacuumUpgrade && vacuumSprite != null
            ? vacuumSprite
            : originalBroomSprite;
    }

    public void ResetForDay()
    {
        hasBeenPickedUp = false;

        if (broomVisual != null)
            broomVisual.SetActive(true);

        UpdateBroomVisual();
    }

    public override void Interact()
    {
        if (hasBeenPickedUp)
        {
            Debug.Log("Broom already picked up.");
            return;
        }


        GameObject player = GameObject.FindGameObjectWithTag("Player");


        if (player == null)
        {
            Debug.LogWarning("Player not found.");
            return;
        }


        SweepingPlayerState playerState =
            player.GetComponent<SweepingPlayerState>();


        if (playerState == null)
        {
            Debug.LogWarning("SweepingPlayerState missing.");
            return;
        }


        playerState.PickUpBroom();

        hasBeenPickedUp = true;

        PlayerEquipmentInventory inventory = player.GetComponent<PlayerEquipmentInventory>();
        if (inventory == null)
            inventory = player.AddComponent<PlayerEquipmentInventory>();

        inventory.Add(EquipmentType.Broom, broomVisual != null ? broomVisual : gameObject);

        Debug.Log("BROOM PICKED UP!");
    }
}