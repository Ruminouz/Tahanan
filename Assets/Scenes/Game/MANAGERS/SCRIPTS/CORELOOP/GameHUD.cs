using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameHUD : MonoBehaviour
{
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

    private TimeManager timeManager;
    private DayManager dayManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;
    private ChoreManager choreManager;
    private EconomyManager economyManager;
    private RectTransform clockHand;
    private Image clockFaceImage;
    private Image clockHandImage;
    private Image clockCenterImage;
    private Canvas currencyCanvas;
    private readonly List<CoinPopup> coinPopups = new();

    private sealed class CoinPopup
    {
        public PlayerMovement player;
        public RectTransform rect;
        public CanvasGroup group;
        public Image icon;
        public float elapsed;
    }

    private void Start()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
        waterSpawner = FindFirstObjectByType<WaterSpawner>();
        choreManager = FindFirstObjectByType<ChoreManager>();
        currencyCanvas = coinsText != null
            ? coinsText.GetComponentInParent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        if (clockFaceSprite != null)
            CreateClockDisplay();

        CreateCoinCounter();
        ResolveEconomyManager();

        if (GetComponent<PlayerInventoryUI>() == null)
            gameObject.AddComponent<PlayerInventoryUI>();
    }

    private void Update()
    {
        ResolveEconomyManager();
        UpdateCoinCounter();
        UpdateCoinPopups();

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

        choreListText.text = "CHORES\n";
        Chore[] chores = dayManager.GetActiveChores();
        bool sweepListed = false;
        HashSet<Chore> listedChores = new HashSet<Chore>();

        if (chores != null)
        {
            foreach (Chore chore in chores)
            {
                if (chore == null)
                    continue;

                if (chore == dayManager.SweepDustChore)
                    sweepListed = true;

                if (listedChores.Add(chore))
                    AppendChore(chore);
            }
        }

        if (!sweepListed && dayManager.SweepDustChore != null &&
            listedChores.Add(dayManager.SweepDustChore))
            AppendChore(dayManager.SweepDustChore);

        if (!sweepListed && dayManager.SweepDustChore == null && sweepingManager != null)
        {
            string marker = sweepingManager.IsSweepingCompleted ? "✓ " : "○ ";
            string status = sweepingManager.IsSweepingCompleted
                ? string.Empty
                : " (" + sweepingManager.RemainingDust + " dust remaining, "
                    + timeManager.FormatClockTime(timeManager.DayLength) + ")";
            choreListText.text += marker + "Sweep Dust" + status + "\n";
        }

        if (waterSpawner == null || !waterSpawner.MopTaskStarted)
            return;

        if (waterSpawner.IsMoppingCompleted)
        {
            choreListText.text += "✓ Mop Floor\n";
        }
        else if (waterSpawner.IsMoppingMissed)
        {
            choreListText.text += "- Mop Floor (MISSED)\n";
        }
        else
        {
            choreListText.text += "○ Mop Floor ("
                + waterSpawner.RemainingWetAreas
                + " remaining, "
                + timeManager.FormatClockTime(timeManager.DayLength)
                + ")\n";
        }
    }

    private void AppendChore(Chore chore)
    {
        string marker = chore.IsCompleted ? "✓ " : chore.IsMissed ? "- " : "○ ";
        string status = chore.IsCompleted
            ? choreManager != null && choreManager.WasCompletedByHelper(chore)
                ? " (HELPER)"
                : string.Empty
            : chore.IsMissed
                ? " (MISSED)"
                : " (" + timeManager.FormatClockTime(chore.DeadlineTime) + ")";

        choreListText.text += marker + chore.ChoreName + status + "\n";
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
