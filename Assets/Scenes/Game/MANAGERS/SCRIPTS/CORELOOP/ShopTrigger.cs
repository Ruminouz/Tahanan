using UnityEngine;

public class ShopTrigger : Interactable
{
    [SerializeField] private GameObject shopPanel;

    public override void Interact()
    {
        if (shopPanel == null)
        {
            Debug.LogWarning("Shop panel is missing from " + gameObject.name + ".");
            return;
        }

        MopShopUI shopUI = shopPanel.GetComponent<MopShopUI>();
        if (shopUI != null)
        {
            shopUI.OpenShop();
            DayManager.Instance?.OnDayTwoShopOpened(this);
            return;
        }

        shopPanel.SetActive(true);
        DayManager.Instance?.OnDayTwoShopOpened(this);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}
