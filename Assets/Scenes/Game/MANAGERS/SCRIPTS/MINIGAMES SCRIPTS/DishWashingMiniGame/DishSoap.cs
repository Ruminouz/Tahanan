using UnityEngine;

public class DishSoap : MonoBehaviour
{
    public void ReceiveSponge(DishSponge sponge)
    {
        if (sponge == null)
            return;

        Debug.Log("Sponge touched dishwashing liquid.");

        sponge.AddSoap();
    }
}