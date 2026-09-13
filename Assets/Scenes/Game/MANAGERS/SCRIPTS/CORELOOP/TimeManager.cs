using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private float dayLength = 300f; // 5 minutes

    private float currentTime;
    private bool timeRunning = false;

<<<<<<< Updated upstream:Assets/Scenes/Game/MANAGERS/SCRIPTS/CORELOOP/TimeManager.cs
=======
<<<<<<< Updated upstream:Assets/JAMESTEST/CORELOOP/TimeManager.cs
<<<<<<< Updated upstream:Assets/JAMESTEST/CORELOOP/TimeManager.cs
=======
=======
<<<<<<< Updated upstream:Assets/Scenes/Game/MANAGERS/SCRIPTS/CORELOOP/TimeManager.cs
>>>>>>> Stashed changes:Assets/Scenes/Game/MANAGERS/SCRIPTS/CORELOOP/TimeManager.cs
>>>>>>> Stashed changes:Assets/JAMESTEST/CORELOOP/TimeManager.cs

    public float DayLength => dayLength;


<<<<<<< Updated upstream:Assets/Scenes/Game/MANAGERS/SCRIPTS/CORELOOP/TimeManager.cs
=======
<<<<<<< Updated upstream:Assets/JAMESTEST/CORELOOP/TimeManager.cs

>>>>>>> Stashed changes:Assets/Scenes/Game/MANAGERS/SCRIPTS/CORELOOP/TimeManager.cs
=======
=======
>>>>>>> Stashed changes:Assets/JAMESTEST/CORELOOP/TimeManager.cs
>>>>>>> Stashed changes:Assets/Scenes/Game/MANAGERS/SCRIPTS/CORELOOP/TimeManager.cs
>>>>>>> Stashed changes:Assets/JAMESTEST/CORELOOP/TimeManager.cs
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