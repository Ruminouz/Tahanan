using UnityEngine;
using UnityEngine.UI;

public class WashableDish : MonoBehaviour
{
    [Header("Dish Visual")]
    [SerializeField] private Image dishImage;
    [SerializeField] private Color dirtyColor = Color.gray;
    [SerializeField] private Color cleanColor = Color.white;
    [SerializeField] private Color lateDayDirtyColor = new Color(0.55f, 0.43f, 0.32f, 1f);


    [Header("Scrubbing")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private float scrubAmount = 0.05f;


    [Header("Scrub Feedback")]
    [SerializeField] private GameObject scrubFoam;
    [SerializeField] private ParticleSystem scrubParticles;
    [SerializeField] private Sprite scrubBubbleSprite;
    [SerializeField, Min(0.05f)] private float scrubBubbleInterval = 0.16f;
    [SerializeField, Min(0.1f)] private float scrubBubbleLifetime = 0.65f;
    [SerializeField, Min(0f)] private float scrubBubbleRiseDistance = 42f;
    [SerializeField, Min(1f)] private float scrubBubbleSize = 14f;

    [Header("Rinse Feedback")]
    [SerializeField] private Sprite rinseSparkleSprite;
    [SerializeField, Min(1)] private int rinseSparkleCount = 6;
    [SerializeField, Min(0.1f)] private float rinseSparkleLifetime = 0.85f;
    [SerializeField, Min(1f)] private float rinseSparkleSize = 24f;
    [SerializeField] private Color rinseSparkleColor = new Color(1f, 0.94f, 0.64f, 1f);


    [Header("Audio")]
    [SerializeField] private AudioClip scrubSound;
    [SerializeField] private AudioClip cleanSound;



    private float progress;

    private bool isClean;

    private bool canScrub;
    private bool canRinse;
    private Color baseDirtyColor;
    private float scrubDifficultyMultiplier = 1f;



    private DishwashingMiniGame miniGame;
    private DishRinsePlate rinsePlate;

    private float lastScrubSfxTime;
    private const float ScrubSfxCooldown = 0.7f;

    private AudioSource audioSource;
    private const int MaxActiveScrubBubbles = 10;
    private float nextScrubBubbleTime;
    private readonly System.Collections.Generic.List<GameObject> activeScrubBubbles =
        new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<GameObject> activeRinseSparkles =
        new System.Collections.Generic.List<GameObject>();
    private static Sprite generatedRinseSparkleSprite;

    private void Awake()
    {
        baseDirtyColor = dirtyColor;
        ResolveScrubFoam();
    }

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
        lastScrubSfxTime = -999f;

        ResetDish();
    }




    // =========================================
    // SCRUB CONTROL
    // =========================================

    public void SetDishAppearance(Sprite dishSprite, float messLevel, float scrubRateMultiplier)
    {
        if (dishImage != null && dishSprite != null)
        {
            dishImage.sprite = dishSprite;
            dishImage.preserveAspect = true;
        }

        dirtyColor = Color.Lerp(baseDirtyColor, lateDayDirtyColor, Mathf.Clamp01(messLevel));
        scrubDifficultyMultiplier = Mathf.Clamp(scrubRateMultiplier, 0.5f, 1f);

        if (!isClean && dishImage != null)
            dishImage.color = dirtyColor;
    }


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




    public void SpongeScrub(float bonusMultiplier = 1f)
    {
        if (!canScrub || isClean)
            return;

        Scrub(1.4f * Mathf.Max(1f, bonusMultiplier));
    }

    private void Scrub(float bonusMultiplier = 1f)
    {
        progress += scrubAmount * scrubDifficultyMultiplier * bonusMultiplier;

        progress =
            Mathf.Clamp01(progress);
        SpawnScrubBubble();


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



        float timeSinceLastScrubSfx = Time.time - lastScrubSfxTime;
        if (timeSinceLastScrubSfx >= ScrubSfxCooldown)
        {
            if (scrubSound != null && audioSource != null)
                audioSource.PlayOneShot(scrubSound);

            SoundEffectManager.Play("Scrub");
            lastScrubSfxTime = Time.time;
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
        StopScrubBubbleAnimation();


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
            scrubFoam.SetActive(true);
            if (dishImage != null && scrubFoam.transform.parent == dishImage.transform.parent)
                scrubFoam.transform.SetSiblingIndex(dishImage.transform.GetSiblingIndex() + 1);
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


        isClean = false;

        canScrub = false;
        canRinse = false;
        transform.localScale = Vector3.one;
        StopScrubBubbleAnimation();
        StopRinseSparkleAnimation();



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

    public void RemoveScrubFoamAfterRinsing()
    {
        if (scrubFoam != null)
            scrubFoam.SetActive(false);
    }

    public void PlayRinseSparkle()
    {
        RectTransform sparkleParent = transform as RectTransform;
        if (sparkleParent == null)
            return;

        Sprite sparkleSprite = rinseSparkleSprite != null
            ? rinseSparkleSprite
            : GetGeneratedRinseSparkleSprite();

        SoundEffectManager.Play("Sparkle");

        for (int i = 0; i < rinseSparkleCount; i++)
        {
            GameObject sparkleObject = new GameObject(
                "RinseSparkle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            RectTransform sparkleRect = sparkleObject.GetComponent<RectTransform>();
            sparkleRect.SetParent(sparkleParent, false);
            sparkleRect.anchorMin = sparkleRect.anchorMax = new Vector2(0.5f, 0.5f);
            sparkleRect.pivot = new Vector2(0.5f, 0.5f);

            float size = rinseSparkleSize * Random.Range(0.75f, 1.25f);
            sparkleRect.sizeDelta = new Vector2(size, size);
            sparkleRect.anchoredPosition = new Vector2(
                Random.Range(-sparkleParent.rect.width * 0.34f, sparkleParent.rect.width * 0.34f),
                Random.Range(-sparkleParent.rect.height * 0.3f, sparkleParent.rect.height * 0.3f));
            sparkleRect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));

            Image sparkleImage = sparkleObject.GetComponent<Image>();
            sparkleImage.sprite = sparkleSprite;
            sparkleImage.color = rinseSparkleColor;
            sparkleImage.raycastTarget = false;
            sparkleImage.preserveAspect = true;
            sparkleRect.SetAsLastSibling();

            activeRinseSparkles.Add(sparkleObject);
            StartCoroutine(AnimateRinseSparkle(
                sparkleObject,
                sparkleRect,
                i * 0.06f,
                Random.Range(-35f, 35f)));
        }
    }

    private static Sprite GetGeneratedRinseSparkleSprite()
    {
        if (generatedRinseSparkleSprite != null)
            return generatedRinseSparkleSprite;

        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "Generated Rinse Sparkle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color32[] pixels = new Color32[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / textureSize * 2f - 1f);
                float ny = Mathf.Abs((y + 0.5f) / textureSize * 2f - 1f);
                float verticalWidth = Mathf.Lerp(0.28f, 0.045f, ny);
                float horizontalWidth = Mathf.Lerp(0.28f, 0.045f, nx);
                float vertical = ny < 1f ? 1f - Mathf.SmoothStep(verticalWidth * 0.65f, verticalWidth, nx) : 0f;
                float horizontal = nx < 1f ? 1f - Mathf.SmoothStep(horizontalWidth * 0.65f, horizontalWidth, ny) : 0f;
                float glow = Mathf.Clamp01((1f - Mathf.Sqrt(nx * nx + ny * ny)) * 0.22f);
                byte alpha = (byte)(Mathf.Clamp01(Mathf.Max(Mathf.Max(vertical, horizontal), glow)) * 255f);
                pixels[y * textureSize + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        generatedRinseSparkleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);
        generatedRinseSparkleSprite.name = "Generated Rinse Sparkle";
        generatedRinseSparkleSprite.hideFlags = HideFlags.HideAndDontSave;
        return generatedRinseSparkleSprite;
    }

    private System.Collections.IEnumerator AnimateRinseSparkle(
        GameObject sparkleObject,
        RectTransform sparkleRect,
        float delay,
        float rotationAmount)
    {
        while (delay > 0f && sparkleObject != null)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        if (sparkleObject == null)
            yield break;

        CanvasGroup canvasGroup = sparkleObject.GetComponent<CanvasGroup>();
        float duration = Mathf.Max(0.1f, rinseSparkleLifetime);
        float elapsed = 0f;
        Vector2 startPosition = sparkleRect.anchoredPosition;

        while (elapsed < duration && sparkleObject != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            sparkleRect.anchoredPosition = startPosition + Vector2.up * (8f * t);
            sparkleRect.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.1f, pulse);
            sparkleRect.Rotate(0f, 0f, rotationAmount * Time.deltaTime);
            canvasGroup.alpha = pulse;
            yield return null;
        }

        if (sparkleObject != null)
        {
            activeRinseSparkles.Remove(sparkleObject);
            Destroy(sparkleObject);
        }
    }

    private void StopRinseSparkleAnimation()
    {
        foreach (GameObject sparkle in activeRinseSparkles)
        {
            if (sparkle != null)
                Destroy(sparkle);
        }

        activeRinseSparkles.Clear();
    }

    private void ResolveScrubFoam()
    {
        if (scrubFoam != null)
            return;

        Transform foamTransform = transform.Find("Foam");
        if (foamTransform != null)
            scrubFoam = foamTransform.gameObject;
    }

    private void SpawnScrubBubble()
    {
        if (scrubBubbleSprite == null)
            return;

        if (Time.time < nextScrubBubbleTime)
            return;

        activeScrubBubbles.RemoveAll(bubble => bubble == null);
        if (activeScrubBubbles.Count >= MaxActiveScrubBubbles)
            return;

        RectTransform bubbleParent = transform as RectTransform;
        if (bubbleParent == null)
            return;

        GameObject bubbleObject = new GameObject(
            "ScrubBubble",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.SetParent(bubbleParent, false);
        bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);

        float size = scrubBubbleSize * Random.Range(0.7f, 1.25f);
        bubbleRect.sizeDelta = new Vector2(size, size);
        bubbleRect.anchoredPosition = new Vector2(
            Random.Range(-bubbleParent.rect.width * 0.28f, bubbleParent.rect.width * 0.28f),
            Random.Range(-bubbleParent.rect.height * 0.12f, bubbleParent.rect.height * 0.12f));

        Image bubbleImage = bubbleObject.GetComponent<Image>();
        bubbleImage.sprite = scrubBubbleSprite;
        bubbleImage.color = new Color(0.45f, 0.9f, 1f, 1f);
        bubbleImage.raycastTarget = false;
        bubbleImage.preserveAspect = true;
        bubbleObject.GetComponent<CanvasGroup>().alpha = 0.9f;
        bubbleRect.SetAsLastSibling();
        activeScrubBubbles.Add(bubbleObject);
        nextScrubBubbleTime = Time.time + scrubBubbleInterval;
        StartCoroutine(AnimateScrubBubble(bubbleObject, bubbleRect));
    }

    private System.Collections.IEnumerator AnimateScrubBubble(
        GameObject bubbleObject,
        RectTransform bubbleRect)
    {
        float duration = Mathf.Max(0.1f, scrubBubbleLifetime);
        float elapsed = 0f;
        Vector2 startPosition = bubbleRect.anchoredPosition;
        float riseDistance = scrubBubbleRiseDistance * Random.Range(0.75f, 1.25f);
        CanvasGroup canvasGroup = bubbleObject.GetComponent<CanvasGroup>();

        while (elapsed < duration && bubbleObject != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bubbleRect.anchoredPosition = startPosition + Vector2.up * (riseDistance * t);
            bubbleRect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, t);
            canvasGroup.alpha = (1f - t) * 0.9f;
            yield return null;
        }

        if (bubbleObject != null)
        {
            activeScrubBubbles.Remove(bubbleObject);
            Destroy(bubbleObject);
        }
    }

    private void StopScrubBubbleAnimation()
    {
        nextScrubBubbleTime = 0f;
        foreach (GameObject bubble in activeScrubBubbles)
        {
            if (bubble != null)
                Destroy(bubble);
        }

        activeScrubBubbles.Clear();
    }
}