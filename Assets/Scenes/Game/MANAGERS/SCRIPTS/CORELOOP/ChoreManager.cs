using UnityEngine;

public class ChoreManager : MonoBehaviour
{
    public int completedChores = 0;
    public int missedChores = 0;

    public int totalPoints = 0;


    // Automatically calculates finished chores
    public int finishedChores
    {
        get
        {
            return completedChores + missedChores;
        }
    }



    public void CompleteChore(int points)
    {
        completedChores++;

        totalPoints += points;


        Debug.Log("Chore Completed!");

        Debug.Log(
            "Completed Chores: " +
            completedChores
        );

        Debug.Log(
            "Finished Chores: " +
            finishedChores
        );
    }



    // OLD VERSION
    public void MissChore()
    {
        missedChores++;


        Debug.Log("Chore Missed!");

        Debug.Log(
            "Missed Chores: " +
            missedChores
        );

        Debug.Log(
            "Finished Chores: " +
            finishedChores
        );
    }



    // NEW VERSION
    public void MissChore(Chore chore)
    {
        missedChores++;


        if (chore != null)
        {
            Debug.Log(
                chore.ChoreName +
                " Marked as Missed!"
            );

            chore.MarkAsMissed();
        }


        Debug.Log(
            "Missed Chores: " +
            missedChores
        );


        Debug.Log(
            "Finished Chores: " +
            finishedChores
        );
    }



    public void ResetDailyProgress()
    {
        completedChores = 0;
        missedChores = 0;
        totalPoints = 0;


        Debug.Log(
            "Daily chore progress reset."
        );
    }
}