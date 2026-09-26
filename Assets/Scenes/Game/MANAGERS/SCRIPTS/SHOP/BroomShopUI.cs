using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BroomShopUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    [Header("Shop Icon")]
    [SerializeField] private Sprite broomUpgradeIcon;

    private EconomyManager economyManager;
    private ShopItemCardUI broomCard;

    private void OnEnable()
    {
        SubscribeToEconomy();

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyBroomUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        ShopItemCardUI.PrepareShopPanel(transform, closeButton);
        broomCard = ShopItemCardUI.GetOrCreate(
            transform,
            "BroomUpgrade",
            1,
            broomUpgradeIcon,
            "Broom Upgrade",
            "Sweep dust faster with an upgraded broom.",
            BuyBroomUpgrade);

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
            economyManager.BroomUpgradeChanged -= OnEconomyChanged;
        }

        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyBroomUpgrade);

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
            economyManager.BroomUpgradeChanged -= OnEconomyChanged;
        }

        economyManager = currentEconomyManager;
        economyManager.CoinsChanged -= OnEconomyChanged;
        economyManager.BroomUpgradeChanged -= OnEconomyChanged;
        economyManager.CoinsChanged += OnEconomyChanged;
        economyManager.BroomUpgradeChanged += OnEconomyChanged;
    }

    public void BuyBroomUpgrade()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null)
            return;

        economyManager.TryPurchaseBroomUpgrade();
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

            if (broomCard != null)
                broomCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            return;
        }

        bool canUpgrade = economyManager.CanUpgradeBroom;
        int cost = economyManager.GetBroomUpgradeCost();

        if (levelText != null)
            levelText.text = "Broom Level: " + economyManager.BroomUpgradeLevel;

        if (costText != null)
            costText.text = canUpgrade ? "Cost: " + cost + " coins" : "MAX LEVEL";

        if (statusText != null)
        {
            statusText.text = !canUpgrade
                ? "Broom fully upgraded"
                : economyManager.Coins >= cost
                    ? "Upgrade sweeping speed"
                    : "Not enough coins";
        }

        if (buyButton != null)
            buyButton.interactable = canUpgrade && economyManager.Coins >= cost;

        if (broomCard != null)
        {
            broomCard.SetPurchaseState(
                canUpgrade ? cost + " COINS" : "MAX LEVEL",
                !canUpgrade ? "MAXED" : economyManager.Coins >= cost ? "UPGRADE" : "NEED COINS",
                canUpgrade && economyManager.Coins >= cost);
        }
    }
}
