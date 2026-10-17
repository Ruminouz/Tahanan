using UnityEngine;

public class BroomChore : Interactable
{
    [SerializeField] private GameObject broomVisual;
    [SerializeField] private Sprite vacuumSprite;
    [SerializeField] private ChoreTutorial tutorial;

    private bool hasBeenPickedUp = false;
    private TutorialManager tutorialManager;
    private SpriteRenderer broomSpriteRenderer;
    private Sprite originalBroomSprite;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
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

        ShowFirstPickupTutorial();
        Debug.Log("BROOM PICKED UP!");
    }

    private void ShowFirstPickupTutorial()
    {
        const string tutorialKey = "Broom";
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
            "SWEEPING",
            "1. Equip the broom.\n" +
            "2. Hold Left Mouse Button.\n" +
            "3. Sweep quickly back and forth over the dust.",
            () => tutorialManager?.MarkAsLearned(tutorialKey));
    }
}