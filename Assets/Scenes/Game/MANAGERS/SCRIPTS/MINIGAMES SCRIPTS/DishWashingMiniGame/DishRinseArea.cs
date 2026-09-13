using UnityEngine;

public class DishRinseArea : MonoBehaviour
{
    [SerializeField] private float rinseTime = 1.5f;

    private DishRinsePlate currentPlate;
    private float rinseProgress = 0f;

    private void Update()
    {
        if (currentPlate == null)
            return;

        rinseProgress += Time.deltaTime;

        if (rinseProgress >= rinseTime)
        {
            FinishRinsing();
        }
    }

    public void ReceivePlate(DishRinsePlate plate)
    {
        if (currentPlate != null)
            return;

        currentPlate = plate;
        rinseProgress = 0f;

        Debug.Log("Rinsing plate...");
    }

    private void FinishRinsing()
    {
        if (currentPlate == null)
            return;

        Debug.Log("Rinse complete!");

        currentPlate.FinishRinsing();

        currentPlate = null;
        rinseProgress = 0f;
    }
}