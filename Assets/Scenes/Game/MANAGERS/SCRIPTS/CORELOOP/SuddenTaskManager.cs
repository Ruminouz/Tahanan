using UnityEngine;

public class SuddenTaskManager : MonoBehaviour
{
    public static SuddenTaskManager Instance { get; private set; }


    public bool HasActiveMopTask => mopTaskActive;



    [Header("Sudden Task UI")]
    [SerializeField] private GameObject suddenTaskPanel;

    [SerializeField] private GameObject mopFloorTask;



    private bool mopTaskActive = false;



    private WaterSpawner waterSpawner;



    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;
    }





    private void Start()
    {
        waterSpawner =
            FindFirstObjectByType<WaterSpawner>();


        ResetMopTask();
    }







    public void ShowMopTask()
    {
        if(mopTaskActive)
            return;



        mopTaskActive = true;



        if(suddenTaskPanel != null)
        {
            suddenTaskPanel.SetActive(true);
        }



        if(mopFloorTask != null)
        {
            mopFloorTask.SetActive(true);
        }



        Debug.Log(
            "SUDDEN TASK: Mop Floor!"
        );
    }








    public void CompleteMopTask()
    {
        if(!mopTaskActive)
            return;



        // Check if there is still water left
        if(waterSpawner != null)
        {
            if(waterSpawner.RemainingWetAreas > 0)
            {
                Debug.Log(
                    "More water remains. Mop task still active."
                );

                return;
            }
        }



        mopTaskActive = false;



        if(mopFloorTask != null)
        {
            mopFloorTask.SetActive(false);
        }



        if(suddenTaskPanel != null)
        {
            suddenTaskPanel.SetActive(false);
        }



        Debug.Log(
            "SUDDEN TASK COMPLETED!"
        );
    }









    public void ResetMopTask()
    {
        mopTaskActive = false;



        if(mopFloorTask != null)
        {
            mopFloorTask.SetActive(false);
        }



        if(suddenTaskPanel != null)
        {
            suddenTaskPanel.SetActive(false);
        }



        Debug.Log(
            "Mop sudden task reset."
        );
    }







    public bool IsMopTaskActive()
    {
        return mopTaskActive;
    }
}