using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class PikoMiniGame : MonoBehaviour, IInteractable
{
    [SerializeField] private TMP_FontAsset daydreamFont;

    private static Sprite pamatoSprite;
    private const float AteWinChance = 0.6f;
    private const float HopDuration = 0.28f;
    private const float HopHeight = 0.22f;
    private const float TightCameraSize = 2.45f;
    private const float MatchActorScale = 0.55f;
    private const float ContestantSideOffset = 0.07f;
    private const float TransitionFadeDuration = 0.4f;
    private const int DailyPlayLimit = 2;

    private struct SpriteSortingState
    {
        public SpriteRenderer renderer;
        public int sortingLayerId;
        public int sortingOrder;
    }

    private static readonly float[] SquareHeights =
    {
        0.09f, 0.21f, 0.33f, 0.45f, 0.45f,
        0.57f, 0.69f, 0.69f, 0.81f, 0.93f
    };

    private SpriteRenderer boardRenderer;
    private GameObject sessionBlocker;
    private GameObject resultCanvasObject;
    private GameObject transitionCanvasObject;
    private CanvasGroup transitionFade;
    private TMP_Text statusText;
    private Button continueButton;
    private Image continueButtonBackground;
    private PlayerMovement player;
    private NPCSocialParticipant ate;
    private Transform playerTransform;
    private Transform ateTransform;
    private Rigidbody2D playerBody;
    private Animator playerAnimator;
    private Animator ateAnimator;
    private WaypointMover ateWaypointMover;
    private BunsoAI ateAi;
    private NPC ateNpc;
    private TimeManager timeManager;
    private DayManager dayManager;
    private CinemachineCamera cameraRig;
    private CinemachineConfiner2D cameraConfiner;
    private GameObject playerPamato;
    private GameObject atePamato;
    private Transform previousCameraFollow;
    private Vector3 previousCameraPosition;
    private float previousCameraSize;
    private float previousTimeScale;
    private float previousPlayerAnimatorSpeed;
    private float previousAteAnimatorSpeed;
    private AnimatorUpdateMode previousPlayerAnimatorUpdateMode;
    private AnimatorUpdateMode previousAteAnimatorUpdateMode;
    private Vector3 previousPlayerScale;
    private Vector3 previousAteScale;
    private SpriteSortingState[] playerSortingStates;
    private SpriteSortingState[] ateSortingStates;
    private Vector3 playerStartPosition;
    private Vector3 ateStartPosition;
    private bool previousDayTimePaused;
    private bool previousPlayerMovementInputEnabled;
    private bool previousConfinerEnabled;
    private bool previousPlayerBodySimulated;
    private bool previousPlayerAnimatorEnabled;
    private bool previousAteAnimatorEnabled;
    private bool previousAteWaypointMoverEnabled;
    private bool previousAteAiEnabled;
    private bool previousAteNpcEnabled;
    private bool ownsGamePause;
    private bool sessionActive;
    private bool resultReady;
    private bool actorsConfigured;
    private int playsUsedToday;
    private int playCountDay = -1;
    private string statusMessage;
    private Coroutine matchCoroutine;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        boardRenderer = GetComponent<SpriteRenderer>();

        BoxCollider2D interactionTrigger = GetComponent<BoxCollider2D>();
        if (interactionTrigger == null)
            interactionTrigger = gameObject.AddComponent<BoxCollider2D>();

        interactionTrigger.isTrigger = true;
        interactionTrigger.size = new Vector2(1.5f, 1.5f);
        interactionTrigger.offset = new Vector2(0f, -1.7f);

        sessionBlocker = new GameObject("Piko Session Blocker");
        sessionBlocker.transform.SetParent(transform, false);
        sessionBlocker.SetActive(false);
        BuildTransitionOverlay();
    }

    public bool CanInteract()
    {
        return boardRenderer != null &&
            !sessionActive &&
            !PauseController.IsGamePaused &&
            !MiniGameInteractionBlocker.IsAnyMiniGameActive;
    }

    private void Update()
    {
        if (sessionActive && !resultReady && statusText != null)
            statusText.text = statusMessage;

        if (resultReady && continueButton != null)
        {
            float pulse = 1f + (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f * 0.08f;
            continueButton.transform.localScale = Vector3.one * pulse;
            if (continueButtonBackground != null)
            {
                Color color = continueButtonBackground.color;
                color.a = 0.84f + (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f * 0.14f;
                continueButtonBackground.color = color;
            }
        }
    }

    public void Interact()
    {
        if (!CanInteract())
            return;

        player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            Debug.LogError(
                "Piko needs an active player in the scene to start.",
                this);
            return;
        }

        RefreshDailyPlayCount();
        if (playsUsedToday >= DailyPlayLimit)
        {
            SpeechBubbleCanvas.Show(
                player.transform,
                "That's it for today! You can play again 2x tomorrow. Let's do the chores for now!",
                4f);
            return;
        }

        ate = FindAte();
        if (ate == null)
        {
            Debug.LogError(
                "Piko needs an active Ate NPC in the scene to start.",
                this);
            return;
        }

        BeginMatch();
    }

    private void RefreshDailyPlayCount()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        }

        int currentDay = dayManager != null ? dayManager.CurrentDay : 1;
        if (currentDay == playCountDay)
            return;

        playCountDay = currentDay;
        playsUsedToday = 0;
    }

    private NPCSocialParticipant FindAte()
    {
        NPCSocialParticipant[] participants =
            FindObjectsByType<NPCSocialParticipant>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (NPCSocialParticipant participant in participants)
        {
            if (participant != null &&
                participant.gameObject.activeInHierarchy &&
                string.Equals(
                    participant.DisplayName,
                    "Ate",
                    StringComparison.OrdinalIgnoreCase))
            {
                return participant;
            }
        }

        return null;
    }

    private void BeginMatch()
    {
        playsUsedToday++;
        playerTransform = player.transform;
        ateTransform = ate.transform;
        playerBody = player.GetComponent<Rigidbody2D>();
        playerAnimator = player.GetComponentInChildren<Animator>();
        ateAnimator = ate.GetComponentInChildren<Animator>();
        ateWaypointMover = ate.WaypointMover;
        ateAi = ate.GetComponent<BunsoAI>();
        ateNpc = ate.GetComponent<NPC>();
        timeManager = FindFirstObjectByType<TimeManager>();

        playerStartPosition = playerTransform.position;
        ateStartPosition = ateTransform.position;
        previousPlayerBodySimulated = playerBody != null && playerBody.simulated;
        previousPlayerAnimatorEnabled = playerAnimator != null && playerAnimator.enabled;
        previousAteAnimatorEnabled = ateAnimator != null && ateAnimator.enabled;
        previousPlayerAnimatorSpeed = playerAnimator != null ? playerAnimator.speed : 1f;
        previousAteAnimatorSpeed = ateAnimator != null ? ateAnimator.speed : 1f;
        previousPlayerAnimatorUpdateMode = playerAnimator != null
            ? playerAnimator.updateMode
            : AnimatorUpdateMode.Normal;
        previousAteAnimatorUpdateMode = ateAnimator != null
            ? ateAnimator.updateMode
            : AnimatorUpdateMode.Normal;
        previousPlayerMovementInputEnabled = player.IsMovementInputEnabled;
        previousAteWaypointMoverEnabled =
            ateWaypointMover != null && ateWaypointMover.enabled;
        previousAteAiEnabled = ateAi != null && ateAi.enabled;
        previousAteNpcEnabled = ateNpc != null && ateNpc.enabled;

        if (timeManager != null)
        {
            previousDayTimePaused = timeManager.IsDayTimePaused;
            timeManager.SetDayTimePaused(true);
        }

        previousTimeScale = Time.timeScale;
        ownsGamePause = !PauseController.IsGamePaused;
        if (ownsGamePause)
            PauseController.SetPause(true);

        sessionActive = true;
        resultReady = false;
        statusMessage = "Piko! First to hop from 1 to 10 without missing wins.";
        BuildResultUI();
        sessionBlocker.SetActive(true);
        MiniGameInteractionBlocker.RegisterPanel(sessionBlocker);
        transitionCoroutine = StartCoroutine(EnterMatchTransition());
    }

    private IEnumerator EnterMatchTransition()
    {
        yield return FadeTransition(1f);

        ConfigureActorsForMatch();
        actorsConfigured = true;
        playerPamato = CreatePamato("Player Pamato", new Color(1f, 0.83f, 0.25f));
        atePamato = CreatePamato("Ate Pamato", new Color(1f, 0.54f, 0.62f));
        ZoomToBoard();

        statusText.text = statusMessage;
        statusText.gameObject.SetActive(true);
        continueButton.interactable = true;
        continueButton.transform.localScale = Vector3.one;
        continueButtonBackground.color = new Color(0.18f, 0.12f, 0.08f, 0.92f);
        continueButton.gameObject.SetActive(false);
        resultCanvasObject.SetActive(true);

        yield return FadeTransition(0f);
        transitionCanvasObject.SetActive(false);
        transitionCoroutine = null;
        matchCoroutine = StartCoroutine(PlayMatch());
    }

    private void ConfigureActorsForMatch()
    {
        player.SetMovementInputEnabled(false);
        previousPlayerScale = playerTransform.localScale;
        previousAteScale = ateTransform.localScale;
        playerTransform.localScale = previousPlayerScale * MatchActorScale;
        ateTransform.localScale = previousAteScale * MatchActorScale;
        playerSortingStates = SetActorSorting(playerTransform, boardRenderer.sortingLayerID,
            boardRenderer.sortingOrder + 1);
        ateSortingStates = SetActorSorting(ateTransform, boardRenderer.sortingLayerID,
            boardRenderer.sortingOrder + 2);

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }

        if (ateWaypointMover != null)
            ateWaypointMover.enabled = false;
        if (ateAi != null)
            ateAi.enabled = false;
        if (ateNpc != null)
            ateNpc.enabled = false;

        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.speed = 1f;
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        if (ateAnimator != null)
        {
            ateAnimator.enabled = true;
            ateAnimator.speed = 1f;
            ateAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        playerTransform.position = GetContestantPosition(
            playerTransform,
            1,
            -ContestantSideOffset);
        ateTransform.position = GetContestantPosition(
            ateTransform,
            1,
            ContestantSideOffset);
        SetWalking(playerAnimator, false);
        SetWalking(ateAnimator, false);
    }

    private void ZoomToBoard()
    {
        cameraRig = FindFirstObjectByType<CinemachineCamera>(
            FindObjectsInactive.Include);
        if (cameraRig == null)
        {
            Debug.LogWarning(
                "CinemachineCamera was not found. The Piko match will run without zooming the camera.",
                this);
            return;
        }

        previousCameraFollow = cameraRig.Follow;
        previousCameraPosition = cameraRig.transform.position;
        previousCameraSize = cameraRig.Lens.OrthographicSize;

        cameraConfiner = cameraRig.GetComponent<CinemachineConfiner2D>();
        if (cameraConfiner != null)
        {
            previousConfinerEnabled = cameraConfiner.enabled;
            cameraConfiner.enabled = false;
        }

        cameraRig.Follow = null;
        Vector3 boardCenter = boardRenderer.bounds.center;
        Vector3 cameraPosition = cameraRig.transform.position;
        cameraPosition.x = boardCenter.x;
        cameraPosition.y = boardCenter.y;
        cameraRig.transform.position = cameraPosition;
        cameraRig.ForceCameraPosition(cameraPosition, cameraRig.transform.rotation);

        var lens = cameraRig.Lens;
        lens.OrthographicSize = TightCameraSize;
        cameraRig.Lens = lens;
    }

    private IEnumerator PlayMatch()
    {
        bool ateWins = UnityEngine.Random.value < AteWinChance;
        int playerMissAt = ateWins ? UnityEngine.Random.Range(3, 10) : int.MaxValue;
        int ateMissAt = ateWins ? int.MaxValue : UnityEngine.Random.Range(3, 10);
        bool playerStillIn = true;
        bool ateStillIn = true;

        yield return WaitUnscaled(0.35f);

        for (int square = 2; square <= 10; square++)
        {
            if (playerStillIn)
            {
                statusMessage = $"Your turn! Hopping to square {square}...";
                yield return TossPamato(playerPamato, GetSquarePosition(square, -0.12f));
                yield return HopToSquare(
                    playerTransform,
                    playerAnimator,
                    square,
                    -ContestantSideOffset);
                playerPamato.SetActive(false);
                if (square == playerMissAt)
                {
                    playerStillIn = false;
                    SetWalking(playerAnimator, false);
                    statusMessage = $"You missed at square {square}! Ate keeps hopping...";
                    yield return WaitUnscaled(0.35f);
                }
            }

            if (ateStillIn)
            {
                statusMessage = $"Ate's turn! Hopping to square {square}...";
                yield return TossPamato(atePamato, GetSquarePosition(square, 0.12f));
                yield return HopToSquare(
                    ateTransform,
                    ateAnimator,
                    square,
                    ContestantSideOffset);
                atePamato.SetActive(false);
                if (square == ateMissAt)
                {
                    ateStillIn = false;
                    SetWalking(ateAnimator, false);
                    statusMessage = $"Ate missed at square {square}! Finish the course!";
                    yield return WaitUnscaled(0.35f);
                }
            }
        }

        CompleteMatch(!ateWins);
        matchCoroutine = null;
    }

    private IEnumerator HopToSquare(
        Transform contestant,
        Animator animator,
        int square,
        float sideOffset)
    {
        Vector3 start = contestant.position;
        Vector3 destination = GetContestantPosition(contestant, square, sideOffset);
        SoundEffectManager.Play("Jump");
        SetWalking(animator, true);

        float elapsed = 0f;
        while (elapsed < HopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / HopDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            Vector3 position = Vector3.Lerp(start, destination, easedProgress);
            position.y += Mathf.Sin(progress * Mathf.PI) * HopHeight;
            contestant.position = position;
            yield return null;
        }

        contestant.position = destination;
        SetWalking(animator, false);
    }

    private IEnumerator TossPamato(GameObject pamato, Vector3 destination)
    {
        if (pamato == null)
            yield break;

        Transform marker = pamato.transform;
        Vector3 start = marker.position;
        marker.gameObject.SetActive(true);

        const float tossDuration = 0.18f;
        float elapsed = 0f;
        while (elapsed < tossDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / tossDuration);
            Vector3 position = Vector3.Lerp(start, destination, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * 0.28f;
            marker.position = position;
            yield return null;
        }

        marker.position = destination;
    }

    private GameObject CreatePamato(string markerName, Color color)
    {
        GameObject markerObject = new GameObject(markerName);
        SpriteRenderer markerRenderer = markerObject.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = GetPamatoSprite();
        markerRenderer.color = color;

        SpriteRenderer characterRenderer = player.GetComponentInChildren<SpriteRenderer>();
        if (characterRenderer != null)
        {
            markerRenderer.sortingLayerID = characterRenderer.sortingLayerID;
            markerRenderer.sortingOrder = characterRenderer.sortingOrder + 1;
        }
        else
        {
            markerRenderer.sortingLayerID = boardRenderer.sortingLayerID;
            markerRenderer.sortingOrder = boardRenderer.sortingOrder + 1;
        }

        markerObject.transform.localScale = Vector3.one * 0.11f;
        markerObject.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        markerObject.transform.position = GetSquarePosition(1, 0f);
        markerObject.SetActive(false);
        return markerObject;
    }

    private static Sprite GetPamatoSprite()
    {
        if (pamatoSprite == null)
        {
            pamatoSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            pamatoSprite.name = "Piko Pamato";
        }

        return pamatoSprite;
    }

    private Vector3 GetSquarePosition(int square, float sideOffset)
    {
        int index = Mathf.Clamp(square - 1, 0, SquareHeights.Length - 1);
        Bounds bounds = boardRenderer.bounds;
        float x = bounds.center.x;

        if (square == 4 || square == 7)
            x -= bounds.size.x * 0.23f;
        else if (square == 5 || square == 8)
            x += bounds.size.x * 0.23f;

        x += sideOffset;
        float y = Mathf.Lerp(bounds.min.y, bounds.max.y, SquareHeights[index]);
        return new Vector3(x, y, playerTransform != null ? playerTransform.position.z : 0f);
    }

    private Vector3 GetContestantPosition(
        Transform contestant,
        int square,
        float sideOffset)
    {
        Vector3 targetVisualCenter = GetSquarePosition(square, sideOffset);
        SpriteRenderer[] renderers =
            contestant.GetComponentsInChildren<SpriteRenderer>(false);
        Bounds visualBounds = default;
        bool hasBounds = false;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || renderer.sprite == null)
                continue;

            if (hasBounds)
                visualBounds.Encapsulate(renderer.bounds);
            else
            {
                visualBounds = renderer.bounds;
                hasBounds = true;
            }
        }

        if (!hasBounds)
            return targetVisualCenter;

        return targetVisualCenter - (visualBounds.center - contestant.position);
    }

    private static void SetWalking(Animator animator, bool walking)
    {
        if (animator == null)
            return;

        animator.SetBool("isWalking", walking);
    }

    private static SpriteSortingState[] SetActorSorting(
        Transform actor,
        int sortingLayerId,
        int sortingOrder)
    {
        SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteSortingState[] states = new SpriteSortingState[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            states[i] = new SpriteSortingState
            {
                renderer = renderer,
                sortingLayerId = renderer.sortingLayerID,
                sortingOrder = renderer.sortingOrder
            };
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }

        return states;
    }

    private static void RestoreActorSorting(SpriteSortingState[] states)
    {
        if (states == null)
            return;

        foreach (SpriteSortingState state in states)
        {
            if (state.renderer == null)
                continue;

            state.renderer.sortingLayerID = state.sortingLayerId;
            state.renderer.sortingOrder = state.sortingOrder;
        }
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void CompleteMatch(bool playerWon)
    {
        resultReady = true;
        if (playerWon)
        {
            MoodManager moodManager = MoodManager.Instance != null
                ? MoodManager.Instance
                : FindFirstObjectByType<MoodManager>();

            if (moodManager != null)
            {
                moodManager.AddMood(20f);
                statusMessage = "You win! +20 mood. Ate will get you next time!";
            }
            else
            {
                Debug.LogWarning(
                    "Piko was won, but MoodManager was not found. The mood reward could not be applied.",
                    this);
                statusMessage = "You win! Ate will get you next time!";
            }
        }
        else
        {
            statusMessage = "Ate wins this round! Better luck next time.";
            SoundEffectManager.Play("BunsoTawa");
        }

        statusText.text = statusMessage;
        continueButton.gameObject.SetActive(true);
    }

    private void BuildResultUI()
    {
        if (resultCanvasObject != null)
            return;

        resultCanvasObject = new GameObject(
            "Piko Result UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = resultCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = resultCanvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        statusText = CreateResultText(
            resultCanvasObject.transform,
            "Piko Status",
            26f,
            new Vector2(0f, 88f),
            new Vector2(1100f, 64f));
        continueButton = CreateContinueButton(resultCanvasObject.transform);

        if (daydreamFont == null)
        {
            Debug.LogError(
                "Assign the Daydream DEMO SDF font asset to PikoMiniGame.",
                this);
        }

        resultCanvasObject.SetActive(false);
    }

    private TMP_Text CreateResultText(
        Transform parent,
        string objectName,
        float fontSize,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = daydreamFont;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color32(38, 29, 23, 255);
        return text;
    }

    private Button CreateContinueButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "Continue Button",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 24f);
        rect.sizeDelta = new Vector2(280f, 50f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.18f, 0.12f, 0.08f, 0.92f);
        background.raycastTarget = true;
        continueButtonBackground = background;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(RequestEndMatch);

        TMP_Text label = CreateResultText(
            buttonObject.transform,
            "Continue Label",
            22f,
            Vector2.zero,
            rect.sizeDelta);
        label.text = "CONTINUE";
        label.color = new Color(1f, 0.9f, 0.62f, 1f);
        return button;
    }

    private void BuildTransitionOverlay()
    {
        transitionCanvasObject = new GameObject(
            "Piko Transition Fade",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster));

        Canvas canvas = transitionCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        transitionFade = transitionCanvasObject.GetComponent<CanvasGroup>();
        transitionFade.alpha = 0f;
        transitionFade.blocksRaycasts = false;
        transitionFade.interactable = false;

        GameObject fadeImageObject = new GameObject(
            "Fade Image",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        fadeImageObject.transform.SetParent(transitionCanvasObject.transform, false);

        RectTransform fadeRect = fadeImageObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image fadeImage = fadeImageObject.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        transitionCanvasObject.SetActive(false);
    }

    private IEnumerator FadeTransition(float targetAlpha)
    {
        transitionCanvasObject.SetActive(true);
        transitionFade.blocksRaycasts = true;

        float startAlpha = transitionFade.alpha;
        float elapsed = 0f;
        while (elapsed < TransitionFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / TransitionFadeDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            transitionFade.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
            yield return null;
        }

        transitionFade.alpha = targetAlpha;
        transitionFade.blocksRaycasts = targetAlpha > 0f;
    }

    private void RequestEndMatch()
    {
        if (!sessionActive || !resultReady || transitionCoroutine != null)
            return;

        resultReady = false;
        continueButton.interactable = false;
        transitionCoroutine = StartCoroutine(ExitMatchTransition());
    }

    private IEnumerator ExitMatchTransition()
    {
        yield return FadeTransition(1f);
        EndMatch(true);
        yield return FadeTransition(0f);
        transitionCanvasObject.SetActive(false);
        transitionCoroutine = null;
    }

    private void EndMatch(bool keepTransitionVisible = false)
    {
        if (!sessionActive)
            return;

        sessionActive = false;
        resultReady = false;
        if (matchCoroutine != null)
        {
            StopCoroutine(matchCoroutine);
            matchCoroutine = null;
        }

        RestoreActors();
        RestoreCamera();
        if (timeManager != null)
            timeManager.SetDayTimePaused(previousDayTimePaused);

        if (ownsGamePause && PauseController.IsGamePaused)
            PauseController.SetPause(false);
        else if (ownsGamePause && Time.timeScale == 0f)
            Time.timeScale = previousTimeScale;

        ownsGamePause = false;
        if (sessionBlocker != null)
            sessionBlocker.SetActive(false);
        if (resultCanvasObject != null)
            resultCanvasObject.SetActive(false);
        if (playerPamato != null)
            Destroy(playerPamato);
        if (atePamato != null)
            Destroy(atePamato);
        playerPamato = null;
        atePamato = null;

        player = null;
        ate = null;
        playerTransform = null;
        ateTransform = null;
        playerBody = null;
        playerAnimator = null;
        ateAnimator = null;
        ateWaypointMover = null;
        ateAi = null;
        ateNpc = null;
        timeManager = null;
        cameraRig = null;
        cameraConfiner = null;
        if (!keepTransitionVisible && transitionCanvasObject != null)
        {
            transitionFade.alpha = 0f;
            transitionFade.blocksRaycasts = false;
            transitionCanvasObject.SetActive(false);
        }
    }

    private void RestoreActors()
    {
        if (!actorsConfigured)
            return;

        if (playerTransform != null)
        {
            playerTransform.position = playerStartPosition;
            playerTransform.localScale = previousPlayerScale;
        }
        if (ateTransform != null)
        {
            ateTransform.position = ateStartPosition;
            ateTransform.localScale = previousAteScale;
        }

        if (playerBody != null)
        {
            playerBody.simulated = previousPlayerBodySimulated;
            playerBody.linearVelocity = Vector2.zero;
        }

        RestoreActorSorting(playerSortingStates);
        RestoreActorSorting(ateSortingStates);
        playerSortingStates = null;
        ateSortingStates = null;

        RestoreAnimator(
            playerAnimator,
            previousPlayerAnimatorEnabled,
            previousPlayerAnimatorSpeed,
            previousPlayerAnimatorUpdateMode);
        RestoreAnimator(
            ateAnimator,
            previousAteAnimatorEnabled,
            previousAteAnimatorSpeed,
            previousAteAnimatorUpdateMode);
        if (player != null)
            player.SetMovementInputEnabled(previousPlayerMovementInputEnabled);

        if (ateWaypointMover != null)
            ateWaypointMover.enabled = previousAteWaypointMoverEnabled;
        if (ateAi != null)
            ateAi.enabled = previousAteAiEnabled;
        if (ateNpc != null)
            ateNpc.enabled = previousAteNpcEnabled;

        actorsConfigured = false;
    }

    private static void RestoreAnimator(
        Animator animator,
        bool wasEnabled,
        float speed,
        AnimatorUpdateMode updateMode)
    {
        if (animator == null)
            return;

        animator.SetBool("isWalking", false);
        animator.speed = speed;
        animator.updateMode = updateMode;
        animator.enabled = wasEnabled;
    }

    private void RestoreCamera()
    {
        if (cameraRig == null)
            return;

        cameraRig.Follow = null;
        cameraRig.transform.position = previousCameraPosition;
        cameraRig.ForceCameraPosition(
            previousCameraPosition,
            cameraRig.transform.rotation);

        var lens = cameraRig.Lens;
        lens.OrthographicSize = previousCameraSize;
        cameraRig.Lens = lens;
        cameraRig.Follow = previousCameraFollow;

        if (cameraConfiner != null)
            cameraConfiner.enabled = previousConfinerEnabled;
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
        EndMatch();
    }

    private void OnDestroy()
    {
        EndMatch();
        if (sessionBlocker != null)
            Destroy(sessionBlocker);
        if (resultCanvasObject != null)
            Destroy(resultCanvasObject);
        if (transitionCanvasObject != null)
            Destroy(transitionCanvasObject);
        if (pamatoSprite != null)
            Destroy(pamatoSprite);
    }
}
