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
            return;
        }
    rb.linearVelocity = moveInput * moveSpeed;
    animator.SetBool("isWalking", rb.linearVelocity.magnitude > 0);
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
}