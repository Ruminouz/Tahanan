using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class SweepingMinigame : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private Slider progressBar;


    [Header("Broom")]
    [SerializeField] private RectTransform broom;
    [SerializeField] private RectTransform upgradedBroomVisual;
    [SerializeField] private RectTransform sweepingArea;


    [Header("Cleaning")]
    [SerializeField] private float cleaningSpeed = 0.5f;


    private DustSpot currentDustSpot;


    private float progress;
    private bool isSweeping;
    private bool mouseIsDown;
    private RectTransform activeBroom;



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

        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (progressBar != null)
            progressBar.value = 0f;

        if (broom != null)
            broom.gameObject.SetActive(false);

        if (upgradedBroomVisual != null)
            upgradedBroomVisual.gameObject.SetActive(false);
    }



    private void Update()
    {
        if (!isSweeping)
            return;


        HandleMouseInput();
    }



    public void StartSweeping(DustSpot dustSpot)
    {
        if (dustSpot == null)
            return;

        currentDustSpot = dustSpot;


        progress = 0;
        isSweeping = true;
        mouseIsDown = false;

        ApplyBroomVisual(GetCurrentBroomLevel());
        ApplyDifficulty();


        if (minigamePanel != null)
            minigamePanel.SetActive(true);


        if (progressBar != null)
            progressBar.value = 0;


        Debug.Log("SWEEPING STARTED");
    }

    private void ApplyDifficulty()
    {
        int difficulty = DayManager.Instance != null ? DayManager.Instance.CurrentDifficulty : 0;
        cleaningSpeed = 0.5f - (difficulty * 0.05f);
        cleaningSpeed = Mathf.Max(cleaningSpeed, 0.25f);

        EconomyManager economyManager = EconomyManager.Instance;
        if (economyManager != null)
            cleaningSpeed *= economyManager.BroomCleaningSpeedMultiplier;

        Debug.Log("Sweeping difficulty: " + difficulty + " | Cleaning Speed: " + cleaningSpeed);
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
            activeBroom = selectedRect;
    }



    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;


        if (Mouse.current.leftButton.wasPressedThisFrame)
            mouseIsDown = true;


        if (Mouse.current.leftButton.wasReleasedThisFrame)
            mouseIsDown = false;


        if (!mouseIsDown)
            return;



        MoveBroom();


        if (IsInsideArea())
        {
            progress += cleaningSpeed * Time.deltaTime;


            if (progressBar != null)
                progressBar.value = progress;


            if (progress >= 1)
            {
                CompleteSweeping();
            }
        }
    }



    private void MoveBroom()
    {
        if (activeBroom == null || minigamePanel == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minigamePanel.GetComponent<RectTransform>(),
            mousePosition,
            null,
            out Vector2 localPosition
        );


        activeBroom.localPosition = localPosition;
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
            progressBar.value = 1;


        if (currentDustSpot != null)
        {
            currentDustSpot.Clean();
        }


        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        currentDustSpot = null;
        activeBroom = null;


        Debug.Log("SWEEPING COMPLETE!");
    }
}