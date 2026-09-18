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

        shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}
