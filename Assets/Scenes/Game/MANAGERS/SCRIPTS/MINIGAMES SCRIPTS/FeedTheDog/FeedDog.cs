using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedDogMiniGame : MonoBehaviour
{
    private const int AttemptsPerMeal = 3;
    private const int BitesNeeded = 2;
    private const float ResultDisplayDuration = 0.9f;

    [SerializeField] private GameObject panel;
    [SerializeField] private ChoreTutorial tutorial;
    [SerializeField] private FeedDogSlider slider;
    [SerializeField] private Component statusText;
    [SerializeField] private TMP_Text tutorialText;

    private Chore currentChore;
    private TutorialManager tutorialManager;
    private DayManager dayManager;
    private bool feedingStarted = false;
    private bool firstTimeTutorialActive;
    private bool tutorialTextWarningLogged;
    private int attemptsMade;
    private int successfulBites;
    private int combo;
    private int score;
    private Coroutine resultCoroutine;
    private Vector3 resultTextBaseScale;
    private Transform feedButton;
    private TMP_Text feedButtonText;
    private Vector3 feedButtonBaseScale;

    public bool IsPanelVisible => panel != null && panel.activeInHierarchy;

    private const float TutorialPulseSpeed = 5f;
    private const float TutorialPulseScale = 1.15f;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        feedButton = FindDescendant(panel != null ? panel.transform : transform, "FeedButton");
        if (feedButton != null)
        {
            feedButtonBaseScale = feedButton.localScale;
            feedButtonText = feedButton.GetComponentInChildren<TMP_Text>(true);
        }

        ResolveTutorialText();
    }

    private void Update()
    {
        if (!IsPanelVisible)
            return;

        if (!firstTimeTutorialActive || !feedingStarted)
        {
            RestoreTutorialPulse();
            return;
        }

        float pulse = 1f + (Mathf.Sin(Time.unscaledTime * TutorialPulseSpeed) + 1f)
            * 0.5f * (TutorialPulseScale - 1f);

        if (feedButton != null)
            feedButton.localScale = feedButtonBaseScale * pulse;

        if (slider != null)
            slider.SetTutorialPulse(pulse);
    }

    public void StartGame(Chore chore)
    {
        if (chore == null || !chore.CanInteract() || panel == null)
            return;

        if (currentChore == chore && feedingStarted)
        {
            MiniGamePanelMinimizer.Attach(panel);
            MiniGameInteractionBlocker.RegisterPanel(panel);
            panel.SetActive(true);
            return;
        }

        if(resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
            if(statusText != null)
            {
                statusText.transform.localScale = resultTextBaseScale;
            }

            resultCoroutine = null;
        }

        currentChore = chore;
        feedingStarted = false;
        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<TutorialManager>();

        firstTimeTutorialActive = tutorialManager != null &&
            !tutorialManager.HasLearned(chore.ChoreName);
        ResolveTutorialText();
        SetTutorialText(string.Empty);
        attemptsMade = 0;
        successfulBites = 0;
        combo = 0;
        score = 0;

        MiniGameInteractionBlocker.RegisterPanel(panel);
        MiniGamePanelMinimizer.Attach(panel);
        panel.SetActive(true);
        if (feedButtonText != null)
            feedButtonText.text = "FEED";

        StartFeeding();
    }

    private void StartFeeding()
    {
        feedingStarted = true;

        if(slider != null)
        {
            DayManager resolvedDayManager = dayManager != null
                ? dayManager
                : (DayManager.Instance != null
                    ? DayManager.Instance
                    : FindFirstObjectByType<DayManager>());

            int difficulty = resolvedDayManager != null
                ? resolvedDayManager.CurrentDifficulty
                : 0;

            slider.SetMiniGame(this);
            slider.ApplyDifficulty(difficulty);
            UpdateStatusText();
            if(!slider.StartSlider())
            {
                ClosePanel();
            }
        }
        else
        {
            Debug.LogError("FeedDogMiniGame is missing its slider reference.");
            feedingStarted = false;
        }
    }

    public void CheckResult(bool success)
    {
        if(!feedingStarted || attemptsMade >= AttemptsPerMeal)
            return;

        attemptsMade++;

        if(success)
        {
            SoundEffectManager.Play("HappyDog");
            successfulBites++;
            combo++;
            score += 100 * combo;
        }
        else
        {
            SoundEffectManager.Play("SadDog");
            combo = 0;
        }

        if(attemptsMade >= AttemptsPerMeal)
        {
            feedingStarted = false;

            if(successfulBites >= BitesNeeded)
            {
                CompleteGame();
            }
            else
            {
                MissGame();
            }

            return;
        }

        UpdateStatusText(success);
        if(!slider.StartSlider())
        {
            ClosePanel();
        }
    }

    public void FeedDog()
    {
        if(feedingStarted && slider != null)
        {
            slider.TryFeed();
        }
    }

    private void UpdateStatusText(bool lastAttemptSucceeded = false)
    {
        string feedback = lastAttemptSucceeded
            ? "Great hit! Get ready for the next try."
            : attemptsMade > 0
                ? "Missed! Watch the bowl and try again."
                : "Press SPACE or FEED when the food is over the bowl.";

        SetStatusText(
            $"FED: {successfulBites}/{BitesNeeded}  |  TRIES LEFT: {AttemptsPerMeal - attemptsMade}\n" +
            $"ATTEMPTS: {attemptsMade}/{AttemptsPerMeal}  |  COMBO x{combo}  |  SCORE: {score}" +
            (firstTimeTutorialActive ? string.Empty : $"\n{feedback}")
        );

        SetTutorialText(!firstTimeTutorialActive
            ? string.Empty
            : attemptsMade == 0
                ? $"Wait until the food is over the bowl, then press SPACE or FEED.\n{feedback}"
                : feedback);
    }

    private void CompleteGame()
    {
        if (dayManager == null)
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        if (dayManager != null)
            dayManager.ResolveDailyAdditionalQuest(
                currentChore,
                successfulBites == AttemptsPerMeal);

        SetStatusText(successfulBites == AttemptsPerMeal
            ? $"PERFECT FEED!\nAll 3 attempts hit | Score: {score}"
            : $"PUPPY IS FULL!\nFeeding complete | Score: {score}");

        if (firstTimeTutorialActive && tutorialManager != null && currentChore != null)
            tutorialManager.MarkAsLearned(currentChore.ChoreName);

        firstTimeTutorialActive = false;
        SetTutorialText(string.Empty);
        if(currentChore != null)
        {
            currentChore.Complete();
            SoundEffectManager.Play("ChoreFinished");
        }

        ShowResultThenClose();
    }

    private void MissGame()
    {
        if(currentChore != null)
        {
            ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
            if(choreManager != null)
            {
                choreManager.MissChore(currentChore);
            }
            else
            {
                currentChore.MarkAsMissed();
            }
        }

        SetStatusText(
            $"THE PUPPY IS STILL HUNGRY!\n{successfulBites}/{BitesNeeded} feeds hit.\n" +
            "Today's chore was not completed."
        );

        firstTimeTutorialActive = false;
        SetTutorialText(string.Empty);
        Debug.Log("Feeding attempt failed. The chore has been marked as missed.", this);
        ShowResultThenClose();
    }

    private void SetStatusText(string message)
    {
        if(statusText is Text legacyText)
        {
            legacyText.text = message;
        }
        else if(statusText is TMP_Text tmpText)
        {
            tmpText.text = message;
        }
        else if(statusText != null)
        {
            Debug.LogError("FeedDogMiniGame status text must be a Unity UI Text or TMP_Text.", this);
        }
    }

    private void ShowResultThenClose()
    {
        if(panel != null)
        {
            if(statusText != null)
            {
                resultTextBaseScale = statusText.transform.localScale;
            }

            resultCoroutine = StartCoroutine(ClosePanelAfterResult());
        }
        else
        {
            ClosePanel();
        }
    }

    private IEnumerator ClosePanelAfterResult()
    {
        if(statusText != null)
        {
            Transform resultTextTransform = statusText.transform;
            float elapsed = 0f;

            while(elapsed < ResultDisplayDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float pulse = 1f + Mathf.Sin(elapsed * 18f) * 0.06f;
                resultTextTransform.localScale = resultTextBaseScale * pulse;
                yield return null;
            }

            resultTextTransform.localScale = resultTextBaseScale;
        }
        else
        {
            yield return new WaitForSecondsRealtime(ResultDisplayDuration);
        }

        resultCoroutine = null;
        ClosePanel();
    }

    private void ClosePanel()
    {
        RestoreTutorialPulse();
        SetTutorialText(string.Empty);

        if(slider != null)
        {
            slider.StopSlider();
        }

        if(panel != null)
        {
            panel.SetActive(false);
        }

        currentChore = null;
        feedingStarted = false;
        firstTimeTutorialActive = false;
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
                return;
            }
        }
    }

    private void SetTutorialText(string message)
    {
        if (tutorialText == null)
        {
            if (firstTimeTutorialActive && !tutorialTextWarningLogged)
            {
                Debug.LogWarning("I-assign ang Tutorial Text ng dog minigame sa Inspector.", this);
                tutorialTextWarningLogged = true;
            }

            return;
        }

        tutorialText.text = message;
        tutorialText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private Transform FindDescendant(Transform root, string targetName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == targetName)
                return child;
        }

        return null;
    }

    private void RestoreTutorialPulse()
    {
        if (feedButton != null)
            feedButton.localScale = feedButtonBaseScale;

        if (slider != null)
            slider.SetTutorialPulse(1f);
    }
}
