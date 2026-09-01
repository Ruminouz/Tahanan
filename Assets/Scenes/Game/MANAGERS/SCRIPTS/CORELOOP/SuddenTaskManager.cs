using UnityEngine;

public class SuddenTaskManager : MonoBehaviour
{
    public static SuddenTaskManager Instance { get; private set; }
    public bool HasActiveMopTask => mopTaskActive;
    [Header("Sudden Task UI")]
    [SerializeField] private GameObject suddenTaskPanel;

    [SerializeField] private GameObject mopFloorTask;

    private bool mopTaskActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (suddenTaskPanel != null)
        {
            suddenTaskPanel.SetActive(false);
        }

        if (mopFloorTask != null)
        {
            mopFloorTask.SetActive(false);
        }
    }

    public void ShowMopTask()
    {
        if (mopTaskActive)
            return;

        mopTaskActive = true;

        if (suddenTaskPanel != null)
        {
            suddenTaskPanel.SetActive(true);
        }

        if (mopFloorTask != null)
        {
            mopFloorTask.SetActive(true);
        }

        Debug.Log("SUDDEN TASK: Mop the Floor!");
    }

    public void CompleteMopTask()
    {
        if (!mopTaskActive)
            return;

        mopTaskActive = false;

        if (mopFloorTask != null)
        {
            mopFloorTask.SetActive(false);
        }

        if (suddenTaskPanel != null)
        {
            suddenTaskPanel.SetActive(false);
        }

        Debug.Log("SUDDEN TASK COMPLETED: Mop the Floor!");
    }

    public bool IsMopTaskActive()
    {
        return mopTaskActive;
    }
}