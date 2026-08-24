using UnityEngine;

public class ChoreManager : MonoBehaviour
{
    public int completedChores = 0;
    public int missedChores = 0;
    public int totalPoints = 0;

    public void CompleteChore(int points)
    {
        completedChores++;
        totalPoints += points;

        Debug.Log("Chore Completed!");
        Debug.Log("Completed Chores: " + completedChores);
        Debug.Log("Total Points: " + totalPoints);
    }

    public void MissChore()
    {
        missedChores++;

        Debug.Log("Chore Missed!");
        Debug.Log("Missed Chores: " + missedChores);
    }
}