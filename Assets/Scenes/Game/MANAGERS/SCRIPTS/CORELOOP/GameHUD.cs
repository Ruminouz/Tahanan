using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class GameHUD : MonoBehaviour
{
    private static Sprite minimizeButtonSprite;
    private const string CompletedChoreColor = "#4CAF50";
    private const string MissedChoreColor = "#F44336";

    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text choreListText;
    [SerializeField] private Slider timeBar;
    [SerializeField] private Sprite clockFaceSprite;
    [SerializeField, Tooltip("Optional Canvas RectTransform used to control the clock's position and size.")]
    private RectTransform clockAnchor;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text successRateText;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private GameObject dailyQuestPanel;
    [SerializeField] private TMP_Text dailyQuestText;
    [SerializeField, Min(0f)] private float nearDeadlineWarningSeconds = 30f;
    [SerializeField] private Color nearDeadlineColor = new Color(1f, 0.35f, 0.2f, 1f);

    private Button choreListMinimizeButton;
    private Button choreListRestoreButton;
    private Button dailyQuestMinimizeButton;
    private Button dailyQuestRestoreButton;
    private bool isChoreListMinimized;
    private bool isDailyQuestMinimized;
    private RectTransform choreListPanelRect;
    private GameObject choreListCollapsedPanel;
    private GameObject dailyQuestCollapsedPanel;
    private Image choreListPanelBackground;
    private GameObject chorePanelDayLabel;
    private GameObject chorePanelTimeLabel;
    private bool choreListBackgroundWasEnabled;
    private bool chorePanelDayTextWasActive;
    private bool chorePanelTimeTextWasActive;
    private TimeManager timeManager;
    private DayManager dayManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;
    private SegregateWasteMiniGame toyPickupMiniGame;
    private ChoreManager choreManager;
    private EconomyManager economyManager;
    private RectTransform clockHand;
    private Image clockFaceImage;
    private Image clockHandImage;
    private Image clockCenterImage;
    private Canvas currencyCanvas;
    private readonly List<CoinPopup> coinPopups = new();
    private RectTransform hoverTooltipRect;
    private TMP_Text hoverTooltipText;
    private GameObject hoverTooltipPanel;
    private readonly List<int> helperIndicatorCharacterIndices = new();
    private readonly List<Image> helperIndicatorImages = new();
    private Sprite helperSprite;
    private bool dailyQuestPanelLookupAttempted;

    private sealed class CoinPopup
    {
        public PlayerMovement player;
        public RectTransform rect;
        public CanvasGroup group;
        public Image icon;
        public float elapsed;
    }

    private sealed class ChoreGroup
    {
        public readonly string name;
        public readonly List<Chore> chores = new();

        public ChoreGroup(string name)
        {
            this.name = name;
        }
    }

    private void Start()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
        waterSpawner = FindFirstObjectByType<WaterSpawner>();
        toyPickupMiniGame = FindFirstObjectByType<SegregateWasteMiniGame>(
            FindObjectsInactive.Include);
        choreManager = FindFirstObjectByType<ChoreManager>();
        currencyCanvas = coinsText != null
            ? coinsText.GetComponentInParent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        ResolveChoreListText();
        ResolveDailyQuestPanel();
        if (clockFaceSprite != null)
            CreateClockDisplay();

        SetupChoreListMinimizeButton();
        CreateCoinCounter();
        SetupHoverTooltips();
        ResolveEconomyManager();

        if (GetComponent<PlayerInventoryUI>() == null)
            gameObject.AddComponent<PlayerInventoryUI>();
    }

    private void Update()
    {
        ResolveEconomyManager();
        UpdateCoinCounter();
        UpdateCoinPopups();
        if (dailyQuestPanel == null || dailyQuestText == null)
            ResolveDailyQuestPanel();
        UpdateDailyQuestPanel();

        if (timeManager == null || dayManager == null)
            return;

        UpdateDay();
        UpdateTime();
        UpdateChoreList();
        UpdateDailyStats();
    }

    private void UpdateDay()
    {
        if (dayText != null)
            dayText.text = "Day " + dayManager.CurrentDay;
    }

    private void UpdateTime()
    {
        bool isTimeFrozen = timeManager.IsTimeFrozen;

        if (clockHand != null)
            clockHand.localRotation = Quaternion.Euler(
                0f,
                0f,
                timeManager.GetClockHandRotation());

        if (timeText != null)
            timeText.text = "Time: " + timeManager.GetClockTimeLabel() +
                (isTimeFrozen
                    ? " (FROZEN " +
                        Mathf.CeilToInt(timeManager.FreezeTimeRemaining) + "s)"
                    : string.Empty);

        if (clockFaceImage != null)
            clockFaceImage.color = isTimeFrozen
                ? new Color(0.62f, 0.84f, 1f)
                : Color.white;

        if (clockHandImage != null)
            clockHandImage.color = isTimeFrozen
                ? new Color(0.05f, 0.35f, 0.72f)
                : new Color(0.12f, 0.08f, 0.04f);

        if (clockCenterImage != null)
            clockCenterImage.color = isTimeFrozen
                ? new Color(0.05f, 0.35f, 0.72f)
                : new Color(0.08f, 0.06f, 0.03f);
    }

    private void CreateClockDisplay()
    {
        Canvas canvas = clockAnchor != null
            ? clockAnchor.GetComponentInParent<Canvas>()
            : null;
        if (canvas == null && timeText != null)
            canvas = timeText.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning(
                "Game clock could not be created because no Canvas was found.",
                this);
            return;
        }

        if (timeBar != null)
            timeBar.gameObject.SetActive(false);

        GameObject clockObject = new GameObject(
            "Day Clock",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        clockObject.layer = canvas.gameObject.layer;
        RectTransform clockRect = clockObject.GetComponent<RectTransform>();
        if (clockAnchor != null)
        {
            clockRect.SetParent(clockAnchor, false);
            clockRect.anchorMin = Vector2.zero;
            clockRect.anchorMax = Vector2.one;
            clockRect.pivot = new Vector2(0.5f, 0.5f);
            clockRect.sizeDelta = Vector2.zero;
        }
        else
        {
            clockRect.SetParent(canvas.transform, false);
            clockRect.anchorMin = Vector2.one;
            clockRect.anchorMax = Vector2.one;
            clockRect.pivot = Vector2.one;
            clockRect.anchoredPosition = new Vector2(-32f, -32f);
            clockRect.sizeDelta = new Vector2(144f, 144f);
        }

        clockFaceImage = clockObject.GetComponent<Image>();
        clockFaceImage.sprite = clockFaceSprite;
        clockFaceImage.preserveAspect = true;
        clockFaceImage.raycastTarget = false;

        GameObject handObject = new GameObject(
            "Clock Hand",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        handObject.layer = canvas.gameObject.layer;
        clockHand = handObject.GetComponent<RectTransform>();
        clockHand.SetParent(clockRect, false);
        clockHand.anchorMin = new Vector2(0.5f, 0.5f);
        clockHand.anchorMax = new Vector2(0.5f, 0.5f);
        clockHand.pivot = new Vector2(0.5f, 0f);
        clockHand.anchoredPosition = Vector2.zero;
        clockHand.sizeDelta = new Vector2(4f, 43f);
        clockHandImage = handObject.GetComponent<Image>();
        clockHandImage.color = new Color(0.12f, 0.08f, 0.04f);
        clockHandImage.raycastTarget = false;

        GameObject centerObject = new GameObject(
            "Clock Center",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        centerObject.layer = canvas.gameObject.layer;
        RectTransform centerRect = centerObject.GetComponent<RectTransform>();
        centerRect.SetParent(clockRect, false);
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = new Vector2(8f, 8f);
        clockCenterImage = centerObject.GetComponent<Image>();
        clockCenterImage.color = new Color(0.08f, 0.06f, 0.03f);
        clockCenterImage.raycastTarget = false;
    }

    private void UpdateChoreList()
    {
        if (choreListText == null)
            return;

        if (isChoreListMinimized)
        {
            choreListText.text = "DAILY CHORES";
            helperIndicatorCharacterIndices.Clear();
            UpdateHelperIndicators();
            return;
        }

        choreListText.text = "CHORES\n";
        helperIndicatorCharacterIndices.Clear();
        Chore[] chores = dayManager.GetActiveChores();
        bool sweepListed = false;
        HashSet<Chore> seenChores = new HashSet<Chore>();
        Dictionary<string, ChoreGroup> choreGroups =
            new Dictionary<string, ChoreGroup>(System.StringComparer.OrdinalIgnoreCase);
        List<ChoreGroup> orderedGroups = new List<ChoreGroup>();

        if (chores != null)
        {
            foreach (Chore chore in chores)
            {
                if (chore == null || !seenChores.Add(chore))
                    continue;

                if (chore == dayManager.SweepDustChore)
                    sweepListed = true;

                AddChoreToGroup(chore, choreGroups, orderedGroups);
            }
        }

        if (!sweepListed && dayManager.SweepDustChore != null &&
            seenChores.Add(dayManager.SweepDustChore))
            AddChoreToGroup(dayManager.SweepDustChore, choreGroups, orderedGroups);

        foreach (ChoreGroup choreGroup in orderedGroups)
            AppendChoreGroup(choreGroup);

        if (!sweepListed && dayManager.SweepDustChore == null && sweepingManager != null)
        {
            string marker = sweepingManager.IsSweepingCompleted ? "✓ " : "○ ";
            string status = sweepingManager.IsSweepingCompleted
                ? string.Empty
                : " · " + Mathf.Max(0, sweepingManager.RemainingDust) + " remaining";
            choreListText.text += sweepingManager.IsSweepingCompleted
                ? "<color=" + CompletedChoreColor + ">✓ <s>Sweep Dust</s></color>\n"
                : marker + "Sweep Dust" + status + "\n";
        }

        if (waterSpawner != null && waterSpawner.MopTaskStarted &&
            waterSpawner.IsMoppingMissed)
        {
            choreListText.text += "<color=" + MissedChoreColor + ">X Mop Floor</color>\n";
        }
        else if (waterSpawner != null && waterSpawner.MopTaskStarted &&
            waterSpawner.IsMoppingCompleted && !waterSpawner.IsSpawning)
        {
            choreListText.text += "<color=" + CompletedChoreColor
                + ">✓ <s>Mop Floor</s></color>\n";
        }
        else if (waterSpawner != null &&
            (waterSpawner.MopTaskStarted || waterSpawner.IsSpawning))
        {
            string mopStatus = waterSpawner.RemainingWetAreas > 0
                ? " · " + waterSpawner.RemainingWetAreas + " "
                    + (waterSpawner.RemainingWetAreas == 1 ? "wet area" : "wet areas")
                    + " remaining"
                : " · waiting for water";
            choreListText.text += "○ Mop Floor" + mopStatus + "\n";
        }

        UpdateHelperIndicators();
    }

    private void UpdateDailyQuestPanel()
    {
        if (dailyQuestPanel == null || dailyQuestText == null)
            ResolveDailyQuestPanel();

        if (dailyQuestPanel == null || dailyQuestText == null || dayManager == null)
            return;

        string questName = dayManager.DailyAdditionalQuestName;
        bool hasQuest = !string.IsNullOrEmpty(questName);
        UpdateDailyQuestPanelVisibility(hasQuest);

        if (!hasQuest)
            return;

        string status = dayManager.IsDailyAdditionalQuestCompleted
            ? "COMPLETED!"
            : dayManager.IsDailyAdditionalQuestMissed
                ? "MISSED"
                : dayManager.IsDailyAdditionalQuestFailed
                    ? "CHALLENGE FAILED"
                    : "IN PROGRESS";

        dailyQuestText.text = "DAILY BONUS QUEST: " + questName.ToUpperInvariant() +
            "\n" + dayManager.DailyAdditionalQuestGoal +
            "\nReward: +" + dayManager.DailyAdditionalQuestRewardCoins + " coins" +
            "\n" + status;
    }

    private void ResolveDailyQuestPanel()
    {
        if (dailyQuestPanel == null)
        {
            Canvas choreCanvas = choreListText != null
                ? choreListText.GetComponentInParent<Canvas>()
                : null;
            if (choreCanvas != null)
                FindDailyChallengeCard(choreCanvas.transform);

            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (dailyQuestPanel == null)
            {
                foreach (Canvas canvas in canvases)
                {
                    FindDailyChallengeCard(canvas.transform);
                    if (dailyQuestPanel != null)
                        break;
                }
            }
        }

        if (dailyQuestPanel != null)
        {
            Canvas hudCanvas = null;
            Transform ancestor = dailyQuestPanel.transform;
            while (ancestor != null)
            {
                Canvas ancestorCanvas = ancestor.GetComponent<Canvas>();
                if (ancestorCanvas != null)
                    hudCanvas = ancestorCanvas;
                ancestor = ancestor.parent;
            }

            if (hudCanvas != null && dailyQuestPanel.transform.parent != hudCanvas.transform)
                dailyQuestPanel.transform.SetParent(hudCanvas.transform, true);
        }

        if (dailyQuestPanel != null && dailyQuestText == null)
        {
            Transform challengeText = dailyQuestPanel.transform.Find("ChallengeText");
            dailyQuestText = challengeText != null
                ? challengeText.GetComponent<TMP_Text>()
                : dailyQuestPanel.GetComponentInChildren<TMP_Text>(true);
        }

        if (dailyQuestPanel != null)
        {
            SetupDailyQuestMinimizeButton();
        }

        if (dailyQuestPanel == null && !dailyQuestPanelLookupAttempted)
        {
            Debug.LogWarning(
                "Daily challenge card has not been found yet; the HUD will keep looking.",
                this);
            dailyQuestPanelLookupAttempted = true;
        }
    }

    private void FindDailyChallengeCard(Transform searchRoot)
    {
        if (searchRoot == null || dailyQuestPanel != null)
            return;

        Transform[] descendants = searchRoot.GetComponentsInChildren<Transform>(true);
        Transform fallbackCard = null;
        foreach (Transform child in descendants)
        {
            if (child.name == "OptionalDailyChallengeCard")
            {
                dailyQuestPanel = child.gameObject;
                return;
            }

            if (fallbackCard == null && child.name == "DailyChallengeCard")
                fallbackCard = child;
        }

        if (fallbackCard != null)
            dailyQuestPanel = fallbackCard.gameObject;
    }

    private void ResolveChoreListText()
    {
        if (choreListText != null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            Transform[] canvasChildren = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in canvasChildren)
            {
                if (child.name != "ChoreListText")
                    continue;

                choreListText = child.GetComponent<TMP_Text>();
                if (choreListText != null)
                    return;
            }
        }
    }

    private void SetupChoreListMinimizeButton()
    {
        if (choreListText == null || choreListMinimizeButton != null)
            return;

        Transform parent = choreListText.transform.parent;
        choreListPanelRect = parent as RectTransform;
        if (choreListPanelRect == null || choreListPanelRect.parent == null)
            return;

        choreListPanelBackground = choreListPanelRect.GetComponent<Image>();
        if (choreListPanelBackground == null)
            return;

        if (chorePanelDayLabel == null)
            chorePanelDayLabel = FindPanelObject(parent, "DayText");
        if (chorePanelTimeLabel == null)
            chorePanelTimeLabel = FindPanelObject(parent, "TimeText");

        RectTransform textRect = choreListText.rectTransform;
        Vector3[] panelCorners = new Vector3[4];
        choreListPanelRect.GetWorldCorners(panelCorners);
        RectTransform collapsedParent = choreListPanelRect.parent as RectTransform;
        Vector3 lowerLeft = collapsedParent.InverseTransformPoint(panelCorners[0]);
        Vector3 upperRight = collapsedParent.InverseTransformPoint(panelCorners[2]);
        Vector2 collapsedSize = new Vector2(
            upperRight.x - lowerLeft.x,
            44f);
        Vector2 collapsedPosition = new Vector2(
            (lowerLeft.x + upperRight.x) * 0.5f,
            upperRight.y - collapsedSize.y * 0.5f);

        choreListCollapsedPanel = new GameObject(
            "DailyChoreListCollapsedPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        choreListCollapsedPanel.layer = choreListText.gameObject.layer;
        choreListCollapsedPanel.transform.SetParent(collapsedParent, false);
        RectTransform collapsedRect = choreListCollapsedPanel.GetComponent<RectTransform>();
        collapsedRect.anchorMin = new Vector2(0.5f, 0.5f);
        collapsedRect.anchorMax = new Vector2(0.5f, 0.5f);
        collapsedRect.pivot = new Vector2(0.5f, 0.5f);
        collapsedRect.anchoredPosition = collapsedPosition;
        collapsedRect.sizeDelta = collapsedSize;
        collapsedRect.SetAsLastSibling();

        Image collapsedBackground = choreListCollapsedPanel.GetComponent<Image>();
        collapsedBackground.sprite = choreListPanelBackground.sprite;
        collapsedBackground.type = choreListPanelBackground.type;
        collapsedBackground.color = choreListPanelBackground.color;
        collapsedBackground.raycastTarget = false;

        GameObject collapsedTitleObject = new GameObject(
            "Title",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        collapsedTitleObject.layer = choreListText.gameObject.layer;
        collapsedTitleObject.transform.SetParent(choreListCollapsedPanel.transform, false);
        RectTransform titleRect = collapsedTitleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(12f, 0f);
        titleRect.offsetMax = new Vector2(-48f, 0f);
        TMP_Text collapsedTitle = collapsedTitleObject.GetComponent<TMP_Text>();
        collapsedTitle.font = choreListText.font;
        collapsedTitle.fontSize = choreListText.fontSize;
        collapsedTitle.color = choreListText.color;
        collapsedTitle.alignment = TextAlignmentOptions.MidlineLeft;
        collapsedTitle.text = "DAILY CHORES";
        collapsedTitle.raycastTarget = false;

        Button minimizeButton = CreateMinimizeButton(
            "ChoreListMinimizeButton",
            choreListPanelRect,
            choreListText,
            new Vector2(-8f, -8f));
        RectTransform minimizeButtonRect = minimizeButton.GetComponent<RectTransform>();
        minimizeButtonRect.anchorMin = new Vector2(1f, 1f);
        minimizeButtonRect.anchorMax = new Vector2(1f, 1f);
        minimizeButtonRect.pivot = new Vector2(1f, 1f);
        minimizeButton.onClick.AddListener(ToggleChoreListMinimized);
        choreListMinimizeButton = minimizeButton;

        Button restoreButton = CreateMinimizeButton(
            "ChoreListRestoreButton",
            choreListCollapsedPanel.transform,
            choreListText,
            new Vector2(-8f, -8f));
        RectTransform restoreButtonRect = restoreButton.GetComponent<RectTransform>();
        restoreButtonRect.anchorMin = new Vector2(1f, 1f);
        restoreButtonRect.anchorMax = new Vector2(1f, 1f);
        restoreButtonRect.pivot = new Vector2(1f, 1f);
        restoreButton.onClick.AddListener(ToggleChoreListMinimized);
        choreListRestoreButton = restoreButton;

        choreListBackgroundWasEnabled = choreListPanelBackground.enabled;
        chorePanelDayTextWasActive = chorePanelDayLabel != null &&
            chorePanelDayLabel.activeSelf;
        chorePanelTimeTextWasActive = chorePanelTimeLabel != null &&
            chorePanelTimeLabel.activeSelf;
        choreListCollapsedPanel.SetActive(false);
        UpdateMinimizeButtonLabel(choreListMinimizeButton, isChoreListMinimized);
        UpdateMinimizeButtonLabel(choreListRestoreButton, isChoreListMinimized);
    }

    private static GameObject FindPanelObject(Transform panel, string objectName)
    {
        Transform textTransform = panel.Find(objectName);
        return textTransform != null ? textTransform.gameObject : null;
    }

    private void SetupDailyQuestMinimizeButton()
    {
        if (dailyQuestMinimizeButton != null)
            return;

        RectTransform panelRect = dailyQuestPanel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        Transform collapsedParent = dailyQuestPanel.transform.parent;
        RectTransform collapsedParentRect = collapsedParent as RectTransform;
        if (collapsedParentRect == null)
            return;

        Vector3[] panelCorners = new Vector3[4];
        panelRect.GetWorldCorners(panelCorners);
        Vector3 lowerLeft = collapsedParentRect.InverseTransformPoint(panelCorners[0]);
        Vector3 upperRight = collapsedParentRect.InverseTransformPoint(panelCorners[2]);
        Vector2 collapsedSize = new Vector2(
            upperRight.x - lowerLeft.x,
            44f);
        Vector2 collapsedPosition = new Vector2(
            (lowerLeft.x + upperRight.x) * 0.5f,
            upperRight.y - collapsedSize.y * 0.5f);

        dailyQuestCollapsedPanel = new GameObject(
            "DailyChallengeCollapsedPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        dailyQuestCollapsedPanel.layer = dailyQuestPanel.layer;
        dailyQuestCollapsedPanel.transform.SetParent(collapsedParent, false);
        RectTransform collapsedRect = dailyQuestCollapsedPanel.GetComponent<RectTransform>();
        collapsedRect.anchorMin = new Vector2(0.5f, 0.5f);
        collapsedRect.anchorMax = new Vector2(0.5f, 0.5f);
        collapsedRect.pivot = new Vector2(0.5f, 0.5f);
        collapsedRect.anchoredPosition = collapsedPosition;
        collapsedRect.sizeDelta = collapsedSize;
        collapsedRect.SetAsLastSibling();

        Image collapsedBackground = dailyQuestCollapsedPanel.GetComponent<Image>();
        Image panelBackground = dailyQuestPanel.GetComponent<Image>();
        if (panelBackground != null)
        {
            collapsedBackground.sprite = panelBackground.sprite;
            collapsedBackground.type = panelBackground.type;
            collapsedBackground.color = panelBackground.color;
        }
        collapsedBackground.raycastTarget = false;

        GameObject collapsedTitleObject = new GameObject(
            "Title",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        collapsedTitleObject.layer = dailyQuestPanel.layer;
        collapsedTitleObject.transform.SetParent(dailyQuestCollapsedPanel.transform, false);
        RectTransform titleRect = collapsedTitleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(12f, 0f);
        titleRect.offsetMax = new Vector2(-48f, 0f);
        TMP_Text collapsedTitle = collapsedTitleObject.GetComponent<TMP_Text>();
        collapsedTitle.font = dailyQuestText.font;
        collapsedTitle.fontSize = dailyQuestText.fontSize;
        collapsedTitle.color = dailyQuestText.color;
        collapsedTitle.alignment = TextAlignmentOptions.MidlineLeft;
        collapsedTitle.text = "DAILY CHALLENGE";
        collapsedTitle.raycastTarget = false;

        Transform existingButton = dailyQuestPanel.transform.Find(
            "DailyChallengeMinimizeButton");
        Button button = existingButton != null
            ? existingButton.GetComponent<Button>()
            : null;
        if (button == null)
        {
            button = CreateMinimizeButton(
                "DailyChallengeMinimizeButton",
                dailyQuestPanel.transform,
                dailyQuestText,
                new Vector2(-22f, -20f));
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-8f, -8f);
        buttonRect.sizeDelta = new Vector2(36f, 32f);
        button.transform.SetAsLastSibling();

        Image buttonBackground = button.GetComponent<Image>();
        if (buttonBackground != null)
        {
            buttonBackground.sprite = GetMinimizeButtonSprite();
            buttonBackground.type = Image.Type.Simple;
            buttonBackground.color = new Color(0.07f, 0.12f, 0.14f, 0.96f);
            buttonBackground.raycastTarget = true;
            button.targetGraphic = buttonBackground;
        }

        EnsureMinimizeButtonLabel(button, dailyQuestText);

        RectTransform textRect = dailyQuestText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-52f, -10f);
        dailyQuestText.enableAutoSizing = true;
        dailyQuestText.fontSizeMin = 12f;
        dailyQuestText.fontSizeMax = 18f;
        dailyQuestText.fontSize = 18f;

        button.onClick.AddListener(ToggleDailyQuestMinimized);
        dailyQuestMinimizeButton = button;

        Button restoreButton = CreateMinimizeButton(
            "DailyChallengeRestoreButton",
            dailyQuestCollapsedPanel.transform,
            dailyQuestText,
            new Vector2(-8f, -8f));
        RectTransform restoreButtonRect = restoreButton.GetComponent<RectTransform>();
        restoreButtonRect.anchorMin = new Vector2(1f, 1f);
        restoreButtonRect.anchorMax = new Vector2(1f, 1f);
        restoreButtonRect.pivot = new Vector2(1f, 1f);
        restoreButton.onClick.AddListener(ToggleDailyQuestMinimized);
        dailyQuestRestoreButton = restoreButton;
        dailyQuestCollapsedPanel.SetActive(false);
        UpdateMinimizeButtonLabel(dailyQuestMinimizeButton, isDailyQuestMinimized);
        UpdateMinimizeButtonLabel(dailyQuestRestoreButton, isDailyQuestMinimized);
    }

    private void EnsureMinimizeButtonLabel(Button button, TMP_Text sourceText)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = button.gameObject.layer;
            labelObject.transform.SetParent(button.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label = labelObject.GetComponent<TMP_Text>();
        }

        label.font = sourceText != null && sourceText.font != null
            ? sourceText.font
            : TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private Button CreateMinimizeButton(
        string objectName,
        Transform parent,
        TMP_Text sourceText,
        Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(36f, 32f);

        Image background = buttonObject.GetComponent<Image>();
        background.sprite = GetMinimizeButtonSprite();
        background.type = Image.Type.Simple;
        background.color = new Color(0.07f, 0.12f, 0.14f, 0.96f);
        background.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = new Color(0.07f, 0.12f, 0.14f, 0.96f),
            highlightedColor = new Color(0.13f, 0.22f, 0.25f, 1f),
            pressedColor = new Color(0.2f, 0.3f, 0.32f, 1f),
            selectedColor = new Color(0.07f, 0.12f, 0.14f, 0.96f),
            disabledColor = new Color(0.07f, 0.12f, 0.14f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.layer = parent.gameObject.layer;
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.font = sourceText != null && sourceText.font != null
            ? sourceText.font
            : TMP_Settings.defaultFontAsset;
        label.fontSize = 20f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return button;
    }

    private static Sprite GetMinimizeButtonSprite()
    {
        if (minimizeButtonSprite == null)
            minimizeButtonSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));

        return minimizeButtonSprite;
    }

    private void ToggleChoreListMinimized()
    {
        isChoreListMinimized = !isChoreListMinimized;
        if (choreListPanelBackground != null)
            choreListPanelBackground.enabled = isChoreListMinimized
                ? false
                : choreListBackgroundWasEnabled;
        if (chorePanelDayLabel != null)
            chorePanelDayLabel.SetActive(isChoreListMinimized
                ? false
                : chorePanelDayTextWasActive);
        if (chorePanelTimeLabel != null)
            chorePanelTimeLabel.SetActive(isChoreListMinimized
                ? false
                : chorePanelTimeTextWasActive);
        if (choreListText != null)
            choreListText.gameObject.SetActive(!isChoreListMinimized);
        if (choreListCollapsedPanel != null)
            choreListCollapsedPanel.SetActive(isChoreListMinimized);
        if (choreListMinimizeButton != null)
            choreListMinimizeButton.gameObject.SetActive(!isChoreListMinimized);
        if (choreListRestoreButton != null)
            choreListRestoreButton.gameObject.SetActive(isChoreListMinimized);
    }

    private void ToggleDailyQuestMinimized()
    {
        isDailyQuestMinimized = !isDailyQuestMinimized;
        UpdateDailyQuestMinimizedState();
    }

    private void UpdateDailyQuestMinimizedState()
    {
        bool hasQuest = dayManager != null &&
            !string.IsNullOrEmpty(dayManager.DailyAdditionalQuestName);
        UpdateDailyQuestPanelVisibility(hasQuest);
        UpdateMinimizeButtonLabel(dailyQuestMinimizeButton, isDailyQuestMinimized);
        UpdateMinimizeButtonLabel(dailyQuestRestoreButton, isDailyQuestMinimized);
    }

    private void UpdateDailyQuestPanelVisibility(bool hasQuest)
    {
        if (dailyQuestPanel != null)
            dailyQuestPanel.SetActive(hasQuest && !isDailyQuestMinimized);
        if (dailyQuestCollapsedPanel != null)
            dailyQuestCollapsedPanel.SetActive(hasQuest && isDailyQuestMinimized);
    }

    private static void UpdateMinimizeButtonLabel(Button button, bool isMinimized)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = isMinimized ? "+" : "-";
    }

    private static string GetDisplayChoreName(string choreName)
    {
        if (string.IsNullOrWhiteSpace(choreName))
            return choreName;

        string trimmedName = choreName.Trim();
        if (trimmedName.IndexOf("pick up", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmedName.IndexOf("toy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            string.Equals(trimmedName, "PickUp", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedName, "Clean Leaves", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                trimmedName,
                CleanLeavesChore.ToyPickupChoreName,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return CleanLeavesChore.ToyPickupChoreName;
        }

        if (trimmedName.IndexOf("sweep", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmedName.IndexOf("dust", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            string.Equals(trimmedName, "SweepDust", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Sweep Dust";
        }

        return trimmedName;
    }

    private void AddChoreToGroup(
        Chore chore,
        Dictionary<string, ChoreGroup> choreGroups,
        List<ChoreGroup> orderedGroups)
    {
        string normalizedName = GetDisplayChoreName(chore.ChoreName);
        string key = string.IsNullOrWhiteSpace(normalizedName)
            ? "#" + chore.GetInstanceID()
            : normalizedName;

        if (!choreGroups.TryGetValue(key, out ChoreGroup choreGroup))
        {
            choreGroup = new ChoreGroup(normalizedName);
            choreGroups.Add(key, choreGroup);
            orderedGroups.Add(choreGroup);
        }

        choreGroup.chores.Add(chore);
    }

    private void AppendChoreGroup(ChoreGroup choreGroup)
    {
        int remainingCount = 0;
        int completedCount = 0;
        int missedCount = 0;
        float nextDeadline = float.MaxValue;
        bool allCompletedByHelper = true;
        int remainingGarbageBags = 0;
        bool hasGarbageChore = false;
        bool hasSpawnedGarbageBag = false;
        string displayName = GetDisplayChoreName(choreGroup.name);

        foreach (Chore chore in choreGroup.chores)
        {
            if (chore is GarbageChore garbageChore)
            {
                hasGarbageChore = true;
                hasSpawnedGarbageBag |= garbageChore.HasSpawnedBagToday;
                remainingGarbageBags += garbageChore.RemainingBagCount;
            }

            if (chore.IsCompleted)
            {
                completedCount++;
                allCompletedByHelper &= choreManager != null &&
                    choreManager.WasCompletedByHelper(chore);
            }
            else if (chore.IsMissed)
            {
                missedCount++;
            }
            else
            {
                remainingCount++;
                nextDeadline = Mathf.Min(nextDeadline, chore.DeadlineTime);
            }
        }

        if (remainingCount > 0)
        {
            string remainingSuffix = string.Empty;
            bool showDeadline = true;
            if (hasGarbageChore)
            {
                remainingSuffix = hasSpawnedGarbageBag
                    ? " · " + remainingGarbageBags + " "
                        + (remainingGarbageBags == 1 ? "trash bag" : "trash bags")
                        + " remaining"
                    : " · waiting for trash bags";
                showDeadline = false;
            }
            else if (string.Equals(
                displayName,
                "Sweep Dust",
                System.StringComparison.OrdinalIgnoreCase) && sweepingManager != null)
            {
                int remainingDust = sweepingManager.RemainingDust;
                remainingSuffix = " · " + remainingDust + " "
                    + (remainingDust == 1 ? "dust spot" : "dust spots")
                    + " remaining";
                showDeadline = false;
            }
            else if (string.Equals(
                displayName,
                CleanLeavesChore.ToyPickupChoreName,
                System.StringComparison.OrdinalIgnoreCase) &&
                toyPickupMiniGame != null &&
                toyPickupMiniGame.IsGameStarted)
            {
                int remainingToys = toyPickupMiniGame.RemainingToyCount;
                remainingSuffix = " · " + remainingToys + " "
                    + (remainingToys == 1 ? "toy" : "toys")
                    + " remaining";
                showDeadline = false;
            }
            else if (remainingCount > 1)
            {
                remainingSuffix = " (" + remainingCount + ")";
            }

            string pendingLine = "○ " + displayName + remainingSuffix;

            if (showDeadline)
                pendingLine += " · " + timeManager.FormatClockTime(nextDeadline);

            if (showDeadline && nextDeadline - timeManager.GetTime() <= nearDeadlineWarningSeconds)
                pendingLine = "<color=#" + ColorUtility.ToHtmlStringRGBA(nearDeadlineColor)
                    + ">" + pendingLine + "</color>";

            choreListText.text += pendingLine + "\n";
        }
        else if (completedCount == choreGroup.chores.Count)
        {
            string completedLine = "✓ " + displayName;
            int helperCharacterIndex = GetVisibleCharacterCount(choreListText.text)
                + completedLine.Length - 1;
            choreListText.text += "<color=" + CompletedChoreColor + ">✓ <s>"
                + displayName + "</s></color>\n";
            if (allCompletedByHelper)
                helperIndicatorCharacterIndices.Add(helperCharacterIndex);
        }
        else
        {
            choreListText.text += "<color=" + MissedChoreColor + ">X "
                + displayName + "</color>\n";
        }
    }

    private int GetVisibleCharacterCount(string text)
    {
        int visibleCharacterCount = 0;
        bool insideRichTextTag = false;

        foreach (char character in text)
        {
            if (character == '<')
                insideRichTextTag = true;
            else if (character == '>')
                insideRichTextTag = false;
            else if (!insideRichTextTag)
                visibleCharacterCount++;
        }

        return visibleCharacterCount;
    }

    private void UpdateHelperIndicators()
    {
        if (helperIndicatorCharacterIndices.Count == 0)
        {
            foreach (Image indicator in helperIndicatorImages)
                indicator.gameObject.SetActive(false);
            return;
        }

        if (helperSprite == null)
        {
            HelperNPC helper = FindFirstObjectByType<HelperNPC>(FindObjectsInactive.Include);
            SpriteRenderer helperRenderer = helper != null
                ? helper.GetComponentInChildren<SpriteRenderer>(true)
                : null;
            helperSprite = helperRenderer != null ? helperRenderer.sprite : null;
        }

        choreListText.ForceMeshUpdate();
        TMP_TextInfo textInfo = choreListText.textInfo;

        for (int i = 0; i < helperIndicatorCharacterIndices.Count; i++)
        {
            if (i >= helperIndicatorImages.Count)
                helperIndicatorImages.Add(CreateHelperIndicator());

            Image indicator = helperIndicatorImages[i];
            if (helperSprite == null)
            {
                indicator.gameObject.SetActive(false);
                continue;
            }

            int characterInfoIndex = -1;
            for (int j = 0; j < textInfo.characterCount; j++)
            {
                if (textInfo.characterInfo[j].index == helperIndicatorCharacterIndices[i])
                {
                    characterInfoIndex = j;
                    break;
                }
            }

            if (characterInfoIndex < 0)
            {
                indicator.gameObject.SetActive(false);
                continue;
            }

            TMP_CharacterInfo character = textInfo.characterInfo[characterInfoIndex];
            float iconSize = Mathf.Max(1f, choreListText.fontSize * 1.5f);
            RectTransform indicatorRect = indicator.rectTransform;
            indicatorRect.sizeDelta = new Vector2(iconSize, iconSize);
            indicatorRect.anchoredPosition = new Vector2(
                character.topRight.x + iconSize * 0.6f,
                (character.topRight.y + character.bottomRight.y) * 0.5f);
            indicator.sprite = helperSprite;
            indicator.gameObject.SetActive(true);
        }

        for (int i = helperIndicatorCharacterIndices.Count; i < helperIndicatorImages.Count; i++)
            helperIndicatorImages[i].gameObject.SetActive(false);
    }

    private Image CreateHelperIndicator()
    {
        GameObject indicatorObject = new GameObject(
            "Helper Chore Sprite",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        indicatorObject.layer = choreListText.gameObject.layer;
        indicatorObject.transform.SetParent(choreListText.transform, false);

        RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);

        Image indicator = indicatorObject.GetComponent<Image>();
        indicator.preserveAspect = true;
        indicator.raycastTarget = false;
        return indicator;
    }

    private void UpdateDailyStats()
    {
        if (choreManager == null)
            return;

        if (pointsText != null)
            pointsText.text = "Points: " + choreManager.totalPoints;

        if (successRateText != null)
            successRateText.text = "Success: " + choreManager.SuccessRate.ToString("0") + "%";
    }

    private void CreateCoinCounter()
    {
        if (currencyCanvas == null)
            currencyCanvas = FindFirstObjectByType<Canvas>();

        if (coinsText == null && currencyCanvas != null)
        {
            TMP_Text[] canvasTexts =
                currencyCanvas.GetComponentsInChildren<TMP_Text>(false);
            foreach (TMP_Text canvasText in canvasTexts)
            {
                if (canvasText.gameObject.name == "Coins")
                {
                    coinsText = canvasText;
                    break;
                }
            }
        }

        if (coinsText != null)
        {
            RectTransform textRect = coinsText.rectTransform;
            Image icon = CreateImage("Coin Icon", textRect, coinSprite);
            icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(22f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(36f, 36f);
            coinsText.alignment = TextAlignmentOptions.MidlineLeft;
            Vector4 margin = coinsText.margin;
            margin.x = 48f;
            coinsText.margin = margin;
            return;
        }

        if (currencyCanvas == null)
        {
            Debug.LogWarning(
                "Coin display could not be created because no Canvas was found.",
                this);
            return;
        }

        GameObject counterObject = new GameObject(
            "Coin Counter",
            typeof(RectTransform));
        counterObject.layer = currencyCanvas.gameObject.layer;
        RectTransform counterRect = counterObject.GetComponent<RectTransform>();
        counterRect.SetParent(currencyCanvas.transform, false);
        counterRect.anchorMin = new Vector2(0f, 1f);
        counterRect.anchorMax = new Vector2(0f, 1f);
        counterRect.pivot = new Vector2(0f, 1f);
        counterRect.anchoredPosition = new Vector2(24f, -24f);
        counterRect.sizeDelta = new Vector2(150f, 48f);

        Image counterIcon = CreateImage("Coin Icon", counterRect, coinSprite);
        counterIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        counterIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        counterIcon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
        counterIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);

        GameObject amountObject = new GameObject(
            "Coin Amount",
            typeof(RectTransform));
        amountObject.layer = currencyCanvas.gameObject.layer;
        RectTransform amountRect = amountObject.GetComponent<RectTransform>();
        amountRect.SetParent(counterRect, false);
        amountRect.anchorMin = new Vector2(0f, 0f);
        amountRect.anchorMax = new Vector2(1f, 1f);
        amountRect.offsetMin = new Vector2(48f, 0f);
        amountRect.offsetMax = Vector2.zero;
        coinsText = amountObject.AddComponent<TextMeshProUGUI>();
        coinsText.font = TMP_Settings.defaultFontAsset;
        coinsText.fontSize = 30f;
        coinsText.color = Color.white;
        coinsText.alignment = TextAlignmentOptions.MidlineLeft;
        coinsText.raycastTarget = false;
    }

    private void SetupHoverTooltips()
    {
        if (currencyCanvas == null)
            currencyCanvas = FindFirstObjectByType<Canvas>();

        if (currencyCanvas == null)
            return;

        CreateHoverTooltip();

        if (coinsText != null)
        {
            coinsText.raycastTarget = true;
            AddHoverTooltip(
                coinsText.gameObject,
                "Coins are used to buy household upgrades.");
        }

        MoodManager moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        if (moodManager != null && moodManager.MoodSlider != null)
        {
            AddHoverTooltip(
                moodManager.MoodSlider.gameObject,
                "Shows the household's mood. Completing chores raises it; missed chores lower it.");
        }
    }

    private void CreateHoverTooltip()
    {
        GameObject panelObject = new GameObject(
            "HUD Hover Tooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.layer = currencyCanvas.gameObject.layer;
        hoverTooltipRect = panelObject.GetComponent<RectTransform>();
        hoverTooltipRect.SetParent(currencyCanvas.transform, false);
        hoverTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        hoverTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        hoverTooltipRect.pivot = new Vector2(0.5f, 0.5f);
        hoverTooltipRect.sizeDelta = new Vector2(300f, 72f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Tooltip Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = currencyCanvas.gameObject.layer;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(hoverTooltipRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 6f);
        textRect.offsetMax = new Vector2(-10f, -6f);

        hoverTooltipText = textObject.GetComponent<TextMeshProUGUI>();
        hoverTooltipText.font = coinsText != null && coinsText.font != null
            ? coinsText.font
            : TMP_Settings.defaultFontAsset;
        hoverTooltipText.fontSize = 16f;
        hoverTooltipText.color = Color.white;
        hoverTooltipText.alignment = TextAlignmentOptions.Center;
        hoverTooltipText.enableWordWrapping = true;
        hoverTooltipText.raycastTarget = false;

        hoverTooltipPanel = panelObject;
        hoverTooltipPanel.SetActive(false);
    }

    private void AddHoverTooltip(GameObject target, string message)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        pointerEnter.callback.AddListener(eventData =>
            ShowHoverTooltip(message, (PointerEventData)eventData));
        trigger.triggers.Add(pointerEnter);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        pointerExit.callback.AddListener(_ => HideHoverTooltip());
        trigger.triggers.Add(pointerExit);
    }

    private void ShowHoverTooltip(string message, PointerEventData eventData)
    {
        if (hoverTooltipPanel == null || currencyCanvas == null)
            return;

        RectTransform canvasRect = currencyCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Camera eventCamera = currencyCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : currencyCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        hoverTooltipText.text = message;
        hoverTooltipRect.anchoredPosition = localPoint + new Vector2(0f, 48f);
        hoverTooltipRect.SetAsLastSibling();
        hoverTooltipPanel.SetActive(true);
    }

    private void HideHoverTooltip()
    {
        if (hoverTooltipPanel != null)
            hoverTooltipPanel.SetActive(false);
    }

    private Image CreateImage(string objectName, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void ResolveEconomyManager()
    {
        EconomyManager currentManager = EconomyManager.Instance != null
            ? EconomyManager.Instance
            : FindFirstObjectByType<EconomyManager>();
        if (currentManager == economyManager)
            return;

        if (economyManager != null)
            economyManager.CoinsEarned -= OnCoinsEarned;

        economyManager = currentManager;
        if (economyManager != null)
            economyManager.CoinsEarned += OnCoinsEarned;
    }

    private void UpdateCoinCounter()
    {
        if (coinsText == null || economyManager == null)
            return;

        coinsText.text = economyManager.Coins.ToString();
    }

    private void OnCoinsEarned(int amount)
    {
        if (amount <= 0)
            return;

        if (currencyCanvas == null)
        {
            Debug.LogWarning(
                "Coin reward popup could not be shown because no Canvas was found.",
                this);
            return;
        }

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Camera worldCamera = Camera.main;
        if (player == null || worldCamera == null)
        {
            Debug.LogWarning(
                "Coin reward popup could not be shown because the player or main camera was not found.",
                this);
            return;
        }

        GameObject popupObject = new GameObject(
            "Coin Reward Popup",
            typeof(RectTransform),
            typeof(CanvasGroup));
        popupObject.layer = currencyCanvas.gameObject.layer;
        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.SetParent(currencyCanvas.transform, false);
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(132f, 44f);

        Image popupIcon = CreateImage("Coin Icon", popupRect, coinSprite);
        popupIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        popupIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        popupIcon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
        popupIcon.rectTransform.sizeDelta = new Vector2(34f, 34f);

        GameObject amountObject = new GameObject(
            "Reward Amount",
            typeof(RectTransform));
        amountObject.layer = currencyCanvas.gameObject.layer;
        RectTransform amountRect = amountObject.GetComponent<RectTransform>();
        amountRect.SetParent(popupRect, false);
        amountRect.anchorMin = new Vector2(0f, 0f);
        amountRect.anchorMax = new Vector2(1f, 1f);
        amountRect.offsetMin = new Vector2(44f, 0f);
        amountRect.offsetMax = Vector2.zero;
        TMP_Text amountText = amountObject.AddComponent<TextMeshProUGUI>();
        amountText.font = coinsText.font;
        amountText.text = "+" + amount;
        amountText.fontSize = 30f;
        amountText.fontStyle = FontStyles.Bold;
        amountText.color = new Color(1f, 0.9f, 0.45f);
        amountText.alignment = TextAlignmentOptions.MidlineLeft;
        amountText.raycastTarget = false;

        coinPopups.Add(new CoinPopup
        {
            player = player,
            rect = popupRect,
            group = popupObject.GetComponent<CanvasGroup>(),
            icon = popupIcon
        });
    }

    private void UpdateCoinPopups()
    {
        Camera worldCamera = Camera.main;
        Camera canvasCamera = currencyCanvas != null &&
            currencyCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? currencyCanvas.worldCamera
            : null;
        RectTransform canvasRect = currencyCanvas != null
            ? currencyCanvas.transform as RectTransform
            : null;

        for (int index = coinPopups.Count - 1; index >= 0; index--)
        {
            CoinPopup popup = coinPopups[index];
            popup.elapsed += Time.unscaledDeltaTime;
            if (popup.elapsed >= 1.2f || popup.player == null ||
                worldCamera == null || canvasRect == null)
            {
                if (popup.rect != null)
                    Destroy(popup.rect.gameObject);
                coinPopups.RemoveAt(index);
                continue;
            }

            Vector3 headPosition = popup.player.transform.position + Vector3.up;
            SpriteRenderer playerSprite =
                popup.player.GetComponentInChildren<SpriteRenderer>();
            if (playerSprite != null)
                headPosition.y = playerSprite.bounds.max.y + 0.1f;

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(headPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition);
            float progress = popup.elapsed / 1.2f;
            popup.rect.anchoredPosition =
                localPosition + Vector2.up * (48f * progress);
            popup.group.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            float spin = Mathf.Max(
                0.12f,
                Mathf.Abs(Mathf.Cos(popup.elapsed * 12f)));
            popup.icon.rectTransform.localScale = new Vector3(spin, 1f, 1f);
        }
    }

    private void OnDestroy()
    {
        if (economyManager != null)
            economyManager.CoinsEarned -= OnCoinsEarned;
    }
}
