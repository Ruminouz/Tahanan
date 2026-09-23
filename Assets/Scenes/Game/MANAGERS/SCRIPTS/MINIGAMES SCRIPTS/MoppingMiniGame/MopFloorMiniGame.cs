using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MoppingMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text feedbackText;
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
    [SerializeField] private float comboWindow = 1.5f;
    [SerializeField] private float comboDecay = 0.5f;
    [SerializeField] private float dragMomentumThreshold = 350f;
    [SerializeField] private float mopSwingAngle = 12f;
    [SerializeField] private float mopBobAmount = 4f;
    [SerializeField] private float mopBobSpeed = 14f;
    [Tooltip("Wet-area opacity at the start of the minigame.")]
    [SerializeField, Range(0f, 1f)] private float wetAreaStartAlpha = 0.65f;
    [Tooltip("Wet-area opacity when the floor is almost clean.")]
    [SerializeField, Range(0f, 1f)] private float wetAreaEndAlpha = 0.08f;

    private WetArea currentWetArea;
    private float progress;
    private float mopDuration;
    private bool isMopping;
    private bool mouseIsDown;
    private RectTransform activeMop;
    private Vector2 lastMousePosition;
    private float currentMomentum;
    private float currentStrokeDistance;
    private float comboTimer;
    private float comboMultiplier = 1f;
    private float rewardCooldown;
    private int currentScore;
    private int currentCombo;
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
        comboTimer = 0f;
        comboMultiplier = 1f;
        rewardCooldown = 0f;
        currentScore = 0;
        currentCombo = 0;
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

        UpdateScoreUI();
    }

    private void Update()
    {
        if (!isMopping)
            return;

        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                currentCombo = 0;
                comboMultiplier = 1f;
                UpdateScoreUI();
            }
        }

        rewardCooldown = Mathf.Max(0f, rewardCooldown - Time.deltaTime);
        HandleMouseInput();
    }

    public void StartMopping(WetArea wetArea)
    {
        if (wetArea == null)
            return;

        currentWetArea = wetArea;
        progress = 0f;
        isMopping = true;
        mouseIsDown = false;
        lastMousePosition = Vector2.zero;
        currentMomentum = 0f;
        currentStrokeDistance = 0f;
        comboTimer = 0f;
        comboMultiplier = 1f;
        rewardCooldown = 0f;
        currentScore = 0;
        currentCombo = 0;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;
        mopAnimationTime = 0f;

        ApplyMopVisual(GetCurrentMopLevel());
        ApplyDifficulty();

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

        UpdateScoreUI();
        ShowFeedback("Scrub back and forth!");
        Debug.Log("MOPPING MINIGAME STARTED!");
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
            comboMultiplier = Mathf.Max(1f, comboMultiplier - comboDecay * 0.25f);
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
            progress += speedBonus * comboMultiplier * upgradeBonus / mopDuration * Time.deltaTime;

            if (rewardCooldown <= 0f)
            {
                AwardComboScore();
                rewardCooldown = 0.2f;
            }
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

        bool directionChanged = expectedStrokeDirection == 0
            || currentStrokeDirection == expectedStrokeDirection;
        expectedStrokeDirection = currentStrokeDirection == 0
            ? expectedStrokeDirection
            : -currentStrokeDirection;
        currentStrokeDistance = 0f;
        comboMultiplier = directionChanged
            ? Mathf.Min(2.5f, comboMultiplier + Time.deltaTime * 0.8f)
            : Mathf.Max(1f, comboMultiplier - comboDecay);

        // Direction changes improve the combo, but are not required to clean.
        // This keeps slow, vertical, and controller-style movement playable.
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

    private void AwardComboScore()
    {
        currentCombo = comboTimer <= 0f ? 1 : currentCombo + 1;
        comboTimer = comboWindow;
        comboMultiplier = Mathf.Min(3f, comboMultiplier + 0.25f);
        currentScore += Mathf.RoundToInt(12f * comboMultiplier);
        UpdateScoreUI();
        ShowFeedback(currentCombo > 1 ? "Nice streak!" : "Good scrub!");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore;
        if (comboText != null)
            comboText.text = currentCombo > 1 ? "Combo x" + currentCombo : "Keep scrubbing!";
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

        if (suddenTaskManager != null)
            suddenTaskManager.CompleteMopTask();
        else
            Debug.LogWarning("SuddenTaskManager was not found.");

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        currentWetArea = null;
        Debug.Log("MOPPING COMPLETE! Score: " + currentScore);
    }
}
