using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MoppingMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;


    [Header("Mop")]
    [SerializeField] private RectTransform mop;
    [SerializeField] private RectTransform moppingArea;
    [SerializeField] private GameObject upgradedMopVisual;


    [Header("Cleaning")]
    [SerializeField] private float cleaningSpeed = 0.5f;


    private WetArea currentWetArea;

    private float progress = 0f;
    private bool isMopping = false;
    private bool mouseIsDown = false;
    private RectTransform activeMop;


    private DayManager dayManager;
    private SuddenTaskManager suddenTaskManager;

    private void Awake()
    {
        ConfigureMinigameUI();
    }


    private DayManager ResolveDayManager()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();
        }

        return dayManager;
    }


    private void Start()
    {
        dayManager = ResolveDayManager();
        suddenTaskManager = FindFirstObjectByType<SuddenTaskManager>();
    }


    private void ConfigureMinigameUI()
    {
        ResetMopping();
    }

    public void ResetMopping()
    {
        currentWetArea = null;
        progress = 0f;
        isMopping = false;
        mouseIsDown = false;
        activeMop = null;

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (progressBar != null)
            progressBar.value = 0f;

        if (mop != null)
            mop.gameObject.SetActive(false);

        if (upgradedMopVisual != null)
            upgradedMopVisual.SetActive(false);
    }



    private void Update()
    {
        if (!isMopping)
            return;


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

        ApplyMopVisual(GetCurrentMopLevel());
        ApplyDifficulty();



        if (minigamePanel != null)
        {
            minigamePanel.SetActive(true);
        }


        if (progressBar != null)
        {
            progressBar.value = 0f;
        }


        Debug.Log(
            "MOPPING MINIGAME STARTED!"
        );
    }



    private void ApplyDifficulty()
    {
        var resolvedDayManager = ResolveDayManager();
        int difficulty = resolvedDayManager != null
            ? resolvedDayManager.CurrentDifficulty
            : 0;

        cleaningSpeed = 0.5f - (difficulty * 0.05f);
        cleaningSpeed = Mathf.Max(cleaningSpeed, 0.25f);

        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager != null)
            cleaningSpeed *= economyManager.MopCleaningSpeedMultiplier;



        Debug.Log(
            "Mopping difficulty: "
            + difficulty
            + " | Cleaning Speed: "
            + cleaningSpeed
        );
    }

    private int GetCurrentMopLevel()
    {
        return EconomyManager.Instance != null && EconomyManager.Instance.HasUpgradedMop
            ? 1
            : 0;
    }

    private void ApplyMopVisual(int level)
    {
        if (mop != null)
            mop.gameObject.SetActive(false);
        if (upgradedMopVisual != null)
            upgradedMopVisual.SetActive(false);

        GameObject selectedVisual = level == 1
            ? upgradedMopVisual
            : (mop != null ? mop.gameObject : null);
        if (selectedVisual == null)
        {
            activeMop = null;
            return;
        }

        selectedVisual.SetActive(true);
        RectTransform selectedRect = selectedVisual.GetComponent<RectTransform>();
        if (selectedRect != null)
            activeMop = selectedRect;
    }



    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;



        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseIsDown = true;
        }



        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            mouseIsDown = false;
        }



        if (!mouseIsDown)
            return;



        MoveMop();



        if (IsMopInsideMoppingArea())
        {
            progress += cleaningSpeed * Time.deltaTime;



            if(progressBar != null)
            {
                progressBar.value = progress;
            }



            if(progress >= 1f)
            {
                CompleteMopping();
            }
        }
    }



    private void MoveMop()
    {
        if(activeMop == null)
            return;



        Vector2 mousePosition =
            Mouse.current.position.ReadValue();



        if (minigamePanel == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minigamePanel.GetComponent<RectTransform>(),
            mousePosition,
            null,
            out Vector2 localPosition
        );



        activeMop.localPosition =
            localPosition;
    }



    private bool IsMopInsideMoppingArea()
    {
        if(activeMop == null || moppingArea == null)
            return false;



        Vector2 mousePosition =
            Mouse.current.position.ReadValue();



        return RectTransformUtility.RectangleContainsScreenPoint(
            moppingArea,
            mousePosition,
            null
        );
    }



    private void CompleteMopping()
    {
        isMopping = false;
        mouseIsDown = false;
        progress = 1f;



        if(progressBar != null)
        {
            progressBar.value = 1f;
        }



        // Remove water
        if(currentWetArea != null)
        {
            currentWetArea.Clean();
        }



        if(suddenTaskManager != null)
        {
            suddenTaskManager.CompleteMopTask();
        }
        else
        {
            Debug.LogWarning(
                "SuddenTaskManager was not found."
            );
        }



        if(minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        currentWetArea = null;



        Debug.Log(
            "MOPPING COMPLETE!"
        );
    }
}