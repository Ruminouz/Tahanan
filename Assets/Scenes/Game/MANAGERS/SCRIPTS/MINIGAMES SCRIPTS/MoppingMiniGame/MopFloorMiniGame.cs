using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MoppingMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMPro.TMP_Text feedbackText;
    [SerializeField] private Image moppingAreaImage;
    [Tooltip("Optional wet-floor sprite shown in the minigame. Recommended: a transparent PNG, 256 x 160 pixels, with the wet area centered.")]
    [SerializeField] private Sprite wetAreaSprite;
    [Tooltip("Recommended display size for the wet-area sprite in the minigame, in UI pixels.")]
    [SerializeField] private Vector2 wetAreaSpriteSize = new Vector2(256f, 160f);

    [Header("Mop")]
    [SerializeField] private RectTransform mop;
    [SerializeField] private RectTransform moppingArea;
    [SerializeField] private GameObject upgradedMopVisual;

    [Header("Cleaning")]
    [SerializeField] private float baseMopDuration = 8f;
    [SerializeField] private float minimumStrokeDistance = 18f;
    [SerializeField] private float minimumMopSpeed = 70f;
    [SerializeField] private float progressDecay = 0.12f;
    [SerializeField] private float dragMomentumThreshold = 350f;
    [SerializeField] private float mopSwingAngle = 12f;
    [SerializeField] private float mopBobAmount = 4f;
    [SerializeField] private float mopBobSpeed = 14f;
    [Tooltip("Wet-area opacity at the start of the minigame.")]
    [SerializeField, Range(0f, 1f)] private float wetAreaStartAlpha = 0.65f;
    [Tooltip("Wet-area opacity when the floor is almost clean.")]
    [SerializeField, Range(0f, 1f)] private float wetAreaEndAlpha = 0.08f;
    [Header("Speed Reward")]
    [SerializeField, Min(0)] private int baseSpeedRewardCoins = 2;
    [SerializeField, Min(0)] private int maximumSpeedBonusCoins = 8;

    private WetArea currentWetArea;
    private float progress;
    private float mopDuration;
    private bool isMopping;
    private bool mouseIsDown;
    private RectTransform activeMop;
    private Vector2 lastMousePosition;
    private float currentMomentum;
    private float currentStrokeDistance;
    private float moppingStartTime;
    private int currentStrokeDirection;
    private int expectedStrokeDirection;
    private float mopAnimationTime;
    private Quaternion mopBaseRotation;
    private Vector3 mopBaseScale = Vector3.one;

    private DayManager dayManager;
    private SuddenTaskManager suddenTaskManager;

    private void Awake()
    {
        if (moppingAreaImage == null && moppingArea != null)
            moppingAreaImage = moppingArea.GetComponent<Image>();

        ResetMopping();
    }

    private void Start()
    {
        dayManager = ResolveDayManager();
        suddenTaskManager = FindFirstObjectByType<SuddenTaskManager>();
    }

    private DayManager ResolveDayManager()
    {
        if (dayManager == null)
            dayManager = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();

        return dayManager;
    }

    public void ResetMopping()
    {
        RestoreMopTransform();
        currentWetArea = null;
        progress = 0f;
        mopDuration = baseMopDuration;
        isMopping = false;
        mouseIsDown = false;
        activeMop = null;
        lastMousePosition = Vector2.zero;
        currentMomentum = 0f;
        currentStrokeDistance = 0f;
        moppingStartTime = 0f;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;
        mopAnimationTime = 0f;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);
        if (progressBar != null)
            progressBar.value = 0f;
        if (mop != null)
            mop.gameObject.SetActive(false);
        if (upgradedMopVisual != null)
            upgradedMopVisual.SetActive(false);
        if (moppingAreaImage != null)
        {
            ApplyWetAreaSprite();
            ApplyWetAreaSpriteSize();
            SetAreaAlpha(wetAreaEndAlpha);
        }

    }

    private void Update()
    {
        if (!isMopping)
        {
            ClosePanelIfSessionIsInvalid();
            return;
        }

        if (currentWetArea == null || !currentWetArea.gameObject.activeInHierarchy)
        {
            ResetMopping();
            return;
        }

        HandleMouseInput();
    }

    public void StartMopping(WetArea wetArea)
    {
        if (wetArea == null || !wetArea.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Mopping minigame ignored an invalid or inactive wet area.");
            ResetMopping();
            return;
        }

        currentWetArea = wetArea;
        progress = 0f;
        isMopping = true;
        mouseIsDown = false;
        lastMousePosition = Vector2.zero;
        currentMomentum = 0f;
        currentStrokeDistance = 0f;
        moppingStartTime = Time.time;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;
        mopAnimationTime = 0f;

        ApplyMopVisual(GetCurrentMopLevel());
        ApplyDifficulty();

        if (activeMop == null || moppingArea == null || minigamePanel == null)
        {
            Debug.LogWarning(
                "Mopping minigame could not start because its panel, mop visual, or mopping area is not assigned."
            );
            ResetMopping();
            return;
        }

        if (minigamePanel != null)
            minigamePanel.SetActive(true);
        if (progressBar != null)
            progressBar.value = 0f;
        if (moppingAreaImage != null)
        {
            ApplyWetAreaSprite();
            ApplyWetAreaSpriteSize();
            UpdateWetAreaVisual();
        }

        ShowFeedback("Scrub back and forth!");
        Debug.Log("MOPPING MINIGAME STARTED!");
    }

    private void ClosePanelIfSessionIsInvalid()
    {
        if (minigamePanel != null && minigamePanel.activeSelf)
            minigamePanel.SetActive(false);

        if (mop != null && mop.gameObject.activeSelf)
            mop.gameObject.SetActive(false);

        if (upgradedMopVisual != null && upgradedMopVisual.activeSelf)
            upgradedMopVisual.SetActive(false);
    }

    private void ApplyDifficulty()
    {
        int difficulty = ResolveDayManager() != null ? ResolveDayManager().CurrentDifficulty : 0;
        float difficultyScale = Mathf.Max(0.75f, 1f - difficulty * 0.05f);
        float upgradeScale = EconomyManager.Instance != null
            ? EconomyManager.Instance.MopCleaningSpeedMultiplier
            : 1f;

        mopDuration = baseMopDuration / Mathf.Max(0.1f, difficultyScale * upgradeScale);
        Debug.Log("Mopping difficulty: " + difficulty + " | Duration: " + mopDuration);
    }

    private int GetCurrentMopLevel()
    {
        return EconomyManager.Instance != null && EconomyManager.Instance.HasUpgradedMop ? 1 : 0;
    }

    private void ApplyMopVisual(int level)
    {
        RestoreMopTransform();

        if (mop != null)
            mop.gameObject.SetActive(false);
        if (upgradedMopVisual != null)
            upgradedMopVisual.SetActive(false);

        GameObject selectedVisual = level == 1
            ? upgradedMopVisual
            : mop != null ? mop.gameObject : null;
        if (selectedVisual == null)
        {
            activeMop = null;
            return;
        }

        selectedVisual.SetActive(true);
        activeMop = selectedVisual.GetComponent<RectTransform>();
        if (activeMop != null)
        {
            mopBaseRotation = activeMop.localRotation;
            mopBaseScale = activeMop.localScale;
            activeMop.SetAsLastSibling();
        }
    }

    private void RestoreMopTransform()
    {
        if (activeMop == null)
            return;

        activeMop.localRotation = mopBaseRotation;
        activeMop.localScale = mopBaseScale;
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null || activeMop == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseIsDown = true;
            lastMousePosition = Mouse.current.position.ReadValue();
            currentStrokeDistance = 0f;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            mouseIsDown = false;
            currentStrokeDistance = 0f;
        }

        if (!mouseIsDown)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 mouseDelta = mousePosition - lastMousePosition;
        lastMousePosition = mousePosition;
        currentMomentum = mouseDelta.magnitude / Mathf.Max(Time.deltaTime, 0.016f);

        MoveMop(mousePosition);
        AnimateMop();

        bool insideArea = IsMopOverlappingMoppingArea();
        bool validStroke = UpdateStroke(mouseDelta);
        if (insideArea && validStroke && currentMomentum >= minimumMopSpeed)
        {
            float speedBonus = 1f + Mathf.Clamp01(currentMomentum / dragMomentumThreshold);
            float upgradeBonus = GetCurrentMopLevel() == 1 ? 1.2f : 1f;
            progress += speedBonus * upgradeBonus / mopDuration * Time.deltaTime;
        }
        else
        {
            progress = Mathf.Max(0f, progress - progressDecay * Time.deltaTime);
            ShowFeedback(insideArea ? "Scrub faster!" : "Move onto the wet spot!");
        }

        UpdateWetAreaVisual();

        if (progressBar != null)
            progressBar.value = progress;
        if (progress >= 1f)
            CompleteMopping();
    }

    private bool UpdateStroke(Vector2 mouseDelta)
    {
        currentStrokeDistance += mouseDelta.magnitude;
        if (Mathf.Abs(mouseDelta.x) >= Mathf.Abs(mouseDelta.y) && Mathf.Abs(mouseDelta.x) > 0.01f)
            currentStrokeDirection = mouseDelta.x > 0f ? 1 : -1;
        else if (Mathf.Abs(mouseDelta.y) > 0.01f)
            currentStrokeDirection = mouseDelta.y > 0f ? 1 : -1;

        if (currentStrokeDistance < minimumStrokeDistance)
            return false;

        expectedStrokeDirection = currentStrokeDirection == 0
            ? expectedStrokeDirection
            : -currentStrokeDirection;
        currentStrokeDistance = 0f;
        return true;
    }

    private void MoveMop(Vector2 mousePosition)
    {
        if (minigamePanel == null || activeMop == null)
            return;

        RectTransform panelRect = minigamePanel.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRect, mousePosition, null, out Vector2 localPosition);
        activeMop.localPosition = localPosition;
    }

    private void AnimateMop()
    {
        mopAnimationTime += Time.deltaTime;
        float swing = Mathf.Clamp(currentMomentum / dragMomentumThreshold, 0f, 1f) * mopSwingAngle;
        activeMop.localRotation = mopBaseRotation * Quaternion.Euler(0f, 0f, -swing);
        activeMop.localScale = mopBaseScale *
            (1f + Mathf.Sin(mopAnimationTime * mopBobSpeed) * mopBobAmount * 0.01f);
    }

    private bool IsMopOverlappingMoppingArea()
    {
        if (activeMop == null || moppingArea == null)
            return false;

        Rect mopRect = GetScreenRect(activeMop);
        Rect areaRect = GetScreenRect(moppingArea);
        return mopRect.Overlaps(areaRect, true);
    }

    private Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void SetAreaAlpha(float alpha)
    {
        Color color = moppingAreaImage.color;
        color.a = alpha;
        moppingAreaImage.color = color;
    }

    private void UpdateWetAreaVisual()
    {
        if (moppingAreaImage == null)
            return;

        float cleanAmount = Mathf.Clamp01(progress);
        float alpha = Mathf.Lerp(wetAreaStartAlpha, wetAreaEndAlpha, cleanAmount);
        SetAreaAlpha(alpha);
    }

    private void ApplyWetAreaSprite()
    {
        if (wetAreaSprite != null)
            moppingAreaImage.sprite = wetAreaSprite;
    }

    private void ApplyWetAreaSpriteSize()
    {
        if (wetAreaSpriteSize.x > 0f && wetAreaSpriteSize.y > 0f)
            moppingAreaImage.rectTransform.sizeDelta = wetAreaSpriteSize;
    }

    private void CompleteMopping()
    {
        isMopping = false;
        mouseIsDown = false;
        progress = 1f;

        if (progressBar != null)
            progressBar.value = 1f;
        ShowFeedback("Spotless!");

        if (currentWetArea != null)
            currentWetArea.Clean();

        float targetDuration = Mathf.Max(0.1f, mopDuration);
        float speedRatio = Mathf.Clamp01(
            (targetDuration - (Time.time - moppingStartTime)) / targetDuration);
        int rewardCoins = baseSpeedRewardCoins
            + Mathf.RoundToInt(maximumSpeedBonusCoins * speedRatio);
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AddCoins(rewardCoins);

        if (suddenTaskManager != null)
            suddenTaskManager.CompleteMopTask();
        else
            Debug.LogWarning("SuddenTaskManager was not found.");

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        currentWetArea = null;
        Debug.Log("MOPPING COMPLETE! +" + rewardCoins + " speed reward coins");
    }
}
