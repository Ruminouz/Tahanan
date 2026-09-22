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
    private float speedMultiplier = 1f;
    private float temporarySpeedMultiplier = 1f;

    public float CurrentMoveSpeed => moveSpeed * speedMultiplier * temporarySpeedMultiplier;
    private bool playingFootsteps = false;
    public float footstepSpeed = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        ResetToDailySpawn();
    }

    public void ResetToDailySpawn()
    {
        moveInput = Vector2.zero;
        temporarySpeedMultiplier = 1f;

        if (dailySpawnPoint != null)
        {
            transform.SetPositionAndRotation(
                dailySpawnPoint.position,
                dailySpawnPoint.rotation);
        }

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

    public void ApplyTemporarySpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(TemporarySpeedBoost(multiplier, duration));
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