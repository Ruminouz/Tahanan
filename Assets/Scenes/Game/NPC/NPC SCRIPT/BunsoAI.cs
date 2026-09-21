using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BunsoAI : MonoBehaviour
{
    [Header("Mood")]
    [SerializeField, Min(0f)] private float actionInterval = 12f;
    [SerializeField, Range(0f, 100f)] private float actionChance = 60f;
    [SerializeField] private string badMoodLine = "Pick up the trash!";

    [Header("Good Mood Rewards")]
    [SerializeField] private BunsoBoostType[] possibleBoosts =
        { BunsoBoostType.Chocolate, BunsoBoostType.ExtraTime };
    [SerializeField] private GameObject chocolatePrefab;
    [SerializeField] private GameObject extraTimePrefab;

    [Header("Bad Mood Chore")]
    [SerializeField] private SegregateWasteChore segregateWastePrefab;
    [SerializeField, Min(0f)] private float choreLifetime = 120f;

    [Header("Mood Bubble")]
    [SerializeField] private Transform topAnchor;
    [SerializeField] private GameObject moodBubblePrefab;
    [SerializeField, Min(0f)] private float bubbleDuration = 3f;

    private MoodManager moodManager;
    private DayManager dayManager;
    private float nextActionTime;
    private readonly List<SegregateWasteChore> spawnedChores = new();

    private void Start()
    {
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
        if (segregateWastePrefab == null)
            segregateWastePrefab = FindFirstObjectByType<SegregateWasteChore>(
                FindObjectsInactive.Include);
        nextActionTime = Time.time + actionInterval;
    }

    private void Update()
    {
        if (moodManager == null || dayManager == null || Time.time < nextActionTime)
            return;

        nextActionTime = Time.time + actionInterval;
        if (Random.Range(0f, 100f) > actionChance)
            return;

        switch (moodManager.CurrentMood)
        {
            case HouseholdMood.Calm:
                DropReward();
                break;
            case HouseholdMood.Angry:
            case HouseholdMood.Concerned:
                SpawnBadMoodChore();
                ShowBadMoodBubble();
                break;
        }
    }

    private void DropReward()
    {
        if (possibleBoosts == null || possibleBoosts.Length == 0)
            return;

        BunsoBoostType boost = possibleBoosts[Random.Range(0, possibleBoosts.Length)];
        GameObject prefab = boost == BunsoBoostType.Chocolate
            ? chocolatePrefab
            : extraTimePrefab;
        if (prefab == null)
        {
            Debug.LogWarning("Bunso reward prefab is not assigned for " + boost + ".", this);
            return;
        }

        GameObject droppedReward = Instantiate(prefab, transform.position, Quaternion.identity);
        BunsoBoostPickup pickup = droppedReward.GetComponent<BunsoBoostPickup>();
        if (pickup == null)
            pickup = droppedReward.AddComponent<BunsoBoostPickup>();
        if (droppedReward.GetComponent<Collider2D>() == null)
            droppedReward.AddComponent<BoxCollider2D>();
        pickup.Configure(boost);
    }

    private void SpawnBadMoodChore()
    {
        if (segregateWastePrefab == null || dayManager.CurrentDay < 2)
            return;

        SegregateWasteChore chore = Instantiate(
            segregateWastePrefab,
            transform.position,
            Quaternion.identity);
        chore.ConfigureSpawnedChore("PickUp");
        dayManager.RegisterDynamicChore(chore);
        spawnedChores.Add(chore);

        if (choreLifetime > 0f)
            Destroy(chore.gameObject, choreLifetime);
    }

    private void ShowBadMoodBubble()
    {
        if (moodBubblePrefab == null)
            return;

        Transform anchor = topAnchor != null ? topAnchor : transform;
        Canvas canvas = SpeechBubbleCanvas.GetOrCreate(anchor);
        GameObject bubble = Instantiate(
            moodBubblePrefab,
            anchor.position,
            Quaternion.identity,
            canvas.transform);
        TMP_Text text = bubble.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = badMoodLine;
        Destroy(bubble, bubbleDuration);
    }

    private void OnDestroy()
    {
        if (dayManager == null)
            return;

        foreach (SegregateWasteChore chore in spawnedChores)
        {
            if (chore != null)
                dayManager.UnregisterDynamicChore(chore);
        }
    }
}
