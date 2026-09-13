using UnityEngine;

public class GarbageBin : Interactable
{

    [SerializeField] private GarbageChore garbageChore;


    public override void Interact()
    {

        if (!GarbageCarry.Instance.HasGarbage())
        {
            Debug.Log("Need garbage bag first!");
            return;
        }


        Debug.Log("Garbage delivered");


        GarbageCarry.Instance.RemoveBag();


        garbageChore.Interact();

    }
}