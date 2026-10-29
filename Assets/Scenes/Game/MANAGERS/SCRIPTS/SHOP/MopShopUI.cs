using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Cinemachine;

public class MopShopUI : MonoBehaviour
{
    private static readonly string[] blockedFurnitureLayers =
    {
       "FURNITURES",
       "Outdoor",
        "NPCS"
        
    };
    private const float FurniturePlacementGridSize = 0.5f;
    private const string FurniturePlacementInstructions =
        "HOLD RIGHT CLICK + DRAG TO MOVE THE CAMERA\n" +
        "LEFT CLICK TO PLACE  |  ESC TO CANCEL";

    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    [Header("Cat")]
    [SerializeField] private TMP_Text catCostText;
    [SerializeField] private TMP_Text catStatusText;
    [SerializeField] private Button catBuyButton;
    [SerializeField] private GameObject catPrefab;
    [SerializeField] private Transform catSpawnPoint;

    [Header("Shop Icons")]
    [SerializeField] private Sprite mopUpgradeIcon;
    [SerializeField] private Sprite catIcon;
    [SerializeField] private Sprite dishwashingLiquidIcon;

    [Header("Furniture Icons")]
    [SerializeField] private Sprite refrigeratorIcon;
    [SerializeField] private Sprite fanIcon;
    [SerializeField] private Sprite televisionIcon;
    [SerializeField] private Sprite plantIcon;
    [SerializeField] private Sprite kitchenStoveIcon;
    [SerializeField] private Sprite waterDispenserIcon;
    [SerializeField] private TMP_FontAsset tutorialFont;

    private EconomyManager economyManager;
    private ShopItemCardUI mopCard;
    private ShopItemCardUI catCard;
    private ShopItemCardUI dishwashingLiquidCard;
    private ShopItemCardUI refrigeratorCard;
    private ShopItemCardUI fanCard;
    private ShopItemCardUI televisionCard;
    private ShopItemCardUI plantCard;
    private ShopItemCardUI kitchenStoveCard;
    private ShopItemCardUI waterDispenserCard;
    private GameObject furniturePlacementObject;
    private SpriteRenderer furniturePlacementRenderer;
    private FurnitureType furnitureBeingPlaced;
    private PlayerMovement playerMovement;
    private CanvasGroup shopCanvasGroup;
    private float shopCanvasAlpha = 1f;
    private bool shopCanvasInteractable = true;
    private bool shopCanvasBlocksRaycasts = true;
    private Vector2 previousPanPointerPosition;
    private bool isPanningCamera;
    private GameObject furniturePlacementTutorial;
    private TMP_Text furniturePlacementTutorialText;
    private bool furniturePlacementValid;
    private bool showingInvalidPlacementMessage;
    private Coroutine catHighlightRoutine;
    private CinemachineCamera catHighlightCameraRig;
    private Camera catHighlightMainCamera;
    private PlayerMovement catHighlightPlayer;
    private Vector3 catHighlightOriginalCameraPosition;
    private float catHighlightOriginalOrthographicSize;
    private bool catHighlightActive;

    private void OnEnable()
    {
        ResolveShopCanvasGroup();
        SetShopVisible(true);
        SubscribeToEconomy();

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyMopUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        if (catBuyButton != null)
            catBuyButton.onClick.AddListener(BuyCat);

        ShopItemCardUI.PrepareShopPanel(transform, closeButton);
        mopCard = ShopItemCardUI.GetOrCreate(
            transform,
            "MopUpgrade",
            0,
            mopUpgradeIcon,
            "Mop Upgrade",
            "Clean spills faster with an upgraded mop.",
            BuyMopUpgrade);
        catCard = ShopItemCardUI.GetOrCreate(
            transform,
            "CatAdoption",
            2,
            catIcon,
            "Adopt a Cat",
            "Bring a friendly cat companion home.",
            BuyCat);
        dishwashingLiquidCard = ShopItemCardUI.GetOrCreate(
            transform,
            "DishwashingLiquid",
            3,
            dishwashingLiquidIcon,
            "Dishwashing Liquid",
            "Scrub plates faster with dishwashing liquid.",
            BuyDishwashingLiquid);
        refrigeratorCard = ShopItemCardUI.GetOrCreate(
            transform,
            "Refrigerator",
            0,
            refrigeratorIcon,
            "Refrigerator",
            "Click to place anywhere in the house. Esc cancels. +10 mood.",
            () => BuyFurniture(FurnitureType.Refrigerator, refrigeratorIcon),
            true);
        fanCard = ShopItemCardUI.GetOrCreate(
            transform,
            "Fan",
            1,
            fanIcon,
            "Fan",
            "Click to place anywhere in the house. +10 mood.",
            () => BuyFurniture(FurnitureType.Fan, fanIcon),
            true);
        televisionCard = ShopItemCardUI.GetOrCreate(
            transform,
            "Television",
            2,
            televisionIcon,
            "Television",
            "Click to place anywhere in the house. +10 mood.",
            () => BuyFurniture(FurnitureType.Television, televisionIcon),
            true);
        plantCard = ShopItemCardUI.GetOrCreate(
            transform,
            "Plant",
            3,
            plantIcon,
            "Plant",
            "Click to place anywhere in the house. +10 mood.",
            () => BuyFurniture(FurnitureType.Plant, plantIcon),
            true);
        kitchenStoveCard = ShopItemCardUI.GetOrCreate(
            transform,
            "KitchenStove",
            4,
            kitchenStoveIcon,
            "Kitchen Stove",
            "Click to place anywhere in the house. +10 mood.",
            () => BuyFurniture(FurnitureType.KitchenStove, kitchenStoveIcon),
            true);
        waterDispenserCard = ShopItemCardUI.GetOrCreate(
            transform,
            "WaterDispenser",
            5,
            waterDispenserIcon,
            "Water Dispenser",
            "Click to place anywhere in the house. +10 mood.",
            () => BuyFurniture(FurnitureType.WaterDispenser, waterDispenserIcon),
            true);

        Refresh();
    }

    private void Start()
    {
        SubscribeToEconomy();
        Refresh();
    }

    private void OnDisable()
    {
        CancelCatHighlight();
        DayManager.Instance?.OnDayTwoShopClosed();

        if (economyManager != null)
        {
            economyManager.CoinsChanged -= OnEconomyChanged;
            economyManager.MopUpgradeChanged -= OnEconomyChanged;
            economyManager.DishwashingLiquidPurchaseChanged -= OnDishwashingLiquidPurchaseChanged;
            economyManager.FurniturePurchaseChanged -= OnFurniturePurchaseChanged;
        }

        CancelFurniturePlacement(true);

        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyMopUpgrade);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseShop);

        if (catBuyButton != null)
            catBuyButton.onClick.RemoveListener(BuyCat);
    }

    private void OnEconomyChanged(int value)
    {
        Refresh();
    }

    private void OnDishwashingLiquidPurchaseChanged(bool purchased)
    {
        Refresh();
    }

    private void OnFurniturePurchaseChanged(FurnitureType furniture, bool purchased)
    {
        Refresh();
    }

    private void SubscribeToEconomy()
    {
        EconomyManager currentEconomyManager = EconomyManager.Instance;
        if (currentEconomyManager == null)
            return;

        if (economyManager != null && economyManager != currentEconomyManager)
        {
            economyManager.CoinsChanged -= OnEconomyChanged;
            economyManager.MopUpgradeChanged -= OnEconomyChanged;
            economyManager.DishwashingLiquidPurchaseChanged -= OnDishwashingLiquidPurchaseChanged;
            economyManager.FurniturePurchaseChanged -= OnFurniturePurchaseChanged;
        }

        economyManager = currentEconomyManager;
        economyManager.CoinsChanged -= OnEconomyChanged;
        economyManager.MopUpgradeChanged -= OnEconomyChanged;
        economyManager.DishwashingLiquidPurchaseChanged -= OnDishwashingLiquidPurchaseChanged;
        economyManager.CoinsChanged += OnEconomyChanged;
        economyManager.MopUpgradeChanged += OnEconomyChanged;
        economyManager.DishwashingLiquidPurchaseChanged += OnDishwashingLiquidPurchaseChanged;
        economyManager.FurniturePurchaseChanged += OnFurniturePurchaseChanged;
    }

    public void BuyMopUpgrade()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null)
            return;

        economyManager.TryPurchaseMopUpgrade();
        Refresh();
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
    }

    public void OpenShop()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        else
            SetShopVisible(true);

        Refresh();
    }

    public void BuyCat()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null || catPrefab == null || !economyManager.TryPurchaseCat())
            return;

        Transform spawnPoint = catSpawnPoint != null ? catSpawnPoint : transform;
        GameObject purchasedCat;
        if (catPrefab.scene.IsValid())
        {
            catPrefab.SetActive(true);
            purchasedCat = catPrefab;
        }
        else
            purchasedCat = Instantiate(catPrefab, spawnPoint.position, spawnPoint.rotation);

        Refresh();
        if (purchasedCat != null)
            HighlightPurchasedCat(purchasedCat.transform);
    }

    private void HighlightPurchasedCat(Transform cat)
    {
        catHighlightCameraRig = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        catHighlightMainCamera = Camera.main;
        if (catHighlightCameraRig == null && catHighlightMainCamera == null)
        {
            Debug.LogWarning(
                "The cat was purchased, but its camera highlight could not start because no camera was found.",
                this);
            return;
        }

        Transform cameraTransform = catHighlightCameraRig != null
            ? catHighlightCameraRig.transform
            : catHighlightMainCamera.transform;
        catHighlightOriginalCameraPosition = cameraTransform.position;
        catHighlightOriginalOrthographicSize = catHighlightCameraRig != null
            ? catHighlightCameraRig.Lens.OrthographicSize
            : catHighlightMainCamera.orthographicSize;
        catHighlightPlayer = FindFirstObjectByType<PlayerMovement>();
        catHighlightActive = true;

        SetShopVisible(false);
        MapTransition.SetConfinerBoundaryAtPosition(cat.position);
        MapTransition.MoveCameraToPosition(cat.position);
        catHighlightRoutine = StartCoroutine(ShowCatHighlight());
    }

    private System.Collections.IEnumerator ShowCatHighlight()
    {
        float targetOrthographicSize = Mathf.Min(
            catHighlightOriginalOrthographicSize,
            2f);
        yield return AnimateCatCameraZoom(targetOrthographicSize, 0.65f);

        float highlightDuration = 2f;
        float elapsed = 0f;
        while (elapsed < highlightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ReturnCameraAfterCatHighlight();
        yield return AnimateCatCameraZoom(catHighlightOriginalOrthographicSize, 0.65f);

        SetShopVisible(true);
        catHighlightActive = false;
        catHighlightRoutine = null;
        catHighlightCameraRig = null;
        catHighlightMainCamera = null;
        catHighlightPlayer = null;
    }

    private System.Collections.IEnumerator AnimateCatCameraZoom(float targetSize, float duration)
    {
        float startSize = GetCatHighlightOrthographicSize();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetCatHighlightOrthographicSize(Mathf.Lerp(startSize, targetSize, progress));
            yield return null;
        }

        SetCatHighlightOrthographicSize(targetSize);
    }

    private float GetCatHighlightOrthographicSize()
    {
        return catHighlightCameraRig != null
            ? catHighlightCameraRig.Lens.OrthographicSize
            : catHighlightMainCamera != null
                ? catHighlightMainCamera.orthographicSize
                : catHighlightOriginalOrthographicSize;
    }

    private void SetCatHighlightOrthographicSize(float orthographicSize)
    {
        if (catHighlightCameraRig != null)
        {
            LensSettings lens = catHighlightCameraRig.Lens;
            lens.OrthographicSize = orthographicSize;
            catHighlightCameraRig.Lens = lens;
        }
        else if (catHighlightMainCamera != null)
        {
            catHighlightMainCamera.orthographicSize = orthographicSize;
        }
    }

    private void ReturnCameraAfterCatHighlight()
    {
        if (catHighlightPlayer != null)
            MapTransition.ReturnCameraToPlayer(catHighlightPlayer);
        else
            MapTransition.MoveCameraToPosition(catHighlightOriginalCameraPosition);
    }

    private void CancelCatHighlight()
    {
        if (catHighlightRoutine != null)
        {
            StopCoroutine(catHighlightRoutine);
            catHighlightRoutine = null;
        }

        if (!catHighlightActive)
            return;

        SetCatHighlightOrthographicSize(catHighlightOriginalOrthographicSize);
        ReturnCameraAfterCatHighlight();
        catHighlightActive = false;
        catHighlightCameraRig = null;
        catHighlightMainCamera = null;
        catHighlightPlayer = null;
    }

    public void BuyDishwashingLiquid()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null)
            return;

        economyManager.TryPurchaseDishwashingLiquid(dishwashingLiquidIcon);
        Refresh();
    }

    private void BuyFurniture(FurnitureType furniture, Sprite icon)
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null || icon == null || furniturePlacementObject != null)
            return;

        if (Mouse.current == null || !TryGetPlacementCamera(out _))
        {
            Debug.LogWarning(
                "Furniture cannot be placed because a mouse or main camera was not found.",
                this);
            return;
        }

        PlayerMovement placementPlayer = FindFirstObjectByType<PlayerMovement>();
        if (placementPlayer == null)
        {
            Debug.LogWarning("Furniture cannot be placed because the player was not found.", this);
            return;
        }

        if (!economyManager.TryPurchaseFurniture(furniture))
            return;

        showingInvalidPlacementMessage = false;
        MapTransition.MoveCameraToPosition(placementPlayer.DailySpawnPosition);
        SetShopVisible(false);

        furnitureBeingPlaced = furniture;
        furniturePlacementObject = new GameObject(furniture + " Placement");
        furniturePlacementRenderer = furniturePlacementObject.AddComponent<SpriteRenderer>();
        furniturePlacementRenderer.sprite = icon;
        furniturePlacementRenderer.sortingLayerName = "Decor";
        furniturePlacementRenderer.sortingOrder = 26;
        furniturePlacementRenderer.color = new Color(1f, 1f, 1f, 0.7f);

        playerMovement = placementPlayer;
        playerMovement.SetMovementInputEnabled(false);

        if (statusText != null)
            statusText.text = "Move the mouse and click to place. Press Esc to cancel.";
        ShowFurniturePlacementTutorial();
        RefreshFurnitureCards();
    }

    private void Update()
    {
        UpdateFurniturePlacementTutorialPulse();

        if (furniturePlacementObject == null)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelFurniturePlacement(true);
            return;
        }

        if (Mouse.current == null || !TryGetPlacementCamera(out Camera placementCamera))
            return;

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isPanningCamera = true;
            previousPanPointerPosition = pointerPosition;
        }

        if (Mouse.current.rightButton.isPressed && isPanningCamera)
        {
            if (TryGetScreenWorldPosition(
                    placementCamera,
                    previousPanPointerPosition,
                    out Vector3 previousWorldPosition) &&
                TryGetScreenWorldPosition(
                    placementCamera,
                    pointerPosition,
                    out Vector3 currentWorldPosition))
            {
                MapTransition.PanCamera(previousWorldPosition - currentWorldPosition);
            }

            previousPanPointerPosition = pointerPosition;
        }
        else
        {
            isPanningCamera = false;
        }

        if (!TryGetScreenWorldPosition(placementCamera, pointerPosition, out Vector3 worldPosition))
        {
            return;
        }

        MoveFurniturePlacement(worldPosition);

        furniturePlacementValid = IsFurniturePlacementValid();
        furniturePlacementRenderer.color = furniturePlacementValid
            ? new Color(1f, 1f, 1f, 0.7f)
            : new Color(1f, 0.15f, 0.15f, 0.8f);

        if (furniturePlacementValid && showingInvalidPlacementMessage)
        {
            SetFurniturePlacementMessage(FurniturePlacementInstructions, new Color(1f, 0.9f, 0.45f, 1f));
            showingInvalidPlacementMessage = false;
        }

        if (!Mouse.current.rightButton.isPressed &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            if (furniturePlacementValid)
                PlaceFurniture();
            else
            {
                SetFurniturePlacementMessage("CAN'T PLACE HERE", Color.red);
                showingInvalidPlacementMessage = true;
            }
        }
    }

    private void MoveFurniturePlacement(Vector3 worldPosition)
    {
        Transform placementTransform = furniturePlacementObject.transform;
        Vector3 snappedCenter = new Vector3(
            Mathf.Round(worldPosition.x / FurniturePlacementGridSize) * FurniturePlacementGridSize,
            Mathf.Round(worldPosition.y / FurniturePlacementGridSize) * FurniturePlacementGridSize,
            0f);
        Vector3 spriteCenterOffset =
            placementTransform.TransformVector(furniturePlacementRenderer.sprite.bounds.center);

        placementTransform.position = snappedCenter - spriteCenterOffset;
    }

    private bool IsFurniturePlacementValid()
    {
        if (furniturePlacementRenderer == null || furniturePlacementRenderer.sprite == null)
            return false;

        Bounds furnitureBounds = furniturePlacementRenderer.bounds;
        Collider2D[] overlappingColliders = Physics2D.OverlapBoxAll(
            furnitureBounds.center,
            furnitureBounds.size,
            furniturePlacementObject.transform.eulerAngles.z,
            ~0);

        foreach (Collider2D overlappingCollider in overlappingColliders)
        {
            if (overlappingCollider == null ||
                !overlappingCollider.enabled ||
                !overlappingCollider.gameObject.activeInHierarchy)
                continue;

            Transform current = overlappingCollider.transform;
            while (current != null)
            {
                string layerName = LayerMask.LayerToName(current.gameObject.layer);
                for (int i = 0; i < blockedFurnitureLayers.Length; i++)
                {
                    if (layerName == blockedFurnitureLayers[i])
                        return false;
                }

                current = current.parent;
            }
        }

        return true;
    }

    private static bool TryGetPlacementCamera(out Camera placementCamera)
    {
        placementCamera = Camera.main;
        return placementCamera != null;
    }

    private static bool TryGetScreenWorldPosition(
        Camera placementCamera,
        Vector2 screenPosition,
        out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        Ray pointerRay = placementCamera.ScreenPointToRay(screenPosition);
        Plane housePlane = new Plane(Vector3.forward, Vector3.zero);
        if (!housePlane.Raycast(pointerRay, out float distance))
            return false;

        worldPosition = pointerRay.GetPoint(distance);
        return true;
    }

    private void ShowFurniturePlacementTutorial()
    {
        HideFurniturePlacementTutorial();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning(
                "Furniture placement tutorial cannot be shown because the shop has no parent Canvas.",
                this);
            return;
        }

        GameObject tutorialObject = new GameObject(
            "FurniturePlacementTutorial",
            typeof(RectTransform),
            typeof(CanvasGroup));
        tutorialObject.transform.SetParent(canvas.transform, false);
        Canvas tutorialCanvas = tutorialObject.AddComponent<Canvas>();
        tutorialCanvas.overrideSorting = true;
        tutorialCanvas.sortingOrder = canvas.sortingOrder + 100;
        tutorialCanvas.renderMode = canvas.renderMode;
        tutorialCanvas.worldCamera = canvas.worldCamera;
        tutorialCanvas.planeDistance = canvas.planeDistance;
        CanvasGroup tutorialCanvasGroup = tutorialObject.GetComponent<CanvasGroup>();
        tutorialCanvasGroup.alpha = 1f;
        tutorialCanvasGroup.interactable = false;
        tutorialCanvasGroup.blocksRaycasts = false;
        tutorialCanvasGroup.ignoreParentGroups = true;

        RectTransform tutorialRect = tutorialObject.GetComponent<RectTransform>();
        tutorialRect.anchorMin = new Vector2(0.5f, 0f);
        tutorialRect.anchorMax = new Vector2(0.5f, 0f);
        tutorialRect.pivot = new Vector2(0.5f, 0f);
        tutorialRect.anchoredPosition = new Vector2(0f, 100f);
        tutorialRect.sizeDelta = new Vector2(1100f, 120f);

        GameObject tutorialTextObject = new GameObject(
            "Instructions",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        tutorialTextObject.transform.SetParent(tutorialObject.transform, false);

        RectTransform textRect = tutorialTextObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        furniturePlacementTutorialText =
            tutorialTextObject.GetComponent<TextMeshProUGUI>();
        furniturePlacementTutorialText.font = tutorialFont != null
            ? tutorialFont
            : TMP_Settings.defaultFontAsset;
        furniturePlacementTutorialText.fontSize = 32f;
        furniturePlacementTutorialText.fontStyle = FontStyles.Bold;
        furniturePlacementTutorialText.color = Color.white;
        furniturePlacementTutorialText.alignment = TextAlignmentOptions.Center;
        furniturePlacementTutorialText.raycastTarget = false;
        furniturePlacementTutorialText.outlineWidth = 0.25f;
        furniturePlacementTutorialText.outlineColor = Color.black;
        furniturePlacementTutorialText.text = FurniturePlacementInstructions;

        tutorialObject.transform.SetAsLastSibling();
        furniturePlacementTutorial = tutorialObject;
    }

    private void UpdateFurniturePlacementTutorialPulse()
    {
        if (furniturePlacementTutorial == null || furniturePlacementTutorialText == null)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
        furniturePlacementTutorial.transform.localScale =
            Vector3.one * Mathf.Lerp(0.96f, 1.04f, pulse);
        Color textColor = furniturePlacementTutorialText.color;
        textColor.a = Mathf.Lerp(0.65f, 1f, pulse);
        furniturePlacementTutorialText.color = textColor;
    }

    private void SetFurniturePlacementMessage(string message, Color color)
    {
        if (furniturePlacementTutorialText == null)
            return;

        furniturePlacementTutorialText.text = message;
        furniturePlacementTutorialText.color = color;
    }

    private void PlaceFurniture()
    {
        HideFurniturePlacementTutorial();
        isPanningCamera = false;

        furniturePlacementRenderer.color = Color.white;
        furniturePlacementObject.name = furnitureBeingPlaced.ToString();
        int furnitureLayer = LayerMask.NameToLayer("FURNITURES");
        if (furnitureLayer >= 0)
            furniturePlacementObject.layer = furnitureLayer;

        BoxCollider2D furnitureCollider =
            furniturePlacementObject.AddComponent<BoxCollider2D>();
        furnitureCollider.size = furniturePlacementRenderer.sprite.bounds.size;
        furnitureCollider.offset = furniturePlacementRenderer.sprite.bounds.center;
        furniturePlacementObject = null;
        furniturePlacementRenderer = null;

        if (playerMovement != null)
        {
            MapTransition.ReturnCameraToPlayer(playerMovement);
            playerMovement.SetMovementInputEnabled(true);
        }
        playerMovement = null;

        MoodManager moodManager = MoodManager.Instance;
        if (moodManager != null)
            moodManager.AddMood(10f);
        else
            Debug.LogWarning("Furniture was placed, but no MoodManager was found to award its mood bonus.", this);

        ShowNpcHeartBubbles();
        Refresh();
        DayManager.Instance?.OnDayTwoShopClosed();
    }

    private void ShowNpcHeartBubbles()
    {
        WaypointMover[] npcs = FindObjectsByType<WaypointMover>(FindObjectsSortMode.None);
        foreach (WaypointMover npc in npcs)
        {
            if (npc == null)
                continue;

            NPCSocialParticipant participant = npc.GetComponent<NPCSocialParticipant>();
            Transform anchor = participant != null ? participant.SpeechAnchor : npc.transform;
            SpeechBubbleCanvas.Show(anchor, "♥", 2f);
        }
    }

    private void CancelFurniturePlacement(bool refundPurchase)
    {
        if (furniturePlacementObject == null)
            return;

        HideFurniturePlacementTutorial();
        isPanningCamera = false;

        Destroy(furniturePlacementObject);
        furniturePlacementObject = null;
        furniturePlacementRenderer = null;

        if (playerMovement != null)
        {
            MapTransition.ReturnCameraToPlayer(playerMovement);
            playerMovement.SetMovementInputEnabled(true);
        }
        playerMovement = null;

        if (refundPurchase && economyManager != null)
            economyManager.RefundFurniturePurchase(furnitureBeingPlaced);

        SetShopVisible(true);
        Refresh();
    }

    private void HideFurniturePlacementTutorial()
    {
        if (furniturePlacementTutorial == null)
            return;

        Destroy(furniturePlacementTutorial);
        furniturePlacementTutorial = null;
        furniturePlacementTutorialText = null;
    }

    private void ResolveShopCanvasGroup()
    {
        if (shopCanvasGroup == null)
        {
            shopCanvasGroup = GetComponent<CanvasGroup>();
            if (shopCanvasGroup == null)
                shopCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            shopCanvasAlpha = shopCanvasGroup.alpha;
            shopCanvasInteractable = shopCanvasGroup.interactable;
            shopCanvasBlocksRaycasts = shopCanvasGroup.blocksRaycasts;
        }
    }

    private void SetShopVisible(bool visible)
    {
        ResolveShopCanvasGroup();
        shopCanvasGroup.alpha = visible ? shopCanvasAlpha : 0f;
        shopCanvasGroup.interactable = visible && shopCanvasInteractable;
        shopCanvasGroup.blocksRaycasts = visible && shopCanvasBlocksRaycasts;
    }

    private void RefreshFurnitureCards()
    {
        RefreshFurnitureCard(refrigeratorCard, FurnitureType.Refrigerator, refrigeratorIcon);
        RefreshFurnitureCard(fanCard, FurnitureType.Fan, fanIcon);
        RefreshFurnitureCard(televisionCard, FurnitureType.Television, televisionIcon);
        RefreshFurnitureCard(plantCard, FurnitureType.Plant, plantIcon);
        RefreshFurnitureCard(kitchenStoveCard, FurnitureType.KitchenStove, kitchenStoveIcon);
        RefreshFurnitureCard(waterDispenserCard, FurnitureType.WaterDispenser, waterDispenserIcon);
    }

    private void RefreshFurnitureCard(ShopItemCardUI card, FurnitureType furniture, Sprite icon)
    {
        if (card == null || economyManager == null)
            return;

        bool purchased = economyManager.HasPurchasedFurniture(furniture);
        bool placingThisFurniture = furniturePlacementObject != null &&
            furnitureBeingPlaced == furniture;
        int cost = economyManager.GetFurnitureCost();
        bool canPurchase = !purchased && icon != null && economyManager.Coins >= cost &&
            furniturePlacementObject == null;

        card.SetPurchaseState(
            purchased ? "OWNED" : cost + " COINS",
            purchased
                ? placingThisFurniture ? "PLACING" : "OWNED"
                : icon == null ? "NOT SET UP" : economyManager.Coins >= cost ? "BUY" : "NEED COINS",
            canPurchase);
    }

    private void Refresh()
    {
        if (economyManager == null)
        {
            if (statusText != null)
                statusText.text = "Shop unavailable";

            if (buyButton != null)
                buyButton.interactable = false;

            if (mopCard != null)
                mopCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (catCard != null)
                catCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (dishwashingLiquidCard != null)
                dishwashingLiquidCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (refrigeratorCard != null)
                refrigeratorCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (fanCard != null)
                fanCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (televisionCard != null)
                televisionCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (plantCard != null)
                plantCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (kitchenStoveCard != null)
                kitchenStoveCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (waterDispenserCard != null)
                waterDispenserCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            return;
        }

        bool canUpgrade = economyManager.CanUpgradeMop;
        int cost = economyManager.GetMopUpgradeCost();

        if (levelText != null)
            levelText.text = "Mop Level: " + economyManager.MopUpgradeLevel;

        if (costText != null)
            costText.text = canUpgrade ? "Cost: " + cost + " coins" : "MAX LEVEL";

        if (statusText != null)
        {
            statusText.text = !canUpgrade
                ? "Mop fully upgraded"
                : economyManager.Coins >= cost
                    ? "Upgrade mopping speed"
                    : "Not enough coins";
        }

        if (buyButton != null)
            buyButton.interactable = canUpgrade && economyManager.Coins >= cost;

        if (mopCard != null)
        {
            mopCard.SetPurchaseState(
                canUpgrade ? cost + " COINS" : "MAX LEVEL",
                !canUpgrade ? "MAXED" : economyManager.Coins >= cost ? "UPGRADE" : "NEED COINS",
                canUpgrade && economyManager.Coins >= cost);
        }

        bool canBuyCat = economyManager.CanPurchaseCat;
        int catCost = economyManager.GetCatCost();
        if (catCostText != null)
            catCostText.text = canBuyCat ? "Cost: " + catCost + " coins" : "PURCHASED";

        if (catStatusText != null)
        {
            catStatusText.text = !canBuyCat
                ? "Cat is at home"
                : economyManager.Coins >= catCost
                    ? "Adopt the cat"
                    : "Not enough coins";
        }

        if (catBuyButton != null)
            catBuyButton.interactable = canBuyCat && catPrefab != null && economyManager.Coins >= catCost;

        if (catCard != null)
        {
            catCard.SetPurchaseState(
                canBuyCat ? catCost + " COINS" : "OWNED",
                !canBuyCat ? "OWNED" : catPrefab == null ? "NOT SET UP" : economyManager.Coins >= catCost ? "ADOPT" : "NEED COINS",
                canBuyCat && catPrefab != null && economyManager.Coins >= catCost);
        }

        bool canBuyDishwashingLiquid = economyManager.CanPurchaseDishwashingLiquid;
        int dishwashingLiquidCost = economyManager.GetDishwashingLiquidCost();
        if (dishwashingLiquidCard != null)
        {
            bool hasDishwashingLiquidIcon = dishwashingLiquidIcon != null;
            dishwashingLiquidCard.SetPurchaseState(
                canBuyDishwashingLiquid ? dishwashingLiquidCost + " COINS" : "OWNED",
                !canBuyDishwashingLiquid
                    ? "OWNED"
                    : !hasDishwashingLiquidIcon
                        ? "NOT SET UP"
                        : economyManager.Coins >= dishwashingLiquidCost ? "BUY" : "NEED COINS",
                canBuyDishwashingLiquid && hasDishwashingLiquidIcon && economyManager.Coins >= dishwashingLiquidCost);
        }

        RefreshFurnitureCards();
    }
}
