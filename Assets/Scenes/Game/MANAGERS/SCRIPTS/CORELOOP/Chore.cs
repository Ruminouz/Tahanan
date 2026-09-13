using UnityEngine;

public class Chore : Interactable
{
    [SerializeField] private string choreName;
    [SerializeField] private int points = 1;
<<<<<<< HEAD
    [Header("Chore Scoring")]
    [Tooltip("Time in seconds from the start of the day before this chore is marked missed.")]
    [Min(1f)]
    [SerializeField] private float deadlineSeconds = 300f;
    [Tooltip("Extra points awarded when this chore is completed before its deadline.")]
    [Min(0)]
    [SerializeField] private int earlyBonusPoints = 1;
=======
>>>>>>> 2ND-MAIN


    private bool isCompleted = false;
    private bool isMissed = false;

    

    public string ChoreName => choreName;

    public bool IsCompleted => isCompleted;

    public bool IsMissed => isMissed;

<<<<<<< HEAD
    public float DeadlineSeconds => deadlineSeconds;

    public float RemainingDeadline
    {
        get
        {
            TimeManager timeManager = FindFirstObjectByType<TimeManager>();
            if (timeManager == null)
                return deadlineSeconds;

            return Mathf.Max(0f, deadlineSeconds - timeManager.GetTime());
        }
    }

=======
>>>>>>> 2ND-MAIN



    // =========================
    // RESET NEW DAY
    // =========================

  public void ResetChore()
{
    isCompleted = false;
    isMissed = false;

    gameObject.SetActive(true);

    Debug.Log(choreName + " reset.");
}




    // =========================
    // DISABLE CHORE
    // =========================

    public void DisableChore()
    {
        isCompleted = false;
        isMissed = false;

        gameObject.SetActive(false);


        Debug.Log(
            choreName + " disabled."
        );
    }



    

    // =========================
    // INTERACT
    // =========================

      public override bool CanInteract()
    {
        return !isCompleted && !isMissed;
    }






    // =========================
    // COMPLETE
    // =========================

<<<<<<< HEAD
    public virtual void Complete()
=======
    public void Complete()
>>>>>>> 2ND-MAIN
    {

        if(isCompleted || isMissed)
            return;



        isCompleted = true;



        ChoreManager manager =
            FindFirstObjectByType<ChoreManager>();


        if(manager != null)
        {
<<<<<<< HEAD
            manager.CompleteChore(this, points, earlyBonusPoints);
=======
            manager.CompleteChore(points);
>>>>>>> 2ND-MAIN
        }



        Debug.Log(
            choreName 
            + " completed!"
        );

    }







    // =========================
    // MARK MISSED
    // =========================

    public void MarkAsMissed()
{
    if(isCompleted)
        return;


    isMissed = true;


    gameObject.SetActive(false);


    Debug.Log(
        choreName + " missed for today."
    );
}




    // =========================
    // STATUS
    // =========================

    public string GetStatus()
    {

        if(isCompleted)
            return "COMPLETED";


        if(isMissed)
            return "MISSED";


        return "PENDING";

    }

}