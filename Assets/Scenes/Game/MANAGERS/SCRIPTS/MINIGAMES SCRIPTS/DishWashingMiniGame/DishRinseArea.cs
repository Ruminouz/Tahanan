using UnityEngine;
using System.Collections.Generic;

public class DishRinseArea : MonoBehaviour
{
    private readonly List<DishRinsePlate> platesWaitingForRinse =
        new List<DishRinsePlate>();

    public void ReceivePlate(DishRinsePlate plate)
    {
        if (plate == null || platesWaitingForRinse.Contains(plate))
            return;

        platesWaitingForRinse.Add(plate);

        Debug.Log("Plate waiting for the faucet.");
    }

    public void CompleteRinsing()
    {
        if (platesWaitingForRinse.Count == 0)
            return;

        DishRinsePlate[] plates = platesWaitingForRinse.ToArray();
        platesWaitingForRinse.Clear();
        foreach (DishRinsePlate plate in plates)
        {
            if (plate != null)
                plate.FinishRinsing();
        }
    }

    public void ResetArea()
    {
        platesWaitingForRinse.Clear();
    }
}