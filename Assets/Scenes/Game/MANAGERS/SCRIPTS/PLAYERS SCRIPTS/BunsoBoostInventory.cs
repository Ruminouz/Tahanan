using System;
using System.Collections.Generic;
using UnityEngine;

public enum BunsoBoostType
{
    Chocolate,
    ExtraTime
}

public class BunsoBoostInventory : MonoBehaviour
{
    public static BunsoBoostInventory Instance { get; private set; }

    [SerializeField, Min(1f)] private float chocolateSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float chocolateDuration = 8f;
    [SerializeField, Min(1f)] private float extraTimeSeconds = 30f;
    [SerializeField, Min(1)] private int capacity = 7;

    private readonly List<BunsoBoostType> items = new();
    private PlayerMovement playerMovement;
    private DayManager dayManager;

    public event Action InventoryChanged;
    public int Count => items.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerMovement = GetComponent<PlayerMovement>();
        dayManager = DayManager.Instance != null
            ? DayManager.Instance
            : FindFirstObjectByType<DayManager>();
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
        if (items.Count >= capacity)
            return false;

        items.Add(boost);
        InventoryChanged?.Invoke();
        return true;
    }

    public BunsoBoostType GetItemAt(int index)
    {
        return items[index];
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
            if (dayManager == null)
                dayManager = DayManager.Instance != null
                    ? DayManager.Instance
                    : FindFirstObjectByType<DayManager>();

            Chore lowestTimerChore = FindLowestTimerChore();
            if (lowestTimerChore == null)
                return false;

            lowestTimerChore.ExtendDeadline(extraTimeSeconds);
        }

        items.RemoveAt(index);
        InventoryChanged?.Invoke();
        return true;
    }

    private Chore FindLowestTimerChore()
    {
        if (dayManager == null)
            return null;

        Chore lowest = null;
        Chore[] chores = dayManager.GetActiveChores();
        if (chores == null)
            return null;

        foreach (Chore chore in chores)
        {
            if (chore == null || chore.IsCompleted || chore.IsMissed)
                continue;

            if (lowest == null || chore.RemainingDeadline < lowest.RemainingDeadline)
                lowest = chore;
        }

        return lowest;
    }
}
