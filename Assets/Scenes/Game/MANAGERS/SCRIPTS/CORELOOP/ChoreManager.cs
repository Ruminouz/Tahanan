using UnityEngine;
<<<<<<< HEAD
using System;
using System.Collections.Generic;

public class ChoreManager : MonoBehaviour
{
    public event Action<Chore> ChoreMissed;
=======

public class ChoreManager : MonoBehaviour
{
>>>>>>> 2ND-MAIN
    public int completedChores = 0;
    public int missedChores = 0;

    public int totalPoints = 0;

<<<<<<< HEAD
    private readonly HashSet<Chore> helperCompletedChores = new HashSet<Chore>();

    public int TotalChores => completedChores + missedChores;

    public float SuccessRate => TotalChores == 0
        ? 0f
        : (float)completedChores / TotalChores * 100f;

    public bool WasCompletedByHelper(Chore chore)
    {
        return chore != null && helperCompletedChores.Contains(chore);
    }

    public void MarkHelperCompleted(Chore chore)
    {
        if (chore != null)
            helperCompletedChores.Add(chore);
    }

=======
>>>>>>> 2ND-MAIN

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

<<<<<<< HEAD
        EconomyManager economyManager = FindFirstObjectByType<EconomyManager>();
        if (economyManager != null)
            economyManager.AwardChorePoints(points);

        MoodManager moodManager = FindFirstObjectByType<MoodManager>();
        if (moodManager != null)
            moodManager.HandleCompletedChore();

=======
>>>>>>> 2ND-MAIN

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

<<<<<<< HEAD
    public void CompleteChore(Chore chore, int points, int earlyBonusPoints)
    {
        int earnedPoints = points;
        if (chore != null && chore.RemainingDeadline > 0f)
            earnedPoints += earlyBonusPoints;

        CompleteChore(earnedPoints);
    }

=======
>>>>>>> 2ND-MAIN


    // OLD VERSION
    public void MissChore()
    {
        missedChores++;
<<<<<<< HEAD
        ChoreMissed?.Invoke(null);
=======
>>>>>>> 2ND-MAIN


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
<<<<<<< HEAD
        ChoreMissed?.Invoke(chore);
=======
>>>>>>> 2ND-MAIN


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
<<<<<<< HEAD
        helperCompletedChores.Clear();
=======
>>>>>>> 2ND-MAIN


        Debug.Log(
            "Daily chore progress reset."
        );
    }
<<<<<<< HEAD

    public void CompleteDynamicChore(int points)
    {
        CompleteChore(points);
    }

    public void MissDynamicChore()
    {
        MissChore();
    }
=======
>>>>>>> 2ND-MAIN
}