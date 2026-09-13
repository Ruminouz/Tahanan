using UnityEngine;

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
    [SerializeField] private int startingCoins;
    [SerializeField] private int startingPoints;
    [SerializeField] private int coinsPerChorePoint = 1;

    [Header("Reward Settings")]
    [SerializeField] private float movementSpeedMultiplier = 1.25f;
    [SerializeField] private float movementBoostDuration = 15f;
    [SerializeField] private float extraTimeSeconds = 15f;

    private int coins;
    private int points;
    private float movementBoostEndTime;

    public int Coins => coins;
    public int Points => points;
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
        coins = Mathf.Max(0, coins + amount);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || coins < amount)
            return false;

        coins -= amount;
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