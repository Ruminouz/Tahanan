using System;
using UnityEngine;

public class MopUpgradeManager : MonoBehaviour
{
    public static MopUpgradeManager Instance { get; private set; }

    public event Action<int> MopVisualChanged;

    public int CurrentLevel
    {
        get
        {
            EconomyManager economyManager = EconomyManager.Instance;
            return economyManager != null && economyManager.HasUpgradedMop ? 1 : 0;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SubscribeToEconomy();
    }

    private void Start()
    {
        SubscribeToEconomy();
        MopVisualChanged?.Invoke(CurrentLevel);
    }

    private void OnDisable()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.MopUpgradeChanged -= OnMopUpgradeChanged;
    }

    public int GetCurrentMopLevel()
    {
        return CurrentLevel;
    }

    private void SubscribeToEconomy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.MopUpgradeChanged -= OnMopUpgradeChanged;
            EconomyManager.Instance.MopUpgradeChanged += OnMopUpgradeChanged;
        }
    }

    private void OnMopUpgradeChanged(int level)
    {
        MopVisualChanged?.Invoke(level);
    }
}
