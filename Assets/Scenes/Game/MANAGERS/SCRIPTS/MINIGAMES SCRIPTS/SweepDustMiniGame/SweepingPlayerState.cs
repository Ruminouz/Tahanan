using UnityEngine;

public class SweepingPlayerState : MonoBehaviour
{
    public bool HasBroom { get; private set; }


    public void PickUpBroom()
    {
        HasBroom = true;

        Debug.Log("Player now has the broom.");
    }


    public void ResetBroom()
    {
        HasBroom = false;
    }
}