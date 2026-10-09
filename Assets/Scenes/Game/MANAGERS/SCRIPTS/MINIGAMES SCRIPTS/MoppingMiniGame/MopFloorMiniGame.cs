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
    [Tooltip("Optional sprites ordered from a full puddle to a completely transparent, mopped floor. Assign all eight frames.")]
    [SerializeField] private Sprite[] wetAreaFadeSprites = new Sprite[8];

    [Header("Mop")]
    [SerializeField] private RectTransform mop;
    [SerializeField] private RectTransform moppingArea;
    [SerializeField] private GameObject upgradedMopVisual;
    [Header("Mop Movement Sprites")]
    [Tooltip("Optional classic mop movement frames in playback order. Assign up to eight sprites.")]
    [SerializeField] private Sprite[] classicMopMovementSprites = new Sprite[8];
    [Tooltip("Optional upgraded mop movement frames in playback order. Assign up to eight sprites.")]
    [SerializeField] private Sprite[] upgradedMopMovementSprites = new Sprite[8];
    [SerializeField, Min(1f)] private float mopMovementFramesPerSecond = 12f;

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
    [Header("Speed Reward")]
    [SerializeField, Min(0)] private int baseSpeedRewardCoins = 2;
    [SerializeField, Min(0)] private int maximumSpeedBonusCoins = 8;

    private WetArea currentWetArea;
    private float progress;
    private float mopDuration;
    private bool isMopping;
    private bool mouseIsDown;
    private RectTransform activeMop;
    private Image activeMopImage;
    private Sprite activeMopIdleSprite;
    private Sprite[] activeMopMovementSprites;
    private Vector2 lastMousePosition;
    private float currentMomentum;
    private float currentStrokeDistance;
    private float moppingStartTime;
    private int currentStrokeDirection;
    private int expectedStrokeDirection;
    private float mopAnimationTime;
    private float mopMovementAnimationTime;
    private Quaternion mopBaseRotation;
    private Vector3 mopBaseScale = Vector3.one;
    private float lastMopSfxTime;
    private const float MopSfxCooldown = 0.7f;

    private DayManager dayManager;
    private SuddenTaskManager suddenTaskManager;

    private void Awake()
    {
        if (moppingArea != null)
        {
            Image areaImage = moppingArea.GetComponent<Image>();
            if (areaImage != null)
                moppingAreaImage = areaImage;
        }
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
        SetMopMovementSprite(false);
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
        mopMovementAnimationTime = 0f;
        lastMopSfxTime = -999f;
        activeMopImage = null;
        activeMopIdleSprite = null;
        activeMopMovementSprites = null;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);
        if (moppingArea != null)
            moppingArea.gameObject.SetActive(false);
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
        if (mop != null)
            mop.gameObject.SetActive(false);
        if (upgradedMopVisual != null)
            upgradedMopVisual.SetActive(false);
        if (moppingAreaImage != null)
        {
            ApplyWetAreaSprite();
            ApplyWetAreaSpriteSize();
            SetAreaAlpha(0f);
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
        mopMovementAnimationTime = 0f;
        lastMopSfxTime = -999f;

        ApplyMopVisual(GetCurrentMopLevel());
        ApplyDifficulty();

        if (activeMop == null || moppingArea == null || moppingAreaImage == null || minigamePanel == null)
        {
            Debug.LogWarning(
                "Mopping minigame could not start because its panel, mop visual, mopping area, or water image is not assigned."
            );
            ResetMopping();
            return;
        }

        if (minigamePanel != null)
            minigamePanel.SetActive(true);
        moppingArea.gameObject.SetActive(true);
        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
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
        SetMopMovementSprite(false);
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
        activeMopImage = selectedVisual.GetComponent<Image>();
        if (activeMopImage == null)
            activeMopImage = selectedVisual.GetComponentInChildren<Image>();
        activeMopIdleSprite = activeMopImage != null ? activeMopImage.sprite : null;
        activeMopMovementSprites = level == 1
            ? upgradedMopMovementSprites
            : classicMopMovementSprites;
        mopAnimationTime = 0f;
        mopMovementAnimationTime = 0f;
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
            SetMopMovementSprite(false);
        }

        if (!mouseIsDown)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 mouseDelta = mousePosition - lastMousePosition;
        lastMousePosition = mousePosition;
        currentMomentum = mouseDelta.magnitude / Mathf.Max(Time.deltaTime, 0.016f);

        MoveMop(mousePosition);
        AnimateMop(mouseDelta);

        bool insideArea = IsMopOverlappingMoppingArea();
        bool validStroke = UpdateStroke(mouseDelta);
        if (insideArea && validStroke && currentMomentum >= minimumMopSpeed)
        {
            float timeSinceLastMopSfx = Time.time - lastMopSfxTime;
            if (timeSinceLastMopSfx >= MopSfxCooldown)
            {
                SoundEffectManager.Play("Mop");
                lastMopSfxTime = Time.time;
            }

            float speedBonus = 1f + Mathf.Clamp01(currentMomentum / dragMomentumThreshold);
            float upgradeBonus = GetCurrentMopLevel() == 1 ? 1.2f : 1f;
            progress = Mathf.Clamp01(progress + speedBonus * upgradeBonus / mopDuration * Time.deltaTime);
        }
        else
        {
            progress = Mathf.Max(0f, progress - progressDecay * Time.deltaTime);
            ShowFeedback(insideArea ? "Scrub faster!" : "Move onto the wet spot!");
        }

        UpdateWetAreaVisual();

        if (IsWetAreaFullyFaded())
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

    private void AnimateMop(Vector2 mouseDelta)
    {
        mopAnimationTime += Time.deltaTime;
        float swing = Mathf.Clamp(currentMomentum / dragMomentumThreshold, 0f, 1f) * mopSwingAngle;
        activeMop.localRotation = mopBaseRotation * Quaternion.Euler(0f, 0f, -swing);
        activeMop.localScale = mopBaseScale *
            (1f + Mathf.Sin(mopAnimationTime * mopBobSpeed) * mopBobAmount * 0.01f);
        bool isMoving = mouseDelta.sqrMagnitude > 0.01f;
        if (isMoving)
            mopMovementAnimationTime += Time.deltaTime;

        SetMopMovementSprite(isMoving);
    }

    private void SetMopMovementSprite(bool isMoving)
    {
        if (activeMopImage == null || activeMopMovementSprites == null
            || activeMopMovementSprites.Length == 0)
            return;

        if (!isMoving)
        {
            activeMopImage.sprite = activeMopIdleSprite;
            return;
        }

        int frameCount = activeMopMovementSprites.Length;
        int firstFrame = Mathf.FloorToInt(
            mopMovementAnimationTime * mopMovementFramesPerSecond) % frameCount;
        for (int offset = 0; offset < frameCount; offset++)
        {
            Sprite movementSprite = activeMopMovementSprites[(firstFrame + offset) % frameCount];
            if (movementSprite == null)
                continue;

            activeMopImage.sprite = movementSprite;
            return;
        }

        activeMopImage.sprite = activeMopIdleSprite;
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
        ApplyWetAreaSprite(cleanAmount);
        float alpha = Mathf.Lerp(wetAreaStartAlpha, 0f, cleanAmount);
        SetAreaAlpha(alpha);
    }

    private void ApplyWetAreaSprite()
    {
        ApplyWetAreaSprite(progress);
    }

    private void ApplyWetAreaSprite(float cleanAmount)
    {
        if (moppingAreaImage == null)
            return;

        Sprite selectedSprite = wetAreaSprite;
        if (wetAreaFadeSprites != null && wetAreaFadeSprites.Length > 0)
        {
            float boundedCleanAmount = Mathf.Clamp01(cleanAmount);
            int lastFrameIndex = wetAreaFadeSprites.Length - 1;
            int frameIndex = lastFrameIndex == 0 || boundedCleanAmount >= 1f
                ? lastFrameIndex
                : Mathf.Min(
                    Mathf.FloorToInt(boundedCleanAmount * lastFrameIndex),
                    lastFrameIndex - 1);
            if (wetAreaFadeSprites[frameIndex] != null)
                selectedSprite = wetAreaFadeSprites[frameIndex];
        }

        if (selectedSprite != null)
            moppingAreaImage.sprite = selectedSprite;
    }

    private bool IsWetAreaFullyFaded()
    {
        return moppingAreaImage != null
            ? moppingAreaImage.color.a <= Mathf.Epsilon
            : progress >= 1f;
    }

    private void ApplyWetAreaSpriteSize()
    {
        if (wetAreaSpriteSize.x > 0f && wetAreaSpriteSize.y > 0f)
        {
            moppingAreaImage.rectTransform.localScale = Vector3.one;
            moppingAreaImage.rectTransform.sizeDelta = wetAreaSpriteSize;
        }
    }

    private void CompleteMopping()
    {
        isMopping = false;
        mouseIsDown = false;
        progress = 1f;
        UpdateWetAreaVisual();
        ShowFeedback("Spotless!");
        SoundEffectManager.Play("ChoreFinished");

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
