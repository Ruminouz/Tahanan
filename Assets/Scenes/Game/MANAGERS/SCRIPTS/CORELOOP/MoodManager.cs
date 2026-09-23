using System;
using UnityEngine;
using UnityEngine.UI;

public enum HouseholdMood
{
    Calm,
    Concerned,
    Angry
}

public class MoodManager : MonoBehaviour
{
    public static MoodManager Instance { get; private set; }
    public event Action<HouseholdMood, HouseholdMood> MoodChanged;

    [Header("Mood")]
    [SerializeField] private Slider moodSlider;
    [SerializeField, Range(0f, 100f)] private float startingMood = 100f;
    [SerializeField, Range(0f, 100f)] private float concernedThreshold = 65f;
    [SerializeField, Range(0f, 100f)] private float angryThreshold = 35f;
    [SerializeField, Min(0f)] private float moodLossPerMissedChore = 20f;
    [SerializeField, Min(0f)] private float moodRecoveryPerCompletedChore = 5f;

    private float mood;

    public float Mood => mood;
    public HouseholdMood CurrentMood => mood <= angryThreshold
        ? HouseholdMood.Angry
        : mood <= concernedThreshold
            ? HouseholdMood.Concerned
            : HouseholdMood.Calm;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mood = Mathf.Clamp(startingMood, 0f, 100f);
        UpdateSlider();
    }

    private void OnEnable()
    {
        SubscribeToChoreManager();
    }

    private void Start()
    {
        SubscribeToChoreManager();
    }

    private void OnDisable()
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
        if (choreManager != null)
            choreManager.ChoreMissed -= HandleMissedChore;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void HandleCompletedChore()
    {
        SetMood(mood + moodRecoveryPerCompletedChore);
    }

    public void AddMood(float amount)
    {
        if (amount == 0f)
            return;

        SetMood(mood + amount);
    }

    public void ResetDailyMood()
    {
        SetMood(startingMood);
    }

    private void HandleMissedChore(Chore chore)
    {
        SetMood(mood - moodLossPerMissedChore);
    }

    private void SetMood(float value)
    {
        HouseholdMood previousMood = CurrentMood;
        mood = Mathf.Clamp(value, 0f, 100f);
        UpdateSlider();

        HouseholdMood newMood = CurrentMood;
        if (newMood != previousMood)
        {
            MoodChanged?.Invoke(previousMood, newMood);
        }
    }

    private void SubscribeToChoreManager()
    {
        ChoreManager choreManager = FindFirstObjectByType<ChoreManager>();
        if (choreManager != null)
        {
            choreManager.ChoreMissed -= HandleMissedChore;
            choreManager.ChoreMissed += HandleMissedChore;
        }
    }

    private void UpdateSlider()
    {
        if (moodSlider != null)
            moodSlider.value = mood / 100f;
    }
}