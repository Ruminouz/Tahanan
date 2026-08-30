using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WashableDish : MonoBehaviour, IPointerEnterHandler
{
    [Header("Dish Visual")]
    [SerializeField] private Image dishImage;
    [SerializeField] private Color dirtyColor = Color.gray;
    [SerializeField] private Color cleanColor = Color.white;

    [Header("Scrubbing")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private float scrubAmount = 0.05f;

    [Header("Scrub Feedback")]
    [SerializeField] private GameObject scrubFoam;
    [SerializeField] private ParticleSystem scrubParticles;

    [Header("Audio")]
    [SerializeField] private AudioClip scrubSound;
    [SerializeField] private AudioClip cleanSound;

    private float progress = 0f;

    private bool isClean = false;
    private bool canScrub = false;
    private bool canRinse = false;

    private DishwashingMiniGame miniGame;
    private DishRinsePlate rinsePlate;

    private AudioSource audioSource;

    public bool IsClean => isClean;
    private bool IsTopPlate()
{
    if (transform.parent == null)
        return true;

    Transform parent = transform.parent;

    // The last sibling is visually on top.
    return transform.GetSiblingIndex() ==
           parent.childCount - 1;
}
    private void Start()
    {
        miniGame = FindFirstObjectByType<DishwashingMiniGame>();
        rinsePlate = GetComponent<DishRinsePlate>();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;

        ResetDish();
    }

    public void EnableScrubbing()
    {
        canScrub = true;
        canRinse = false;

        if (rinsePlate != null)
        {
            rinsePlate.DisableRinsing();
        }

        if (scrubFoam != null)
        {
            scrubFoam.SetActive(false);
        }

        Debug.Log("Dish is ready to scrub!");
    }

    public void EnableRinsing()
    {
        canScrub = false;
        canRinse = true;

        if (scrubFoam != null)
        {
            scrubFoam.SetActive(false);
        }

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

    // Only the TOP plate can be scrubbed.
    if (!IsTopPlate())
        return;

    Scrub();
}
    private void Scrub()
    {
        progress += scrubAmount;

        progress = Mathf.Clamp01(progress);

        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        // Show foam while scrubbing.
        if (scrubFoam != null && !scrubFoam.activeSelf)
        {
            scrubFoam.SetActive(true);
        }

        // Play particles.
        if (scrubParticles != null && !scrubParticles.isPlaying)
        {
            scrubParticles.Play();
        }

        // Scrub sound.
        if (scrubSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(scrubSound);
        }

        // Slightly transition dirty plate toward clean.
        if (dishImage != null)
        {
            dishImage.color = Color.Lerp(
                dirtyColor,
                cleanColor,
                progress
            );
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
            dishImage.color = cleanColor;
        }

        if (scrubFoam != null)
        {
            scrubFoam.SetActive(false);
        }

        if (scrubParticles != null)
        {
            scrubParticles.Stop();
        }

        if (cleanSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cleanSound);
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
            dishImage.color = dirtyColor;
        }

        if (scrubFoam != null)
        {
            scrubFoam.SetActive(false);
        }

        if (scrubParticles != null)
        {
            scrubParticles.Stop();
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
