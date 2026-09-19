using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventoryUI : MonoBehaviour
{
    private static readonly EquipmentType[] EquipmentOrder =
    {
        EquipmentType.Broom,
        EquipmentType.Mop,
        EquipmentType.GarbageBag
    };

    private readonly Dictionary<EquipmentType, Button> buttons = new();
    private PlayerEquipmentInventory inventory;

    private void Start()
    {
        inventory = FindFirstObjectByType<PlayerEquipmentInventory>();
        if (inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                inventory = player.AddComponent<PlayerEquipmentInventory>();
        }

        if (inventory == null)
            return;

        BuildPanel();
        inventory.InventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.InventoryChanged -= Refresh;
    }

    private void BuildPanel()
    {
        GameObject panelObject = new GameObject("EquipmentInventoryPanel");
        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 18f);
        panelRect.sizeDelta = new Vector2(330f, 72f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);

        HorizontalLayoutGroup layout = panelObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (EquipmentType equipment in EquipmentOrder)
        {
            GameObject buttonObject = new GameObject(equipment.ToString());
            buttonObject.transform.SetParent(panelObject.transform, false);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.18f, 0.24f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            EquipmentType selectedEquipment = equipment;
            button.onClick.AddListener(() => inventory.Select(selectedEquipment));
            buttons[equipment] = button;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.color = Color.white;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private void Refresh()
    {
        foreach (EquipmentType equipment in EquipmentOrder)
        {
            Button button = buttons[equipment];
            bool owned = inventory.Owns(equipment);
            bool selected = inventory.SelectedEquipment == equipment;
            button.interactable = owned;
            button.image.color = selected
                ? new Color(0.24f, 0.55f, 0.3f, 1f)
                : owned
                    ? new Color(0.15f, 0.18f, 0.24f, 1f)
                    : new Color(0.08f, 0.09f, 0.12f, 0.75f);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = owned ? equipment.ToString() : equipment + "\nLocked";
        }
    }
}
