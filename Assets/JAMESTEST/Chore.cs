using UnityEngine;

public class Chore : Interactable
{
    [SerializeField] private string choreName;
    [SerializeField] private int points = 1;

    private bool isCompleted = false;

    public string ChoreName => choreName;
    public bool IsCompleted => isCompleted;

    public void ResetChore()
    {
        isCompleted = false;
        gameObject.SetActive(true);
    }

    public void DisableChore()
    {
        gameObject.SetActive(false);
    }

    public override void Interact()
    {
        if (isCompleted)
        {
            Debug.Log(choreName + " is already completed.");
            return;
        }

        Debug.Log("Starting chore: " + choreName);
    }

    public void Complete()
    {
        if (isCompleted)
            return;

        isCompleted = true;

        ChoreManager manager = FindFirstObjectByType<ChoreManager>();

        if (manager != null)
        {
            manager.CompleteChore(points);
        }

        Debug.Log(choreName + " completed!");
    }
}