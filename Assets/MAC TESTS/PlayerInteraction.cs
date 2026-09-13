using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 1.5f;

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("PLAYER PRESSED E!");

            TryInteract();
        }
    }

    private void TryInteract()
    {
        Debug.Log("Trying to interact...");

        Collider2D[] objects = Physics2D.OverlapCircleAll(
            transform.position,
            interactRange
        );

        Debug.Log("Objects detected: " + objects.Length);

        foreach (Collider2D obj in objects)
        {
            Debug.Log("Detected object: " + obj.gameObject.name);

            Interactable interactable = obj.GetComponent<Interactable>();

            if (interactable != null)
            {
                Debug.Log("INTERACTABLE FOUND!");

                interactable.Interact();
                return;
            }
        }

        Debug.Log("No interactable found.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}