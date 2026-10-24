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
            return;
        }

        shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}
