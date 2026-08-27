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
        platesToRinse = plateCount;
        platesToDry = plateCount;

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
            "Plate scrubbed! Remaining: " +
            platesRemaining
        );

        if (dish != null)
        {
            dish.EnableRinsing();
        }

        if (platesRemaining == 0)
        {
            StartRinsing();
        }
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
        "Plate rinsed! Remaining: " +
        platesToRinse
    );

    if (platesToRinse == 0)
    {
        StartDrying();
    }
}

    private void StartDrying()
{
    currentStage = WashStage.Dry;

    platesToDry = plateCount;

    Debug.Log("==============================");
    Debug.Log("ALL PLATES RINSED!");
    Debug.Log("CURRENT STAGE: DRY");
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
    Debug.Log("================================");
    Debug.Log("PlateDried() RECEIVED!");
    Debug.Log("Current Stage: " + currentStage);
    Debug.Log("Plates To Dry BEFORE: " + platesToDry);
    Debug.Log("================================");

    if (currentStage != WashStage.Dry)
    {
        Debug.LogWarning(
            "PlateDried() ignored because current stage is: " +
            currentStage
        );

        return;
    }

    platesToDry--;

    if (platesToDry < 0)
        platesToDry = 0;

    Debug.Log(
        "Plate placed on drying rack! Remaining: " +
        platesToDry
    );

    if (platesToDry <= 0)
    {
        Debug.Log("================================");
        Debug.Log("ALL PLATES ARE ON DRYING RACK!");
        Debug.Log("DISHWASHING MINI GAME COMPLETE!");
        Debug.Log("================================");

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

            float spacing = 100f;

            plate.transform.localPosition =
                new Vector3(
                    i * spacing,
                    0f,
                    0f
                );

            plate.transform.localRotation =
                Quaternion.identity;
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

        if (currentChore != null)
        {
            currentChore.Complete();
        }

        if (panel != null)
            panel.SetActive(false);

        currentChore = null;
    }
}