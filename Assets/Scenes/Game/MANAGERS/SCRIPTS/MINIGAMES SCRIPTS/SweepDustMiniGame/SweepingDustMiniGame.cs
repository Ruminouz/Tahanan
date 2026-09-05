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
        minigamePanel.SetActive(false);

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
        currentDustSpot = dustSpot;


        progress = 0;
        isSweeping = true;
        mouseIsDown = false;


        minigamePanel.SetActive(true);


        progressBar.value = 0;


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


        progressBar.value = 1;


        if (currentDustSpot != null)
        {
            currentDustSpot.Clean();
        }


        minigamePanel.SetActive(false);


        Debug.Log("SWEEPING COMPLETE!");
    }
}