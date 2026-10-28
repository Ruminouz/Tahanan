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

        EnsurePanelSurface(shopRoot);

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
        UnityAction onPurchase,
        bool furnitureRow = false)
    {
        Transform grid = EnsureGrid(shopRoot, furnitureRow);
        Transform existing = grid.Find(itemKey + "Card");
        ShopItemCardUI card = existing != null
            ? existing.GetComponent<ShopItemCardUI>()
            : CreateCard(grid, itemKey + "Card");

        card.Configure(order, icon, itemName, description, onPurchase);
        return card;
    }

    public static void RefreshShopLayout(Transform shopRoot)
    {
        if (shopRoot == null)
            return;

        Canvas.ForceUpdateCanvases();
        RefreshRowLayout(shopRoot, GridName);
        RefreshRowLayout(shopRoot, "FurnitureShopRow");
    }

    public static void SetCoinBalance(Transform shopRoot, string balance)
    {
        if (shopRoot == null)
            return;

        EnsureCoinBalance(shopRoot).text = balance;
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

    private static Transform EnsureGrid(Transform shopRoot, bool furnitureRow)
    {
        string viewportName = furnitureRow ? "FurnitureShopRow" : GridName;
        Transform viewportTransform = shopRoot.Find(viewportName);
        if (viewportTransform != null)
        {
            RectTransform existingViewportRect =
                viewportTransform.GetComponent<RectTransform>();
            SetRowBounds(existingViewportRect, furnitureRow);
            EnsureSectionLabel(shopRoot, furnitureRow);
            Transform existingContent = viewportTransform.Find("Content");
            if (existingContent != null)
                ConfigureRowContent(existingViewportRect, existingContent);
            return existingContent != null ? existingContent : viewportTransform;
        }

        GameObject viewportObject = new GameObject(
            viewportName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(RectMask2D),
            typeof(ScrollRect));
        viewportObject.transform.SetParent(shopRoot, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        SetRowBounds(viewportRect, furnitureRow);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        EnsureSectionLabel(shopRoot, furnitureRow);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        contentObject.transform.SetParent(viewportObject.transform, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;

        ScrollRect scrollRect = viewportObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        ConfigureRowContent(viewportRect, contentObject.transform);
        return contentObject.transform;
    }

    private static void ConfigureRowContent(RectTransform viewportRect, Transform content)
    {
        RectTransform contentRect = content as RectTransform;
        if (contentRect == null)
            return;

        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;

        ContentSizeFitter contentSize = content.GetComponent<ContentSizeFitter>();
        if (contentSize != null)
        {
            contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSize.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            return;

        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void RefreshRowLayout(Transform shopRoot, string rowName)
    {
        Transform viewport = shopRoot.Find(rowName);
        Transform content = viewport != null ? viewport.Find("Content") : null;
        if (content == null)
            return;

        RectTransform viewportRect = viewport as RectTransform;
        RectTransform contentRect = content as RectTransform;
        HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
        if (viewportRect == null || contentRect == null || layout == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        float contentWidth = Mathf.Max(viewportRect.rect.width, layout.preferredWidth);
        contentRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            contentWidth);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private static void EnsurePanelSurface(Transform shopRoot)
    {
        Transform existingSurface = shopRoot.Find("ShopPanelSurface");
        GameObject surfaceObject = existingSurface != null
            ? existingSurface.gameObject
            : new GameObject(
                "ShopPanelSurface",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline));

        surfaceObject.transform.SetParent(shopRoot, false);
        surfaceObject.transform.SetAsFirstSibling();

        RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
        surfaceRect.anchorMin = new Vector2(0.035f, 0.055f);
        surfaceRect.anchorMax = new Vector2(0.965f, 0.945f);
        surfaceRect.offsetMin = Vector2.zero;
        surfaceRect.offsetMax = Vector2.zero;

        Image surfaceImage = surfaceObject.GetComponent<Image>();
        surfaceImage.color = new Color(0.2f, 0.14f, 0.09f, 0.96f);
        surfaceImage.raycastTarget = false;

        Outline outline = surfaceObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.82f, 0.62f, 0.27f, 1f);
        outline.effectDistance = new Vector2(3f, 3f);
        outline.useGraphicAlpha = false;
    }

    private static void EnsureSectionLabel(Transform shopRoot, bool furnitureRow)
    {
        string objectName = furnitureRow
            ? "FurnitureSectionLabel"
            : "ShopItemsSectionLabel";
        GameObject labelObject = shopRoot.Find(objectName) != null
            ? shopRoot.Find(objectName).gameObject
            : CreateTextObject(objectName, shopRoot);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        float anchorY = furnitureRow ? 0.455f : 0.86f;
        labelRect.anchorMin = new Vector2(0.06f, anchorY);
        labelRect.anchorMax = new Vector2(0.94f, anchorY);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 30f);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = furnitureRow ? "HOME FURNITURE" : "UPGRADES & BOOSTS";
        label.fontSize = 21f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.95f, 0.82f, 0.52f);
        label.alignment = TextAlignmentOptions.Left;
        label.characterSpacing = 2f;
    }

    private static void SetRowBounds(RectTransform viewportRect, bool furnitureRow)
    {
        if (viewportRect == null)
            return;

        viewportRect.anchorMin = new Vector2(0.06f, furnitureRow ? 0.09f : 0.48f);
        viewportRect.anchorMax = new Vector2(0.94f, furnitureRow ? 0.42f : 0.82f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
    }

    private static void EnsureTitle(Transform shopRoot)
    {
        Transform existingTitle = shopRoot.Find("ShopTitle");
        GameObject titleObject = existingTitle != null
            ? existingTitle.gameObject
            : CreateTextObject("ShopTitle", shopRoot);
        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.91f);
        rect.anchorMax = new Vector2(0.5f, 0.91f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(320f, 56f);

        TMP_Text title = titleObject.GetComponent<TMP_Text>();
        title.text = "SHOP";
        title.fontSize = 46f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.78f, 0.25f);
    }

    private static TMP_Text EnsureCoinBalance(Transform shopRoot)
    {
        Transform existingBalance = shopRoot.Find("ShopCoinBalance");
        GameObject balanceObject = existingBalance != null
            ? existingBalance.gameObject
            : CreateTextObject("ShopCoinBalance", shopRoot);

        RectTransform balanceRect = balanceObject.GetComponent<RectTransform>();
        balanceRect.anchorMin = new Vector2(0.68f, 0.91f);
        balanceRect.anchorMax = new Vector2(0.87f, 0.91f);
        balanceRect.anchoredPosition = Vector2.zero;
        balanceRect.sizeDelta = new Vector2(0f, 44f);

        TMP_Text balance = balanceObject.GetComponent<TMP_Text>();
        balance.fontSize = 24f;
        balance.fontStyle = FontStyles.Bold;
        balance.color = new Color(1f, 0.88f, 0.58f);
        balance.alignment = TextAlignmentOptions.Right;
        balance.textWrappingMode = TextWrappingModes.NoWrap;
        return balance;
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
        cardSize.preferredHeight = 320f;
        cardSize.flexibleWidth = 0f;
        cardSize.flexibleHeight = 0f;

        VerticalLayoutGroup content = cardObject.GetComponent<VerticalLayoutGroup>();
        content.padding = new RectOffset(14, 14, 14, 14);
        content.spacing = 10f;
        content.childAlignment = TextAnchor.UpperCenter;
        content.childControlWidth = true;
        content.childControlHeight = false;
        content.childForceExpandWidth = true;
        content.childForceExpandHeight = false;

        Outline outline = cardObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.2f, 0.1f, 0.75f);
        outline.effectDistance = new Vector2(2f, 2f);

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
        LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = 150f;
        iconLayout.preferredHeight = 76f;
        iconObject.GetComponent<Image>().preserveAspect = true;
        iconObject.GetComponent<Image>().raycastTarget = false;

        TMP_Text nameText = CreateTextObject("ItemName", cardRoot).GetComponent<TMP_Text>();
        nameText.fontSize = 25f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.24f, 0.14f, 0.08f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.GetComponent<LayoutElement>().preferredHeight = 34f;

        TMP_Text descriptionText = CreateTextObject("ItemDescription", cardRoot).GetComponent<TMP_Text>();
        descriptionText.fontSize = 17f;
        descriptionText.color = new Color(0.28f, 0.24f, 0.18f);
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.GetComponent<LayoutElement>().preferredHeight = 62f;

        costText = CreateTextObject("ItemCost", cardRoot).GetComponent<TMP_Text>();
        costText.fontSize = 21f;
        costText.fontStyle = FontStyles.Bold;
        costText.color = new Color(0.12f, 0.38f, 0.12f);
        costText.alignment = TextAlignmentOptions.Center;
        costText.GetComponent<LayoutElement>().preferredHeight = 28f;

        GameObject buttonObject = new GameObject(
            "PurchaseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(cardRoot, false);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 40f;

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
