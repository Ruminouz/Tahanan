using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BroomShopUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;

    [Header("Shop Icon")]
    [FormerlySerializedAs("broomUpgradeIcon")]
    [SerializeField] private Sprite vacuumUpgradeIcon;

    private EconomyManager economyManager;
    private ShopItemCardUI vacuumCard;

    private void OnEnable()
    {
        SubscribeToEconomy();

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyBroomUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        ShopItemCardUI.PrepareShopPanel(transform, closeButton);
        vacuumCard = ShopItemCardUI.GetOrCreate(
            transform,
            "BroomUpgrade",
            1,
            vacuumUpgradeIcon,
            "Vacuum Upgrade",
            "Suck up dust faster with an upgraded vacuum.",
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

            if (vacuumCard != null)
                vacuumCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            return;
        }

        bool canUpgrade = economyManager.CanUpgradeBroom;
        int cost = economyManager.GetBroomUpgradeCost();

        if (levelText != null)
            levelText.text = "Vacuum Level: " + economyManager.BroomUpgradeLevel;

        if (costText != null)
            costText.text = canUpgrade ? "Cost: " + cost + " coins" : "MAX LEVEL";

        if (statusText != null)
        {
            statusText.text = !canUpgrade
                ? "Vacuum fully upgraded"
                : economyManager.Coins >= cost
                    ? "Upgrade vacuum speed"
                    : "Not enough coins";
        }

        if (buyButton != null)
            buyButton.interactable = canUpgrade && economyManager.Coins >= cost;

        if (vacuumCard != null)
        {
            vacuumCard.SetPurchaseState(
                canUpgrade ? cost + " COINS" : "MAX LEVEL",
                !canUpgrade ? "MAXED" : economyManager.Coins >= cost ? "UPGRADE" : "NEED COINS",
                canUpgrade && economyManager.Coins >= cost);
        }
    }
}
