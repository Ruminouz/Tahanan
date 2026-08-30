using UnityEngine;

public class DayManager : MonoBehaviour
{
    [SerializeField] private int currentDay = 1;

    [Header("Monday Chores")]
    [SerializeField] private Chore washDishes;
    [SerializeField] private Chore mopFloor;
    [SerializeField] private Chore sweepDust;

    [Header("Tuesday Chores")]
    [SerializeField] private Chore feedDog;
    [SerializeField] private Chore cleanLeaves;
    [SerializeField] private Chore throwTrash;

    private ChoreManager choreManager;

    private Chore[] activeChores;

    private int requiredChores;
    private bool dayFinished = false;

    public int CurrentDay => currentDay;

    // Allows other scripts to access the existing Mop Floor chore.
    public Chore MopFloorChore => mopFloor;

    private void Start()
    {
        choreManager = FindFirstObjectByType<ChoreManager>();

        StartDay();
    }

    private void Update()
    {
        if (dayFinished || choreManager == null)
            return;

        if (choreManager.completedChores >= requiredChores)
        {
            FinishDay();
        }
    }

    private void StartDay()
    {
        Debug.Log("Starting Day " + currentDay);

        ResetAllChores();

        if (currentDay == 1)
        {
            SetupMonday();
        }
        else if (currentDay == 2)
        {
            SetupTuesday();
        }
        else
        {
            Debug.Log("Day " + currentDay + " setup is not ready yet.");
            activeChores = new Chore[0];
        }
    }

    private void SetupMonday()
    {
        requiredChores = 3;

        EnableChore(washDishes);
        EnableChore(mopFloor);
        EnableChore(sweepDust);

        DisableChore(feedDog);
        DisableChore(cleanLeaves);
        DisableChore(throwTrash);

        activeChores = new Chore[]
        {
            washDishes,
            mopFloor,
            sweepDust
        };

        Debug.Log("Monday chores loaded: " + activeChores.Length);
    }

    private void SetupTuesday()
    {
        requiredChores = 6;

        EnableChore(washDishes);
        EnableChore(mopFloor);
        EnableChore(sweepDust);

        EnableChore(feedDog);
        EnableChore(cleanLeaves);
        EnableChore(throwTrash);

        activeChores = new Chore[]
        {
            washDishes,
            mopFloor,
            sweepDust,
            feedDog,
            cleanLeaves,
            throwTrash
        };

        Debug.Log("Tuesday chores loaded: " + activeChores.Length);
    }

    private void EnableChore(Chore chore)
    {
        if (chore != null)
        {
            chore.ResetChore();
        }
    }

    private void DisableChore(Chore chore)
    {
        if (chore != null)
        {
            chore.DisableChore();
        }
    }

    private void ResetAllChores()
    {
        DisableChore(washDishes);
        DisableChore(mopFloor);
        DisableChore(sweepDust);
        DisableChore(feedDog);
        DisableChore(cleanLeaves);
        DisableChore(throwTrash);
    }

    public Chore[] GetActiveChores()
    {
        return activeChores;
    }

    private void FinishDay()
    {
        dayFinished = true;

        Debug.Log("DAY " + currentDay + " COMPLETE!");

        if (currentDay < 7)
        {
            StartNextDay();
        }
        else
        {
            Debug.Log("WEEK COMPLETE!");
        }
    }

    private void StartNextDay()
    {
        currentDay++;

        choreManager.completedChores = 0;

        dayFinished = false;

        StartDay();
    }
}