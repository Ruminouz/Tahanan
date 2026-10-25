using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform dailySpawnPoint;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private Vector3 dailySpawnPosition;
    private Quaternion dailySpawnRotation;
    private bool hasDailySpawnPoint;
    private bool movementInputEnabled = true;
    private float speedMultiplier = 1f;
    private float temporarySpeedMultiplier = 1f;
    private float temporarySlowMultiplier = 1f;
    private Coroutine temporarySlowCoroutine;
    private GameObject slowIcon;
    [SerializeField] private Sprite slowIconSprite;

    public float CurrentMoveSpeed =>
        moveSpeed * speedMultiplier * temporarySpeedMultiplier * temporarySlowMultiplier;
    public Vector3 DailySpawnPosition => hasDailySpawnPoint
        ? dailySpawnPosition
        : transform.position;
    public Vector2 CurrentMoveInput => moveInput;
    private bool playingFootsteps = false;
    public float footstepSpeed = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (dailySpawnPoint != null)
        {
            dailySpawnPosition = dailySpawnPoint.position;
            dailySpawnRotation = dailySpawnPoint.rotation;
            hasDailySpawnPoint = true;
        }
    }

    private void Start()
    {
        ResetToDailySpawn();
    }

    public void ResetToDailySpawn()
    {
        moveInput = Vector2.zero;
        temporarySpeedMultiplier = 1f;
        ClearTemporarySlow();

        if (hasDailySpawnPoint)
            transform.SetPositionAndRotation(dailySpawnPosition, dailySpawnRotation);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetBool("isWalking", false);
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (PauseController.IsGamePaused)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetBool("isWalking", false);
            StopFootsteps();
            return;
        }

        float moveVelocity = CurrentMoveSpeed > 0 ? CurrentMoveSpeed : moveSpeed;
        rb.linearVelocity = moveInput * moveVelocity;

        if (animator != null)
            animator.SetBool("isWalking", rb.linearVelocity.magnitude > 0);
        if(rb.linearVelocity.magnitude > 0 && !playingFootsteps)
    {
        StartFootsteps();
    }
    else if(rb.linearVelocity.magnitude == 0)
    {
        StopFootsteps();
    }
}

    public void Move(InputAction.CallbackContext context)
    {
        if (!movementInputEnabled)
            return;

        // Read raw input
        Vector2 input = context.ReadValue<Vector2>();

        if (context.performed)
        {
            moveInput = input;
            // Update direction vectors while moving
            if (animator != null)
            {
                animator.SetFloat("InputX", moveInput.x);
                animator.SetFloat("InputY", moveInput.y);
            }
        }
        else if (context.canceled)
        {
            // Store last valid movement direction BEFORE clearing moveInput
            if (moveInput != Vector2.zero && animator != null)
            {
                animator.SetFloat("LastInputX", moveInput.x);
                animator.SetFloat("LastInputY", moveInput.y);
            }

            moveInput = Vector2.zero;
            if (animator != null)
                animator.SetBool("isWalking", false);
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void SetMovementInputEnabled(bool enabled)
    {
        movementInputEnabled = enabled;
        if (enabled)
            return;

        moveInput = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (animator != null)
            animator.SetBool("isWalking", false);
        StopFootsteps();
    }

    public void ApplyTemporarySpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(TemporarySpeedBoost(multiplier, duration));
    }

    public void ApplyTemporarySlow(float multiplier, float duration)
    {
        if (duration <= 0f)
        {
            ClearTemporarySlow();
            return;
        }

        temporarySlowMultiplier = Mathf.Min(temporarySlowMultiplier, Mathf.Clamp(multiplier, 0.1f, 1f));
        ShowSlowIcon();

        if (temporarySlowCoroutine != null)
            StopCoroutine(temporarySlowCoroutine);

        temporarySlowCoroutine = StartCoroutine(TemporarySlow(duration));
    }

    private IEnumerator TemporarySlow(float duration)
    {
        yield return new WaitForSeconds(duration);
        temporarySlowCoroutine = null;
        temporarySlowMultiplier = 1f;
        DestroySlowIcon();
    }

    private void ClearTemporarySlow()
    {
        if (temporarySlowCoroutine != null)
        {
            StopCoroutine(temporarySlowCoroutine);
            temporarySlowCoroutine = null;
        }

        temporarySlowMultiplier = 1f;
        DestroySlowIcon();
    }

    private void DestroySlowIcon()
    {
        if (slowIcon != null)
        {
            Destroy(slowIcon);
            slowIcon = null;
        }
    }

    private void ShowSlowIcon()
    {
        if (slowIcon != null)
            return;

        if (slowIconSprite == null)
        {
            Debug.LogWarning("Slow icon sprite is not assigned on PlayerMovement.", this);
            return;
        }

        SpriteRenderer playerRenderer = GetComponentInChildren<SpriteRenderer>();
        slowIcon = new GameObject("Slow Debuff Icon");
        slowIcon.transform.SetParent(transform, false);
        slowIcon.transform.localPosition = new Vector3(
            0f,
            playerRenderer != null
                ? transform.InverseTransformPoint(playerRenderer.bounds.max).y + 0.2f
                : 1f,
            0f);
        slowIcon.transform.localScale = Vector3.one * 0.05f;

        GameObject iconVisual = new GameObject("Icon Visual");
        iconVisual.transform.SetParent(slowIcon.transform, false);
        SpriteRenderer iconRenderer = iconVisual.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = slowIconSprite;
        iconVisual.transform.localPosition = -iconRenderer.sprite.bounds.center;
        if (playerRenderer != null)
        {
            iconRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            iconRenderer.sortingOrder = playerRenderer.sortingOrder + 1;
        }
    }

    private IEnumerator TemporarySpeedBoost(float multiplier, float duration)
    {
        temporarySpeedMultiplier = Mathf.Max(temporarySpeedMultiplier, multiplier);
        yield return new WaitForSeconds(duration);
        temporarySpeedMultiplier = 1f;
    }

    void StartFootsteps()
    {
        playingFootsteps = true;
        InvokeRepeating(nameof(PlayFootsteps), 0f, footstepSpeed);
        
    }

    void StopFootsteps()
    {
        playingFootsteps = false;
        CancelInvoke(nameof(PlayFootsteps));
    }

    void PlayFootsteps()
    {
        SoundEffectManager.Play("Footstep", true);
    }
}