using UnityEngine;

public class MoppingPlayerState : MonoBehaviour
{
    public bool HasMop { get; private set; }

    public void PickUpMop()
    {
        HasMop = true;

        Debug.Log("Player now has the mop.");
    }

    public void ResetMop()
    {
        HasMop = false;
    }
}