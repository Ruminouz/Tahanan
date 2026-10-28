using UnityEngine;
using System;

public enum EconomyRewardType
{
    Coins,
    MovementSpeedBoost,
    ExtraTime
}

public enum FurnitureType
{
    Refrigerator,
    Fan,
    Television,
    Plant,
    KitchenStove,
    WaterDispenser
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

    [Header("Shop - Broom")]
    [SerializeField, Min(1)] private int broomUpgradeBaseCost = 10;
    [SerializeField, Min(0f)] private float upgradedBroomCleaningSpeedMultiplier = 1.35f;

    [Header("Shop - Cat")]
    [SerializeField, Min(1)] private int catBaseCost = 50;

    [Header("Shop - Dishwashing Liquid")]
    [SerializeField, Min(1)] private int dishwashingLiquidBaseCost = 10;
    [SerializeField, Min(1f)] private float dishwashingLiquidScrubSpeedMultiplier = 1.5f;

    [Header("Shop - Furniture")]
    [SerializeField, Min(1)] private int furnitureBaseCost = 10;

    private int coins;
    private int points;
    private float movementBoostEndTime;
    private int mopUpgradeLevel;
    private int broomUpgradeLevel;
    private bool catPurchased;
    private bool dishwashingLiquidPurchased;
    private readonly bool[] purchasedFurniture = new bool[6];
    private Sprite dishwashingLiquidSprite;

    public event Action<int> CoinsChanged;
    public event Action<int> CoinsEarned;
    public event Action<int> MopUpgradeChanged;
    public event Action<int> BroomUpgradeChanged;
    public event Action<bool> CatPurchaseChanged;
    public event Action<bool> DishwashingLiquidPurchaseChanged;
    public event Action<FurnitureType, bool> FurniturePurchaseChanged;

    public int Coins => coins;
    public int Points => points;
    public int MopUpgradeLevel => mopUpgradeLevel;
    public int BroomUpgradeLevel => broomUpgradeLevel;
    public bool HasUpgradedMop => mopUpgradeLevel == 1;
    public bool CanUpgradeMop => !HasUpgradedMop;
    public bool HasUpgradedBroom => broomUpgradeLevel == 1;
    public bool CanUpgradeBroom => !HasUpgradedBroom;
    public bool HasPurchasedCat => catPurchased;
    public bool CanPurchaseCat => !catPurchased;
    public bool HasPurchasedDishwashingLiquid => dishwashingLiquidPurchased;
    public bool CanPurchaseDishwashingLiquid => !dishwashingLiquidPurchased;
    public Sprite DishwashingLiquidSprite => dishwashingLiquidSprite;
    public float DishwashingScrubSpeedMultiplier => dishwashingLiquidPurchased
        ? Mathf.Max(1f, dishwashingLiquidScrubSpeedMultiplier)
        : 1f;
    public float MopCleaningSpeedMultiplier => HasUpgradedMop
        ? Mathf.Max(1f, upgradedMopCleaningSpeedMultiplier)
        : 1f;
    public float BroomCleaningSpeedMultiplier => HasUpgradedBroom
        ? Mathf.Max(1f, upgradedBroomCleaningSpeedMultiplier)
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
        broomUpgradeLevel = 0;
        catPurchased = false;
        dishwashingLiquidPurchased = false;
        Array.Clear(purchasedFurniture, 0, purchasedFurniture.Length);
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
        if (amount > 0)
            CoinsEarned?.Invoke(amount);
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
        SoundEffectManager.Play("Buy");
        return true;
    }

    public int GetBroomUpgradeCost()
    {
        if (!CanUpgradeBroom)
            return -1;

        return broomUpgradeBaseCost;
    }

    public bool TryPurchaseBroomUpgrade()
    {
        int cost = GetBroomUpgradeCost();
        if (cost < 0 || !TrySpendCoins(cost))
            return false;

        broomUpgradeLevel = 1;
        BroomUpgradeChanged?.Invoke(broomUpgradeLevel);
        SoundEffectManager.Play("Buy");
        return true;
    }

    public bool TryPurchaseBoost(BunsoBoostInventory inventory, BunsoBoostType boost, int cost)
    {
        if (inventory == null || cost <= 0 || coins < cost || !inventory.CanAdd(boost))
            return false;

        if (!inventory.Add(boost))
            return false;

        coins -= cost;
        CoinsChanged?.Invoke(coins);
        SoundEffectManager.Play("Buy");
        return true;
    }

    public int GetCatCost()
    {
        return CanPurchaseCat ? catBaseCost : -1;
    }

    public bool TryPurchaseCat()
    {
        int cost = GetCatCost();
        if (cost < 0 || !TrySpendCoins(cost))
            return false;

        catPurchased = true;
        CatPurchaseChanged?.Invoke(catPurchased);
        SoundEffectManager.Play("Buy");
        return true;
    }

    public int GetDishwashingLiquidCost()
    {
        return CanPurchaseDishwashingLiquid ? dishwashingLiquidBaseCost : -1;
    }

    public bool TryPurchaseDishwashingLiquid(Sprite liquidSprite)
    {
        if (liquidSprite == null)
        {
            Debug.LogWarning("Cannot purchase dishwashing liquid because its shop sprite is not assigned.", this);
            return false;
        }

        int cost = GetDishwashingLiquidCost();
        if (cost < 0 || !TrySpendCoins(cost))
            return false;

        dishwashingLiquidSprite = liquidSprite;
        dishwashingLiquidPurchased = true;
        DishwashingLiquidPurchaseChanged?.Invoke(dishwashingLiquidPurchased);
        SoundEffectManager.Play("Buy");
        return true;
    }

    public int GetFurnitureCost()
    {
        return furnitureBaseCost;
    }

    public bool HasPurchasedFurniture(FurnitureType furniture)
    {
        int index = (int)furniture;
        return index >= 0 && index < purchasedFurniture.Length && purchasedFurniture[index];
    }

    public bool TryPurchaseFurniture(FurnitureType furniture)
    {
        int index = (int)furniture;
        if (index < 0 || index >= purchasedFurniture.Length ||
            purchasedFurniture[index] || !TrySpendCoins(furnitureBaseCost))
        {
            return false;
        }

        purchasedFurniture[index] = true;
        FurniturePurchaseChanged?.Invoke(furniture, true);
        SoundEffectManager.Play("Buy");
        return true;
    }

    public bool RefundFurniturePurchase(FurnitureType furniture)
    {
        int index = (int)furniture;
        if (index < 0 || index >= purchasedFurniture.Length || !purchasedFurniture[index])
            return false;

        purchasedFurniture[index] = false;
        coins += furnitureBaseCost;
        CoinsChanged?.Invoke(coins);
        FurniturePurchaseChanged?.Invoke(furniture, false);
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