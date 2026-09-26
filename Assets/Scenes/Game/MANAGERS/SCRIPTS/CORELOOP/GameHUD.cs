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

    private TimeManager timeManager;
    private DayManager dayManager;
    private SweepingManager sweepingManager;
    private WaterSpawner waterSpawner;
    private ChoreManager choreManager;
    private RectTransform clockHand;

    private void Start()
    {
        timeManager = FindFirstObjectByType<TimeManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        sweepingManager = FindFirstObjectByType<SweepingManager>();
        waterSpawner = FindFirstObjectByType<WaterSpawner>();
        choreManager = FindFirstObjectByType<ChoreManager>();

        if (clockFaceSprite != null)
            CreateClockDisplay();

        if (GetComponent<PlayerInventoryUI>() == null)
            gameObject.AddComponent<PlayerInventoryUI>();
    }

    private void Update()
    {
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
        if (clockHand != null)
            clockHand.localRotation = Quaternion.Euler(
                0f,
                0f,
                timeManager.GetClockHandRotation());

        if (timeText != null)
            timeText.text = "Time: " + timeManager.GetClockTimeLabel();
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

        Image clockFace = clockObject.GetComponent<Image>();
        clockFace.sprite = clockFaceSprite;
        clockFace.preserveAspect = true;
        clockFace.raycastTarget = false;

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
        handObject.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.04f);
        handObject.GetComponent<Image>().raycastTarget = false;

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
        centerObject.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.03f);
        centerObject.GetComponent<Image>().raycastTarget = false;
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

        EconomyManager economyManager = EconomyManager.Instance != null
            ? EconomyManager.Instance
            : FindFirstObjectByType<EconomyManager>();
        if (coinsText != null && economyManager != null)
            coinsText.text = "Coins: " + economyManager.Coins;

        if (successRateText != null)
            successRateText.text = "Success: " + choreManager.SuccessRate.ToString("0") + "%";
    }
}
