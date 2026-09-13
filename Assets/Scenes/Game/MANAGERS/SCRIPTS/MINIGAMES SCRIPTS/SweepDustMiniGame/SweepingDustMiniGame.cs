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
<<<<<<< HEAD
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        if (progressBar != null)
            progressBar.value = 0;
=======
        minigamePanel.SetActive(false);

        progressBar.value = 0;
>>>>>>> 2ND-MAIN
    }



    private void Update()
    {
        if (!isSweeping)
            return;


        HandleMouseInput();
    }



    public void StartSweeping(DustSpot dustSpot)
    {
<<<<<<< HEAD
        if (dustSpot == null)
            return;

=======
>>>>>>> 2ND-MAIN
        currentDustSpot = dustSpot;


        progress = 0;
        isSweeping = true;
        mouseIsDown = false;


<<<<<<< HEAD
        if (minigamePanel != null)
            minigamePanel.SetActive(true);


        if (progressBar != null)
            progressBar.value = 0;


        if (broom != null)
            broom.gameObject.SetActive(true);
=======
        minigamePanel.SetActive(true);


        progressBar.value = 0;


        broom.gameObject.SetActive(true);
>>>>>>> 2ND-MAIN


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


<<<<<<< HEAD
        if (broom == null || minigamePanel == null)
            return;

=======
>>>>>>> 2ND-MAIN
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
<<<<<<< HEAD
        if (sweepingArea == null || Mouse.current == null)
            return false;

=======
>>>>>>> 2ND-MAIN
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


<<<<<<< HEAD
        if (progressBar != null)
            progressBar.value = 1;
=======
        progressBar.value = 1;
>>>>>>> 2ND-MAIN


        if (currentDustSpot != null)
        {
            currentDustSpot.Clean();
        }


<<<<<<< HEAD
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        currentDustSpot = null;
=======
        minigamePanel.SetActive(false);
>>>>>>> 2ND-MAIN


        Debug.Log("SWEEPING COMPLETE!");
    }
}