using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private bool playingFootsteps = false;
    public float footstepSpeed = 0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

  private void FixedUpdate()
{
    if(PauseController.IsGamePaused)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("isWalking", false);
            StopFootsteps();
            return;
        }
    rb.linearVelocity = moveInput * moveSpeed;
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
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);
        }
        else if (context.canceled)
        {
            // Store last valid movement direction BEFORE clearing moveInput
            if (moveInput != Vector2.zero)
            {
                animator.SetFloat("LastInputX", moveInput.x);
                animator.SetFloat("LastInputY", moveInput.y);
            }

            moveInput = Vector2.zero;
            animator.SetBool("isWalking", false);
        }
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