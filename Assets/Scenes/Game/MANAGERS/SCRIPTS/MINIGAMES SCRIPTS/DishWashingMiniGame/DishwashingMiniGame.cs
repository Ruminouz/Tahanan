using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
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
   [Header("Mini Game Timer")]
[SerializeField] private float dishwashingTime = 90f;

[SerializeField] private Text timerText;

private float currentTimer;
private bool timerRunning;
    [Header("Main Panel")]
    [SerializeField] private GameObject panel;
    private DayManager dayManager;
[SerializeField] private GameObject garbageBag;
[SerializeField] private Transform garbageSpawnPoint;
    [Header("Dish Setup")]
    [SerializeField] private GameObject platePrefab;
    [SerializeField] private Transform washingArea;
    [SerializeField] private int plateCount = 3;
    private WashableDish currentScrubbingPlate;
    private List<WashableDish> spawnedPlates =
    new List<WashableDish>();
    [Header("Leftovers")]
    [SerializeField] private GameObject leftoverPrefab;
    [SerializeField] private Transform leftoverSpawnArea;
    [SerializeField] private int leftoverCount = 1;

   [Header("Dishwashing Tools")]
[SerializeField] private GameObject sponge;
[SerializeField] private DishSponge dishSponge;
[SerializeField] private GameObject soap;


[Header("Day Scaling")]


private int currentPlateAmount;
private int currentLeftoverAmount;

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
private int platesScrubbed;
private int platesRinsed;

private int platesToRinse;
private int platesToDry;

public void StartGame(Chore chore)
{
    currentChore = chore;


    // RESET EVERYTHING FIRST
    ResetMiniGameState();



    // OPEN PANEL
    if(panel != null)
    {
        panel.SetActive(true);
    }



    // ENABLE SPONGE AGAIN
    if(sponge != null)
    {
        sponge.SetActive(true);

        Debug.Log("Sponge Enabled");
    }
    else
    {
        Debug.LogWarning("Sponge reference missing!");
    }




    // RESET SPONGE POSITION + STATE
    if(dishSponge != null)
    {
        dishSponge.ResetSponge();
    }
    else
    {
        Debug.LogWarning("DishSponge reference missing!");
    }





    // RESET GARBAGE BAG STATE
    if(garbageBag != null)
    {
        garbageBag.SetActive(false);

        Debug.Log(
            "Garbage bag hidden during dishwashing"
        );
    }






    StartMiniGameTimer();



    ApplyDayDifficulty();



    SpawnPlates();

    SpawnLeftovers();



    currentStage = WashStage.RemoveLeftovers;





    bool alreadyLearned = false;



    if(tutorialManager != null)
    {
        alreadyLearned =
            tutorialManager.HasLearned(
                chore.ChoreName
            );
    }





    if(alreadyLearned)
    {
        StartDishwashing();
    }
    else
    {
        ShowTutorial();
    }



    Debug.Log(
        "Dishwashing Started"
    );
}
   private void StartMiniGameTimer()
{
    currentTimer = dishwashingTime;

    timerRunning = true;


    UpdateTimerUI();


    Debug.Log(
        "Dishwashing Timer Started: "
        + currentTimer
        + " seconds"
    );
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
    private void Update()
{
    if(!timerRunning)
        return;


    currentTimer -= Time.deltaTime;


    if(currentTimer < 0)
        currentTimer = 0;


    UpdateTimerUI();


    if(currentTimer <= 0)
    {
        FailDishwashing();
    }
}
private void FailDishwashing()
{
    timerRunning = false;


    Debug.Log(
        "DISHWASHING FAILED - TIME OUT"
    );


    ChoreManager choreManager =
        FindFirstObjectByType<ChoreManager>();


    if(choreManager != null)
    {
       ChoreManager manager =
FindFirstObjectByType<ChoreManager>();


if(manager != null)
{
    manager.MissChore(currentChore);
}
    }


    if(panel != null)
    {
        panel.SetActive(false);
    }
    if(timerText != null)
{
    timerText.text = "";
}


    currentChore = null;
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
    public bool CanScrubThisPlate(WashableDish dish)
{
    return dish == currentScrubbingPlate;
}

   private void EnablePlateScrubbing()
{
    foreach(WashableDish dish in spawnedPlates)
    {
        dish.DisableScrubbing();
    }


    if(spawnedPlates.Count == 0)
    {
        Debug.LogError(
            "No plates found!"
        );

        return;
    }



    // LAST SPAWNED = TOP PLATE
    currentScrubbingPlate =
        spawnedPlates[spawnedPlates.Count - 1];


    currentScrubbingPlate.EnableScrubbing();


    Debug.Log(
        "TOP PLATE ENABLED FOR SCRUBBING"
    );
}
    // =========================================================
    // SCRUB
    // =========================================================
private void EnableNextPlate()
{
    // Disable lahat muna
    foreach(WashableDish dish in spawnedPlates)
    {
        dish.DisableScrubbing();
    }


    // Hanapin next plate sa stack
    for(int i = spawnedPlates.Count - 1; i >= 0; i--)
    {
        WashableDish next =
            spawnedPlates[i];


        if(!next.IsClean)
        {
            currentScrubbingPlate = next;

            next.EnableScrubbing();


            Debug.Log(
                "NEXT PLATE ENABLED FOR SCRUBBING"
            );

            return;
        }
    }


    Debug.Log(
        "NO MORE PLATES TO SCRUB"
    );
}
 public void PlateScrubbed(WashableDish dish)
{
    if(dish != currentScrubbingPlate)
        return;


    Debug.Log(
        "Plate scrubbed."
    );


    DishRinsePlate rinse =
        dish.GetComponent<DishRinsePlate>();


    if(rinse != null)
    {
        rinse.EnableRinsing();
    }


    Debug.Log(
        "Plate ready for rinse."
    );
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
    platesRinsed++;

    Debug.Log(
        "Rinsed: " +
        platesRinsed +
        "/" +
        currentPlateAmount
    );


    if(platesRinsed < currentPlateAmount)
    {
        EnableNextPlate();
        return;
    }


    Debug.Log(
        "ALL PLATES RINSED. START DRYING."
    );


    StartDrying();
}
    private void StartDrying()
{
    currentStage = WashStage.Dry;

    platesToDry = currentPlateAmount;


    DishRinsePlate[] plates =
        FindObjectsByType<DishRinsePlate>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );


    foreach(DishRinsePlate plate in plates)
    {
        plate.EnableDrying();
    }


    Debug.Log(
        "DRYING ENABLED FOR ALL PLATES"
    );
}

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


    spawnedPlates.Clear();


    for (int i = 0; i < currentPlateAmount; i++)
    {
        GameObject plate =
            Instantiate(
                platePrefab,
                washingArea
            );


        float verticalOffset = i * 12f;
        float horizontalOffset = i * 4f;


        plate.transform.localPosition =
            new Vector3(
                horizontalOffset,
                verticalOffset,
                0f
            );


        float rotation =
            (i % 2 == 0) ? -2f : 2f;


        plate.transform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotation
            );


        WashableDish dish =
            plate.GetComponent<WashableDish>();


        if(dish != null)
        {
            spawnedPlates.Add(dish);
        }
    }


    Debug.Log(
        "Spawned plates: " +
        spawnedPlates.Count
    );
}



        private void SpawnLeftovers()
{
    if (leftoverPrefab == null ||
        leftoverSpawnArea == null)
        return;


    leftoversRemaining = currentLeftoverAmount;

            for (int i = 0; i < currentLeftoverAmount; i++)
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
    ClearChildren(washingArea);

    ClearChildren(leftoverSpawnArea);

    ClearChildren(rinsingArea);

    ClearChildren(dryingRack);


    Debug.Log(
        "Dishwashing objects cleared."
    );
}


private void ClearChildren(Transform parent)
{
    if(parent == null)
        return;


    foreach(Transform child in parent)
    {
        Destroy(child.gameObject);
    }
}
private void ResetSponge()
{
    DishSponge dishSponge =
        FindFirstObjectByType<DishSponge>();


    if(dishSponge != null)
    {
        dishSponge.ResetSponge();

        Debug.Log(
            "Sponge reset for new day."
        );
    }
}
        // =========================================================
        // COMPLETE
        // =========================================================

    
 public void CompleteGame()
{

    timerRunning = false;


    currentStage = WashStage.Complete;


    Debug.Log("DISHWASHING COMPLETE!");



    // RESET SPONGE
    ResetSponge();


    if(timerText != null)
    {
        timerText.text = "";
    }


    if(dishSponge != null)
    {
        dishSponge.ResetSponge();
    }


    if(sponge != null)
    {
        sponge.SetActive(false);
    }



    // COMPLETE CHORE
    if(currentChore != null)
    {
        currentChore.Complete();
    }




    // ============================
    // GET DAY FROM DAY MANAGER
    // ============================

    // ============================
// GARBAGE SYSTEM DAY 2-7
// ============================

if(DayManager.Instance != null)
{

    int day = DayManager.Instance.CurrentDay;


    Debug.Log(
        "Checking Garbage Unlock Day: "
        + day
    );


    if(day >= 2)
    {

        GarbageChore garbageChore =
        FindFirstObjectByType<GarbageChore>();


        if(GarbageChore.Instance != null)
{

    GarbageChore.Instance.SpawnGarbageBag();


    Debug.Log(
        "Garbage Bag Spawn Called"
    );

}
else
{

    Debug.LogError(
        "GarbageChore Instance Missing!"
    );

}

    }
    else
    {

        Debug.Log(
            "Garbage Sorting Locked Day "
            + day
        );

    }

}
else
{

    Debug.LogWarning(
        "DayManager Instance Missing!"
    );

}




    // ============================
    // CLOSE PANEL
    // ============================

    if(panel != null)
    {

        panel.SetActive(false);


        Debug.Log(
            "Dishwashing Panel Closed"
        );

    }



    currentChore = null;

}
private void UpdateTimerUI()
{
    if(timerText == null)
        return;


    int minutes =
        Mathf.FloorToInt(currentTimer / 60);


    int seconds =
        Mathf.FloorToInt(currentTimer % 60);


    timerText.text =
        string.Format(
            "{0:00}:{1:00}",
            minutes,
            seconds
        );
}


private void ResetMiniGameState()
{
    leftoversRemaining = 0;

    platesRemaining = 0;
    platesScrubbed = 0;
    platesRinsed = 0;

    platesToRinse = 0;
    platesToDry = 0;


    currentScrubbingPlate = null;


    ClearOldObjects();


    if(dishSponge != null)
    {
        dishSponge.ResetSponge();
    }


    if(sponge != null)
    {
        sponge.SetActive(true);
    }


    currentStage = WashStage.RemoveLeftovers;


    Debug.Log(
        "Dishwashing MiniGame fully reset."
    );
}


private void ApplyDayDifficulty()
{
    int day = 1;

    if(dayManager != null)
    {
        day = dayManager.CurrentDay;
    }


    switch(day)
    {
        case 1:
            currentPlateAmount = 2;
            currentLeftoverAmount = 1;
            break;


        case 2:
            currentPlateAmount = 2;
            currentLeftoverAmount = 2;
            break;


        case 3:
            currentPlateAmount = 3;
            currentLeftoverAmount = 2;
            break;


        case 4:
            currentPlateAmount = 4;
            currentLeftoverAmount = 3;
            break;


        case 5:
            currentPlateAmount = 5;
            currentLeftoverAmount = 3;
            break;


        case 6:
            currentPlateAmount = 5;
            currentLeftoverAmount = 4;
            break;


        case 7:
            currentPlateAmount = 6;
            currentLeftoverAmount = 4;
            break;


        default:
            currentPlateAmount = 2;
            currentLeftoverAmount = 1;
            break;
    }


    Debug.Log(
        "Dishwashing Difficulty Applied. Day: " 
        + day +
        " Plates: " +
        currentPlateAmount
    );
}

}