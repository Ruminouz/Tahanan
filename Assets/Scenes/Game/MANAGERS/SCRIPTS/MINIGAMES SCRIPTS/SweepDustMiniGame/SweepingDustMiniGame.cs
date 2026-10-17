using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SweepingMinigame : MonoBehaviour
{
    [Header("Mga Elemento ng UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;
    [Tooltip("Opsyonal: dito makikita kung gaano kabilis ka magwalis.")]
    [SerializeField] private TMP_Text speedGradeText;
    [SerializeField] private Image sweepingAreaImage;
    [SerializeField] private int dustParticleCount = 18;
    [SerializeField, Min(0)] private int additionalDustPerDay = 3;
    [SerializeField] private Sprite dustSprite;
    [Tooltip("Dito lilitaw ang mga tipak ng alikabok.")]
    [SerializeField] private RectTransform dustSpawnArea;

    [Header("Alikabok")]
    [Tooltip("Opsyonal na mga larawan mula makapal na alikabok hanggang malinis na. Ilagay dito ang pitong frame.")]
    [SerializeField] private Sprite[] dustFadeSprites = new Sprite[7];
    [SerializeField] private Vector2 dustParticleSizeMin = new Vector2(100f, 100f);
    [SerializeField] private Vector2 dustParticleSizeMax = new Vector2(150f, 150f);

    [Header("Panglinis")]
    [SerializeField] private RectTransform broom;
    [Tooltip("Itsura ng vacuum kapag nabili na ang upgrade ng walis.")]
    [SerializeField] private RectTransform upgradedBroomVisual;
    [SerializeField] private RectTransform sweepingArea;
    [SerializeField] private float broomDisplayScale = 1f;
    [SerializeField] private float broomSwingAngle = 28f;
    [SerializeField] private float broomBobAmount = 3f;
    [SerializeField] private float broomBobSpeed = 14f;
    [SerializeField] private float broomMotionScale = 0.14f;
    [SerializeField] private Sprite broomUpSprite;
    [SerializeField] private Sprite broomDownSprite;
    [SerializeField] private Sprite broomLeftSprite;
    [SerializeField] private Sprite broomRightSprite;
    [Tooltip("Walong sunod-sunod na frame ng animation para sa upgraded na vacuum.")]
    [SerializeField] private Sprite[] vacuumAnimationFrames = new Sprite[8];
    [SerializeField, Min(0.1f)] private float vacuumAnimationFramesPerSecond = 8f;
    [SerializeField] private Vector2 brushColliderOffset = new Vector2(0f, -28f);
    [SerializeField] private float brushColliderRadius = 52f;
    [SerializeField] private Vector2 vacuumNozzleOffset = new Vector2(0f, 35f);
    [SerializeField, Min(1f)] private float vacuumSuctionRadius = 180f;
    [SerializeField, Min(1f)] private float vacuumSuctionSpeed = 650f;
    [SerializeField, Min(0.1f)] private float vacuumSuctionFadeSpeed = 2.5f;
    [SerializeField] private float minimumStrokeDistance = 35f;
    [SerializeField] private float minimumSweepSpeed = 80f;

    [Header("Pagwawalis")]
    [SerializeField] private float sweepBonusMultiplier = 1.75f;
    [SerializeField] private float dragMomentumThreshold = 400f;
    [SerializeField] private float sweepAreaDrift = 40f;
    [SerializeField] private float sweepAreaDriftSpeed = 1.5f;
    [SerializeField] private Vector2 dustSpread = new Vector2(140f, 100f);
    [Header("Dagdag-Gantimpala sa Bilis")]
    [SerializeField, Min(0)] private int baseSpeedRewardCoins = 2;
    [SerializeField, Min(0)] private int maximumSpeedBonusCoins = 8;
    [SerializeField, Min(0.1f)] private float targetSweepDuration = 12f;

    private DustSpot currentDustSpot;
    private List<Image> dustVisuals = new List<Image>();
    private List<float> dustBrightness = new List<float>();

    private float progress;
    private bool isSweeping;
    private bool mouseIsDown;
    private RectTransform activeBroom;
    private Image activeBroomImage;
    private Vector2 lastMousePosition;
    private float currentMomentum;
    private float sweepStartTime;
    private Vector2 baseSweepingAreaPosition;
    private float driftPhase;
    private Vector3 activeBroomBaseScale;
    private Quaternion activeBroomBaseRotation;
    private Vector2 broomTargetPosition;
    private float broomAnimationTime;
    private Vector3 lastBrushWorldPosition;
    private float currentStrokeDistance;
    private int currentStrokeDirection;
    private int expectedStrokeDirection;
    private float lastSweepSfxTime;
    private const float SweepSfxCooldown = .8f;
    private const float DustCompletionAlphaThreshold = 0.1f;
    private bool isVacuumUpgrade;
    private SweepingManager sweepingManager;

    private void Start()
    {
        ResetSweeping();
    }

    private void ResetSweeping()
    {
        currentDustSpot = null;
        progress = 0f;
        isSweeping = false;
        mouseIsDown = false;
        activeBroom = null;
        lastMousePosition = Vector2.zero;
        currentMomentum = 0f;
        sweepStartTime = 0f;
        driftPhase = 0f;
        broomAnimationTime = 0f;
        lastBrushWorldPosition = Vector3.zero;
        currentStrokeDistance = 0f;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;
        lastSweepSfxTime = -999f;
        isVacuumUpgrade = false;

        ClearDustVisuals();

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (progressBar != null)
            progressBar.value = 0f;
        if (speedGradeText != null)
            speedGradeText.text = string.Empty;

        if (broom != null)
            broom.gameObject.SetActive(false);

        if (upgradedBroomVisual != null)
            upgradedBroomVisual.gameObject.SetActive(false);

        if (sweepingArea != null)
            baseSweepingAreaPosition = sweepingArea.anchoredPosition;
    }

    private void Update()
    {
        if (!isSweeping)
            return;

        HandleMouseInput();
    }

    public void StartSweeping(DustSpot dustSpot)
    {
        if (dustSpot == null || isSweeping)
            return;

        if (dustSpawnArea == null)
        {
            Debug.LogError("Hindi naka-assign kung saan lilitaw ang alikabok.", this);
            return;
        }

        if (sweepingManager == null)
            sweepingManager = FindFirstObjectByType<SweepingManager>();
        if (sweepingManager != null)
            sweepingManager.BeginDailyQuestAttempt();
        
        currentDustSpot = dustSpot;
        progress = 0f;
        isSweeping = true;
        mouseIsDown = false;
        sweepStartTime = Time.time;
        currentMomentum = 0f;
        lastMousePosition = Vector2.zero;
        driftPhase = 0f;
        broomAnimationTime = 0f;
        currentStrokeDistance = 0f;
        currentStrokeDirection = 0;
        expectedStrokeDirection = 0;
        lastSweepSfxTime = -999f;

        isVacuumUpgrade = HasVacuumUpgrade();
        ApplyCleaningToolVisual(isVacuumUpgrade);
        SetVacuumAnimationFrame(0);
        CreateDustVisuals();
        EnsureBroomOnTop();
        lastBrushWorldPosition = GetBrushWorldPosition();

        if (sweepingArea != null)
            baseSweepingAreaPosition = sweepingArea.anchoredPosition;

        if (minigamePanel != null)
        {
            MiniGameInteractionBlocker.RegisterPanel(minigamePanel);
            minigamePanel.SetActive(true);
        }

        if (progressBar != null)
            progressBar.value = 0f;
        if (speedGradeText != null)
            speedGradeText.text = "Game na!";

        Debug.Log("NAGSIMULA NA ANG PAGWAWALIS");
    }

    private void ApplyDifficulty()
    {
        int difficulty = DayManager.Instance != null ? DayManager.Instance.CurrentDifficulty : 0;
        float difficultyScale = Mathf.Max(0.75f, 1f - (difficulty * 0.05f));

        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager != null)
            difficultyScale *= economyManager.BroomCleaningSpeedMultiplier;

        Debug.Log("Hirap ng pagwawalis: " + difficulty + " | Bilis ng walis: " + difficultyScale);
    }

    private bool HasVacuumUpgrade()
    {
        return EconomyManager.Instance != null && EconomyManager.Instance.HasUpgradedBroom;
    }

    private void ApplyCleaningToolVisual(bool useVacuum)
    {
        if (broom != null)
            broom.gameObject.SetActive(false);

        if (upgradedBroomVisual != null)
            upgradedBroomVisual.gameObject.SetActive(false);

        GameObject selectedVisual = useVacuum
            ? upgradedBroomVisual != null ? upgradedBroomVisual.gameObject : null
            : broom != null ? broom.gameObject : null;

        if (selectedVisual == null)
        {
            activeBroom = null;
            activeBroomImage = null;
            return;
        }

        selectedVisual.SetActive(true);
        RectTransform selectedRect = selectedVisual.GetComponent<RectTransform>();
        if (selectedRect != null)
        {
            activeBroom = selectedRect;
            activeBroomImage = selectedRect.GetComponent<Image>();
            if (activeBroomImage == null)
                activeBroomImage = selectedRect.GetComponentInChildren<Image>();
            activeBroomBaseScale = Vector3.one;
            activeBroomBaseRotation = selectedRect.localRotation;
            selectedRect.localScale = activeBroomBaseScale * broomDisplayScale;
            broomAnimationTime = 0f;
        }
    }

    private void CreateDustVisuals()
    {
        ClearDustVisuals();

        if (dustSpawnArea == null)
            return;

        int day = DayManager.Instance != null
            ? DayManager.Instance.CurrentDifficulty
            : 1;
        int dustCount = dustParticleCount + (day - 1) * additionalDustPerDay;

        for (int i = 0; i < dustCount; i++)
        {
            GameObject dustObject = new GameObject("DustChunk" + i, typeof(RectTransform), typeof(Image));
            RectTransform dustRect = dustObject.GetComponent<RectTransform>();
            Image dustImage = dustObject.GetComponent<Image>();

            dustRect.SetParent(dustSpawnArea, false);

            dustRect.sizeDelta = GetDustParticleSize();
            float randomRotation = Random.Range(0f, 360f);
            dustRect.localRotation = Quaternion.Euler(0f, 0f, randomRotation);
            dustRect.anchoredPosition = GetDustPositionInsideSpawnArea(dustRect.sizeDelta, randomRotation);

            dustImage.sprite = GetDustFadeSprite(1f);
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

    private Vector2 GetDustPositionInsideSpawnArea(Vector2 dustSize, float rotationDegrees)
    {
        Rect areaRect = dustSpawnArea.rect;
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
        UpdateSpeedGrade(insideArea);
        bool validSweepStroke = isVacuumUpgrade
            ? brushDelta.sqrMagnitude > 0.01f
            : UpdateSweepStroke(brushDelta);

        if (insideArea && validSweepStroke && currentMomentum >= minimumSweepSpeed)
        {
            float timeSinceLastSweepSfx = Time.time - lastSweepSfxTime;
            if (timeSinceLastSweepSfx >= SweepSfxCooldown)
            {
                SoundEffectManager.Play(isVacuumUpgrade ? "Vacuum" : "Sweep");
                lastSweepSfxTime = Time.time;
            }
        }

        float rewardMultiplier = insideArea && validSweepStroke
            ? 1f + Mathf.Clamp01(currentMomentum / dragMomentumThreshold) * sweepBonusMultiplier
            : 0.2f;

        UpdateDustVisuals(insideArea && validSweepStroke && currentMomentum >= minimumSweepSpeed, rewardMultiplier);

        if (UpdateProgressFromDust())
        {
            CompleteSweeping();
        }
    }

    private void UpdateDustVisuals(bool insideArea, float rewardMultiplier)
    {
        if (!insideArea || dustVisuals.Count == 0 || activeBroom == null)
            return;

        Vector3 brushWorldPos = GetBrushWorldPosition();

        if (isVacuumUpgrade)
        {
            UpdateVacuumDustVisuals(brushWorldPos, rewardMultiplier);
            return;
        }

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
                color.a = SnapDustAlphaToCompletion(color.a - cleanAmount);
                dustImage.color = color;
                dustImage.sprite = GetDustFadeSprite(color.a);
                dustImage.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, 1f - color.a);
            }
        }
    }

    private void UpdateVacuumDustVisuals(Vector3 nozzleWorldPosition, float rewardMultiplier)
    {
        float suctionRadius = Mathf.Max(1f, vacuumSuctionRadius);
        float suctionSpeed = Mathf.Max(1f, vacuumSuctionSpeed);

        for (int i = 0; i < dustVisuals.Count; i++)
        {
            Image dustImage = dustVisuals[i];
            if (dustImage == null)
                continue;

            RectTransform dustRect = dustImage.GetComponent<RectTransform>();
            float distance = Vector3.Distance(dustRect.position, nozzleWorldPosition);
            if (distance > suctionRadius)
                continue;

            float suctionStrength = 1f - Mathf.Clamp01(distance / suctionRadius);
            float speed = suctionSpeed * (0.35f + suctionStrength) * Mathf.Max(1f, rewardMultiplier);
            dustRect.position = Vector3.MoveTowards(
                dustRect.position,
                nozzleWorldPosition,
                speed * Time.deltaTime);

            Color color = dustImage.color;
            color.a = Mathf.MoveTowards(
                color.a,
                0f,
                Mathf.Max(0.1f, vacuumSuctionFadeSpeed)
                * (0.5f + suctionStrength)
                * Time.deltaTime);
            color.a = SnapDustAlphaToCompletion(color.a);
            dustImage.color = color;
            dustImage.sprite = GetDustFadeSprite(color.a);
            dustRect.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, color.a);
        }
    }

    private float SnapDustAlphaToCompletion(float alpha)
    {
        return alpha <= DustCompletionAlphaThreshold ? 0f : alpha;
    }

    private bool UpdateProgressFromDust()
    {
        int activeDustCount = 0;
        float remainingDust = 0f;
        bool allDustFaded = true;

        for (int i = 0; i < dustVisuals.Count; i++)
        {
            Image dustImage = dustVisuals[i];
            if (dustImage == null)
                continue;

            activeDustCount++;
            Color color = dustImage.color;
            color.a = SnapDustAlphaToCompletion(Mathf.Clamp01(color.a));
            if (color.a != dustImage.color.a)
            {
                dustImage.color = color;
                dustImage.sprite = GetDustFadeSprite(color.a);
            }

            float alpha = color.a;
            remainingDust += alpha;
            if (alpha > 0f)
                allDustFaded = false;
        }

        progress = activeDustCount > 0
            ? 1f - remainingDust / activeDustCount
            : 0f;

        if (progressBar != null)
            progressBar.value = progress;

        return activeDustCount > 0 && allDustFaded;
    }

    private Sprite GetDustFadeSprite(float alpha)
    {
        if (dustFadeSprites == null || dustFadeSprites.Length == 0)
            return dustSprite;

        int frameIndex = Mathf.RoundToInt((1f - Mathf.Clamp01(alpha)) * (dustFadeSprites.Length - 1));
        Sprite fadeSprite = dustFadeSprites[frameIndex];
        return fadeSprite != null ? fadeSprite : dustSprite;
    }

    private Vector3 GetBrushWorldPosition()
    {
        if (activeBroom == null)
            return Vector3.zero;

        return activeBroom.TransformPoint(isVacuumUpgrade ? vacuumNozzleOffset : brushColliderOffset);
    }

    private bool UpdateSweepStroke(Vector3 brushDelta)
    {
        UpdateBroomSprite(brushDelta);
      

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
        return true;
    }

    private void UpdateBroomSprite(Vector3 brushDelta)
    {
        if (isVacuumUpgrade || activeBroomImage == null || brushDelta.sqrMagnitude < 0.01f)
            return;

        Sprite directionalSprite;
        if (Mathf.Abs(brushDelta.x) > Mathf.Abs(brushDelta.y))
            directionalSprite = brushDelta.x > 0f ? broomRightSprite : broomLeftSprite;
        else
            directionalSprite = brushDelta.y > 0f ? broomUpSprite : broomDownSprite;

        if (directionalSprite != null)
            activeBroomImage.sprite = directionalSprite;
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
        if (isVacuumUpgrade)
        {
            int frameCount = vacuumAnimationFrames != null ? vacuumAnimationFrames.Length : 0;
            if (frameCount > 0)
            {
                int frameIndex = Mathf.FloorToInt(
                    broomAnimationTime * vacuumAnimationFramesPerSecond) % frameCount;
                SetVacuumAnimationFrame(frameIndex);
            }

            float vibrationTime = broomAnimationTime * Mathf.Max(10f, broomBobSpeed);
            float vibration = Mathf.Sin(vibrationTime * 2f) * 2f * movement;
            activeBroom.localRotation = activeBroomBaseRotation * Quaternion.Euler(0f, 0f, vibration);
            activeBroom.localScale = activeBroomBaseScale * broomDisplayScale
                * (1f + Mathf.Sin(vibrationTime * 3f) * 0.015f * movement);
            activeBroom.anchoredPosition = broomTargetPosition
                + Vector2.up * Mathf.Sin(vibrationTime) * broomBobAmount * movement;
            return;
        }

        float swing = Mathf.Sin(broomAnimationTime * broomBobSpeed) * broomSwingAngle * movement;
        float bob = Mathf.Sin(broomAnimationTime * broomBobSpeed * 1.35f) * broomBobAmount * movement;
        float squash = 1f + Mathf.Sin(broomAnimationTime * broomBobSpeed * 1.8f) * broomMotionScale * movement;

        activeBroom.localRotation = activeBroomBaseRotation * Quaternion.Euler(0f, 0f, swing);
        activeBroom.localScale = Vector3.Scale(
            activeBroomBaseScale * broomDisplayScale,
            new Vector3(1f + (squash - 1f) * 0.35f, squash, 1f));
        activeBroom.anchoredPosition = broomTargetPosition + Vector2.up * bob;
    }

    private void SetVacuumAnimationFrame(int frameIndex)
    {
        if (!isVacuumUpgrade || activeBroomImage == null
            || vacuumAnimationFrames == null || vacuumAnimationFrames.Length == 0)
            return;

        Sprite frame = vacuumAnimationFrames[frameIndex % vacuumAnimationFrames.Length];
        if (frame != null)
            activeBroomImage.sprite = frame;
    }

    private void UpdateSpeedGrade(bool insideArea)
    {
        if (speedGradeText == null)
            return;

        if (isVacuumUpgrade && insideArea && currentMomentum >= minimumSweepSpeed)
        {
            speedGradeText.text = "Higop, alikabok!";
            return;
        }

        if (currentMomentum < minimumSweepSpeed)
        {
            speedGradeText.text = "Ang bagal naman!";
            return;
        }

        float speedRatio = currentMomentum / Mathf.Max(1f, dragMomentumThreshold);
        if (speedRatio < 0.5f)
            speedGradeText.text = "Bilis-bilisan pa!";
        else if (speedRatio < 0.8f)
            speedGradeText.text = "Ayan, bumibilis!";
        else if (speedRatio < 1.15f)
            speedGradeText.text = "Ang bilis na!";
        else
            speedGradeText.text = "Walang alikabok ang makakatakas!";
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
        SoundEffectManager.Play("ChoreFinished");

        if (progressBar != null)
            progressBar.value = 1f;

        if (currentDustSpot != null)
            currentDustSpot.Clean();

        int rewardCoins = CalculateSpeedRewardCoins(Time.time - sweepStartTime, targetSweepDuration);
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.AddCoins(rewardCoins);

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (sweepingArea != null)
            sweepingArea.anchoredPosition = baseSweepingAreaPosition;

        ClearDustVisuals();
        currentDustSpot = null;
        activeBroom = null;
        activeBroomImage = null;

        Debug.Log("TAPOS NA MAGWALIS! +" + rewardCoins + " barya bilang dagdag-gantimpala sa bilis");
    }

    private int CalculateSpeedRewardCoins(float elapsedSeconds, float targetSeconds)
    {
        float speedRatio = Mathf.Clamp01((targetSeconds - elapsedSeconds) / targetSeconds);
        return baseSpeedRewardCoins + Mathf.RoundToInt(maximumSpeedBonusCoins * speedRatio);
    }
}