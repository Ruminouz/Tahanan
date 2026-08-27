using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WashableDish : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Image dishImage;
    [SerializeField] private Slider progressBar;

    [SerializeField] private float scrubAmount = 0.05f;

    private float progress = 0f;

    private bool isClean = false;
    private bool canScrub = false;
    private bool canRinse = false;

    private DishwashingMiniGame miniGame;
    private DishRinsePlate rinsePlate;

    public bool IsClean => isClean;

    private void Start()
    {
        miniGame = FindFirstObjectByType<DishwashingMiniGame>();
        rinsePlate = GetComponent<DishRinsePlate>();
    }

    public void EnableScrubbing()
    {
        canScrub = true;
        canRinse = false;

        if (rinsePlate != null)
        {
            rinsePlate.DisableRinsing();
        }

        Debug.Log("Dish is ready to scrub!");
    }

    public void EnableRinsing()
    {
        canScrub = false;
        canRinse = true;

        if (rinsePlate != null)
        {
            rinsePlate.EnableRinsing();
        }

        Debug.Log("Dish is ready for rinsing!");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canScrub)
            return;

        if (isClean)
            return;

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.isPressed)
            return;

        Scrub();
    }

    private void Scrub()
    {
        progress += scrubAmount;

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progress >= 1f)
        {
            CleanDish();
        }
    }

    private void CleanDish()
    {
        if (isClean)
            return;

        isClean = true;
        canScrub = false;
        canRinse = true;

        progress = 1f;

        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        if (dishImage != null)
        {
            dishImage.color = Color.white;
        }

        Debug.Log("Dish scrubbed!");

        if (miniGame != null)
        {
            miniGame.PlateScrubbed(this);
        }
    }

    public void ResetDish()
    {
        progress = 0f;

        isClean = false;
        canScrub = false;
        canRinse = false;

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        if (dishImage != null)
        {
            dishImage.color = Color.gray;
        }

        if (rinsePlate != null)
        {
            rinsePlate.DisableRinsing();
        }
    }

    public bool CanRinse()
    {
        return canRinse;
    }
}