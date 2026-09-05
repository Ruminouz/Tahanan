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

        currentBag = bag;


        originalPosition =
        bag.transform.position;



        bag.transform.SetParent(
            carryPoint
        );


        bag.transform.localPosition =
        Vector3.zero;


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

}