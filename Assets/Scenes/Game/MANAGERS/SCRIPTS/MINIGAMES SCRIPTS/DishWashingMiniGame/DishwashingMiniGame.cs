using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class DishwashingMiniGame : MonoBehaviour, IPointerClickHandler
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
private Coroutine resultCoroutine;
private const float ResultDisplayDuration = 1.5f;

    [Header("Rinsing")]
    [SerializeField] private Transform rinsingArea;
    [SerializeField, Min(0.1f)] private float waterFillDuration = 3f;
    [SerializeField] private Texture2D waterStreamSpriteSheet;
    [SerializeField] private Transform waterStreamSpawnPoint;
    [SerializeField, Min(1)] private int waterStreamFrameCount = 8;
    [SerializeField, Min(1f)] private float waterStreamFramesPerSecond = 12f;
    [SerializeField, Min(0.01f)] private float waterStreamWidth = 112.6723f;
    [SerializeField, Min(0.01f)] private float waterStreamHeight = 50.24773f;

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
private Transform faucet;
private Image faucetImage;
private Outline faucetOutline;
private Color faucetOriginalColor;
private Vector3 faucetOriginalScale;
private Image waterFillImage;
private Sprite waterOverlaySprite;
private Image waterStreamImage;
private RectTransform waterStreamRect;
private Image waterSurfaceImage;
private Sprite[] waterStreamFrames;
private Coroutine waterFillCoroutine;
private bool waterFilled;
private EconomyManager economyManager;

private void OnEnable()
{
    SubscribeToEconomy();
    RefreshDishwashingLiquidVisual();
}

private void Start()
{
    SubscribeToEconomy();
    RefreshDishwashingLiquidVisual();

    tutorialManager = FindFirstObjectByType<TutorialManager>();
    dayManager = DayManager.Instance != null
        ? DayManager.Instance
        : FindFirstObjectByType<DayManager>();

    ApplyDayDifficulty();
    ResolveRinseVisuals();
}

private void OnDisable()
{
    SoundEffectManager.Stop("Faucet");

    if (economyManager != null)
        economyManager.DishwashingLiquidPurchaseChanged -= OnDishwashingLiquidPurchaseChanged;
}

private void SubscribeToEconomy()
{
    EconomyManager currentEconomyManager = EconomyManager.Instance;
    if (currentEconomyManager == null)
        return;

    if (economyManager != null && economyManager != currentEconomyManager)
        economyManager.DishwashingLiquidPurchaseChanged -= OnDishwashingLiquidPurchaseChanged;

    economyManager = currentEconomyManager;
    economyManager.DishwashingLiquidPurchaseChanged -= OnDishwashingLiquidPurchaseChanged;
    economyManager.DishwashingLiquidPurchaseChanged += OnDishwashingLiquidPurchaseChanged;
}

private void OnDishwashingLiquidPurchaseChanged(bool purchased)
{
    RefreshDishwashingLiquidVisual();
}

private void RefreshDishwashingLiquidVisual()
{
    if (economyManager == null || !economyManager.HasPurchasedDishwashingLiquid)
        return;

    if (soap == null)
    {
        Debug.LogWarning("Dishwashing liquid was purchased, but the soap visual is not assigned.", this);
        return;
    }

    Sprite liquidSprite = economyManager.DishwashingLiquidSprite;
    if (liquidSprite == null)
    {
        Debug.LogWarning("Dishwashing liquid was purchased, but its sprite is not available.", this);
        return;
    }

    Image soapImage = soap.GetComponentInChildren<Image>(true);
    if (soapImage == null)
    {
        Debug.LogWarning("Dishwashing liquid visual has no UI Image to update.", soap);
        return;
    }

    soapImage.sprite = liquidSprite;
    soapImage.preserveAspect = true;
}

private void OnDestroy()
{
    if (waterStreamFrames != null)
    {
        foreach (Sprite frame in waterStreamFrames)
        {
            if (frame != null)
                Destroy(frame);
        }
    }

    if (waterOverlaySprite != null)
        Destroy(waterOverlaySprite);
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

    ResolveRinseVisuals();

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
            "4. Put all clean plates in the rinsing area.\n" +
            "5. Click the faucet and wait 3 seconds for the area to fill with water.\n" +
            "6. Put the rinsed plates on the drying rack.",
            FinishTutorial
        );
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject pressedObject = eventData.pointerPressRaycast.gameObject;
        if (faucet == null || pressedObject == null)
            return;

        Transform clickedTransform = pressedObject.transform;
        if (clickedTransform != faucet && !clickedTransform.IsChildOf(faucet))
            return;

        if (currentStage != WashStage.Rinse ||
            waterFilled ||
            waterFillCoroutine != null ||
            platesToRinse < currentPlateAmount)
            return;

        waterFillCoroutine = StartCoroutine(FillRinsingArea());
    }
    private void Update()
{
    UpdateFaucetCue();

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

        if (!CanAddSoap())
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

    public bool CanAddSoap()
    {
        return currentStage == WashStage.AddSoap &&
            leftoversRemaining == 0;
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

    if (platesToRinse >= currentPlateAmount)
        return;

    platesToRinse++;

    Debug.Log(
        "Plate moved to rinsing area! " +
        "Plates waiting to rinse: " +
        platesToRinse
    );

    currentStage = WashStage.Rinse;
    Debug.Log("RINSE STAGE ACTIVE.");

    // Allow the next plate to be scrubbed while this one waits in the rinse area.
    EnableNextPlate();
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
    if (currentStage != WashStage.Rinse || platesRinsed >= currentPlateAmount)
        return;

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
        return;
    }


    Debug.Log(
        "ALL PLATES RINSED. START DRYING."
    );


    StartDrying();
}

private IEnumerator FillRinsingArea()
{
    waterFilled = false;
    SoundEffectManager.Play("Faucet");

    float fillDuration = Mathf.Max(0.1f, waterFillDuration);
    RectTransform rinseRect = rinsingArea as RectTransform;
    RectTransform parentRect = rinsingArea != null
        ? rinsingArea.parent as RectTransform
        : null;
    bool streamPositionValid = waterStreamRect != null &&
        rinseRect != null &&
        parentRect != null &&
        UpdateWaterStreamPosition(parentRect, rinseRect);

    if (waterFillImage != null)
    {
        waterFillImage.fillAmount = 0f;
        waterFillImage.gameObject.SetActive(true);
    }

    if (waterStreamImage != null)
        waterStreamImage.gameObject.SetActive(streamPositionValid);

    float elapsed = 0f;
    int lastFrame = -1;
    while (elapsed < fillDuration)
    {
        elapsed += Time.deltaTime;
        float fillAmount = Mathf.Clamp01(elapsed / fillDuration);
        if (waterFillImage != null)
            waterFillImage.fillAmount = fillAmount;

        UpdateWaterSurface(fillAmount);

        if (waterStreamFrames != null && waterStreamFrames.Length > 0 &&
            waterStreamImage != null)
        {
            int frame = (int)(elapsed * waterStreamFramesPerSecond) % waterStreamFrames.Length;
            if (frame != lastFrame)
            {
                waterStreamImage.sprite = waterStreamFrames[frame];
                lastFrame = frame;
            }
        }

        yield return null;
    }

    if (waterFillImage != null)
        waterFillImage.fillAmount = 1f;

    UpdateWaterSurface(1f);

    if (waterStreamImage != null)
        waterStreamImage.gameObject.SetActive(false);

    SoundEffectManager.Stop("Faucet");

    if (waterFillImage != null)
        waterFillImage.gameObject.SetActive(false);

    waterFilled = true;
    waterFillCoroutine = null;

    DishRinseArea area = rinsingArea != null
        ? rinsingArea.GetComponent<DishRinseArea>()
        : null;
    if (area == null)
    {
        Debug.LogWarning("Dishwashing rinse could not finish because the rinsing area has no DishRinseArea component.");
        yield break;
    }

    area.CompleteRinsing();
}

private void UpdateWaterSurface(float fillAmount)
{
    if (waterSurfaceImage == null || waterFillImage == null)
        return;

    RectTransform fillRect = waterFillImage.rectTransform;
    RectTransform surfaceRect = waterSurfaceImage.rectTransform;
    surfaceRect.anchoredPosition = new Vector2(
        Mathf.Sin(Time.time * 5f) * 3f,
        fillRect.rect.height * fillAmount);
    waterSurfaceImage.gameObject.SetActive(fillAmount > 0.02f);

    Color surfaceColor = waterSurfaceImage.color;
    surfaceColor.a = 0.65f + (Mathf.Sin(Time.time * 7f) + 1f) * 0.12f;
    waterSurfaceImage.color = surfaceColor;
}

private void ResolveRinseVisuals()
{
    Transform faucetParent = panel != null ? panel.transform : transform;
    if (faucet == null)
        faucet = FindDescendant(faucetParent, "Faucet");

    if (waterStreamSpawnPoint == null && faucet != null)
    {
        waterStreamSpawnPoint = FindDescendant(faucet, "Water Stream Spawn Point");
        if (waterStreamSpawnPoint == null)
            waterStreamSpawnPoint = FindDescendant(faucet, "WaterStreamSpawnPoint");
    }

    if (faucet != null && faucetImage == null)
    {
        faucetImage = faucet.GetComponent<Image>();
        if (faucetImage != null)
        {
            faucetOriginalColor = faucetImage.color;
            faucetOriginalScale = faucet.localScale;
            faucetOutline = faucetImage.GetComponent<Outline>();
            if (faucetOutline == null)
                faucetOutline = faucetImage.gameObject.AddComponent<Outline>();

            faucetOutline.effectColor = new Color(0.3f, 0.9f, 1f, 0.95f);
            faucetOutline.effectDistance = new Vector2(4f, 4f);
            faucetOutline.enabled = false;
        }
    }

    if (waterStreamFrames == null || waterStreamFrames.Length == 0)
        CreateWaterStreamFrames();

    if (rinsingArea == null)
        return;

    RectTransform rinseRect = rinsingArea as RectTransform;
    RectTransform parentRect = rinsingArea.parent as RectTransform;
    if (rinseRect == null || parentRect == null)
        return;

    if (waterFillImage == null)
    {
        if (waterOverlaySprite == null)
        {
            waterOverlaySprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
        }

        var waterObject = new GameObject(
            "RinseWaterFill",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform waterRect = waterObject.GetComponent<RectTransform>();
        waterRect.SetParent(parentRect, false);
        waterRect.anchorMin = rinseRect.anchorMin;
        waterRect.anchorMax = rinseRect.anchorMax;
        waterRect.anchoredPosition = rinseRect.anchoredPosition;
        waterRect.sizeDelta = rinseRect.sizeDelta;
        waterRect.pivot = rinseRect.pivot;
        waterRect.localRotation = rinseRect.localRotation;
        waterRect.localScale = rinseRect.localScale;
        waterRect.SetSiblingIndex(rinseRect.GetSiblingIndex() + 1);

        waterFillImage = waterObject.GetComponent<Image>();
        waterFillImage.sprite = waterOverlaySprite;
        waterFillImage.color = new Color(0.2f, 0.65f, 0.95f, 0.45f);
        waterFillImage.type = Image.Type.Filled;
        waterFillImage.fillMethod = Image.FillMethod.Vertical;
        waterFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        waterFillImage.fillAmount = 0f;
        waterFillImage.raycastTarget = false;
        waterObject.SetActive(false);

        var surfaceObject = new GameObject(
            "RinseWaterSurface",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
        surfaceRect.SetParent(waterRect, false);
        surfaceRect.anchorMin = new Vector2(0f, 0f);
        surfaceRect.anchorMax = new Vector2(1f, 0f);
        surfaceRect.pivot = new Vector2(0.5f, 0.5f);
        surfaceRect.sizeDelta = new Vector2(-12f, 5f);
        surfaceRect.anchoredPosition = Vector2.zero;

        waterSurfaceImage = surfaceObject.GetComponent<Image>();
        waterSurfaceImage.sprite = waterOverlaySprite;
        waterSurfaceImage.color = new Color(0.72f, 0.96f, 1f, 0.8f);
        waterSurfaceImage.raycastTarget = false;
        surfaceObject.SetActive(false);
    }

    CreateWaterStream(parentRect, rinseRect);
}

private void CreateWaterStreamFrames()
{
    if (waterStreamSpriteSheet == null)
    {
        Debug.LogWarning("Dishwashing water stream sprite sheet is not assigned.");
        return;
    }

    int frameCount = Mathf.Max(1, waterStreamFrameCount);
    Rect sheetRect = new Rect(0f, 0f, waterStreamSpriteSheet.width, waterStreamSpriteSheet.height);
    float frameWidth = sheetRect.width / frameCount;
    waterStreamFrames = new Sprite[frameCount];

    for (int i = 0; i < frameCount; i++)
    {
        Rect frameRect = new Rect(
            sheetRect.x + i * frameWidth,
            sheetRect.y,
            frameWidth,
            sheetRect.height);
        waterStreamFrames[i] = Sprite.Create(
            waterStreamSpriteSheet,
            frameRect,
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        waterStreamFrames[i].name = "FaucetWaterStream_" + i;
    }
}

private void CreateWaterStream(RectTransform parentRect, RectTransform rinseRect)
{
    if (waterStreamImage != null)
        return;

    if (faucet == null)
    {
        Debug.LogWarning("Dishwashing water stream could not spawn because the faucet was not found.");
        return;
    }

    if (waterStreamFrames == null || waterStreamFrames.Length == 0)
    {
        Debug.LogWarning("Dishwashing water stream could not spawn because its sprite frames are unavailable.");
        return;
    }

    if (waterFillImage == null)
    {
        Debug.LogWarning("Dishwashing water stream could not spawn because the rinse water UI was not created.");
        return;
    }

    GameObject streamObject = new GameObject(
        "FaucetWaterStream",
        typeof(RectTransform),
        typeof(CanvasRenderer),
        typeof(Image));
    waterStreamRect = streamObject.GetComponent<RectTransform>();
    waterStreamRect.SetParent(parentRect, false);
    waterStreamRect.anchorMin = waterStreamRect.anchorMax = parentRect.pivot;
    waterStreamRect.pivot = new Vector2(0.5f, 1f);
    waterStreamRect.localScale = new Vector3(1f, 3.395195f, 1f);

    Sprite firstFrame = waterStreamFrames[0];
    waterStreamImage = streamObject.GetComponent<Image>();
    waterStreamImage.sprite = firstFrame;
    waterStreamImage.color = Color.white;
    waterStreamImage.preserveAspect = false;
    waterStreamImage.raycastTarget = false;
    streamObject.SetActive(false);

    UpdateWaterStreamPosition(parentRect, rinseRect);
}

private bool UpdateWaterStreamPosition(RectTransform parentRect, RectTransform rinseRect)
{
    if (waterStreamRect == null || faucet == null || parentRect == null || rinseRect == null)
        return false;

    Vector3 streamStart;
    if (waterStreamSpawnPoint != null)
    {
        streamStart = waterStreamSpawnPoint.position;
    }
    else if (faucet is RectTransform faucetRect)
    {
        streamStart = faucetRect.position -
            faucetRect.up * (faucetRect.rect.height * faucetRect.lossyScale.y * 0.3f);
    }
    else
    {
        Debug.LogWarning("Dishwashing water stream has no valid faucet spawn point.");
        return false;
    }

    Vector2 streamStartInParent = parentRect.InverseTransformPoint(streamStart);

    Rect parentBounds = parentRect.rect;
    if (Mathf.Approximately(parentBounds.width, 0f) ||
        Mathf.Approximately(parentBounds.height, 0f))
    {
        Debug.LogWarning("Dishwashing water stream could not align because its UI parent has no size.");
        return false;
    }

    Vector2 spawnAnchor = new Vector2(
        (streamStartInParent.x - parentBounds.xMin) / parentBounds.width,
        (streamStartInParent.y - parentBounds.yMin) / parentBounds.height);
    waterStreamRect.anchorMin = spawnAnchor;
    waterStreamRect.anchorMax = spawnAnchor;
    waterStreamRect.anchoredPosition = new Vector2(0f, 42.9f);
    waterStreamRect.sizeDelta = new Vector2(
        waterStreamWidth,
        waterStreamHeight);
    waterStreamRect.localRotation = Quaternion.Euler(0f, 0f, -2.456f);
    waterStreamRect.SetAsLastSibling();
    return true;
}

private static Transform FindDescendant(Transform root, string targetName)
{
    if (root == null)
        return null;

    Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
    foreach (Transform descendant in descendants)
    {
        if (string.Equals(
                descendant.name,
                targetName,
                System.StringComparison.OrdinalIgnoreCase))
            return descendant;
    }

    return null;
}

private void UpdateFaucetCue()
{
    bool shouldHighlight = faucetOutline != null &&
        currentStage == WashStage.Rinse &&
        platesToRinse >= currentPlateAmount &&
        !waterFilled &&
        waterFillCoroutine == null;

    if (faucetOutline != null)
        faucetOutline.enabled = shouldHighlight;

    if (faucetImage == null)
        return;

    if (shouldHighlight)
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
        faucetImage.color = Color.Lerp(faucetOriginalColor, Color.white, pulse * 0.35f);
        faucet.localScale = faucetOriginalScale * (1f + pulse * 0.08f);
    }
    else
    {
        faucetImage.color = faucetOriginalColor;
        faucet.localScale = faucetOriginalScale;
    }
}

private void ResetRinseState()
{
    SoundEffectManager.Stop("Faucet");

    if (waterFillCoroutine != null)
    {
        StopCoroutine(waterFillCoroutine);
        waterFillCoroutine = null;
    }

    if (waterFillImage != null)
    {
        waterFillImage.fillAmount = 0f;
        waterFillImage.gameObject.SetActive(false);
    }

    if (waterSurfaceImage != null)
        waterSurfaceImage.gameObject.SetActive(false);

    if (waterStreamImage != null)
        waterStreamImage.gameObject.SetActive(false);

    waterFilled = false;
    DishRinseArea area = rinsingArea != null
        ? rinsingArea.GetComponent<DishRinseArea>()
        : null;
    if (area != null)
        area.ResetArea();
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
    SoundEffectManager.Play("ChoreFinished");

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
        DayManager resolvedDayManager = dayManager != null
            ? dayManager
            : (DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>());
        if (resolvedDayManager != null)
        {
            resolvedDayManager.ResolveDailyAdditionalQuest(
                currentChore,
                currentTimer >= resolvedDayManager.DailyQuestDishwashingTimeRemaining);
        }

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
    ResetRinseState();

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

    UpdateTimerUI();

    Debug.Log(
        "Dishwashing Difficulty Applied. Day: " 
        + day +
        " Plates: " +
        currentPlateAmount
    );
}

}