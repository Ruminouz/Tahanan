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

    [Header("Cat")]
    [SerializeField] private TMP_Text catCostText;
    [SerializeField] private TMP_Text catStatusText;
    [SerializeField] private Button catBuyButton;
    [SerializeField] private GameObject catPrefab;
    [SerializeField] private Transform catSpawnPoint;

    [Header("Shop Icons")]
    [SerializeField] private Sprite mopUpgradeIcon;
    [SerializeField] private Sprite catIcon;

    private EconomyManager economyManager;
    private ShopItemCardUI mopCard;
    private ShopItemCardUI catCard;

    private void OnEnable()
    {
        SubscribeToEconomy();

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyMopUpgrade);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);

        if (catBuyButton != null)
            catBuyButton.onClick.AddListener(BuyCat);

        ShopItemCardUI.PrepareShopPanel(transform, closeButton);
        mopCard = ShopItemCardUI.GetOrCreate(
            transform,
            "MopUpgrade",
            0,
            mopUpgradeIcon,
            "Mop Upgrade",
            "Clean spills faster with an upgraded mop.",
            BuyMopUpgrade);
        catCard = ShopItemCardUI.GetOrCreate(
            transform,
            "CatAdoption",
            2,
            catIcon,
            "Adopt a Cat",
            "Bring a friendly cat companion home.",
            BuyCat);

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

        if (catBuyButton != null)
            catBuyButton.onClick.RemoveListener(BuyCat);
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

    public void BuyCat()
    {
        if (economyManager == null)
            economyManager = EconomyManager.Instance;

        if (economyManager == null || catPrefab == null || !economyManager.TryPurchaseCat())
            return;

        Transform spawnPoint = catSpawnPoint != null ? catSpawnPoint : transform;
        if (catPrefab.scene.IsValid())
            catPrefab.SetActive(true);
        else
            Instantiate(catPrefab, spawnPoint.position, spawnPoint.rotation);

        Refresh();
    }

    private void Refresh()
    {
        if (economyManager == null)
        {
            if (statusText != null)
                statusText.text = "Shop unavailable";

            if (buyButton != null)
                buyButton.interactable = false;

            if (mopCard != null)
                mopCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

            if (catCard != null)
                catCard.SetPurchaseState("Shop unavailable", "UNAVAILABLE", false);

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

        if (mopCard != null)
        {
            mopCard.SetPurchaseState(
                canUpgrade ? cost + " COINS" : "MAX LEVEL",
                !canUpgrade ? "MAXED" : economyManager.Coins >= cost ? "UPGRADE" : "NEED COINS",
                canUpgrade && economyManager.Coins >= cost);
        }

        bool canBuyCat = economyManager.CanPurchaseCat;
        int catCost = economyManager.GetCatCost();
        if (catCostText != null)
            catCostText.text = canBuyCat ? "Cost: " + catCost + " coins" : "PURCHASED";

        if (catStatusText != null)
        {
            catStatusText.text = !canBuyCat
                ? "Cat is at home"
                : economyManager.Coins >= catCost
                    ? "Adopt the cat"
                    : "Not enough coins";
        }

        if (catBuyButton != null)
            catBuyButton.interactable = canBuyCat && catPrefab != null && economyManager.Coins >= catCost;

        if (catCard != null)
        {
            catCard.SetPurchaseState(
                canBuyCat ? catCost + " COINS" : "OWNED",
                !canBuyCat ? "OWNED" : catPrefab == null ? "NOT SET UP" : economyManager.Coins >= catCost ? "ADOPT" : "NEED COINS",
                canBuyCat && catPrefab != null && economyManager.Coins >= catCost);
        }
    }
}
