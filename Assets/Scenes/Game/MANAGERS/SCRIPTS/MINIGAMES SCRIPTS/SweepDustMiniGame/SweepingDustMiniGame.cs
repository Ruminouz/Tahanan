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
    [SerializeField] private RectTransform sweepingArea;


    [Header("Cleaning")]
    [SerializeField] private float cleaningSpeed = 0.5f;


    private DustSpot currentDustSpot;


    private float progress;
    private bool isSweeping;
    private bool mouseIsDown;



    private void Start()
    {
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (progressBar != null)
            progressBar.value = 0;
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


        if (minigamePanel != null)
            minigamePanel.SetActive(true);


        if (progressBar != null)
            progressBar.value = 0;


        if (broom != null)
            broom.gameObject.SetActive(true);


        Debug.Log("SWEEPING STARTED");
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


            progressBar.value = progress;


            if (progress >= 1)
            {
                CompleteSweeping();
            }
        }
    }



    private void MoveBroom()
    {
        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        if (broom == null || minigamePanel == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minigamePanel.GetComponent<RectTransform>(),
            mousePosition,
            null,
            out Vector2 localPosition
        );


        broom.localPosition = localPosition;
    }



    private bool IsInsideArea()
    {
        if (sweepingArea == null || Mouse.current == null)
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


        Debug.Log("SWEEPING COMPLETE!");
    }
}