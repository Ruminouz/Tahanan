using System;
using System.Collections.Generic;
using UnityEngine;

public enum BunsoBoostType
{
    Chocolate,
    TimeFreezer
}

public class BunsoBoostInventory : MonoBehaviour
{
    public static BunsoBoostInventory Instance { get; private set; }

    [SerializeField, Min(1f)] private float chocolateSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float chocolateDuration = 8f;
    [SerializeField, Min(0.1f)] private float timeFreezeDuration = 7f;
    [SerializeField, Min(1)] private int capacity = 7;
    [SerializeField, Min(0f)] private float chocolateMoodPenalty = 10f;

    private readonly List<BunsoBoostType> items = new();
    private PlayerMovement playerMovement;
    private TimeManager timeManager;
    private MoodManager moodManager;

    public event Action InventoryChanged;
    public int Count => items.Count;

    public int StackCount
    {
        get
        {
            int stackCount = 0;
            for (int index = 0; index < items.Count; index++)
            {
                bool alreadyCounted = false;
                for (int previousIndex = 0; previousIndex < index; previousIndex++)
                {
                    if (items[previousIndex] == items[index])
                    {
                        alreadyCounted = true;
                        break;
                    }
                }

                if (!alreadyCounted)
                    stackCount++;
            }

            return stackCount;
        }
    }

    public bool CanAdd(BunsoBoostType boost)
    {
        return items.Contains(boost) || StackCount < capacity;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerMovement = GetComponent<PlayerMovement>();
        timeManager = FindFirstObjectByType<TimeManager>();
        moodManager = MoodManager.Instance != null
            ? MoodManager.Instance
            : FindFirstObjectByType<MoodManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetQuantity(BunsoBoostType boost)
    {
        int quantity = 0;
        foreach (BunsoBoostType item in items)
        {
            if (item == boost)
                quantity++;
        }

        return quantity;
    }

    public bool Add(BunsoBoostType boost)
    {
        if (!CanAdd(boost))
            return false;

        items.Add(boost);
        InventoryChanged?.Invoke();
        return true;
    }

    public BunsoBoostType GetItemAt(int index)
    {
        return items[index];
    }

    public BunsoBoostType GetStackAt(int stackIndex)
    {
        int currentStack = 0;
        for (int index = 0; index < items.Count; index++)
        {
            bool alreadyCounted = false;
            for (int previousIndex = 0; previousIndex < index; previousIndex++)
            {
                if (items[previousIndex] == items[index])
                {
                    alreadyCounted = true;
                    break;
                }
            }

            if (alreadyCounted)
                continue;

            if (currentStack == stackIndex)
                return items[index];

            currentStack++;
        }

        throw new System.ArgumentOutOfRangeException(nameof(stackIndex));
    }

    public void ResetForDay()
    {
        items.Clear();
        InventoryChanged?.Invoke();
    }

    public bool ConsumeAt(int index)
    {
        if (index < 0 || index >= items.Count)
            return false;

        BunsoBoostType boost = items[index];
        return ConsumeAt(index, boost);
    }

    public bool ConsumeOne(BunsoBoostType boost)
    {
        int index = items.IndexOf(boost);
        return index >= 0 && ConsumeAt(index, boost);
    }

    private bool ConsumeAt(int index, BunsoBoostType boost)
    {
        if (boost == BunsoBoostType.Chocolate)
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
                return false;

            playerMovement.ApplyTemporarySpeedBoost(
                chocolateSpeedMultiplier,
                chocolateDuration);
        }
        else
        {
            if (timeManager == null)
                timeManager = FindFirstObjectByType<TimeManager>();

            if (timeManager == null || !timeManager.FreezeTime(timeFreezeDuration))
                return false;
        }

        items.RemoveAt(index);
        InventoryChanged?.Invoke();
        SoundEffectManager.Play(
            boost == BunsoBoostType.Chocolate ? "Chocolate" : "Frozen");

        if (boost == BunsoBoostType.Chocolate)
        {
            if (moodManager == null)
                moodManager = MoodManager.Instance != null
                    ? MoodManager.Instance
                    : FindFirstObjectByType<MoodManager>();

            if (moodManager != null)
                moodManager.AddMood(-chocolateMoodPenalty);
            else
                Debug.LogWarning("Chocolate was consumed, but no MoodManager was found.", this);

            BunsoAI bunso = FindFirstObjectByType<BunsoAI>();
            if (bunso != null)
                bunso.ReactToChocolateConsumed();
            else
                Debug.LogWarning("Chocolate was consumed, but no BunsoAI was found.", this);
        }

        return true;
    }
}
