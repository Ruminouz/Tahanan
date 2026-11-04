using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    private static readonly string[] LoadingTips =
    {
        "Complete your chores to earn coins for useful upgrades.",
        "Visit the shop to improve your cleaning tools.",
        "Keep an eye on your family's mood while you work.",
        "Try to finish each day's chores before time runs out.",
        "Talk to your family members for helpful hints.",
        "Every day brings new tasks and surprises around the house.",
        "Upgraded tools can make chores faster and easier.",
        "Check your chore list to see what still needs to be done."
    };

    private static bool isLoading;
    private static LoadingScreenController instance;

    [Header("Design in the Main Menu scene")]
    [SerializeField] private Image panelImage;
    [SerializeField] private Sprite[] panelFrames = new Sprite[3];
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Sprite[] handleWalkFrames = new Sprite[4];
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private TMP_Text continueText;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float fadeToGameDuration = 0.6f;
    [SerializeField, Min(0f)] private float minimumLoadingDuration = 2.5f;
    [SerializeField, Min(0f)] private float dayTransitionLoadingDuration = 4f;
    [SerializeField, Min(0f)] private float continuePromptDelay = 0.2f;
    [SerializeField, Min(0.01f)] private float frameDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float handleFrameDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float tipDuration = 4f;
    [SerializeField, Min(0.01f)] private float textPulseSpeed = 2.5f;
    [SerializeField, Min(1f)] private float textPulseScale = 1.08f;

    private float frameTimer;
    private float handleFrameTimer;
    private float tipTimer;
    private int frameIndex;
    private int handleFrameIndex;
    private int previousTipIndex = -1;
    private Image progressHandleImage;
    private CanvasGroup canvasGroup;
    private Vector3 tipTextBaseScale;
    private Vector3 continueTextBaseScale;
    private bool isWaitingForContinue;
    private bool isShowingForDayTransition;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void LoadScene(int buildIndex)
    {
        if (isLoading || isShowingForDayTransition)
        {
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (panelImage == null || progressSlider == null || tipText == null || continueText == null)
        {
            Debug.LogError(
                "Loading screen references are missing. Assign the frame Image, progress Slider, tip Text, and continue Text in the Main Menu scene.",
                this);
            return;
        }

        if (HasAssignedFrames(handleWalkFrames))
        {
            progressHandleImage = progressSlider.handleRect != null
                ? progressSlider.handleRect.GetComponent<Image>()
                : null;
            if (progressHandleImage == null)
            {
                Debug.LogWarning(
                    "Handle walk frames are assigned, but the loading slider handle has no Image component.",
                    this);
            }
        }

        isLoading = true;
        ApplyTipStyle();
        ResetLoadingScreen();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(LoadSceneAsync(buildIndex));
    }

    private void Update()
    {
        frameTimer += Time.unscaledDeltaTime;
        if (panelFrames != null && panelFrames.Length > 0 && frameTimer >= frameDuration)
        {
            frameTimer %= frameDuration;
            frameIndex = (frameIndex + 1) % panelFrames.Length;
            panelImage.sprite = panelFrames[frameIndex];
        }

        if (progressHandleImage != null &&
            HasAssignedFrames(handleWalkFrames) &&
            handleFrameTimer >= handleFrameDuration)
        {
            handleFrameTimer %= handleFrameDuration;
            handleFrameIndex = (handleFrameIndex + 1) % handleWalkFrames.Length;
            SetHandleFrame(handleFrameIndex);
        }
        handleFrameTimer += Time.unscaledDeltaTime;

        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * textPulseSpeed) + 1f) * 0.5f
            * (textPulseScale - 1f);
        tipText.transform.localScale = tipTextBaseScale * pulse;
        if (isWaitingForContinue)
        {
            continueText.transform.localScale = continueTextBaseScale * pulse;
        }

        tipTimer += Time.unscaledDeltaTime;
        if (tipTimer >= tipDuration)
        {
            tipTimer %= tipDuration;
            ShowRandomTip();
        }
    }

    private IEnumerator LoadSceneAsync(int buildIndex)
    {
        yield return FadeIn();

        var operation = SceneManager.LoadSceneAsync(buildIndex);
        if (operation == null)
        {
            Debug.LogError($"Could not start loading scene at build index {buildIndex}.", this);
            isLoading = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            gameObject.SetActive(false);
            yield break;
        }

        operation.allowSceneActivation = false;
        float startTime = Time.unscaledTime;

        while (operation.progress < 0.9f ||
               Time.unscaledTime - startTime < minimumLoadingDuration)
        {
            float elapsed = Time.unscaledTime - startTime;
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timedProgress = minimumLoadingDuration > 0f
                ? Mathf.Clamp01(elapsed / minimumLoadingDuration)
                : 1f;
            float displayedProgress = Mathf.Min(loadProgress, timedProgress) * 0.9f;
            SetProgress(displayedProgress);
            yield return null;
        }

        SetProgress(1f);
        yield return new WaitForSecondsRealtime(0.2f);
        continueText.gameObject.SetActive(true);
        isWaitingForContinue = true;
        while (true)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                break;
            }

            yield return null;
        }

        isWaitingForContinue = false;
        operation.allowSceneActivation = true;
    }

    public IEnumerator ShowForDayTransition()
    {
        if (isLoading || isShowingForDayTransition)
        {
            yield break;
        }

        if (panelImage == null || progressSlider == null || tipText == null || continueText == null)
        {
            Debug.LogError(
                "Loading screen references are missing. Assign the frame Image, progress Slider, tip Text, and continue Text in the Main Menu scene.",
                this);
            yield break;
        }

        isShowingForDayTransition = true;
        ApplyTipStyle();
        ResetLoadingScreen();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        float transitionStartTime = Time.unscaledTime;
        yield return FadeIn();

        float elapsed = 0f;
        float progressDuration = Mathf.Max(
            0f,
            dayTransitionLoadingDuration - fadeInDuration - continuePromptDelay);
        while (elapsed < progressDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float timedProgress = progressDuration > 0f
                ? Mathf.Clamp01(elapsed / progressDuration)
                : 1f;
            SetProgress(timedProgress);
            yield return null;
        }

        SetProgress(1f);
        float remainingPromptDelay = continuePromptDelay -
            (Time.unscaledTime - transitionStartTime - fadeInDuration - progressDuration);
        if (remainingPromptDelay > 0f)
            yield return new WaitForSecondsRealtime(remainingPromptDelay);
        continueText.gameObject.SetActive(true);
        isWaitingForContinue = true;
        while (true)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                break;
            }

            yield return null;
        }

        isWaitingForContinue = false;
        yield return FadeToGameScene();
        isShowingForDayTransition = false;
    }

    private IEnumerator FadeIn()
    {
        if (fadeInDuration <= 0f)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void ResetLoadingScreen()
    {
        frameIndex = 0;
        frameTimer = 0f;
        handleFrameIndex = 0;
        handleFrameTimer = 0f;
        tipTimer = 0f;
        previousTipIndex = -1;
        isWaitingForContinue = false;
        tipTextBaseScale = tipText.transform.localScale;
        continueTextBaseScale = continueText.transform.localScale;
        continueText.gameObject.SetActive(false);
        SetProgress(0f);

        if (panelFrames != null && panelFrames.Length > 0 && panelFrames[0] != null)
        {
            panelImage.sprite = panelFrames[0];
        }

        if (progressHandleImage != null && HasAssignedFrames(handleWalkFrames))
        {
            SetHandleFrame(0);
        }

        ShowRandomTip();
    }

    private void SetProgress(float normalizedProgress)
    {
        progressSlider.value = Mathf.Lerp(
            progressSlider.minValue,
            progressSlider.maxValue,
            Mathf.Clamp01(normalizedProgress));
    }

    private void SetHandleFrame(int index)
    {
        if (index < handleWalkFrames.Length && handleWalkFrames[index] != null)
        {
            progressHandleImage.sprite = handleWalkFrames[index];
        }
    }

    private static bool HasAssignedFrames(Sprite[] frames)
    {
        if (frames == null)
        {
            return false;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowRandomTip()
    {
        int nextTipIndex = Random.Range(0, LoadingTips.Length);
        if (LoadingTips.Length > 1 && nextTipIndex == previousTipIndex)
        {
            nextTipIndex = (nextTipIndex + 1) % LoadingTips.Length;
        }

        previousTipIndex = nextTipIndex;
        tipText.text = $"TIP: {LoadingTips[nextTipIndex]}";
    }

    private void ApplyTipStyle()
    {
        tipText.color = Color.white;
        continueText.color = Color.green;
        ApplyTextStyle(tipText);
        ApplyTextStyle(continueText);
    }

    private void ApplyTextStyle(TMP_Text text)
    {
        var material = text.fontMaterial;
        if (!material.HasProperty("_OutlineColor") || !material.HasProperty("_OutlineWidth"))
        {
            Debug.LogError($"The text font material for '{text.name}' does not support an outline.", this);
            return;
        }

        material.SetColor("_OutlineColor", Color.black);
        material.SetFloat("_OutlineWidth", 0.2f);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoading = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(FadeToGameScene());
    }

    private IEnumerator FadeToGameScene()
    {
        if (fadeToGameDuration <= 0f)
        {
            canvasGroup.alpha = 0f;
            yield break;
        }

        float startingAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeToGameDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(
                startingAlpha,
                0f,
                Mathf.Clamp01(elapsed / fadeToGameDuration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    public static void DestroyPersistentInstance()
    {
        if (instance == null)
        {
            return;
        }

        LoadingScreenController persistentInstance = instance;
        instance = null;
        Destroy(persistentInstance.gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
