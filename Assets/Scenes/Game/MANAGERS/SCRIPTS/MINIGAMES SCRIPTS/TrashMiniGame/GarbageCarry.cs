using UnityEngine;


public class GarbageCarry : MonoBehaviour
{

    public static GarbageCarry Instance;


    [SerializeField] private Transform carryPoint;


    private GameObject currentBag;


    private Vector3 originalPosition;



    private void Awake()
    {
        Instance = this;
    }





    public void Pickup(GameObject bag)
    {
        if (bag == null)
            return;

        originalPosition =
        bag.transform.position;

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

        if(currentBag == null)
            return;



        currentBag.transform.SetParent(null);


        currentBag.transform.position =
        originalPosition;



        currentBag.SetActive(false);



        currentBag = null;



        Debug.Log(
        "Garbage bag returned"
        );

    }

    public void ClearBag()
    {
        if (currentBag == null)
            return;

        currentBag.transform.SetParent(null);
        currentBag.SetActive(false);
        currentBag = null;
    }

    public void ResetForDay()
    {
        currentBag = null;
    }

}