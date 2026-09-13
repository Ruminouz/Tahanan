using UnityEngine;
using System;
using System.Collections.Generic;

public class ChoreManager : MonoBehaviour
{
    public event Action<Chore> ChoreMissed;
    public int completedChores = 0;
    public int missedChores = 0;

    public int totalPoints = 0;

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

        EconomyManager economyManager = FindFirstObjectByType<EconomyManager>();
        if (economyManager != null)
            economyManager.AwardChorePoints(points);

        MoodManager moodManager = FindFirstObjectByType<MoodManager>();
        if (moodManager != null)
            moodManager.HandleCompletedChore();


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

    public void CompleteChore(Chore chore, int points, int earlyBonusPoints)
    {
        int earnedPoints = points;
        if (chore != null && chore.RemainingDeadline > 0f)
            earnedPoints += earlyBonusPoints;

        CompleteChore(earnedPoints);
    }



    // OLD VERSION
    public void MissChore()
    {
        missedChores++;
        ChoreMissed?.Invoke(null);


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
        ChoreMissed?.Invoke(chore);


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
        helperCompletedChores.Clear();


        Debug.Log(
            "Daily chore progress reset."
        );
    }

    public void CompleteDynamicChore(int points)
    {
        CompleteChore(points);
    }

    public void MissDynamicChore()
    {
        MissChore();
    }
}