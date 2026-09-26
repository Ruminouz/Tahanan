using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopItemCardUI : MonoBehaviour
{
    private const string GridName = "ShopItemGrid";

    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Button actionButton;

    public static void PrepareShopPanel(Transform shopRoot, Button closeButton)
    {
        if (shopRoot == null)
            return;

        for (int i = 0; i < shopRoot.childCount; i++)
        {
            Transform child = shopRoot.GetChild(i);
            if (child.name.StartsWith("LevelText")
                || child.name.StartsWith("CostText")
                || child.name.StartsWith("StatusText")
                || child.name.StartsWith("BuyButton")
                || child.name.StartsWith("CloseButton"))
            {
                child.gameObject.SetActive(closeButton != null && child.gameObject == closeButton.gameObject);
            }
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);

            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.92f, 0.9f);
                closeRect.anchorMax = new Vector2(0.92f, 0.9f);
                closeRect.anchoredPosition = Vector2.zero;
                closeRect.sizeDelta = new Vector2(64f, 54f);
            }

            TMP_Text closeText = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeText != null)
                closeText.text = "X";

            closeButton.transform.SetAsLastSibling();
        }

        EnsureTitle(shopRoot);
    }

    public static ShopItemCardUI GetOrCreate(
        Transform shopRoot,
        string itemKey,
        int order,
        Sprite icon,
        string itemName,
        string description,
        UnityAction onPurchase)
    {
        Transform grid = EnsureGrid(shopRoot);
        Transform existing = grid.Find(itemKey + "Card");
        ShopItemCardUI card = existing != null
            ? existing.GetComponent<ShopItemCardUI>()
            : CreateCard(grid, itemKey + "Card");

        card.Configure(order, icon, itemName, description, onPurchase);
        return card;
    }

    public void SetPurchaseState(string cost, string action, bool canPurchase)
    {
        if (costText != null)
            costText.text = cost;

        if (actionText != null)
            actionText.text = action;

        if (actionButton != null)
            actionButton.interactable = canPurchase;
    }

    private static Transform EnsureGrid(Transform shopRoot)
    {
        Transform grid = shopRoot.Find(GridName);
        if (grid != null)
            return grid;

        GameObject gridObject = new GameObject(GridName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        gridObject.transform.SetParent(shopRoot, false);

        RectTransform rect = gridObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.16f);
        rect.anchorMax = new Vector2(0.92f, 0.8f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = gridObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return gridObject.transform;
    }

    private static void EnsureTitle(Transform shopRoot)
    {
        if (shopRoot.Find("ShopTitle") != null)
            return;

        GameObject titleObject = CreateTextObject("ShopTitle", shopRoot);
        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -64f);
        rect.sizeDelta = new Vector2(420f, 76f);

        TMP_Text title = titleObject.GetComponent<TMP_Text>();
        title.text = "SHOP";
        title.fontSize = 52f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.78f, 0.25f);
    }

    private static ShopItemCardUI CreateCard(Transform grid, string cardName)
    {
        GameObject cardObject = new GameObject(
            cardName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(LayoutElement));
        cardObject.transform.SetParent(grid, false);

        Image background = cardObject.GetComponent<Image>();
        background.color = new Color(0.96f, 0.88f, 0.69f);
        background.raycastTarget = true;

        LayoutElement cardSize = cardObject.GetComponent<LayoutElement>();
        cardSize.preferredWidth = 250f;
        cardSize.preferredHeight = 450f;
        cardSize.flexibleWidth = 0f;
        cardSize.flexibleHeight = 0f;

        VerticalLayoutGroup content = cardObject.GetComponent<VerticalLayoutGroup>();
        content.padding = new RectOffset(14, 14, 14, 14);
        content.spacing = 8f;
        content.childAlignment = TextAnchor.UpperCenter;
        content.childControlWidth = true;
        content.childControlHeight = false;
        content.childForceExpandWidth = true;
        content.childForceExpandHeight = false;

        ShopItemCardUI card = cardObject.AddComponent<ShopItemCardUI>();
        card.CreateContent(cardObject.transform);
        return card;
    }

    private void CreateContent(Transform cardRoot)
    {
        GameObject iconObject = new GameObject(
            "ItemIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        iconObject.transform.SetParent(cardRoot, false);
        iconObject.GetComponent<LayoutElement>().preferredHeight = 170f;
        iconObject.GetComponent<Image>().preserveAspect = true;
        iconObject.GetComponent<Image>().raycastTarget = false;

        TMP_Text nameText = CreateTextObject("ItemName", cardRoot).GetComponent<TMP_Text>();
        nameText.fontSize = 25f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.24f, 0.14f, 0.08f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.GetComponent<LayoutElement>().preferredHeight = 42f;

        TMP_Text descriptionText = CreateTextObject("ItemDescription", cardRoot).GetComponent<TMP_Text>();
        descriptionText.fontSize = 17f;
        descriptionText.color = new Color(0.28f, 0.24f, 0.18f);
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.GetComponent<LayoutElement>().preferredHeight = 88f;

        costText = CreateTextObject("ItemCost", cardRoot).GetComponent<TMP_Text>();
        costText.fontSize = 21f;
        costText.fontStyle = FontStyles.Bold;
        costText.color = new Color(0.12f, 0.38f, 0.12f);
        costText.alignment = TextAlignmentOptions.Center;
        costText.GetComponent<LayoutElement>().preferredHeight = 34f;

        GameObject buttonObject = new GameObject(
            "PurchaseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(cardRoot, false);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 48f;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.19f, 0.62f, 0.2f);
        actionButton = buttonObject.GetComponent<Button>();
        actionButton.targetGraphic = buttonImage;
        actionButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = actionButton.colors;
        colors.highlightedColor = new Color(0.27f, 0.74f, 0.28f);
        colors.pressedColor = new Color(0.12f, 0.44f, 0.14f);
        colors.disabledColor = new Color(0.48f, 0.48f, 0.44f);
        actionButton.colors = colors;

        actionText = CreateTextObject("ButtonLabel", buttonObject.transform).GetComponent<TMP_Text>();
        RectTransform buttonLabelRect = actionText.GetComponent<RectTransform>();
        buttonLabelRect.anchorMin = Vector2.zero;
        buttonLabelRect.anchorMax = Vector2.one;
        buttonLabelRect.offsetMin = Vector2.zero;
        buttonLabelRect.offsetMax = Vector2.zero;
        actionText.fontSize = 19f;
        actionText.fontStyle = FontStyles.Bold;
        actionText.color = Color.white;
        actionText.alignment = TextAlignmentOptions.Center;

        _nameText = nameText;
        _descriptionText = descriptionText;
        _iconImage = iconObject.GetComponent<Image>();
    }

    private TMP_Text _nameText;
    private TMP_Text _descriptionText;
    private Image _iconImage;
    private UnityAction purchaseAction;

    private void Configure(int order, Sprite icon, string itemName, string description, UnityAction onPurchase)
    {
        transform.SetSiblingIndex(Mathf.Min(order, transform.parent.childCount - 1));

        if (_iconImage != null)
            _iconImage.sprite = icon;

        if (_nameText != null)
            _nameText.text = itemName;

        if (_descriptionText != null)
            _descriptionText.text = description;

        if (actionButton != null && purchaseAction != onPurchase)
        {
            if (purchaseAction != null)
                actionButton.onClick.RemoveListener(purchaseAction);

            purchaseAction = onPurchase;
            if (purchaseAction != null)
                actionButton.onClick.AddListener(purchaseAction);
        }
    }

    private static GameObject CreateTextObject(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return textObject;
    }
}
