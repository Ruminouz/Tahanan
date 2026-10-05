using UnityEngine;


public class GarbageCarry : MonoBehaviour
{

    public static GarbageCarry Instance;


    [SerializeField] private Transform carryPoint;


    private GameObject currentBag;

    private void Awake()
    {
        Instance = this;
    }





    public void Pickup(GameObject bag)
    {
        if (bag == null)
            return;

        PlayerEquipmentInventory inventory = GetComponent<PlayerEquipmentInventory>();
        if (inventory == null)
            inventory = gameObject.AddComponent<PlayerEquipmentInventory>();

        inventory.Add(EquipmentType.GarbageBag, bag, carryPoint);
        currentBag = bag;

        Debug.Log(
        "Player carrying garbage"
        );

    }






    public bool HasGarbage()
    {
        return currentBag != null;
    }





    public void RemoveBag()
    {
        if (currentBag == null)
            return;

        PlayerEquipmentInventory inventory = GetComponent<PlayerEquipmentInventory>();
        inventory?.Remove(EquipmentType.GarbageBag, false);
        currentBag = null;
    }

    public void ConsumeBag()
    {
        if (currentBag == null)
            return;

        PlayerEquipmentInventory inventory = GetComponent<PlayerEquipmentInventory>();
        if (inventory != null)
            inventory.Remove(EquipmentType.GarbageBag, true);
        else
            Destroy(currentBag);

        currentBag = null;
    }

    public void ClearBag()
    {
        if (currentBag == null)
            return;

        ConsumeBag();
    }

    public void ResetForDay()
    {
        currentBag = null;
    }

}