using UnityEngine;

public class PlayerTool : MonoBehaviour
{
    public bool hasBroom = false;

    public void PickupBroom()
    {
        hasBroom = true;

        Debug.Log("Player picked up broom!");
    }
}