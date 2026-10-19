using UnityEngine;
using TMPro;


public class GarbageSortingMiniGame : MonoBehaviour
{

    public static GarbageSortingMiniGame Instance;


    [SerializeField] private GameObject panel;

    [SerializeField] private GarbageSpawner spawner;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private TMP_Text mistakesCounterText;
    [SerializeField] private TMP_Text correctCounterText;


    private Chore currentChore;
    private TutorialManager tutorialManager;
    private bool firstTimeTutorialActive;
    private bool tutorialTextWarningLogged;


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
        Instance = this;
    }

    private void Start()
    {
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        ResolveTutorialText();
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
    if (tutorialManager == null)
        tutorialManager = FindFirstObjectByType<TutorialManager>();

    ResolveTutorialText();
    firstTimeTutorialActive = tutorialManager == null ||
        !tutorialManager.HasLearned(chore.ChoreName);
    SetTutorialText(string.Empty);

    spawner.StopSpawning();


    mistakes = 0;
    correct = 0;



    DayManager resolvedDayManager = ResolveDayManager();

    if (resolvedDayManager != null)
        currentDay = resolvedDayManager.CurrentDay;

    currentDay = Mathf.Clamp(currentDay, 1, 7);

    totalTrash = GetTrashAmount();
    UpdateCounterTexts();
    MiniGameInteractionBlocker.RegisterPanel(panel);
    panel.SetActive(true);
    ShowTutorial(
        "I-drag ang bawat nahuhulog na basura sa tamang lalagyan. Itugma ang uri sa label: Nabubulok, Di-nabubulok, o Recyclable. Bawat maling lagay o hindi nasagip na basura ay dagdag sa 5 pagkakamali; kapag umabot dito, matatalo ka.");



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
            WrongTrash(trash.trashType, bin.binType);
        }

    }





private void CorrectTrash(TrashItem trash)
{

    correct++;
    UpdateCounterTexts();
    SoundEffectManager.Play("Trash");
    ShowTutorial(
        $"Tama! {GetTrashTypeLabel(trash.trashType)} ay nasa tamang lalagyan. Naayos: {correct}/{totalTrash}  |  Pagkakamali: {mistakes}/{maxMistakes}");

    


    Debug.Log(
        "Correct Trash "
        + correct
        + "/"
        + totalTrash
    );


    Destroy(trash.gameObject);


    CheckCompletion();

}







    private void WrongTrash(TrashType itemType, TrashType binType)
    {

        mistakes++;
        UpdateCounterTexts();
        SoundEffectManager.Play("Wrong");
        ShowTutorial(
            $"Hindi tugma: {GetTrashTypeLabel(itemType)} ay napunta sa {GetTrashTypeLabel(binType)}. Itugma ang basura sa parehong label. Pagkakamali: {mistakes}/{maxMistakes}");


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
        UpdateCounterTexts();
        SoundEffectManager.Play("Wrong");
        ShowTutorial(
            $"May basurang nahulog. Bilisan ang pag-drag sa susunod na basura papunta sa tamang lalagyan. Pagkakamali: {mistakes}/{maxMistakes}");


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

    ResolveDailyQuest(mistakes == 0);
    if (firstTimeTutorialActive && tutorialManager != null)
        tutorialManager.MarkAsLearned(currentChore.ChoreName);

    Debug.Log(
        "Garbage Sorting Complete!"
    );


    currentChore.Complete();
    SoundEffectManager.Play("ChoreFinished");


    if(spawner != null)
    {
        spawner.StopSpawning();
    }


    panel.SetActive(false);
    SetTutorialText(string.Empty);
    firstTimeTutorialActive = false;


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
        ResolveDailyQuest(false);

        if (currentChore is GarbageChore garbageChore)
        {
            garbageChore.MissCarriedBag();
        }
        else
        {
            ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();

            if (choreManager != null)
                choreManager.MissChore(currentChore);
        }
    }


    panel.SetActive(false);
    SetTutorialText(string.Empty);
    firstTimeTutorialActive = false;


    currentChore = null;

}

private void ResolveTutorialText()
{
    if (tutorialText != null || panel == null)
        return;

    foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
    {
        if (text.gameObject.name == "Tutorial Text")
        {
            tutorialText = text;
            break;
        }
    }

    if (tutorialText == null && !tutorialTextWarningLogged)
    {
        Debug.LogWarning("Assign the garbage sorting Tutorial Text TMP object in the Inspector.", this);
        tutorialTextWarningLogged = true;
    }
}

private void ShowTutorial(string message)
{
    if (!firstTimeTutorialActive || tutorialText == null)
        return;

    SetTutorialText(message);
}

private void SetTutorialText(string message)
{
    if (tutorialText == null)
        return;

    tutorialText.text = message;
    tutorialText.gameObject.SetActive(!string.IsNullOrEmpty(message));
}

private void UpdateCounterTexts()
{
    if (mistakesCounterText != null)
        mistakesCounterText.text = $"Mali : {mistakes}";

    if (correctCounterText != null)
        correctCounterText.text = $"Tama : {correct}";
}

private static string GetTrashTypeLabel(TrashType type)
{
    switch (type)
    {
        case TrashType.Nabubulok:
            return "Nabubulok";
        case TrashType.DiNabubulok:
            return "Di-nabubulok";
        case TrashType.Recyclable:
            return "Recyclable";
        default:
            return type.ToString();
    }
}

private void ResolveDailyQuest(bool metChallenge)
{
    DayManager resolvedDayManager = ResolveDayManager();
    if (resolvedDayManager != null)
        resolvedDayManager.ResolveDailyAdditionalQuest(currentChore, metChallenge);
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