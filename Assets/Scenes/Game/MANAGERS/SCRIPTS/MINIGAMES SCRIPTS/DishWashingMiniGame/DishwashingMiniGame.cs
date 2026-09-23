using TMPro;
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

[SerializeField] private TMP_Text timerText;
   [SerializeField] private TMP_Text scoreText;
   [SerializeField] private TMP_Text comboText;
   [SerializeField] private float comboWindow = 2.5f;

   private float currentTimer;
   private bool timerRunning;
   private int currentScore;
   private int currentCombo;
   private float comboTimer;
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

private void Start()
{
    tutorialManager = FindFirstObjectByType<TutorialManager>();
    dayManager = DayManager.Instance != null
        ? DayManager.Instance
        : FindFirstObjectByType<DayManager>();
}

public void StartGame(Chore chore)
{
    if (chore == null)
        return;

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

        if(comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;

            if(comboTimer <= 0f)
            {
                comboTimer = 0f;
                currentCombo = 0;
                UpdateScoreUI();
            }
        }

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

        AwardComboScore(30, "Leftover cleared!");

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

    AwardComboScore(45, "Nice scrub!");

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
    AwardComboScore(35, "Rinse bonus!");

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
            AwardComboScore(60, "Rack master!");

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
    if(timerText != null)
    {
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

    UpdateScoreUI();
}

private void UpdateScoreUI()
{
    if(scoreText != null)
    {
        scoreText.text = "Score: " + currentScore;
    }

    if(comboText != null)
    {
        if(currentCombo > 1)
        {
            comboText.text = "Combo x" + currentCombo;
        }
        else
        {
            comboText.text = "Clean streak";
        }
    }
}

private void AwardComboScore(int baseScore, string message)
{
    if(!timerRunning)
        return;

    if(comboTimer <= 0f)
    {
        currentCombo = 1;
    }
    else
    {
        currentCombo++;
    }

    comboTimer = comboWindow;

    float multiplier = 1f + (currentCombo - 1) * 0.35f;
    int awardedScore = Mathf.RoundToInt(baseScore * multiplier);

    currentScore += awardedScore;
    currentTimer = Mathf.Min(dishwashingTime, currentTimer + 0.2f + currentCombo * 0.12f);

    if(!string.IsNullOrEmpty(message))
    {
        Debug.Log(message + " | Combo x" + currentCombo + " | +" + awardedScore + " score");
    }

    UpdateScoreUI();
}

private void ResetMiniGameState()
{
    leftoversRemaining = 0;

    platesRemaining = 0;
    platesScrubbed = 0;
    platesRinsed = 0;

    platesToRinse = 0;
    platesToDry = 0;
    currentScore = 0;
    currentCombo = 0;
    comboTimer = 0f;


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
    UpdateScoreUI();


    Debug.Log(
        "Dishwashing MiniGame fully reset."
    );
}


private void ApplyDayDifficulty()
{
    DayManager resolvedDayManager = dayManager != null
        ? dayManager
        : (DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>());

    if (resolvedDayManager != null)
        dayManager = resolvedDayManager;

    int day = resolvedDayManager != null
        ? resolvedDayManager.CurrentDay
        : 1;

    day = Mathf.Max(1, day);


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
            currentPlateAmount = Mathf.Clamp(2 + day - 1, 2, 6);
            currentLeftoverAmount = Mathf.Clamp(1 + Mathf.CeilToInt((day - 1) * 0.5f), 1, 4);
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