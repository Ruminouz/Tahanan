using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MopShopUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    private EconomyManager economyManager;

    private void OnEnable()
    {
        SubscribeToEconomy();

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyMopUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        Refresh();
    }

    private void Start()
    {
        SubscribeToEconomy();
        Refresh();
    }

    private void OnDisable()
    {
        if (economyManager != null)
        {
            economyManager.CoinsChanged -= OnEconomyChanged;
            economyManager.MopUpgradeChanged -= OnEconomyChanged;
        }

        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyMopUpgrade);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseShop);
    }

    private void OnEconomyChanged(int value)
    {
        Refresh();
    }

    private void SubscribeToEconomy()
    {
        EconomyManager currentEconomyManager = EconomyManager.Instance;
        if (currentEconomyManager == null)
            return;

        if (economyManager != null && economyManager != currentEconomyManager)
        {
            economyManager.CoinsChanged -= OnEconomyChanged;
            economyManager.MopUpgradeChanged -= OnEconomyChanged;
        }

        economyManager = currentEconomyManager;
        economyManager.CoinsChanged -= OnEconomyChanged;
        economyManager.MopUpgradeChanged -= OnEconomyChanged;
        economyManager.CoinsChanged += OnEconomyChanged;
        economyManager.MopUpgradeChanged += OnEconomyChanged;
    }

    public void BuyMopUpgrade()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null)
            return;

        economyManager.TryPurchaseMopUpgrade();
        Refresh();
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (economyManager == null)
        {
            if (statusText != null)
                statusText.text = "Shop unavailable";

            if (buyButton != null)
                buyButton.interactable = false;

            return;
        }

        bool canUpgrade = economyManager.CanUpgradeMop;
        int cost = economyManager.GetMopUpgradeCost();

        if (levelText != null)
            levelText.text = "Mop Level: " + economyManager.MopUpgradeLevel;

        if (costText != null)
            costText.text = canUpgrade ? "Cost: " + cost + " coins" : "MAX LEVEL";

        if (statusText != null)
        {
            statusText.text = !canUpgrade
                ? "Mop fully upgraded"
                : economyManager.Coins >= cost
                    ? "Upgrade mopping speed"
                    : "Not enough coins";
        }

        if (buyButton != null)
            buyButton.interactable = canUpgrade && economyManager.Coins >= cost;
    }
}
