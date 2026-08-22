using UnityEngine;

namespace HouseChoresGame
{
    public class MiniGameTrigger : MonoBehaviour
    {
        public ChoreData choreData;          // assign the chore ScriptableObject (Sweeping OR Dishwashing)
        public GameObject miniGamePrefab;    // assign the panel prefab (SweepingPanel OR DishwashingPanel)
        public Transform canvasTransform;    // assign your MiniGameCanvas

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
            {
                // Spawn panel
                GameObject panel = Instantiate(miniGamePrefab, canvasTransform);

                // Initialize depending on which script is attached
                Sweeping sweeping = panel.GetComponent<Sweeping>();
                if (sweeping != null)
                {
                    sweeping.Initialize(GameManager.Instance, choreData);
                }

                Dishwashing dishwashing = panel.GetComponent<Dishwashing>();
                if (dishwashing != null)
                {
                    dishwashing.Initialize(GameManager.Instance, choreData);
                }

                // Register with OverlayManager
                MiniGameOverlayManager.Instance.RegisterMiniGame(choreData, panel);

                Debug.Log($"Mini-game {choreData.choreName} launched and registered.");
            }
        }
    }
}
