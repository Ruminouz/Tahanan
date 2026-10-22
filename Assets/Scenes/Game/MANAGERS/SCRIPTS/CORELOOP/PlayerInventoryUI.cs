using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("Inventory panel and pre-placed slots")]
    [SerializeField] private Image[] slots = new Image[7];
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite chocolateSprite;
    [SerializeField] private Sprite extraTimeSprite;
    [SerializeField] private Sprite broomSprite;
    [SerializeField] private Sprite vacuumSprite;
    [SerializeField] private Sprite mopSprite;
    [SerializeField] private Sprite upgradedMopSprite;
    [SerializeField] private Sprite garbageBagSprite;

    private BunsoBoostInventory boostInventory;
    private PlayerEquipmentInventory equipmentInventory;
    private EconomyManager economyManager;
    private GameObject playerObject;
    private readonly List<Image> itemIcons = new();
    private readonly List<TMP_Text> itemQuantities = new();
    private readonly List<TMP_Text> itemNames = new();
    [SerializeField] private RectTransform inventoryPanel;
    private Image inventoryPanelBackground;
    private bool inventoryOpen;
    private bool slotContentsInitialized;
    private bool tutorialInputEnabled = true;
    private bool hideHotbarForTutorial;
    private bool hotbarTutorialHighlighted;
    private Vector3 hotbarOriginalScale;
    private Vector3 hotbarIconOriginalScale;
    private Color hotbarOriginalColor;
    private bool hasHotbarSelection;
    private bool hotbarSelectionIsEquipment;
    private EquipmentType selectedEquipmentType;
    private BunsoBoostType selectedBoostType;

    private struct DisplayItem
    {
        public bool isEquipment;
        public EquipmentType equipment;
        public BunsoBoostType boost;
        public int quantity;
        public Sprite sprite;
    }

    private void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        ResolveInventoryPanel();
        SetInventoryPanelVisible(false);

        boostInventory = playerObject.GetComponent<BunsoBoostInventory>();
        if (boostInventory == null)
            boostInventory = playerObject.AddComponent<BunsoBoostInventory>();
        if (playerObject.GetComponent<PlayerEquipmentInventory>() == null)
            playerObject.AddComponent<PlayerEquipmentInventory>();
        ResolveEquipmentInventory();
        ResolveEconomyManager();

        boostInventory.InventoryChanged += Refresh;
        Refresh();
    }

    private void Update()
    {
        ResolveEquipmentInventory();
        ResolveEconomyManager();

        if (!tutorialInputEnabled || Keyboard.current == null)
            return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            SelectNextItem();

        if (Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInventoryPanel();

        if (Keyboard.current.eKey.wasPressedThisFrame)
            ConsumeSelectedBoost();
    }

    private void OnDestroy()
    {
        if (boostInventory != null)
            boostInventory.InventoryChanged -= Refresh;
        UnsubscribeEquipmentInventory();
        UnsubscribeEconomyManager();
    }

    private void ResolveEconomyManager()
    {
        EconomyManager currentEconomyManager = EconomyManager.Instance;
        if (currentEconomyManager == economyManager)
            return;

        UnsubscribeEconomyManager();
        economyManager = currentEconomyManager;
        if (economyManager != null)
        {
            economyManager.BroomUpgradeChanged += OnBroomUpgradeChanged;
            economyManager.MopUpgradeChanged += OnMopUpgradeChanged;
        }
        Refresh();
    }

    private void UnsubscribeEconomyManager()
    {
        if (economyManager != null)
        {
            economyManager.BroomUpgradeChanged -= OnBroomUpgradeChanged;
            economyManager.MopUpgradeChanged -= OnMopUpgradeChanged;
        }
    }

    private void OnBroomUpgradeChanged(int level)
    {
        Refresh();
    }

    private void OnMopUpgradeChanged(int level)
    {
        Refresh();
    }

    private void ResolveEquipmentInventory()
    {
        if (playerObject == null)
            return;

        PlayerEquipmentInventory currentInventory =
            playerObject.GetComponent<PlayerEquipmentInventory>();
        if (currentInventory == equipmentInventory)
            return;

        UnsubscribeEquipmentInventory();
        equipmentInventory = currentInventory;
        if (equipmentInventory != null)
            equipmentInventory.InventoryChanged += Refresh;
        Refresh();
    }

    private void UnsubscribeEquipmentInventory()
    {
        if (equipmentInventory != null)
            equipmentInventory.InventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (slots == null || slots.Length == 0)
            return;

        List<DisplayItem> displayItems = BuildDisplayItems();
        int selectedIndex = FindSelectedEquipment(displayItems);
        if (selectedIndex >= 0)
            SetHotbarSelection(displayItems[selectedIndex]);
        if (selectedIndex < 0)
            selectedIndex = FindHotbarSelection(displayItems);
        if (selectedIndex < 0 && displayItems.Count > 0)
        {
            bool replacingConsumedBoost = hasHotbarSelection &&
                !hotbarSelectionIsEquipment;
            selectedIndex = FindSelectedEquipment(displayItems);
            if (selectedIndex < 0)
                selectedIndex = 0;
            SetHotbarSelection(displayItems[selectedIndex]);
            if (replacingConsumedBoost &&
                displayItems[selectedIndex].isEquipment &&
                equipmentInventory != null)
            {
                equipmentInventory.Select(displayItems[selectedIndex].equipment);
            }
        }

        EnsureSlotContents();
        for (int index = 0; index < slots.Length; index++)
        {
            Image slot = slots[index];
            if (slot == null)
                continue;

            bool isHotbarSlot = index == 0;
            bool slotIsVisible = (isHotbarSlot && !hideHotbarForTutorial) ||
                inventoryOpen;
            slot.gameObject.SetActive(slotIsVisible);

            int itemIndex = inventoryOpen ? index : selectedIndex;
            bool hasItem = itemIndex >= 0 && itemIndex < displayItems.Count;
            DisplayItem displayedItem = hasItem
                ? displayItems[itemIndex]
                : default;

            if (emptySlotSprite != null)
                slot.sprite = emptySlotSprite;
            itemIcons[index].sprite = hasItem ? displayedItem.sprite : null;
            itemIcons[index].color = hasItem && displayedItem.sprite != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0f);
            itemQuantities[index].text = hasItem &&
                !displayedItem.isEquipment &&
                displayedItem.quantity > 1
                    ? displayedItem.quantity + "x"
                    : string.Empty;
            itemNames[index].text = (inventoryOpen || index == 0) && hasItem
                ? GetDisplayName(displayedItem)
                : string.Empty;

            Button button = slot.GetComponent<Button>();
            if (button == null)
                button = slot.gameObject.AddComponent<Button>();
            button.interactable = hasItem;
            button.onClick.RemoveAllListeners();
            if (hasItem)
            {
                if (inventoryOpen)
                {
                    DisplayItem item = displayedItem;
                    button.onClick.AddListener(() => ActivateItem(item));
                }
                else
                    button.onClick.AddListener(ActivateHotbarItem);
            }
        }
    }

    private List<DisplayItem> BuildDisplayItems()
    {
        List<DisplayItem> displayItems = new();

        if (equipmentInventory != null)
        {
            List<EquipmentType> ownedEquipment =
                new(equipmentInventory.OwnedEquipment);
            ownedEquipment.Sort();
            foreach (EquipmentType equipment in ownedEquipment)
            {
                displayItems.Add(new DisplayItem
                {
                    isEquipment = true,
                    equipment = equipment,
                    sprite = GetSprite(equipment)
                });
            }
        }

        if (boostInventory != null)
        {
            for (int index = 0; index < boostInventory.StackCount; index++)
            {
                BunsoBoostType boost = boostInventory.GetStackAt(index);
                displayItems.Add(new DisplayItem
                {
                    isEquipment = false,
                    boost = boost,
                    quantity = boostInventory.GetQuantity(boost),
                    sprite = GetSprite(boost)
                });
            }
        }

        return displayItems;
    }

    private void SelectNextItem()
    {
        List<DisplayItem> displayItems = BuildDisplayItems();
        if (displayItems.Count == 0)
            return;

        int currentIndex = FindHotbarSelection(displayItems);
        int nextIndex = (currentIndex + 1) % displayItems.Count;
        DisplayItem nextItem = displayItems[nextIndex];
        SetHotbarSelection(nextItem);
        if (nextItem.isEquipment && equipmentInventory != null)
            equipmentInventory.Select(nextItem.equipment);
        else if (equipmentInventory != null)
            equipmentInventory.SelectBoost(
                nextItem.boost,
                nextItem.sprite);
        else
            Refresh();
    }

    private void ToggleInventoryPanel()
    {
        if (inventoryPanel == null)
            return;

        SetInventoryPanelVisible(!inventoryOpen);
        Refresh();
    }

    public bool IsInventoryOpen => inventoryOpen;
    public bool HasInventoryPanel
    {
        get
        {
            if (inventoryPanel == null)
                ResolveInventoryPanel();
            return inventoryPanel != null;
        }
    }

    public void SetTutorialInputEnabled(bool enabled)
    {
        tutorialInputEnabled = enabled;
    }

    public void SetHotbarHiddenForTutorial(bool hidden)
    {
        hideHotbarForTutorial = hidden;
        Refresh();
    }

    public void CloseInventoryForTutorial()
    {
        if (inventoryPanel == null)
            ResolveInventoryPanel();
        if (inventoryPanel == null)
            return;

        SetInventoryPanelVisible(false);
        Refresh();
    }

    public bool SelectEquipmentForTutorial(EquipmentType equipment)
    {
        if (equipmentInventory == null ||
            !equipmentInventory.Owns(equipment))
            return false;

        List<DisplayItem> displayItems = BuildDisplayItems();
        int itemIndex = displayItems.FindIndex(item =>
            item.isEquipment && item.equipment == equipment);
        if (itemIndex < 0)
            return false;

        SetHotbarSelection(displayItems[itemIndex]);
        equipmentInventory.Select(equipment);
        Refresh();
        return true;
    }

    public void HighlightSelectedHotbarForTutorial(bool highlighted)
    {
        if (slots == null || slots.Length == 0 || slots[0] == null)
            return;

        Image hotbarSlot = slots[0];
        if (highlighted)
        {
            if (hotbarTutorialHighlighted)
                return;

            hotbarTutorialHighlighted = true;
            hotbarOriginalScale = hotbarSlot.transform.localScale;
            hotbarOriginalColor = hotbarSlot.color;
            hotbarSlot.transform.localScale = hotbarOriginalScale * 1.25f;
            hotbarSlot.color = new Color(1f, 0.82f, 0.3f, 1f);
            if (itemIcons.Count > 0 && itemIcons[0] != null)
            {
                RectTransform iconRect = itemIcons[0].rectTransform;
                hotbarIconOriginalScale = iconRect.localScale;
                iconRect.localScale = hotbarIconOriginalScale * 1.35f;
            }
            return;
        }

        if (!hotbarTutorialHighlighted)
            return;

        hotbarSlot.transform.localScale = hotbarOriginalScale;
        hotbarSlot.color = hotbarOriginalColor;
        if (itemIcons.Count > 0 && itemIcons[0] != null)
            itemIcons[0].rectTransform.localScale = hotbarIconOriginalScale;
        hotbarTutorialHighlighted = false;
    }

    private void ResolveInventoryPanel()
    {
        Transform panelTransform = inventoryPanel != null
            ? inventoryPanel
            : transform.name == "InventoryPanel"
                ? transform
                : null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (panelTransform == null && canvas != null)
        {
            RectTransform[] canvasElements =
                canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform canvasElement in canvasElements)
            {
                if (canvasElement.name == "InventoryPanel")
                {
                    panelTransform = canvasElement;
                    break;
                }
            }
        }

        if (panelTransform == null)
        {
            Debug.LogWarning(
                "InventoryPanel was not found in the scene. Assign an InventoryPanel object.",
                this);
            return;
        }

        inventoryPanel = panelTransform as RectTransform;
        inventoryPanelBackground = panelTransform.GetComponent<Image>();
        ResolvePreplacedSlots(panelTransform);
    }

    private void ResolvePreplacedSlots(Transform panelTransform)
    {
        List<Image> preplacedSlots = new();
        foreach (Transform child in panelTransform)
        {
            if (!child.name.StartsWith("Slot"))
                continue;

            Image slot = child.GetComponent<Image>();
            if (slot != null)
                preplacedSlots.Add(slot);
        }

        if (preplacedSlots.Count > 0)
        {
            slots = preplacedSlots.ToArray();
            return;
        }

        Debug.LogWarning(
            "InventoryPanel has no direct child Image objects named Slot. " +
            "Add pre-placed slot Images as children of InventoryPanel.",
            this);
    }

    private void SetInventoryPanelVisible(bool visible)
    {
        inventoryOpen = visible;
        if (inventoryPanelBackground != null)
        {
            inventoryPanelBackground.enabled = visible;
            inventoryPanelBackground.raycastTarget = visible;
        }
    }

    private void EnsureSlotContents()
    {
        if (slots == null || slotContentsInitialized)
            return;

        slotContentsInitialized = true;
        for (int index = 0; index < slots.Length; index++)
        {
            Image slot = slots[index];
            if (slot == null)
            {
                itemIcons.Add(null);
                itemQuantities.Add(null);
                itemNames.Add(null);
                continue;
            }

            GameObject iconObject = new GameObject("ItemIcon");
            iconObject.transform.SetParent(slot.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.15f, 0.25f);
            iconRect.anchorMax = new Vector2(0.85f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            itemIcons.Add(icon);

            TMP_Text quantity = CreateSlotText(
                slot.transform,
                "ItemQuantity",
                12f,
                FontStyles.Bold,
                new Vector2(0.48f, 0.55f),
                Vector2.one,
                new Vector2(-3f, -3f),
                TextAlignmentOptions.TopRight);
            itemQuantities.Add(quantity);

            TMP_Text itemName = CreateSlotText(
                slot.transform,
                "ItemName",
                10f,
                FontStyles.Bold,
                Vector2.zero,
                new Vector2(1f, 0.28f),
                Vector2.zero,
                TextAlignmentOptions.Center);
            itemNames.Add(itemName);

            if (index == 0)
            {
                TMP_Text keyHint = CreateSlotText(
                    slot.transform,
                    "InventoryKeyHint",
                    9f,
                    FontStyles.Bold,
                    new Vector2(0f, 0f),
                    Vector2.one,
                    new Vector2(-4f, 1f),
                    TextAlignmentOptions.BottomRight);
                    
            }
        }
    }

    private TMP_Text CreateSlotText(
        Transform parent,
        string objectName,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.alignment = alignment;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;
        return text;
    }

    private void ActivateItem(DisplayItem displayItem)
    {
        if (displayItem.isEquipment)
        {
            SetHotbarSelection(displayItem);
            equipmentInventory.Select(displayItem.equipment);
        }
        else
            boostInventory.ConsumeOne(displayItem.boost);
    }

    private void ActivateHotbarItem()
    {
        if (!hasHotbarSelection)
            return;

        if (hotbarSelectionIsEquipment)
        {
            if (equipmentInventory != null)
                equipmentInventory.Select(selectedEquipmentType);
        }
        else if (boostInventory != null)
        {
            boostInventory.ConsumeOne(selectedBoostType);
        }
    }

    private void ConsumeSelectedBoost()
    {
        if (!hasHotbarSelection ||
            hotbarSelectionIsEquipment ||
            boostInventory == null)
            return;

        boostInventory.ConsumeOne(selectedBoostType);
    }

    private int FindHotbarSelection(List<DisplayItem> displayItems)
    {
        if (!hasHotbarSelection)
            return -1;

        return displayItems.FindIndex(item =>
            item.isEquipment == hotbarSelectionIsEquipment &&
            (item.isEquipment
                ? item.equipment == selectedEquipmentType
                : item.boost == selectedBoostType));
    }

    private int FindSelectedEquipment(List<DisplayItem> displayItems)
    {
        if (equipmentInventory == null ||
            !equipmentInventory.SelectedEquipment.HasValue)
            return -1;

        EquipmentType selected = equipmentInventory.SelectedEquipment.Value;
        return displayItems.FindIndex(item =>
            item.isEquipment && item.equipment == selected);
    }

    private void SetHotbarSelection(DisplayItem displayItem)
    {
        hasHotbarSelection = true;
        hotbarSelectionIsEquipment = displayItem.isEquipment;
        if (displayItem.isEquipment)
            selectedEquipmentType = displayItem.equipment;
        else
            selectedBoostType = displayItem.boost;
    }

    private string GetDisplayName(DisplayItem displayItem)
    {
        return displayItem.isEquipment
            ? GetEquipmentName(displayItem.equipment)
            : GetBoostName(displayItem.boost);
    }

    private string GetEquipmentName(EquipmentType equipment)
    {
        switch (equipment)
        {
            case EquipmentType.Broom:
                return economyManager != null && economyManager.HasUpgradedBroom
                    ? "Vacuum"
                    : "Broom";
            case EquipmentType.Mop:
                return economyManager != null && economyManager.HasUpgradedMop
                    ? "Upgraded Mop"
                    : "Mop";
            case EquipmentType.GarbageBag:
                return "Garbage Bag";
            default:
                return equipment.ToString();
        }
    }

    private string GetBoostName(BunsoBoostType boost)
    {
        return boost == BunsoBoostType.Chocolate
            ? "Chocolate"
            : "Time Freezer";
    }

    private Sprite GetSprite(EquipmentType equipment)
    {
        Sprite assignedSprite;
        switch (equipment)
        {
            case EquipmentType.Broom:
                assignedSprite = economyManager != null && economyManager.HasUpgradedBroom
                    ? vacuumSprite
                    : broomSprite;
                break;
            case EquipmentType.Mop:
                assignedSprite = economyManager != null && economyManager.HasUpgradedMop
                    ? upgradedMopSprite
                    : mopSprite;
                break;
            case EquipmentType.GarbageBag:
                assignedSprite = garbageBagSprite;
                break;
            default:
                return null;
        }

        if (assignedSprite != null)
            return assignedSprite;

        GameObject visual = equipmentInventory.GetVisual(equipment);
        if (visual == null)
            return null;

        SpriteRenderer spriteRenderer =
            visual.GetComponentInChildren<SpriteRenderer>(true);
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private Sprite GetSprite(BunsoBoostType boost)
    {
        return boost == BunsoBoostType.Chocolate
            ? chocolateSprite
            : extraTimeSprite;
    }
}
