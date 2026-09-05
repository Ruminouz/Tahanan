using UnityEngine;


public class GarbageBag : Interactable
{

    [SerializeField] private GarbageChore garbageChore;



    public override void Interact()
    {

        Debug.Log(
            "Picked up garbage bag"
        );



        if(GarbageCarry.Instance != null)
        {

            GarbageCarry.Instance.Pickup(
                gameObject
            );


            garbageChore.EnableGarbageBin();

        }


    }

}