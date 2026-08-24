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

    public bool IsClean => isClean;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isClean)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Scrub();
        }
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
    public void ResetDish()
{
    progress = 0f;
    isClean = false;

    if (progressBar != null)
    {
        progressBar.value = 0f;
    }

    if (dishImage != null)
    {
        dishImage.color = Color.gray;
    }
}

    private void CleanDish()
    {
        isClean = true;
        progress = 1f;

        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        if (dishImage != null)
        {
            dishImage.color = Color.white;
        }

        Debug.Log("Dish washed!");
    }
}