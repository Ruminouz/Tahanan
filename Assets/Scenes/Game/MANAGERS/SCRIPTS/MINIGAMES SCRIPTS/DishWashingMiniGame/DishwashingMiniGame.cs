using UnityEngine;

public class DishwashingMiniGame : MonoBehaviour
{
    public enum WashStage
    {
        RemoveLeftovers,
        AddSoap,
        Scrub,
        Rinse,
        Dry,
        Complete
    }

    [Header("Main Panel")]
    [SerializeField] private GameObject panel;

    [Header("Dish Setup")]
    [SerializeField] private GameObject platePrefab;
    [SerializeField] private Transform washingArea;
    [SerializeField] private int plateCount = 3;

    [Header("Leftovers")]
    [SerializeField] private GameObject leftoverPrefab;
    [SerializeField] private Transform leftoverSpawnArea;
    [SerializeField] private int leftoverCount = 1;

    [Header("Dishwashing Tools")]
    [SerializeField] private GameObject sponge;
    [SerializeField] private GameObject soap;

    [Header("Rinsing")]
    [SerializeField] private Transform rinsingArea;

    [Header("Drying Rack")]
    [SerializeField] private Transform dryingRack;

    [Header("Tutorial")]
    [SerializeField] private ChoreTutorial tutorial;

    private Chore currentChore;
    private TutorialManager tutorialManager;

    private WashStage currentStage;

    private int leftoversRemaining;
    private int platesRemaining;
    private int platesToRinse;
    private int platesToDry;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();

        if (panel != null)
            panel.SetActive(false);
    }

    public void StartGame(Chore chore)
    {
        currentChore = chore;

        if (panel != null)
            panel.SetActive(true);

        ClearOldObjects();

        leftoversRemaining = leftoverCount;
        platesRemaining = plateCount;
        platesToRinse = 0;
        platesToDry = 0;

        SpawnPlates();
        SpawnLeftovers();

        currentStage = WashStage.RemoveLeftovers;

        bool alreadyLearned = false;

        if (tutorialManager != null)
        {
            alreadyLearned =
                tutorialManager.HasLearned(chore.ChoreName);
        }

        if (alreadyLearned)
        {
            StartDishwashing();
        }
        else
        {
            ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        if (tutorial == null)
        {
            StartDishwashing();
            return;
        }

        tutorial.ShowTutorial(
            "DISHWASHING",
            "1. Remove all leftovers and put them in the trash.\n" +
            "2. Add dishwashing liquid to the sponge.\n" +
            "3. Scrub each plate until clean.\n" +
            "4. Rinse each plate under the faucet.\n" +
            "5. Put the clean plates on the drying rack.",
            FinishTutorial
        );
    }

    private void FinishTutorial()
    {
        if (tutorialManager != null && currentChore != null)
        {
            tutorialManager.MarkAsLearned(
                currentChore.ChoreName
            );
        }

        StartDishwashing();
    }

    private void StartDishwashing()
    {
        currentStage = WashStage.RemoveLeftovers;

        Debug.Log("Dishwashing started!");
        Debug.Log("Remove all leftovers first.");
    }

    // =========================================================
    // LEFTOVERS
    // =========================================================

    public void LeftoverRemoved()
{
    if (currentStage != WashStage.RemoveLeftovers)
        return;

    leftoversRemaining--;

    if (leftoversRemaining < 0)
        leftoversRemaining = 0;

    Debug.Log("Leftover removed!");
    Debug.Log("Leftovers remaining: " + leftoversRemaining);

    if (leftoversRemaining <= 0)
    {
        StartAddSoap();
    }
}

    private void StartAddSoap()
    {
        currentStage = WashStage.AddSoap;

        Debug.Log("ALL LEFTOVERS REMOVED!");
        Debug.Log("CURRENT STAGE: " + currentStage);
        Debug.Log("Add dishwashing liquid to the sponge.");
    }

    // =========================================================
    // SOAP
    // =========================================================

    public void SoapAdded()
    {
        Debug.Log("SoapAdded() received.");
        Debug.Log("Current stage: " + currentStage);

        if (currentStage != WashStage.AddSoap)
        {
            Debug.LogWarning(
                "Cannot add soap. Current stage is: " +
                currentStage
            );

            return;
        }

        currentStage = WashStage.Scrub;

        Debug.Log("SOAP ADDED!");
        Debug.Log("Start scrubbing the plates.");

        EnablePlateScrubbing();
    }

    private void EnablePlateScrubbing()
    {
        WashableDish[] dishes =
            FindObjectsByType<WashableDish>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (WashableDish dish in dishes)
        {
            dish.EnableScrubbing();
        }

        Debug.Log(
            "Scrubbing enabled for " +
            dishes.Length +
            " plates."
        );
    }

    // =========================================================
    // SCRUB
    // =========================================================

   public void PlateScrubbed(WashableDish dish)
{
    if (currentStage != WashStage.Scrub)
        return;

    platesRemaining--;

    if (platesRemaining < 0)
        platesRemaining = 0;

    Debug.Log(
        "Plate scrubbed! Plates still to scrub: " +
        platesRemaining
    );

    // This particular plate is now allowed to be dragged
    // into the rinsing area.
    if (dish != null)
    {
        dish.EnableRinsing();

        Debug.Log(
            "Scrubbed plate is now READY TO DRAG into rinsing area."
        );
    }

    // DO NOT switch to Dry here.
    // The player still needs to rinse this plate.
}
public void PlateMovedToRinsing(DishRinsePlate plate)
{
    if (currentStage != WashStage.Scrub &&
        currentStage != WashStage.Rinse)
        return;

    // Count this plate as part of the rinse queue.
    platesToRinse++;

    Debug.Log(
        "Plate moved to rinsing area! " +
        "Plates waiting to rinse: " +
        platesToRinse
    );

    // Once the first scrubbed plate enters the rinse area,
    // the player can start rinsing immediately.
    currentStage = WashStage.Rinse;

    Debug.Log("RINSE STAGE ACTIVE.");
}


    private void StartRinsing()
    {
        currentStage = WashStage.Rinse;

        Debug.Log("ALL PLATES SCRUBBED!");
        Debug.Log("Move plates to the rinsing area.");
    }

    // =========================================================
    // RINSE
    // =========================================================

 public void PlateRinsed(DishRinsePlate plate)
{
    if (currentStage != WashStage.Rinse)
        return;

    platesToRinse--;

    if (platesToRinse < 0)
        platesToRinse = 0;

    Debug.Log(
        "Plate rinsed! Plates still waiting to rinse: " +
        platesToRinse
    );

    // =====================================================
    // NOT ALL PLATES HAVE BEEN SCRUBBED YET
    // =====================================================

    if (platesRemaining > 0)
    {
        currentStage = WashStage.Scrub;

        Debug.Log(
            "More plates still need to be scrubbed."
        );

        return;
    }

    // =====================================================
    // ALL PLATES HAVE BEEN SCRUBBED
    // =====================================================

    if (platesToRinse > 0)
    {
        currentStage = WashStage.Rinse;

        Debug.Log(
            "All plates scrubbed, but some still need rinsing."
        );

        return;
    }

    // =====================================================
    // ALL PLATES SCRUBBED + ALL PLATES RINSED
    // =====================================================

    StartDrying();
}
private void StartDrying()
{
    currentStage = WashStage.Dry;

    platesToDry = plateCount;

    Debug.Log("==============================");
    Debug.Log("ALL PLATES SCRUBBED!");
    Debug.Log("ALL PLATES RINSED!");
    Debug.Log("DRYING STAGE ENABLED!");
    Debug.Log("Plates to dry: " + platesToDry);
    Debug.Log("Put plates on the drying rack.");
    Debug.Log("==============================");

    DishRinsePlate[] plates =
        FindObjectsByType<DishRinsePlate>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

    foreach (DishRinsePlate plate in plates)
    {
        plate.EnableDrying();
    }

    Debug.Log(
        "Drying enabled for " +
        plates.Length +
        " plates."
    );
}
    // =========================================================
    // DRY
    // =========================================================

    public void PlateDried()
{
    if (currentStage != WashStage.Dry)
        return;

    platesToDry--;

    if (platesToDry < 0)
        platesToDry = 0;

    Debug.Log(
        "Plate placed on drying rack! Remaining: " +
        platesToDry
    );

    if (platesToDry == 0)
    {
        Debug.Log("ALL PLATES ARE ON DRYING RACK!");
        CompleteGame();
    }
}

    // =========================================================
    // GETTERS
    // =========================================================

    public int GetPlateCount()
    {
        return plateCount;
    }

    public WashStage GetCurrentStage()
    {
        return currentStage;
    }

    // =========================================================
    // SPAWN
    // =========================================================

private void SpawnPlates()
{
    if (platePrefab == null || washingArea == null)
        return;

    for (int i = 0; i < plateCount; i++)
    {
        GameObject plate = Instantiate(
            platePrefab,
            washingArea
        );

        // Stack the plates in one pile.
        // Each plate is slightly offset so the pile is visible.
        float verticalOffset = i * 12f;
        float horizontalOffset = i * 4f;

        plate.transform.localPosition = new Vector3(
            horizontalOffset,
            verticalOffset,
            0f
        );

        // Slight rotation makes the pile feel more natural.
        float rotation = (i % 2 == 0) ? -2f : 2f;

        plate.transform.localRotation =
            Quaternion.Euler(0f, 0f, rotation);
    }
}



    private void SpawnLeftovers()
    {
        if (leftoverPrefab == null ||
            leftoverSpawnArea == null)
            return;

        for (int i = 0; i < leftoverCount; i++)
        {
            GameObject leftover = Instantiate(
                leftoverPrefab,
                leftoverSpawnArea
            );

            float spacing = 50f;

            leftover.transform.localPosition =
                new Vector3(
                    i * spacing,
                    0f,
                    0f
                );

            leftover.transform.localRotation =
                Quaternion.identity;

            DishLeftOver script =
                leftover.GetComponent<DishLeftOver>();

            if (script != null)
            {
                script.SetManager(this);
            }
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void ClearOldObjects()
    {
        if (washingArea != null)
        {
            foreach (Transform child in washingArea)
            {
                Destroy(child.gameObject);
            }
        }

        if (leftoverSpawnArea != null)
        {
            foreach (Transform child in leftoverSpawnArea)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // =========================================================
    // COMPLETE
    // =========================================================

 
public void CompleteGame()
{
    currentStage = WashStage.Complete;

    Debug.Log("DISHWASHING COMPLETE!");

    // Disable sponge after the mini-game finishes.
    if (sponge != null)
    {
        sponge.SetActive(false);
        Debug.Log("Sponge disabled.");
    }

    if (currentChore != null)
    {
        currentChore.Complete();
    }

    if (panel != null)
    {
        panel.SetActive(false);
    }

    currentChore = null;
}


}