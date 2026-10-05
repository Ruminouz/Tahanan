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

    private Chore currentChore;
    private TutorialManager tutorialManager;
    private DayManager dayManager;
    private bool feedingStarted = false;
    private int attemptsMade;
    private int successfulBites;
    private int combo;
    private int score;
    private Coroutine resultCoroutine;
    private Vector3 resultTextBaseScale;

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
    }



    public void StartGame(Chore chore)
    {
        if (chore == null || panel == null)
            return;

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
        attemptsMade = 0;
        successfulBites = 0;
        combo = 0;
        score = 0;

        panel.SetActive(true);

        bool alreadyLearned = false;
        if(tutorialManager != null)
        {
            alreadyLearned = tutorialManager.HasLearned(chore.ChoreName);
        }

        if(alreadyLearned)
        {
            StartFeeding();
        }
        else
        {
            ShowTutorial();
        }
    }




    private void ShowTutorial()
    {
        if (tutorial == null)
        {
            StartFeeding();
            return;
        }

        tutorial.ShowTutorial(
            "FEED THE DOG",
            "Watch the slider and press SPACE or tap FEED when the food is in the bowl.\n" +
            "Land at least 2 of 3 bites to finish the meal. Consecutive bites build your combo!",
            FinishTutorial
        );
    }




    private void FinishTutorial()
    {
        if(tutorialManager != null)
        {
            tutorialManager.MarkAsLearned(currentChore.ChoreName);
        }

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
            successfulBites++;
            combo++;
            score += 100 * combo;
        }
        else
        {
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
            ? "Yum! Bite landed."
            : attemptsMade > 0
                ? "Missed! Try the next toss."
                : "Press SPACE or tap FEED!";

        SetStatusText(
            $"BOWL [{(successfulBites > 0 ? "X" : " ")}][{(successfulBites > 1 ? "X" : " ")}] {successfulBites}/{BitesNeeded}\n" +
            $"TOSS {attemptsMade}/{AttemptsPerMeal} | COMBO x{combo} | {score} pts\n" +
            feedback
        );
    }

    private void CompleteGame()
    {
        SetStatusText(successfulBites == AttemptsPerMeal
            ? $"PERFECT MEAL!\n3 bites | Score: {score}"
            : $"HAPPY PUP!\nMeal complete | Score: {score}");

        if(currentChore != null)
        {
            currentChore.Complete();
        }

        ShowResultThenClose();
    }

    private void MissGame()
    {
        SetStatusText($"PUP IS STILL HUNGRY!\nYou landed {successfulBites}/{BitesNeeded} bites.");

        Debug.Log("Feeding attempt failed. The dog and chore remain available for another try.", this);

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
    }
}