using UnityEngine;

public class BroomChore : Interactable
{
    [SerializeField] private GameObject broomVisual;
    [SerializeField] private ChoreTutorial tutorial;

    private bool hasBeenPickedUp = false;
    private TutorialManager tutorialManager;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void ResetForDay()
    {
        hasBeenPickedUp = false;

        if (broomVisual != null)
            broomVisual.SetActive(true);
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