using UnityEngine;

public class Chore : Interactable
{
    [SerializeField] private string choreName;
    [SerializeField] private int points = 1;


    private bool isCompleted = false;
    private bool isMissed = false;

    

    public string ChoreName => choreName;

    public bool IsCompleted => isCompleted;

    public bool IsMissed => isMissed;




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

    public void Complete()
    {

        if(isCompleted || isMissed)
            return;



        isCompleted = true;



        ChoreManager manager =
            FindFirstObjectByType<ChoreManager>();


        if(manager != null)
        {
            manager.CompleteChore(points);
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