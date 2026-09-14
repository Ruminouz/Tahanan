using UnityEngine;


public class GarbageSortingMiniGame : MonoBehaviour
{

    public static GarbageSortingMiniGame Instance;


    [SerializeField] private GameObject panel;

    [SerializeField] private GarbageSpawner spawner;


    private Chore currentChore;


    [Header("DAY SETTINGS")]
    [SerializeField] private int currentDay = 2;


    private int mistakes;
    private int correct;


    private int maxMistakes = 5;


    private int totalTrash;

    private DayManager dayManager;
    



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private DayManager ResolveDayManager()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance != null
                ? DayManager.Instance
                : FindFirstObjectByType<DayManager>();
        }

        return dayManager;
    }





    public void StartGame(Chore chore)
{
    if (chore == null || panel == null || spawner == null)
    {
        Debug.LogWarning("Garbage sorting references are incomplete.");
        return;
    }

    currentChore = chore;

    spawner.StopSpawning();


    mistakes = 0;
    correct = 0;
    



    DayManager resolvedDayManager = ResolveDayManager();

    if (resolvedDayManager != null)
        currentDay = resolvedDayManager.CurrentDay;

    currentDay = Mathf.Clamp(currentDay, 1, 7);

    totalTrash = GetTrashAmount();



    panel.SetActive(true);



    Debug.Log(
        "Garbage Target: "
        + totalTrash
    );



    spawner.StartSpawning(
        GetSpawnRate(),
        GetFallSpeed()
    );

}





    public void CheckTrash(
        TrashItem trash,
        TrashBin bin
    )
    {

        Debug.Log(
            "Trash: "
            + trash.trashType +
            " Bin: "
            + bin.binType
        );



        if(trash.trashType == bin.binType)
        {
            CorrectTrash(trash);
        }
        else
        {
            WrongTrash();
        }

    }





private void CorrectTrash(TrashItem trash)
{

    correct++;

    


    Debug.Log(
        "Correct Trash "
        + correct
        + "/"
        + totalTrash
    );


    Destroy(trash.gameObject);


    CheckCompletion();

}







    private void WrongTrash()
    {

        mistakes++;


        Debug.Log(
            "Mistakes "
            + mistakes
            +
            "/5"
        );



        if(mistakes >= maxMistakes)
        {
            FailedGame();
        }

    }








    public void TrashMissed()
    {

        mistakes++;


        Debug.Log(
            "Trash missed "
            +
            mistakes
            +
            "/5"
        );



        if(mistakes >= maxMistakes)
        {
            FailedGame();
        }

    }









    private void CheckCompletion()
{

    if(correct >= totalTrash)
    {
        CompleteGame();
    }

}





private void CompleteGame()
{

    if(currentChore == null)
        return;


    Debug.Log(
        "Garbage Sorting Complete!"
    );


    currentChore.Complete();


    if(spawner != null)
    {
        spawner.StopSpawning();
    }


    panel.SetActive(false);


    currentChore = null;

}



   private void FailedGame()
{

    Debug.Log(
        "Garbage Sorting FAILED"
    );


    if(spawner != null)
    {
        spawner.StopSpawning();
    }

    if (currentChore != null)
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();

        if (choreManager != null)
            choreManager.MissChore(currentChore);
    }


    panel.SetActive(false);


    currentChore = null;

}








private int GetTrashAmount()
{
    switch(currentDay)
    {
        case 2:
            return 5;

        case 3:
            return 7;

        case 4:
            return 8;

        case 5:
        case 6:
            return 10;

        case 7:
            return 15;


        default:
            return 5;
    }
}





    private float GetSpawnRate()
    {

        switch(currentDay)
        {

            case 2:
                return 1.5f;

            case 3:
                return 1.3f;

            case 4:
                return 1.1f;

            case 5:
                return 0.9f;

            case 6:
                return 0.7f;

            case 7:
                return 0.6f;


            default:
                return 1.5f;

        }

    }








    private float GetFallSpeed()
    {

        switch(currentDay)
        {

            case 2:
                return 100f;

            case 3:
                return 120f;

            case 4:
                return 140f;

            case 5:
                return 160f;

            case 6:
                return 190f;

            case 7:
                return 220f;


            default:
                return 100f;

        }

    }

}