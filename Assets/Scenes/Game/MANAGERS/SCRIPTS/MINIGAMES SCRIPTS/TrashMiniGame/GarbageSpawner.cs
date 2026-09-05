using UnityEngine;


public class GarbageSpawner : MonoBehaviour
{

    [SerializeField] private GameObject[] trashPrefabs;

    [SerializeField] private RectTransform spawnArea;



    private float spawnDelay;

    private float fallSpeed;


    private bool isSpawning = false;




    public void StartSpawning(
        float rate,
        float speed
    )
    {

        spawnDelay = rate;

        fallSpeed = speed;


        isSpawning = true;



        InvokeRepeating(
            nameof(SpawnTrash),
            0.5f,
            spawnDelay
        );



        Debug.Log(
            "Garbage Spawner Started Unlimited"
        );

    }






    private void SpawnTrash()
    {

        if(!isSpawning)
            return;



        if(trashPrefabs.Length == 0)
        {
            Debug.LogError(
                "No trash prefabs assigned!"
            );

            return;
        }




        int random =
        Random.Range(
            0,
            trashPrefabs.Length
        );





        GameObject trash =
        Instantiate(
            trashPrefabs[random],
            spawnArea
        );






        RectTransform rect =
        trash.GetComponent<RectTransform>();



        if(rect != null)
        {

            float randomX =
            Random.Range(
                -spawnArea.rect.width / 2,
                spawnArea.rect.width / 2
            );



            rect.anchoredPosition =
            new Vector2(
                randomX,
                spawnArea.rect.height / 2
            );

        }







        FallingTrash falling =
        trash.GetComponent<FallingTrash>();



        if(falling != null)
        {

            falling.SetSpeed(
                fallSpeed
            );

        }





        Debug.Log(
            "Trash Spawned"
        );

    }








    public void StopSpawning()
    {

        isSpawning = false;


        CancelInvoke(
            nameof(SpawnTrash)
        );


        Debug.Log(
            "Garbage Spawner Stopped"
        );

    }


}