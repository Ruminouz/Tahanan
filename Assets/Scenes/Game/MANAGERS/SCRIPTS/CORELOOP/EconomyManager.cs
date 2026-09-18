using UnityEngine;
using System;

public enum EconomyRewardType
{
    Coins,
    MovementSpeedBoost,
    ExtraTime
}

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Wallet")]
    [SerializeField, Min(0)] private int startingCoins;
    [SerializeField, Min(0)] private int startingPoints;
    [SerializeField] private int coinsPerChorePoint = 1;

    [Header("Reward Settings")]
    [SerializeField] private float movementSpeedMultiplier = 1.25f;
    [SerializeField] private float movementBoostDuration = 15f;
    [SerializeField] private float extraTimeSeconds = 15f;

    [Header("Shop - Mop")]
    [SerializeField, Min(1)] private int mopUpgradeBaseCost = 10;
    [SerializeField, Min(0f)] private float upgradedMopCleaningSpeedMultiplier = 1.2f;

    private int coins;
    private int points;
    private float movementBoostEndTime;
    private int mopUpgradeLevel;

    public event Action<int> CoinsChanged;
    public event Action<int> MopUpgradeChanged;

    public int Coins => coins;
    public int Points => points;
    public int MopUpgradeLevel => mopUpgradeLevel;
    public bool HasUpgradedMop => mopUpgradeLevel == 1;
    public bool CanUpgradeMop => !HasUpgradedMop;
    public float MopCleaningSpeedMultiplier => HasUpgradedMop
        ? Mathf.Max(1f, upgradedMopCleaningSpeedMultiplier)
        : 1f;
    public bool HasMovementBoost => Time.time < movementBoostEndTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        coins = startingCoins;
        points = startingPoints;
        mopUpgradeLevel = 0;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (HasMovementBoost)
            return;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            player.SetSpeedMultiplier(1f);
    }

    public void AwardChorePoints(int amount)
    {
        if (amount <= 0)
            return;

        points += amount;
        AddCoins(amount * Mathf.Max(0, coinsPerChorePoint));
    }

    public void AddCoins(int amount)
    {
        if (amount == 0)
            return;

        coins = Mathf.Max(0, coins + amount);
        CoinsChanged?.Invoke(coins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0 || coins < amount)
            return false;

        coins -= amount;
        CoinsChanged?.Invoke(coins);
        return true;
    }

    public int GetMopUpgradeCost()
    {
        if (!CanUpgradeMop)
            return -1;

        return mopUpgradeBaseCost;
    }

    public bool TryPurchaseMopUpgrade()
    {
        int cost = GetMopUpgradeCost();
        if (cost < 0 || !TrySpendCoins(cost))
            return false;

        mopUpgradeLevel = 1;
        MopUpgradeChanged?.Invoke(mopUpgradeLevel);
        return true;
    }

    public bool RedeemReward(EconomyRewardType rewardType, int cost)
    {
        if (!TrySpendCoins(cost))
            return false;

        switch (rewardType)
        {
            case EconomyRewardType.MovementSpeedBoost:
                ApplyMovementSpeedBoost();
                break;
            case EconomyRewardType.ExtraTime:
                TimeManager timeManager = FindFirstObjectByType<TimeManager>();
                if (timeManager != null)
                    timeManager.AddTime(extraTimeSeconds);
                break;
        }

        return true;
    }

    private void ApplyMovementSpeedBoost()
    {
        movementBoostEndTime = Time.time + movementBoostDuration;
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            player.SetSpeedMultiplier(movementSpeedMultiplier);
    }
}