using UnityEngine;

public class Interactable : MonoBehaviour, IInteractable
{
    public virtual void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);
    }

    public virtual bool CanInteract()
    {
        return true;
    }
}