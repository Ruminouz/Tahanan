using UnityEngine;
using HouseChoresGame;

public class MopMiniGame : MonoBehaviour
{
    public ChoreData choreData;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("🪣 Mop chore completed!");
            ChoreManager.Instance.CompleteChore(choreData);
            gameObject.SetActive(false);
        }
    }
}
