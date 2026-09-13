using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private float dayLength = 300f; // 5 minutes

    private float currentTime;
    private bool timeRunning = false;


    public float DayLength => dayLength;


    private void Start()
    {
        StartDayTime();
    }


    private void Update()
    {
        if (!timeRunning)
            return;


        currentTime += Time.deltaTime;


        if (currentTime >= dayLength)
        {
            currentTime = dayLength;
            timeRunning = false;

            Debug.Log("TIME'S UP!");
        }
    }



    public void StartDayTime()
    {
        currentTime = 0f;
        timeRunning = true;

        Debug.Log("Day timer reset and started.");
    }



    public void ResetDayTimer()
    {
        currentTime = 0f;
        timeRunning = true;

        Debug.Log("New day timer started.");
    }



    public float GetTime()
    {
        return currentTime;
    }



    public float GetRemainingTime()
    {
        return dayLength - currentTime;
    }



    public float GetTimePercentage()
    {
        return currentTime / dayLength;
    }
}