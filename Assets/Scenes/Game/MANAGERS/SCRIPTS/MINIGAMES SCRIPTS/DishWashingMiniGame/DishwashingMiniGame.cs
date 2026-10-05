using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
public class DishwashingMiniGame : MonoBehaviour
{
    private enum DailyChallenge
    {
        RushFinish,
        ComboStreak,
        LeftoverDash
    }

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

[SerializeField] private Component timerText;
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
    [SerializeField] private Sprite[] alternateDishSprites;
    [SerializeField] private Sprite fryingPanSprite;
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

[SerializeField, Min(0)] private int dailyChallengeRewardCoins = 5;
[SerializeField, Min(2)] private int fryingPanStartDay = 2;
[SerializeField, Min(0f)] private float fryingPanChancePerDay = 0.12f;
[SerializeField, Range(0f, 1f)] private float maximumFryingPanChance = 0.4f;
[SerializeField, Range(0f, 1f)] private float lateDayMessPerDay = 0.08f;
[SerializeField, Range(0f, 1f)] private float maximumLateDayMess = 0.5f;
[SerializeField, Range(0.5f, 1f)] private float fryingPanScrubRateMultiplier = 0.72f;
[SerializeField, Min(0f)] private float maximumDishScatter = 18f;

private int currentPlateAmount;
private int currentLeftoverAmount;
private int currentDay = 1;
private DailyChallenge dailyChallenge;
private string dailyChallengeLabel;
private string dailyChallengeResult;
private float elapsedRunTime;
private bool dailyChallengeResolved;
private GameObject dailyChallengeCard;
private TMP_Text dailyChallengeText;
private Coroutine resultCoroutine;
private const float ResultDisplayDuration = 1.5f;

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

    ApplyDayDifficulty();
    EnsureDailyChallengeCard();
    UpdateDailyChallengeCard();
}

public void StartGame(Chore chore)
{
    if (chore == null)
        return;

    if (resultCoroutine != null)
    {
        StopCoroutine(resultCoroutine);
        resultCoroutine = null;
    }

    currentChore = chore;


    // RESET EVERYTHING FIRST
    ResetMiniGameState();



    // OPEN PANEL
    if(panel != null)
    {
        panel.SetActive(true);
    }

    EnsureDailyChallengeCard();



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

        elapsedRunTime += Time.deltaTime;

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
    ResolveDailyChallenge(false);


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
    SetTimerText("");


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
        elapsedRunTime = 0f;
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
            if (dailyChallenge == DailyChallenge.LeftoverDash)
            {
                ResolveDailyChallenge(elapsedRunTime <= 12f);
            }

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

            float messLevel = Mathf.Clamp01((currentDay - 1) * lateDayMessPerDay);
            messLevel = Mathf.Min(messLevel, maximumLateDayMess);
            float scrubRateMultiplier = 1f - messLevel * 0.35f;
            float panChance = currentDay < fryingPanStartDay
                ? 0f
                : Mathf.Min((currentDay - fryingPanStartDay + 1) * fryingPanChancePerDay,
                    maximumFryingPanChance);

            for (int i = 0; i < currentPlateAmount; i++)
            {
                GameObject plate = Instantiate(platePrefab, washingArea);
                WashableDish dish = plate.GetComponent<WashableDish>();

                if (dish != null)
                {
                    bool isFryingPan = fryingPanSprite != null && Random.value < panChance;
                    Sprite dishSprite = isFryingPan
                        ? fryingPanSprite
                        : GetRandomDishVariant();
                    float dishScrubRate = isFryingPan
                        ? scrubRateMultiplier * fryingPanScrubRateMultiplier
                        : scrubRateMultiplier;

                    dish.SetDishAppearance(dishSprite, messLevel, dishScrubRate);
                    spawnedPlates.Add(dish);
                }

                Vector2 scatter = Random.insideUnitCircle * (maximumDishScatter * messLevel);
                plate.transform.localPosition = new Vector3(
                    i * 4f + scatter.x,
                    i * 12f + scatter.y,
                    0f
                );
                plate.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(-5f, 5f)
                );
            }

            Debug.Log("Spawned plates: " + spawnedPlates.Count);
        }

        private Sprite GetRandomDishVariant()
        {
            if (alternateDishSprites == null || alternateDishSprites.Length == 0)
                return null;

            int startIndex = Random.Range(0, alternateDishSprites.Length);
            for (int offset = 0; offset < alternateDishSprites.Length; offset++)
            {
                Sprite variant = alternateDishSprites[(startIndex + offset) % alternateDishSprites.Length];
                if (variant != null)
                    return variant;
            }

            return null;
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

    if (dailyChallenge == DailyChallenge.RushFinish)
    {
        ResolveDailyChallenge(currentTimer >= 30f);
    }
    else if (!dailyChallengeResolved)
    {
        ResolveDailyChallenge(false);
    }

    timerRunning = false;


    currentStage = WashStage.Complete;


    Debug.Log("DISHWASHING COMPLETE!");



    // RESET SPONGE
    ResetSponge();


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




    if (panel != null)
    {
        resultCoroutine = StartCoroutine(ClosePanelAfterResult());
    }
    else
    {
        currentChore = null;
    }

}

private IEnumerator ClosePanelAfterResult()
{
    yield return new WaitForSecondsRealtime(ResultDisplayDuration);

    if (panel != null)
    {
        panel.SetActive(false);
    }

    currentChore = null;
    resultCoroutine = null;
}

private void UpdateTimerUI()
{
    int minutes = Mathf.FloorToInt(currentTimer / 60);
    int seconds = Mathf.FloorToInt(currentTimer % 60);
    SetTimerText(string.Format("{0:00}:{1:00}", minutes, seconds));
    UpdateDailyChallengeCard();

    UpdateScoreUI();
}

private void SetTimerText(string value)
{
    if (timerText is Text legacyText)
    {
        legacyText.text = value;
    }
    else if (timerText is TMP_Text tmpText)
    {
        tmpText.text = value;
    }
}

private void EnsureDailyChallengeCard()
{
    if (dailyChallengeCard != null)
        return;

    Canvas canvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
    if (canvas == null)
    {
        Debug.LogError(
            "DishwashingMiniGame cannot display the optional challenge because it is not under a Canvas.",
            this
        );
        return;
    }

    Transform existingCard = canvas.transform.Find("OptionalDailyChallengeCard");
    if (existingCard != null)
    {
        dailyChallengeCard = existingCard.gameObject;
        dailyChallengeText = existingCard.Find("ChallengeText")
            ?.GetComponent<TMP_Text>();
        return;
    }

    dailyChallengeCard = new GameObject(
        "OptionalDailyChallengeCard",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image)
    );
    dailyChallengeCard.transform.SetParent(canvas.transform, false);
    dailyChallengeCard.transform.SetAsLastSibling();

    RectTransform cardRect = dailyChallengeCard.GetComponent<RectTransform>();
    cardRect.anchorMin = new Vector2(0f, 1f);
    cardRect.anchorMax = new Vector2(0f, 1f);
    cardRect.pivot = new Vector2(0f, 1f);
    cardRect.anchoredPosition = new Vector2(24f, -24f);
    cardRect.sizeDelta = new Vector2(350f, 112f);

    Image background = dailyChallengeCard.GetComponent<Image>();
    background.color = new Color(0.07f, 0.12f, 0.14f, 0.96f);
    background.raycastTarget = false;

    dailyChallengeText = CreateChallengeText(
        "ChallengeText",
        dailyChallengeCard.transform,
        16f,
        FontStyles.Normal,
        new Vector2(14f, -12f),
        new Vector2(322f, 88f)
    );
}

private static TMP_Text CreateChallengeText(
    string objectName,
    Transform parent,
    float fontSize,
    FontStyles fontStyle,
    Vector2 anchoredPosition,
    Vector2 size
)
{
    GameObject textObject = new GameObject(
        objectName,
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(TextMeshProUGUI)
    );
    textObject.transform.SetParent(parent, false);

    RectTransform rect = textObject.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0f, 1f);
    rect.anchorMax = new Vector2(0f, 1f);
    rect.pivot = new Vector2(0f, 1f);
    rect.anchoredPosition = anchoredPosition;
    rect.sizeDelta = size;

    TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
    if (TMP_Settings.defaultFontAsset != null)
        text.font = TMP_Settings.defaultFontAsset;
    text.fontSize = fontSize;
    text.fontStyle = fontStyle;
    text.color = Color.white;
    text.alignment = TextAlignmentOptions.TopLeft;
    text.enableWordWrapping = true;
    text.raycastTarget = false;
    return text;
}

private void UpdateDailyChallengeCard()
{
    if (dailyChallengeText == null)
        return;

    if (!string.IsNullOrEmpty(dailyChallengeResult))
    {
        dailyChallengeText.text = "DAILY CHALLENGE: " +
            dailyChallengeLabel.ToUpperInvariant() + "\n" +
            dailyChallengeResult;
        return;
    }

    if (!timerRunning)
    {
        dailyChallengeText.text = "DAILY CHALLENGE: " +
            dailyChallengeLabel.ToUpperInvariant() +
            "\nOptional dishwashing bonus\nReward: +" +
            dailyChallengeRewardCoins + " coins";
        return;
    }

    switch (dailyChallenge)
    {
        case DailyChallenge.RushFinish:
            dailyChallengeText.text =
                "DAILY CHALLENGE: " + dailyChallengeLabel.ToUpperInvariant() +
                "\nFinish with 30+ seconds left\nTime remaining: " +
                Mathf.CeilToInt(currentTimer) + "s | Reward: +" +
                dailyChallengeRewardCoins + " coins";
            break;
        case DailyChallenge.ComboStreak:
            dailyChallengeText.text =
                "DAILY CHALLENGE: " + dailyChallengeLabel.ToUpperInvariant() +
                "\nBuild a combo of 3\nCurrent streak: " +
                Mathf.Min(currentCombo, 3) + "/3 | Reward: +" +
                dailyChallengeRewardCoins + " coins";
            break;
        case DailyChallenge.LeftoverDash:
            dailyChallengeText.text =
                "DAILY CHALLENGE: " + dailyChallengeLabel.ToUpperInvariant() +
                "\nClear leftovers in 12 seconds\nLeft: " +
                leftoversRemaining + " | Time: " +
                Mathf.FloorToInt(elapsedRunTime) + "s | Reward: +" +
                dailyChallengeRewardCoins + " coins";
            break;
    }
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

    if (dailyChallenge == DailyChallenge.ComboStreak &&
        currentCombo >= 3)
    {
        ResolveDailyChallenge(true);
    }

    comboTimer = comboWindow;

    float multiplier = 1f + (currentCombo - 1) * 0.35f;
    int awardedScore = Mathf.RoundToInt(baseScore * multiplier);

    currentScore += awardedScore;
    currentTimer = Mathf.Min(dishwashingTime, currentTimer + 0.2f + currentCombo * 0.12f);
    UpdateDailyChallengeCard();

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
    elapsedRunTime = 0f;
    dailyChallengeLabel = "";
    dailyChallengeResult = "";
    dailyChallengeResolved = false;
    dailyChallenge = DailyChallenge.RushFinish;


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
    currentDay = day;


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

    switch ((day - 1) % 3)
    {
        case 0:
            dailyChallenge = DailyChallenge.RushFinish;
            dailyChallengeLabel = "Rush Finish";
            break;
        case 1:
            dailyChallenge = DailyChallenge.ComboStreak;
            dailyChallengeLabel = "Combo Streak";
            break;
        default:
            dailyChallenge = DailyChallenge.LeftoverDash;
            dailyChallengeLabel = "Leftover Dash";
            break;
    }

    UpdateTimerUI();

    Debug.Log(
        "Dishwashing Difficulty Applied. Day: " 
        + day +
        " Plates: " +
        currentPlateAmount
    );
}

private void ResolveDailyChallenge(bool completed)
{
    if (dailyChallengeResolved)
        return;

    dailyChallengeResolved = true;
    if (completed)
    {
        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager != null)
        {
            economyManager.AddCoins(dailyChallengeRewardCoins);
            dailyChallengeResult = "COMPLETED! +" +
                dailyChallengeRewardCoins + " coins";
        }
        else
        {
            dailyChallengeResult = "COMPLETED! Reward unavailable";
            Debug.LogError(
                "Daily dishwashing challenge completed, but EconomyManager is unavailable.",
                this
            );
        }
    }
    else
    {
        dailyChallengeResult = "Not completed - no penalty";
    }

    UpdateDailyChallengeCard();
}

}