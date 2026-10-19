using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum HouseholdMood
{
    Calm,
    Concerned,
    Angry
}

public class MoodManager : MonoBehaviour
{
    public static MoodManager Instance { get; private set; }
    public event Action<HouseholdMood, HouseholdMood> MoodChanged;

    [Header("Mood")]
    [SerializeField] private Slider moodSlider;
    [SerializeField, Range(0f, 100f)] private float startingMood = 30f;
    [SerializeField, Range(0f, 100f)] private float concernedThreshold = 65f;
    [SerializeField, Range(0f, 100f)] private float angryThreshold = 35f;
    [SerializeField, Min(0f)] private float moodLossPerMissedChore = 20f;
    [SerializeField, Min(0f)] private float dailyMoodLoss = 20f;
    [SerializeField, Min(0f)] private float moodRecoveryPerCompletedChore = 5f;
    [SerializeField] private Sprite moodGainSprite;
    [SerializeField] private Sprite moodLossSprite;

    private float mood;
    private Canvas popupCanvas;
    private int activePopupCount;
    private bool warnedAboutPopupSetup;

    public float Mood => mood;
    public Slider MoodSlider => moodSlider;
    public HouseholdMood CurrentMood => mood <= angryThreshold
        ? HouseholdMood.Angry
        : mood <= concernedThreshold
            ? HouseholdMood.Concerned
            : HouseholdMood.Calm;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mood = Mathf.Clamp(startingMood, 0f, 100f);
        UpdateSlider();
    }

    private void OnEnable()
    {
        SubscribeToChoreManager();
    }

    private void Start()
    {
        SubscribeToChoreManager();
    }

    private void OnDisable()
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
        if (choreManager != null)
            choreManager.ChoreMissed -= HandleMissedChore;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void HandleCompletedChore()
    {
        SetMood(mood + moodRecoveryPerCompletedChore);
    }

    public void AddMood(float amount)
    {
        if (amount == 0f)
            return;

        SetMood(mood + amount);
    }

    public void ResetDailyMood()
    {
        SetMood(mood - dailyMoodLoss);
    }

    private void HandleMissedChore(Chore chore)
    {
        SetMood(mood - moodLossPerMissedChore);
    }

    private void SetMood(float value)
    {
        float previousValue = mood;
        HouseholdMood previousMood = CurrentMood;
        mood = Mathf.Clamp(value, 0f, 100f);
        UpdateSlider();

        float moodChange = mood - previousValue;
        if (!Mathf.Approximately(moodChange, 0f))
        {
            SoundEffectManager.Play(moodChange > 0f ? "MoodUp" : "MoodDown");
            ShowMoodChangePopup(moodChange);
        }

        HouseholdMood newMood = CurrentMood;
        if (newMood != previousMood)
        {
            MoodChanged?.Invoke(previousMood, newMood);
        }
    }

    private void ShowMoodChangePopup(float moodChange)
    {
        Sprite moodSprite = moodChange > 0f ? moodGainSprite : moodLossSprite;
        if (moodSprite == null)
        {
            if (!warnedAboutPopupSetup)
            {
                Debug.LogWarning(
                    "Mood change popup sprite is not assigned on MoodManager.",
                    this);
                warnedAboutPopupSetup = true;
            }

            return;
        }

        if (popupCanvas == null)
            popupCanvas = moodSlider != null
                ? moodSlider.GetComponentInParent<Canvas>()
                : FindFirstObjectByType<Canvas>();

        if (popupCanvas == null)
        {
            if (!warnedAboutPopupSetup)
            {
                Debug.LogWarning(
                    "Mood change popup cannot be shown because the scene has no Canvas.",
                    this);
                warnedAboutPopupSetup = true;
            }

            return;
        }

        GameObject popupObject = new GameObject(
            "Mood Change Popup",
            typeof(RectTransform),
            typeof(CanvasGroup));
        popupObject.layer = popupCanvas.gameObject.layer;
        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.SetParent(popupCanvas.transform, false);
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(180f, 52f);
        popupRect.anchoredPosition = GetPopupPosition() +
            Vector2.up * (activePopupCount * 52f);
        activePopupCount++;

        GameObject iconObject = new GameObject(
            "Mood Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.layer = popupCanvas.gameObject.layer;
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(popupRect, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(48f, 48f);

        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = moodSprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject valueObject = new GameObject(
            "Mood Value",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        valueObject.layer = popupCanvas.gameObject.layer;
        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.SetParent(popupRect, false);
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(56f, 0f);
        valueRect.offsetMax = Vector2.zero;

        TextMeshProUGUI valueText = valueObject.GetComponent<TextMeshProUGUI>();
        valueText.font = TMP_Settings.defaultFontAsset;
        valueText.fontSize = 32f;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = moodChange > 0f
            ? new Color(0.35f, 0.9f, 0.4f)
            : new Color(1f, 0.35f, 0.3f);
        valueText.alignment = TextAlignmentOptions.MidlineLeft;
        valueText.raycastTarget = false;
        valueText.text = (moodChange > 0f ? "+" : string.Empty) +
            moodChange.ToString("0.#");

        StartCoroutine(AnimateMoodPopup(
            popupObject,
            popupRect,
            popupObject.GetComponent<CanvasGroup>()));
    }

    private Vector2 GetPopupPosition()
    {
        if (moodSlider == null)
            return new Vector2(0f, 140f);

        RectTransform canvasRect = popupCanvas.transform as RectTransform;
        if (canvasRect == null)
            return new Vector2(0f, 140f);

        Camera eventCamera = popupCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : popupCanvas.worldCamera;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            moodSlider.transform.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                eventCamera,
                out Vector2 localPosition))
        {
            return localPosition + Vector2.up * 38f;
        }

        return new Vector2(0f, 140f);
    }

    private IEnumerator AnimateMoodPopup(
        GameObject popupObject,
        RectTransform popupRect,
        CanvasGroup popupGroup)
    {
        const float duration = 1.2f;
        Vector2 startPosition = popupRect.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * 42f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            popupRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                progress);
            popupGroup.alpha = 1f - progress;
            yield return null;
        }

        activePopupCount = Mathf.Max(0, activePopupCount - 1);
        Destroy(popupObject);
    }

    private void SubscribeToChoreManager()
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
        if (choreManager != null)
        {
            choreManager.ChoreMissed -= HandleMissedChore;
            choreManager.ChoreMissed += HandleMissedChore;
        }
    }

    private void UpdateSlider()
    {
        if (moodSlider != null)
            moodSlider.value = mood / 100f;
    }
}