using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SweepingMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Image sweepingAreaImage;
    [SerializeField] private int dustParticleCount = 18;
    [SerializeField] private Sprite dustSprite;
    [SerializeField] private float comboWindow = 2.25f;

    [Header("Dust")]
    [SerializeField] private Vector2 dustParticleSizeMin = new Vector2(20f, 20f);
    [SerializeField] private Vector2 dustParticleSizeMax = new Vector2(38f, 38f);

    [Header("Broom")]
    [SerializeField] private RectTransform broom;
    [SerializeField] private RectTransform upgradedBroomVisual;
    [SerializeField] private RectTransform sweepingArea;
    [SerializeField] private float broomDisplayScale = 1f;
    [SerializeField] private float broomSwingAngle = 28f;
    [SerializeField] private float broomBobAmount = 3f;
    [SerializeField] private float broomBobSpeed = 14f;
    [SerializeField] private float broomMotionScale = 0.14f;
    [SerializeField] private Vector2 brushColliderOffset = new Vector2(0f, -19f);
    [SerializeField] private float brushColliderRadius = 23f;
    [SerializeField] private float minimumStrokeDistance = 22f;
    [SerializeField] private float minimumSweepSpeed = 80f;

    [Header("Cleaning")]
    [SerializeField] private float baseSweepDuration = 12f;
    [SerializeField] private float sweepBonusMultiplier = 1.75f;
    [SerializeField] private float dragMomentumThreshold = 400f;
    [SerializeField] private float comboDecay = 0.8f;
    [SerializeField] private float sweepAreaDrift = 40f;
    [SerializeField] private float sweepAreaDriftSpeed = 1.5f;
    [SerializeField] private Vector2 dustSpread = new Vector2(140f, 100f);

    private DustSpot currentDustSpot;
    private List<Image> dustVisuals = new List<Image>();
    private List<float> dustBrightness = new List<float>();

    private float progress;
    private bool isSweeping;
    private bool mouseIsDown;
    private RectTransform activeBroom;
    private Image activeAreaImage;
    private Vector2 lastMousePosition;
    private float currentMomentum;
    private float comboMultiplier = 1f;
    private float comboTimer;
    private float rewardCooldown;
    private int currentScore;
    private int currentCombo;
    private Vector2 baseSweepingAreaPosition;
    private float driftPhase;
    private Vector3 activeBroomBaseScale;
    private Quaternion activeBroomBaseRotation;
    private Vector2 broomTargetPosition;
    private float broomAnimationTime;
    private Vector3 normalBroomBaseScale = Vector3.one;
    private Vector3 upgradedBroomBaseScale = Vector3.one;
    private bool broomVisualDefaultsCached;
    private Vector3 lastBrushWorldPosition;
    private float currentStrokeDistance;
    private int currentStrokeDirection;
    private int expectedStrokeDirection;

    private const float UpgradeBroomBoost = 1.15f;

    private void Start()
    {
        CacheBroomVisualDefaults();
        ResetSweeping();
    }

    private void CacheBroomVisualDefaults()
    {
        if (broomVisualDefaultsCached)
            return;

        if (broom != null)
            normalBroomBaseScale = broom.localScale;

        if (upgradedBroomVisual != null)
            upgradedBroomBaseScale = upgradedBroomVisual.localScale;

        broomVisualDefaultsCached = true;
    }

    private void ResetSweeping()
    {
        currentDustSpot = null;
        progress = 0f;
        isSweeping = false;
        mouseIsDown = false;
        activeBroom = null;
        activeAreaImage = null;
        lastMousePosition = Vector2.zero;
        currentMomentum = 0f;
        comboMultiplier = 1f;
        comboTimer = 0f;
        rewardCooldown = 0f;
        currentScore = 0;
        currentCombo = 0;
        driftPhase = 0f;
        broomAnimationTime = 0f;
        lastBrushWorldPosition = Vector3.zero;
        currentStrokeDistance = 0f;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;

        ClearDustVisuals();

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (progressBar != null)
            progressBar.value = 0f;

        UpdateScoreUI();

        if (broom != null)
            broom.gameObject.SetActive(false);

        if (upgradedBroomVisual != null)
            upgradedBroomVisual.gameObject.SetActive(false);

        if (sweepingArea != null)
        {
            baseSweepingAreaPosition = sweepingArea.anchoredPosition;
            Image areaImage = sweepingArea.GetComponent<Image>();
            if (areaImage != null)
            {
                var color = areaImage.color;
                color.a = 0.2f;
                areaImage.color = color;
            }
        }
    }

    private void Update()
    {
        if (!isSweeping)
            return;

        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                comboTimer = 0f;
                currentCombo = 0;
                comboMultiplier = 1f;
                UpdateScoreUI();
            }
        }

        if (rewardCooldown > 0f)
            rewardCooldown -= Time.deltaTime;

        HandleMouseInput();
    }

    public void StartSweeping(DustSpot dustSpot)
    {
        if (dustSpot == null)
            return;

        currentDustSpot = dustSpot;
        progress = 0f;
        isSweeping = true;
        mouseIsDown = false;
        comboMultiplier = 1f;
        comboTimer = 0f;
        rewardCooldown = 0f;
        currentScore = 0;
        currentCombo = 0;
        currentMomentum = 0f;
        lastMousePosition = Vector2.zero;
        driftPhase = 0f;
        broomAnimationTime = 0f;
        currentStrokeDistance = 0f;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;

        ApplyBroomVisual(GetCurrentBroomLevel());
        CreateDustVisuals();
        EnsureBroomOnTop();
        lastBrushWorldPosition = GetBrushWorldPosition();
        activeAreaImage = sweepingArea != null ? sweepingArea.GetComponent<Image>() : null;

        if (sweepingArea != null)
            baseSweepingAreaPosition = sweepingArea.anchoredPosition;

        if (minigamePanel != null)
            minigamePanel.SetActive(true);

        if (progressBar != null)
            progressBar.value = 0f;

        UpdateScoreUI();

        if (activeAreaImage != null)
        {
            var color = activeAreaImage.color;
            color.a = 0.35f;
            activeAreaImage.color = color;
        }

        Debug.Log("SWEEPING STARTED");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore;

        if (comboText != null)
            comboText.text = currentCombo > 1 ? "Combo x" + currentCombo : "Clean streak";
    }

    private void AwardComboScore(int baseScore, string reason)
    {
        if (!isSweeping)
            return;

        if (comboTimer <= 0f)
            currentCombo = 1;
        else
            currentCombo++;

        comboTimer = comboWindow;
        comboMultiplier = Mathf.Min(3.25f, comboMultiplier + 0.25f);

        int awardedScore = Mathf.RoundToInt(baseScore * comboMultiplier);
        currentScore += awardedScore;

        if (!string.IsNullOrEmpty(reason))
            Debug.Log(reason + " | Combo x" + currentCombo + " | +" + awardedScore + " score");

        UpdateScoreUI();
    }

    private void ApplyDifficulty()
    {
        int difficulty = DayManager.Instance != null ? DayManager.Instance.CurrentDifficulty : 0;
        float difficultyScale = Mathf.Max(0.75f, 1f - (difficulty * 0.05f));

        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager != null)
            difficultyScale *= economyManager.BroomCleaningSpeedMultiplier;

        Debug.Log("Sweeping difficulty: " + difficulty + " | Sweep scale: " + difficultyScale);
    }

    private int GetCurrentBroomLevel()
    {
        return EconomyManager.Instance != null && EconomyManager.Instance.HasUpgradedBroom ? 1 : 0;
    }

    private void ApplyBroomVisual(int level)
    {
        if (broom != null)
            broom.gameObject.SetActive(false);

        if (upgradedBroomVisual != null)
            upgradedBroomVisual.gameObject.SetActive(false);

        GameObject selectedVisual = level == 1
            ? upgradedBroomVisual != null ? upgradedBroomVisual.gameObject : null
            : broom != null ? broom.gameObject : null;

        if (selectedVisual == null)
        {
            activeBroom = null;
            return;
        }

        selectedVisual.SetActive(true);
        RectTransform selectedRect = selectedVisual.GetComponent<RectTransform>();
        if (selectedRect != null)
        {
            activeBroom = selectedRect;
            activeBroomBaseScale = level == 1 ? upgradedBroomBaseScale : normalBroomBaseScale;
            activeBroomBaseRotation = selectedRect.localRotation;
            selectedRect.localScale = activeBroomBaseScale * broomDisplayScale;
            broomAnimationTime = 0f;
        }
    }

    private void CreateDustVisuals()
    {
        ClearDustVisuals();

        if (minigamePanel == null || sweepingArea == null)
            return;

        for (int i = 0; i < dustParticleCount; i++)
        {
            GameObject dustObject = new GameObject("DustChunk" + i, typeof(RectTransform), typeof(Image));
            RectTransform dustRect = dustObject.GetComponent<RectTransform>();
            Image dustImage = dustObject.GetComponent<Image>();

            dustRect.SetParent(sweepingArea, false);

            dustRect.sizeDelta = GetDustParticleSize();
            float randomRotation = Random.Range(0f, 360f);
            dustRect.localRotation = Quaternion.Euler(0f, 0f, randomRotation);
            dustRect.anchoredPosition = GetDustPositionInsideArea(dustRect.sizeDelta, randomRotation);

            dustImage.sprite = dustSprite;
            dustImage.color = new Color(0.74f, 0.56f, 0.2f, 1f);
            dustImage.raycastTarget = false;

            dustVisuals.Add(dustImage);
            dustBrightness.Add(Random.Range(0.6f, 1.1f));
        }
    }

    private Vector2 GetDustParticleSize()
    {
        Vector2 minSize = new Vector2(
            Mathf.Max(1f, dustParticleSizeMin.x),
            Mathf.Max(1f, dustParticleSizeMin.y));
        Vector2 maxSize = new Vector2(
            Mathf.Max(minSize.x, dustParticleSizeMax.x),
            Mathf.Max(minSize.y, dustParticleSizeMax.y));

        return new Vector2(
            Random.Range(minSize.x, maxSize.x),
            Random.Range(minSize.y, maxSize.y));
    }

    private Vector2 GetDustPositionInsideArea(Vector2 dustSize, float rotationDegrees)
    {
        Rect areaRect = sweepingArea.rect;
        float radians = rotationDegrees * Mathf.Deg2Rad;
        float halfWidth = (Mathf.Abs(Mathf.Cos(radians)) * dustSize.x
            + Mathf.Abs(Mathf.Sin(radians)) * dustSize.y) * 0.5f;
        float halfHeight = (Mathf.Abs(Mathf.Sin(radians)) * dustSize.x
            + Mathf.Abs(Mathf.Cos(radians)) * dustSize.y) * 0.5f;

        float minX = areaRect.xMin + halfWidth;
        float maxX = areaRect.xMax - halfWidth;
        float minY = areaRect.yMin + halfHeight;
        float maxY = areaRect.yMax - halfHeight;

        if (minX > maxX)
            minX = maxX = areaRect.center.x;

        if (minY > maxY)
            minY = maxY = areaRect.center.y;

        float spreadX = Mathf.Min(dustSpread.x, Mathf.Max(0f, (maxX - minX) * 0.5f));
        float spreadY = Mathf.Min(dustSpread.y, Mathf.Max(0f, (maxY - minY) * 0.5f));
        Vector2 center = areaRect.center;

        return new Vector2(
            Random.Range(center.x - spreadX, center.x + spreadX),
            Random.Range(center.y - spreadY, center.y + spreadY));
    }

    private void EnsureBroomOnTop()
    {
        if (activeBroom == null)
            return;

        activeBroom.SetAsLastSibling();
    }

    private void ClearDustVisuals()
    {
        for (int i = dustVisuals.Count - 1; i >= 0; i--)
        {
            if (dustVisuals[i] != null)
                Destroy(dustVisuals[i].gameObject);
        }

        dustVisuals.Clear();
        dustBrightness.Clear();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        bool pressThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
        bool releaseThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;

        if (pressThisFrame)
        {
            mouseIsDown = true;
            lastMousePosition = Mouse.current.position.ReadValue();
        }

        if (releaseThisFrame)
        {
            mouseIsDown = false;
            comboMultiplier = Mathf.Max(1f, comboMultiplier - 0.5f);
        }

        UpdateSweepAreaMotion();

        if (!mouseIsDown)
            return;

        MoveBroom();
        AnimateBroom();

        Vector3 currentBrushWorldPosition = GetBrushWorldPosition();
        if (pressThisFrame)
        {
            lastBrushWorldPosition = currentBrushWorldPosition;
            currentMomentum = 0f;
            return;
        }

        Vector3 brushDelta = currentBrushWorldPosition - lastBrushWorldPosition;
        float delta = brushDelta.magnitude;
        currentMomentum = delta / Mathf.Max(Time.deltaTime, 0.016f);
        lastBrushWorldPosition = currentBrushWorldPosition;

        bool insideArea = IsInsideArea();
        bool validSweepStroke = UpdateSweepStroke(brushDelta);
        float upgradeBonus = GetCurrentBroomLevel() == 1 ? UpgradeBroomBoost : 1f;
        float baseCleanRate = 1f / baseSweepDuration;
        float rewardMultiplier = insideArea && validSweepStroke
            ? 1f + Mathf.Clamp01(currentMomentum / dragMomentumThreshold) * sweepBonusMultiplier
            : 0.2f;

        if (insideArea && validSweepStroke && currentMomentum >= minimumSweepSpeed)
        {
            progress += baseCleanRate * rewardMultiplier * comboMultiplier * upgradeBonus * Time.deltaTime;
            comboMultiplier = Mathf.Min(2.25f, comboMultiplier + Time.deltaTime * 0.8f);

            if (rewardCooldown <= 0f)
            {
                AwardComboScore(Mathf.RoundToInt(18f * (1f + Mathf.Clamp01(currentMomentum / dragMomentumThreshold))), "Sweep streak!");
                rewardCooldown = 0.18f;
            }
        }
        else
        {
            progress = Mathf.Max(0f, progress - Time.deltaTime * 0.18f);
            comboMultiplier = Mathf.Max(1f, comboMultiplier - Time.deltaTime * comboDecay);
        }

        UpdateDustVisuals(insideArea && validSweepStroke && currentMomentum >= minimumSweepSpeed, rewardMultiplier);

        if (activeAreaImage != null)
        {
            float targetAlpha = insideArea ? 0.7f : 0.25f;
            Color color = activeAreaImage.color;
            color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * 8f);
            activeAreaImage.color = color;
        }

        if (progressBar != null)
            progressBar.value = progress;

        if (progress >= 1f)
        {
            CompleteSweeping();
        }
    }

    private void UpdateDustVisuals(bool insideArea, float rewardMultiplier)
    {
        if (dustVisuals.Count == 0 || activeBroom == null)
            return;

        Vector3 brushWorldPos = GetBrushWorldPosition();

        for (int i = 0; i < dustVisuals.Count; i++)
        {
            Image dustImage = dustVisuals[i];
            if (dustImage == null)
                continue;

            RectTransform dustRect = dustImage.GetComponent<RectTransform>();
            Vector3 dustWorldPos = dustRect.TransformPoint(dustRect.rect.center);
            float distance = Vector3.Distance(brushWorldPos, dustWorldPos);
            Color color = dustImage.color;

            bool nearBrush = distance < brushColliderRadius;
            if (insideArea && nearBrush)
            {
                float fade = Mathf.Max(0f, 1f - (distance / brushColliderRadius));
                float cleanAmount = (Time.deltaTime * (0.85f + rewardMultiplier * 0.55f)) * Mathf.Clamp01(fade + 0.5f);
                color.a = Mathf.Max(0f, color.a - cleanAmount);
                dustImage.color = color;
                dustImage.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, 1f - color.a);
            }
            else if (color.a < 1f)
            {
                color.a = Mathf.Min(1f, color.a + Time.deltaTime * 0.2f);
                dustImage.color = color;
            }
        }
    }

    private Vector3 GetBrushWorldPosition()
    {
        if (activeBroom == null)
            return Vector3.zero;

        return activeBroom.TransformPoint(brushColliderOffset);
    }

    private bool UpdateSweepStroke(Vector3 brushDelta)
    {
        float verticalDistance = Mathf.Abs(brushDelta.y);
        if (verticalDistance < 0.01f)
            return false;

        int direction = brushDelta.y > 0f ? 1 : -1;
        bool mostlyVertical = verticalDistance >= Mathf.Abs(brushDelta.x) * 0.65f;

        if (!mostlyVertical)
            return false;

        if (currentStrokeDirection != direction)
        {
            if (currentStrokeDirection != 0 && currentStrokeDistance < minimumStrokeDistance)
                return false;

            currentStrokeDirection = direction;
            currentStrokeDistance = 0f;
            expectedStrokeDirection = expectedStrokeDirection == 0
                ? direction
                : -expectedStrokeDirection;
        }

        currentStrokeDistance += verticalDistance;
        return direction == expectedStrokeDirection;
    }

    private void UpdateSweepAreaMotion()
    {
        if (sweepingArea == null)
            return;

        driftPhase += Time.deltaTime * sweepAreaDriftSpeed;
        Vector2 drift = new Vector2(
            Mathf.Sin(driftPhase) * sweepAreaDrift,
            Mathf.Cos(driftPhase * 1.25f) * sweepAreaDrift
        );

        sweepingArea.anchoredPosition = baseSweepingAreaPosition + drift;
    }

    private void MoveBroom()
    {
        if (activeBroom == null || minigamePanel == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minigamePanel.GetComponent<RectTransform>(),
            mousePosition,
            null,
            out Vector2 localPosition
        );

        broomTargetPosition = localPosition;
        activeBroom.localPosition = broomTargetPosition;
    }

    private void AnimateBroom()
    {
        if (activeBroom == null)
            return;

        broomAnimationTime += Time.deltaTime;
        float movement = Mathf.Clamp01(currentMomentum / 900f);
        float swing = Mathf.Sin(broomAnimationTime * broomBobSpeed) * broomSwingAngle * movement;
        float bob = Mathf.Sin(broomAnimationTime * broomBobSpeed * 1.35f) * broomBobAmount * movement;
        float squash = 1f + Mathf.Sin(broomAnimationTime * broomBobSpeed * 1.8f) * broomMotionScale * movement;

        activeBroom.localRotation = activeBroomBaseRotation * Quaternion.Euler(0f, 0f, swing);
        activeBroom.localScale = Vector3.Scale(
            activeBroomBaseScale * broomDisplayScale,
            new Vector3(1f + (squash - 1f) * 0.35f, squash, 1f));
        activeBroom.anchoredPosition = broomTargetPosition + Vector2.up * bob;
    }

    private bool IsInsideArea()
    {
        if (activeBroom == null || sweepingArea == null || Mouse.current == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            sweepingArea,
            Mouse.current.position.ReadValue(),
            null
        );
    }

    private void CompleteSweeping()
    {
        isSweeping = false;
        mouseIsDown = false;

        if (progressBar != null)
            progressBar.value = 1f;

        if (currentDustSpot != null)
            currentDustSpot.Clean();

        if (currentScore > 0)
        {
            int finishBonus = Mathf.RoundToInt(75f * Mathf.Max(1f, comboMultiplier));
            currentScore += finishBonus;
            UpdateScoreUI();
            Debug.Log("Perfect sweep! +" + finishBonus + " score");
        }

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (activeAreaImage != null)
        {
            Color color = activeAreaImage.color;
            color.a = 0.2f;
            activeAreaImage.color = color;
        }

        if (sweepingArea != null)
            sweepingArea.anchoredPosition = baseSweepingAreaPosition;

        ClearDustVisuals();
        currentDustSpot = null;
        activeBroom = null;
        activeAreaImage = null;

        Debug.Log("SWEEPING COMPLETE!");
    }
}