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

    [Header("Cleaning")]
    [SerializeField] private float cleaningSpeed = 0.5f;

    private WetArea currentWetArea;

    private float progress = 0f;
    private bool isMopping = false;
    private bool mouseIsDown = false;

    private void Start()
    {
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
    }

    private void Update()
    {
        if (!isMopping)
            return;

        HandleMouseInput();
    }

    public void StartMopping(WetArea wetArea)
    {
        currentWetArea = wetArea;

        progress = 0f;
        isMopping = true;
        mouseIsDown = false;

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(true);
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        if (mop != null)
        {
            mop.gameObject.SetActive(true);
        }

        Debug.Log("MOPPING MINIGAME STARTED!");
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

            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progress >= 1f)
            {
                CompleteMopping();
            }
        }
    }

    private void MoveMop()
    {
        if (mop == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minigamePanel.GetComponent<RectTransform>(),
            mousePosition,
            null,
            out Vector2 localPosition
        );

        mop.localPosition = localPosition;
    }

    private bool IsMopInsideMoppingArea()
    {
        if (mop == null || moppingArea == null)
            return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

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

        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        // Clean the spawned water.
        if (currentWetArea != null)
        {
            currentWetArea.Clean();
        }

        // Complete the sudden task.
        SuddenTaskManager suddenTaskManager =
            FindFirstObjectByType<SuddenTaskManager>();

        if (suddenTaskManager != null)
        {
            suddenTaskManager.CompleteMopTask();
        }
        else
        {
            Debug.LogWarning("SuddenTaskManager was not found.");
        }

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        Debug.Log("MOPPING COMPLETE!");
    }
}

