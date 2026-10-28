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
    [Header("Consumables")]
    [SerializeField, Min(1)] private int chocolateCost = 10;
    [SerializeField, Min(1)] private int timeFreezerCost = 10;
    [SerializeField] private Sprite chocolateIcon;
    [SerializeField] private Sprite timeFreezerIcon;

    private EconomyManager economyManager;
    private ShopItemCardUI vacuumCard;
    private ShopItemCardUI chocolateCard;
    private ShopItemCardUI timeFreezerCard;
    private BunsoBoostInventory boostInventory;

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
        chocolateCard = ShopItemCardUI.GetOrCreate(
            transform,
            "Chocolate",
            1,
            chocolateIcon,
            "Chocolate Bar",
            "Temporarily move faster, but it makes Bunso upset.",
            BuyChocolate);
        timeFreezerCard = ShopItemCardUI.GetOrCreate(
            transform,
            "TimeFreezer",
            2,
            timeFreezerIcon,
            "Time Freezer",
            "Freeze the timer for a short time.",
            BuyTimeFreezer);

        Refresh();
    }

    private void Start()
    {
        SubscribeToEconomy();
        SubscribeToBoostInventory();
        Refresh();
    }

    private void OnDisable()
    {
        if (economyManager != null)
        {
            economyManager.CoinsChanged -= OnEconomyChanged;
            economyManager.BroomUpgradeChanged -= OnEconomyChanged;
        }
        if (boostInventory != null)
        {
            boostInventory.InventoryChanged -= OnBoostInventoryChanged;
            boostInventory = null;
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

    private void OnBoostInventoryChanged()
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

    private void SubscribeToBoostInventory()
    {
        BunsoBoostInventory currentInventory = BunsoBoostInventory.Instance;
        if (currentInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                currentInventory = player.GetComponent<BunsoBoostInventory>();
        }

        if (boostInventory == currentInventory)
            return;

        if (boostInventory != null)
            boostInventory.InventoryChanged -= OnBoostInventoryChanged;

        boostInventory = currentInventory;
        if (boostInventory != null)
            boostInventory.InventoryChanged += OnBoostInventoryChanged;
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

    public void BuyChocolate()
    {
        BuyBoost(BunsoBoostType.Chocolate, chocolateCost);
    }

    public void BuyTimeFreezer()
    {
        BuyBoost(BunsoBoostType.TimeFreezer, timeFreezerCost);
    }

    private void BuyBoost(BunsoBoostType boost, int cost)
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        SubscribeToBoostInventory();
        if (economyManager != null)
            economyManager.TryPurchaseBoost(boostInventory, boost, cost);

        Refresh();
    }

    private void Refresh()
    {
        ShopItemCardUI.SetCoinBalance(
            transform,
            economyManager != null ? "COINS  " + economyManager.Coins : "COINS  --");
        ShopItemCardUI.RefreshShopLayout(transform);

        SubscribeToBoostInventory();
        if (economyManager == null)
        {
            if (statusText != null)
                statusText.text = "Shop unavailable";

            if (buyButton != null)
                buyButton.interactable = false;

            if (vacuumCard != null)
                vacuumCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (chocolateCard != null)
                chocolateCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (timeFreezerCard != null)
                timeFreezerCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

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

        SetConsumablePurchaseState(
            chocolateCard,
            BunsoBoostType.Chocolate,
            chocolateCost,
            chocolateIcon != null);
        SetConsumablePurchaseState(
            timeFreezerCard,
            BunsoBoostType.TimeFreezer,
            timeFreezerCost,
            timeFreezerIcon != null);
    }

    private void SetConsumablePurchaseState(
        ShopItemCardUI card,
        BunsoBoostType boost,
        int itemCost,
        bool isConfigured)
    {
        if (card == null)
            return;

        bool canAfford = economyManager.Coins >= itemCost;
        bool canAdd = boostInventory != null && boostInventory.CanAdd(boost);
        bool canPurchase = isConfigured && canAfford && canAdd;
        string action = boostInventory == null
            ? "NO INVENTORY"
            : !canAdd
                ? "INVENTORY FULL"
                : !isConfigured
                    ? "NOT SET UP"
                    : canAfford ? "BUY" : "NEED COINS";
        card.SetPurchaseState(itemCost + " COINS", action, canPurchase);
    }
}
