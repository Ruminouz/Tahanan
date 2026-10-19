using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class SegregateWasteMiniGame : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform spawnArea;
    [SerializeField] private DropZone boxDropZone;
    [SerializeField] private DropZone[] dropZones;
    [FormerlySerializedAs("mistakesText")]
    [SerializeField] private TextMeshProUGUI toysRemainingText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private WasteSpawner spawner;
    [SerializeField] private ChoreTutorial tutorial;
    [SerializeField] private Sprite[] antWalkUpFrames = new Sprite[AntFramesPerDirection];
    [SerializeField] private Sprite[] antWalkDownFrames = new Sprite[AntFramesPerDirection];
    [SerializeField] private Sprite[] antWalkLeftFrames = new Sprite[AntFramesPerDirection];
    [SerializeField] private Sprite[] antWalkRightFrames = new Sprite[AntFramesPerDirection];

    private const int MaximumAntHits = 2;
    private const int AntFramesPerDirection = 8;
    private const float AntHitCooldown = 0.75f;
    private const float AntAnimationFrameDuration = 0.12f;
    private const float FirstAntSpawnDelay = 2.5f;
    private const float MinimumAntMoveSpeed = 38f;
    private const float MaximumAntMoveSpeed = 50f;
    private const float TutorialPulseSpeed = 4f;
    private const float TutorialPulseScale = 1.15f;

    private sealed class Ant
    {
        public RectTransform rectTransform;
        public Image image;
        public Vector2 direction;
        public float directionTimer;
        public float animationTimer;
        public int animationFrame;
        public bool touchingPlayer;
    }

    private Chore currentChore;
    private TutorialManager tutorialManager;
    private RectTransform antArea;
    private Camera eventCamera;

    private WasteObject[] spawnedWaste;
    private readonly HashSet<WasteObject> collectedToys = new HashSet<WasteObject>();
    private readonly List<Ant> ants = new List<Ant>();
    private int correctlySegregatedCount;
    private int totalItemsNeeded;
    private int maximumAntCount;
    private int antHitCount;
    private float antMoveSpeed;
    private float elapsedTime;
    private float antSpawnTimer;
    private float antSpawnInterval;
    private float antHitCooldown;
    private bool gameStarted;
    private bool firstTimeTutorialActive;
    private bool tutorialTextWarningLogged;
    private readonly List<Transform> tutorialPulseTargets = new List<Transform>();
    private readonly List<Vector3> tutorialPulseBaseScales = new List<Vector3>();

    public bool IsGameStarted => gameStarted;
    public int RemainingToyCount => gameStarted && spawnedWaste != null
        ? Mathf.Max(0, spawnedWaste.Length - collectedToys.Count)
        : 0;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        antArea = spawnArea as RectTransform;
        Canvas canvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
        eventCamera = canvas != null ? canvas.worldCamera : null;
        ValidateAntFrames();
    }

    public void StartGame(Chore chore)
    {
        if (chore == null)
            return;

        currentChore = chore;
        ResolveTutorialText();
        HideTutorialText();
        CleanupWaste();
        CleanupAnts();
        collectedToys.Clear();
        correctlySegregatedCount = 0;
        antHitCount = 0;
        antSpawnTimer = 0f;
        antHitCooldown = 0f;
        elapsedTime = 0f;
        gameStarted = false;

        MiniGameInteractionBlocker.RegisterPanel(panel);
        panel.SetActive(true);
        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<TutorialManager>();

        int currentDay = FindFirstObjectByType<DayManager>()?.CurrentDifficulty ?? 1;
        ConfigureDifficulty(currentDay);

        if (tutorialManager == null)
        {
            Debug.LogWarning(
                "Hindi maipapakita ang unang tutorial dahil walang TutorialManager.",
                this);
            firstTimeTutorialActive = false;
        }
        else
        {
            firstTimeTutorialActive = !tutorialManager.HasLearned(chore.ChoreName);
        }

        StartSegregation();
    }

    private void ConfigureDifficulty(int day)
    {
        int clampedDay = Mathf.Clamp(day, 1, 7);
        float difficultyProgress = (clampedDay - 1) / 6f;
        maximumAntCount = 1 + Mathf.FloorToInt(difficultyProgress * 3f);
        antSpawnInterval = Mathf.Lerp(8f, 4f, difficultyProgress);
        antMoveSpeed = Mathf.Lerp(MinimumAntMoveSpeed, MaximumAntMoveSpeed, difficultyProgress);

        switch (clampedDay)
        {
            case 1:
                totalItemsNeeded = 3;
                break;
            case 2:
                totalItemsNeeded = 5;
                break;
            case 3:
                totalItemsNeeded = 7;
                break;
            case 4:
                totalItemsNeeded = 9;
                break;
            case 5:
                totalItemsNeeded = 10;
                break;
            case 6:
                totalItemsNeeded = 12;
                break;
            case 7:
                totalItemsNeeded = 14;
                break;
            default:
                totalItemsNeeded = 3;
                break;
        }
    }

    private void StartSegregation()
    {
        if (!SpawnWaste())
        {
            firstTimeTutorialActive = false;
            HideTutorialText();
            panel.SetActive(false);
            currentChore = null;
            return;
        }

        gameStarted = true;
        elapsedTime = 0f;
        antSpawnTimer = 0f;
        UpdateUI();
        UpdateTutorialPrompt(false);

        Debug.Log("Toy collection game started!");
    }

    private bool SpawnWaste()
    {
        if (spawner == null)
        {
            Debug.LogError("WasteSpawner not assigned!");
            return false;
        }

        if (spawnArea == null)
        {
            Debug.LogError("Toy spawn area not assigned!");
            return false;
        }

        DropZone boxDropZone = GetBoxDropZone();
        if (boxDropZone == null)
        {
            Debug.LogError("Toy box drop zone not assigned!");
            return false;
        }

        spawnedWaste = spawner.SpawnWaste(totalItemsNeeded, spawnArea, boxDropZone.transform);

        int spawnedCount = 0;
        foreach (WasteObject waste in spawnedWaste)
        {
            if (waste != null)
            {
                waste.SetMiniGame(this);
                spawnedCount++;
            }
        }

        if (spawnedCount != totalItemsNeeded)
        {
            Debug.LogError($"Expected {totalItemsNeeded} toys, but only spawned {spawnedCount}.");
            CleanupWaste();
            return false;
        }

        return true;
    }

    private void Update()
    {
        UpdateTutorialPulse();

        if (!gameStarted)
            return;

        elapsedTime += Time.deltaTime;
        UpdateAntChallenge();
        if (!gameStarted)
            return;

        ReconcileCollectedToys();
        UpdateUI();
        CheckCompletion();
    }

    public bool TryCollectToy(WasteObject waste, DropZone dropZone)
    {
        if (!gameStarted ||
            waste == null ||
            waste.IsCollected ||
            collectedToys.Contains(waste) ||
            dropZone != GetBoxDropZone())
        {
            return false;
        }

        dropZone.StackToy(waste, correctlySegregatedCount);
        collectedToys.Add(waste);
        correctlySegregatedCount = collectedToys.Count;

        SoundEffectManager.Play("Toys");
        Debug.Log($"Toy collected! Progress: {correctlySegregatedCount}/{totalItemsNeeded}");

        UpdateUI();
        UpdateTutorialPrompt(true);
        CheckCompletion();
        return true;
    }

    public bool TryCollectToyAtScreenPosition(
        WasteObject waste,
        Vector2 screenPosition,
        Camera eventCamera)
    {
        DropZone box = GetBoxDropZone();
        RectTransform boxRect = box != null ? box.transform as RectTransform : null;

        if (boxRect == null ||
            !RectTransformUtility.RectangleContainsScreenPoint(boxRect, screenPosition, eventCamera))
        {
            return false;
        }

        return TryCollectToy(waste, box);
    }

    private DropZone GetBoxDropZone()
    {
        if (boxDropZone != null &&
            boxDropZone.isActiveAndEnabled &&
            boxDropZone.gameObject.activeInHierarchy)
        {
            return boxDropZone;
        }

        if (dropZones != null)
        {
            foreach (DropZone zone in dropZones)
            {
                if (zone != null && zone.isActiveAndEnabled && zone.gameObject.activeInHierarchy)
                    return zone;
            }
        }

        return null;
    }

    private void UpdateUI()
    {
        if (toysRemainingText != null)
            toysRemainingText.text =
                $"Mali: {antHitCount}/{MaximumAntHits}  Mga laruan: {totalItemsNeeded - correctlySegregatedCount}";

        if (progressText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            float seconds = elapsedTime - minutes * 60f;
            progressText.text =
                $"Nakuha: {correctlySegregatedCount}/{totalItemsNeeded}  |  Oras: {minutes:00}:{seconds:00.0}";
        }
    }

    private void UpdateTutorialPrompt(bool toyCollected)
    {
        if (!firstTimeTutorialActive)
            return;

        ResolveTutorialText();

        if (tutorialText != null)
        {
            int remaining = Mathf.Max(0, totalItemsNeeded - correctlySegregatedCount);
            tutorialText.gameObject.SetActive(true);
            tutorialText.text = toyCollected
                ? $"Ayos! {remaining} laruan pa. Iwasan ang mga langgam, 2 beses mong mahawakan to talo na!"
                : "Pulutin ang mga laruan at ilagay sa kahon!";
        }

        RestoreTutorialPulseTargets();

        DropZone box = GetBoxDropZone();
        if (box != null)
            AddTutorialPulseTarget(box.transform);

        if (spawnedWaste == null)
            return;

        foreach (WasteObject toy in spawnedWaste)
        {
            if (toy != null && !toy.IsCollected)
            {
                AddTutorialPulseTarget(toy.transform);
                break;
            }
        }
    }

    private void ResolveTutorialText()
    {
        if (tutorialText != null || panel == null)
            return;

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == "Tutorial Text")
            {
                tutorialText = text;
                break;
            }
        }

        if (tutorialText == null && !tutorialTextWarningLogged)
        {
            Debug.LogWarning("I-assign ang Tutorial Text ng toy minigame sa Inspector.", this);
            tutorialTextWarningLogged = true;
        }
    }

    private void AddTutorialPulseTarget(Transform target)
    {
        if (target == null || tutorialPulseTargets.Contains(target))
            return;

        tutorialPulseTargets.Add(target);
        tutorialPulseBaseScales.Add(target.localScale);
    }

    private void UpdateTutorialPulse()
    {
        if (!firstTimeTutorialActive)
            return;

        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * TutorialPulseSpeed) + 1f)
            * 0.5f * (TutorialPulseScale - 1f);
        for (int i = tutorialPulseTargets.Count - 1; i >= 0; i--)
        {
            Transform target = tutorialPulseTargets[i];
            if (target == null)
            {
                tutorialPulseTargets.RemoveAt(i);
                tutorialPulseBaseScales.RemoveAt(i);
                continue;
            }

            target.localScale = tutorialPulseBaseScales[i] * pulse;
        }
    }

    private void RestoreTutorialPulseTargets()
    {
        for (int i = 0; i < tutorialPulseTargets.Count; i++)
        {
            if (tutorialPulseTargets[i] != null)
                tutorialPulseTargets[i].localScale = tutorialPulseBaseScales[i];
        }

        tutorialPulseTargets.Clear();
        tutorialPulseBaseScales.Clear();
    }

    private void HideTutorialText()
    {
        RestoreTutorialPulseTargets();
        if (tutorialText == null)
            return;

        tutorialText.text = string.Empty;
        tutorialText.gameObject.SetActive(false);
    }

    private void CheckCompletion()
    {
        ReconcileCollectedToys();

        if (gameStarted &&
            spawnedWaste != null &&
            collectedToys.Count >= spawnedWaste.Length)
        {
            CompleteGame();
        }
    }

    private void ReconcileCollectedToys()
    {
        if (spawnedWaste == null)
            return;

        DropZone box = GetBoxDropZone();
        if (box == null)
            return;

        foreach (WasteObject toy in spawnedWaste)
        {
            if (toy != null &&
                toy.IsCollected &&
                toy.transform.IsChildOf(box.transform))
            {
                collectedToys.Add(toy);
            }
        }

        correctlySegregatedCount = collectedToys.Count;
    }

    private void CompleteGame()
    {
        if (!gameStarted)
            return;

        gameStarted = false;
        firstTimeTutorialActive = false;
        HideTutorialText();

        Debug.Log($"Toy collection complete in {elapsedTime:0.0} seconds!");

        panel.SetActive(false);
        CleanupWaste();
        CleanupAnts();

        if (currentChore != null)
        {
            if (tutorialManager != null)
                tutorialManager.MarkAsLearned(currentChore.ChoreName);

            DayManager dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
            if (dayManager != null)
            {
                dayManager.ResolveDailyAdditionalQuest(
                    currentChore,
                    elapsedTime <= dayManager.DailyQuestToyPickupTimeLimit);
            }

            Debug.Log($"Completing chore: {currentChore.ChoreName}");
            currentChore.Complete();
            SoundEffectManager.Play("ChoreFinished");
        }

        currentChore = null;
    }

    private void CleanupWaste()
    {
        if (spawnedWaste == null)
            return;

        foreach (WasteObject waste in spawnedWaste)
        {
            if (waste != null)
            {
                Destroy(waste.gameObject);
            }
        }

        spawnedWaste = null;
    }

    private void ValidateAntFrames()
    {
        ValidateAntFrames(antWalkUpFrames, "up");
        ValidateAntFrames(antWalkDownFrames, "down");
        ValidateAntFrames(antWalkLeftFrames, "left");
        ValidateAntFrames(antWalkRightFrames, "right");
    }

    private void ValidateAntFrames(Sprite[] frames, string direction)
    {
        if (frames == null || frames.Length != AntFramesPerDirection)
        {
            Debug.LogError($"Assign exactly {AntFramesPerDirection} ant walk-{direction} sprites.", this);
            return;
        }

        int missingFrames = 0;
        foreach (Sprite frame in frames)
        {
            if (frame == null)
                missingFrames++;
        }

        if (missingFrames > 0)
            Debug.LogError($"Assign all {AntFramesPerDirection} ant walk-{direction} sprites.", this);
    }

    private bool AreAntFramesAssigned()
    {
        return HasAntFrames(antWalkUpFrames) &&
            HasAntFrames(antWalkDownFrames) &&
            HasAntFrames(antWalkLeftFrames) &&
            HasAntFrames(antWalkRightFrames);
    }

    private bool HasAntFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length != AntFramesPerDirection)
            return false;

        foreach (Sprite frame in frames)
        {
            if (frame == null)
                return false;
        }

        return true;
    }

    private void UpdateAntChallenge()
    {
        if (antArea == null || !AreAntFramesAssigned())
            return;

        float deltaTime = Time.deltaTime;
        antSpawnTimer += deltaTime;
        antHitCooldown = Mathf.Max(0f, antHitCooldown - deltaTime);

        if (ants.Count < maximumAntCount &&
            antSpawnTimer >= (ants.Count == 0 ? FirstAntSpawnDelay : antSpawnInterval))
        {
            SpawnAnt();
            antSpawnTimer = 0f;
        }

        Vector2 pointerPosition = GetPointerPosition();
        foreach (Ant ant in ants)
        {
            ant.directionTimer -= deltaTime;
            ant.animationTimer += deltaTime;

            if (ant.directionTimer <= 0f)
                ChooseAntDirection(ant);

            ant.rectTransform.anchoredPosition += ant.direction * antMoveSpeed * deltaTime;
            if (KeepAntInBounds(ant))
                ChooseAntDirection(ant);

            if (ant.animationTimer >= AntAnimationFrameDuration)
            {
                ant.animationTimer %= AntAnimationFrameDuration;
                Sprite[] frames = GetAntFrames(ant.direction);
                ant.animationFrame = (ant.animationFrame + 1) % frames.Length;
                ant.image.sprite = frames[ant.animationFrame];
            }

            bool touchingPlayer = RectTransformUtility.RectangleContainsScreenPoint(
                ant.rectTransform,
                pointerPosition,
                eventCamera);

            if (touchingPlayer && !ant.touchingPlayer && antHitCooldown <= 0f)
                RegisterAntHit();

            ant.touchingPlayer = touchingPlayer;
            if (!gameStarted)
                return;
        }
    }

    private void SpawnAnt()
    {
        if (!AreAntFramesAssigned())
            return;

        GameObject antObject = new GameObject(
            "Walking Ant",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        antObject.transform.SetParent(antArea, false);

        Ant ant = new Ant
        {
            rectTransform = antObject.GetComponent<RectTransform>(),
            image = antObject.GetComponent<Image>(),
            directionTimer = Random.Range(1f, 2f)
        };

        ant.image.preserveAspect = true;
        ant.image.raycastTarget = false;
        ChooseAntDirection(ant);

        Rect bounds = antArea.rect;
        Vector2 halfSize = ant.rectTransform.sizeDelta * 0.5f;
        float minX = Mathf.Min(bounds.center.x, bounds.xMin + halfSize.x);
        float maxX = Mathf.Max(bounds.center.x, bounds.xMax - halfSize.x);
        float minY = Mathf.Min(bounds.center.y, bounds.yMin + halfSize.y);
        float maxY = Mathf.Max(bounds.center.y, bounds.yMax - halfSize.y);
        ant.rectTransform.anchoredPosition = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY));

        ants.Add(ant);
    }

    private void ChooseAntDirection(Ant ant)
    {
        int directionIndex = Random.Range(0, 4);
        ant.direction = directionIndex switch
        {
            0 => Vector2.left,
            1 => Vector2.right,
            2 => Vector2.up,
            _ => Vector2.down
        };
        ant.directionTimer = Random.Range(1f, 2.5f);
        ant.animationTimer = 0f;
        ant.animationFrame = 0;

        ant.rectTransform.sizeDelta = ant.direction.x != 0f
            ? new Vector2(70f, 50f)
            : new Vector2(50f, 70f);
        ant.image.sprite = GetAntFrames(ant.direction)[0];
    }

    private Sprite[] GetAntFrames(Vector2 direction)
    {
        if (direction.x < 0f)
            return antWalkLeftFrames;

        if (direction.x > 0f)
            return antWalkRightFrames;

        return direction.y > 0f ? antWalkUpFrames : antWalkDownFrames;
    }

    private bool KeepAntInBounds(Ant ant)
    {
        Rect bounds = antArea.rect;
        Vector2 halfSize = ant.rectTransform.sizeDelta * 0.5f;
        float minX = Mathf.Min(bounds.center.x, bounds.xMin + halfSize.x);
        float maxX = Mathf.Max(bounds.center.x, bounds.xMax - halfSize.x);
        float minY = Mathf.Min(bounds.center.y, bounds.yMin + halfSize.y);
        float maxY = Mathf.Max(bounds.center.y, bounds.yMax - halfSize.y);
        Vector2 position = ant.rectTransform.anchoredPosition;
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY));

        ant.rectTransform.anchoredPosition = clampedPosition;
        return position != clampedPosition;
    }

    private Vector2 GetPointerPosition()
    {
        Pointer pointer = Pointer.current;
        return pointer != null
            ? pointer.position.ReadValue()
            : Vector2.negativeInfinity;
    }

    private void RegisterAntHit()
    {
        if (!gameStarted)
            return;

        antHitCount++;
        antHitCooldown = AntHitCooldown;
        SoundEffectManager.Play("Ant");
        UpdateUI();
        Debug.Log($"Ant collision! Hits: {antHitCount}/{MaximumAntHits}", this);

        if (antHitCount >= MaximumAntHits)
            FailGame();
    }

    private void FailGame()
    {
        if (!gameStarted)
            return;

        gameStarted = false;
        firstTimeTutorialActive = false;
        HideTutorialText();
        Debug.Log("Toy collection game failed after two ant collisions.", this);
        panel.SetActive(false);
        CleanupWaste();
        CleanupAnts();

        if (currentChore != null)
        {
            DayManager dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
            if (dayManager != null)
                dayManager.ResolveDailyAdditionalQuest(currentChore, false);

            ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
            if (choreManager != null)
                choreManager.MissChore(currentChore);
        }

        currentChore = null;
    }

    private void CleanupAnts()
    {
        foreach (Ant ant in ants)
        {
            if (ant != null && ant.rectTransform != null)
                Destroy(ant.rectTransform.gameObject);
        }

        ants.Clear();
    }
}
