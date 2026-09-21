using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("Assign seven empty UI Image slots in order")]
    [SerializeField] private Image[] slots = new Image[7];
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite chocolateSprite;
    [SerializeField] private Sprite extraTimeSprite;
    [SerializeField] private Sprite broomSprite;
    [SerializeField] private Sprite mopSprite;
    [SerializeField] private Sprite garbageBagSprite;

    private BunsoBoostInventory boostInventory;
    private PlayerEquipmentInventory equipmentInventory;
    private GameObject playerObject;
    private readonly List<Image> itemIcons = new();

    private struct DisplayItem
    {
        public bool isEquipment;
        public EquipmentType equipment;
        public int boostIndex;
        public Sprite sprite;
    }

    private void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        boostInventory = playerObject.GetComponent<BunsoBoostInventory>();
        if (boostInventory == null)
            boostInventory = playerObject.AddComponent<BunsoBoostInventory>();
        ResolveEquipmentInventory();

        boostInventory.InventoryChanged += Refresh;
        Refresh();
    }

    private void Update()
    {
        ResolveEquipmentInventory();
    }

    private void OnDestroy()
    {
        if (boostInventory != null)
            boostInventory.InventoryChanged -= Refresh;
        UnsubscribeEquipmentInventory();
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
        if (slots == null)
            return;

        List<DisplayItem> displayItems = BuildDisplayItems();

        for (int index = 0; index < slots.Length; index++)
        {
            Image slot = slots[index];
            if (slot == null)
                continue;

            while (itemIcons.Count <= index)
            {
                GameObject iconObject = new GameObject("ItemIcon");
                iconObject.transform.SetParent(slot.transform, false);
                Image icon = iconObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0.15f, 0.15f);
                iconRect.anchorMax = new Vector2(0.85f, 0.85f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                itemIcons.Add(icon);
            }

            bool hasItem = index < displayItems.Count;
            if (emptySlotSprite != null)
                slot.sprite = emptySlotSprite;
            itemIcons[index].sprite = hasItem
                ? displayItems[index].sprite
                : null;
            itemIcons[index].color = hasItem
                ? Color.white
                : new Color(1f, 1f, 1f, 0f);

            Button button = slot.GetComponent<Button>();
            if (button == null)
                button = slot.gameObject.AddComponent<Button>();

            button.interactable = hasItem;
            button.onClick.RemoveAllListeners();
            if (hasItem)
            {
                DisplayItem displayItem = displayItems[index];
                if (displayItem.isEquipment)
                    button.onClick.AddListener(() => equipmentInventory.Select(displayItem.equipment));
                else
                    button.onClick.AddListener(() => boostInventory.ConsumeAt(displayItem.boostIndex));
            }
        }
    }

    private List<DisplayItem> BuildDisplayItems()
    {
        List<DisplayItem> displayItems = new();

        if (equipmentInventory != null)
        {
            foreach (EquipmentType equipment in equipmentInventory.OwnedEquipment)
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
            for (int index = 0; index < boostInventory.Count; index++)
            {
                BunsoBoostType boost = boostInventory.GetItemAt(index);
                displayItems.Add(new DisplayItem
                {
                    isEquipment = false,
                    boostIndex = index,
                    sprite = GetSprite(boost)
                });
            }
        }

        return displayItems;
    }

    private Sprite GetSprite(EquipmentType equipment)
    {
        Sprite assignedSprite;
        switch (equipment)
        {
            case EquipmentType.Broom:
                assignedSprite = broomSprite;
                break;
            case EquipmentType.Mop:
                assignedSprite = mopSprite;
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
