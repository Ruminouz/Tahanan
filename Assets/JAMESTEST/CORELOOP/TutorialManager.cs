using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    private List<string> learnedChores = new List<string>();

    public bool HasLearned(string choreName)
    {
        return learnedChores.Contains(choreName);
    }

    public void MarkAsLearned(string choreName)
    {
        if (!learnedChores.Contains(choreName))
        {
            learnedChores.Add(choreName);

            Debug.Log("Learned chore: " + choreName);
        }
    }
}