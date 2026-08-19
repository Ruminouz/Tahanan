using UnityEngine;

namespace HouseChoresGame
{
    public class NPCController : MonoBehaviour
    {
        [Header("Chase Settings")]
        public Transform player;
        public bool isChasing = false;

        [Header("Missed Chore Limit")]
        public int missedChoreLimit = 2;

        private GameManager gameManager;

        private void Awake()
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        private void Update()
        {
            // Simple chase trigger based on missed chores
            if (gameManager != null && gameManager.TotalMissed >= missedChoreLimit && !isChasing)
            {
                TriggerChase();
            }

            if (isChasing && player != null)
            {
                // Basic chase movement (straight line)
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    2f * Time.deltaTime
                );

                float distance = Vector2.Distance(transform.position, player.position);
                if (distance < 0.5f)
                {
                    Debug.Log("NPC caught the player → Game Over!");
                    // Hook into your GameOverManager here if needed
                }
            }
        }

        public void TriggerChase()
        {
            isChasing = true;
            Debug.Log("NPC started chasing the player!");
        }

        public void StopChase()
        {
            isChasing = false;
            Debug.Log("NPC stopped chasing.");
        }

        public void ShowRandomPositiveBubble()
        {
            Debug.Log("NPC shows a positive bubble (placeholder).");
        }

        public void ShowRandomNegativeBubble()
        {
            Debug.Log("NPC shows a negative bubble (placeholder).");
        }
    }
}
