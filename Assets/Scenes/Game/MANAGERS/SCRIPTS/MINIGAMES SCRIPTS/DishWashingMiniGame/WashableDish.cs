using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WashableDish : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
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



    private float progress;

    private bool isClean;

    private bool canScrub;
    private bool canRinse;
    private Vector2 lastPointerPosition;



    private DishwashingMiniGame miniGame;
    private DishRinsePlate rinsePlate;

    private AudioSource audioSource;



    public bool IsClean => isClean;



    private void Start()
    {
        miniGame =
            FindFirstObjectByType<DishwashingMiniGame>();

        rinsePlate =
            GetComponent<DishRinsePlate>();


        audioSource =
            GetComponent<AudioSource>();


        if(audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();
        }


        audioSource.playOnAwake = false;


        ResetDish();
    }




    // =========================================
    // SCRUB CONTROL
    // =========================================


    public void EnableScrubbing()
    {
        canScrub = true;
        canRinse = false;


        if(rinsePlate != null)
        {
            rinsePlate.DisableRinsing();
        }


        Debug.Log(
            "Dish enabled for scrubbing."
        );
    }



    public void DisableScrubbing()
    {
        canScrub = false;


        Debug.Log(
            "Dish scrubbing disabled."
        );
    }




    // =========================================
    // POINTER SCRUB
    // =========================================


    public void OnPointerEnter(
        PointerEventData eventData)
    {
        ProcessScrubInput(eventData);
    }

    public void OnPointerMove(
        PointerEventData eventData)
    {
        ProcessScrubInput(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        lastPointerPosition = Vector2.zero;
    }

    private void ProcessScrubInput(PointerEventData eventData)
    {
        if(!canScrub)
            return;


        if(isClean)
            return;


        if(Mouse.current == null)
            return;


        if(!Mouse.current.leftButton.isPressed)
        {
            lastPointerPosition = Vector2.zero;
            return;
        }


        if(miniGame != null)
        {
            if(!miniGame.CanScrubThisPlate(this))
                return;
        }


        float scrubBoost = 1f;

        if(lastPointerPosition != Vector2.zero)
        {
            float pointerTravel =
                Vector2.Distance(
                    eventData.position,
                    lastPointerPosition
                );

            scrubBoost = Mathf.Clamp01(pointerTravel / 18f) * 0.6f + 1f;
        }

        lastPointerPosition = eventData.position;
        Scrub(scrubBoost);
    }





    private void Scrub(float bonusMultiplier = 1f)
    {
        progress += scrubAmount * bonusMultiplier;

        progress =
            Mathf.Clamp01(progress);


        if(progressBar != null)
        {
            progressBar.value = progress;
        }



        if(scrubFoam != null &&
           !scrubFoam.activeSelf)
        {
            scrubFoam.SetActive(true);
        }



        if(scrubParticles != null)
        {
            if(!scrubParticles.isPlaying)
            {
                scrubParticles.Play();
            }

            scrubParticles.Emit(
                Mathf.CeilToInt(2f + bonusMultiplier * 2f)
            );
        }



        if(scrubSound != null &&
           audioSource != null)
        {
            audioSource.PlayOneShot(scrubSound);
        }



        if(dishImage != null)
        {
            dishImage.color =
                Color.Lerp(
                    dirtyColor,
                    cleanColor,
                    progress
                );
        }

        float targetScale = 1f + progress * 0.12f;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            new Vector3(targetScale, targetScale, 1f),
            0.35f
        );



        if(progress >= 1f)
        {
            CleanDish();
        }
    }




    private void CleanDish()
    {
        if(isClean)
            return;


        isClean = true;

        canScrub = false;
        canRinse = true;


        progress = 1f;



        if(progressBar != null)
        {
            progressBar.value = 1f;
        }



        if(dishImage != null)
        {
            dishImage.color = cleanColor;
        }



        if(scrubFoam != null)
        {
            scrubFoam.SetActive(false);
        }



        if(scrubParticles != null)
        {
            scrubParticles.Stop();
        }



        if(cleanSound != null &&
           audioSource != null)
        {
            audioSource.PlayOneShot(cleanSound);
        }



        Debug.Log(
            "Dish scrubbed successfully."
        );



        if(miniGame != null)
        {
            miniGame.PlateScrubbed(this);
        }
    }





    // =========================================
    // RESET
    // =========================================


    public void ResetDish()
    {
        progress = 0f;
        lastPointerPosition = Vector2.zero;


        isClean = false;

        canScrub = false;
        canRinse = false;
        transform.localScale = Vector3.one;



        if(progressBar != null)
        {
            progressBar.value = 0f;
        }



        if(dishImage != null)
        {
            dishImage.color = dirtyColor;
        }



        if(scrubFoam != null)
        {
            scrubFoam.SetActive(false);
        }



        if(scrubParticles != null)
        {
            scrubParticles.Stop();
        }



        if(rinsePlate != null)
        {
            rinsePlate.DisableRinsing();
        }
    }




    public bool CanRinse()
    {
        return canRinse;
    }
}