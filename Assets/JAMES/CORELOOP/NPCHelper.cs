using UnityEngine;

namespace HouseChoresGame
{
    public class HelperNPC : MonoBehaviour
    {
        public GameManager gameManager;
        public MoodSystem moodSystem;

        public bool isActive = true;

        private void Update()
        {
            // Simple auto-help trigger
            if (isActive && moodSystem != null && moodSystem.IsHelperActive())
            {
                // For prototype: just auto-complete a random chore
                if (Random.value < 0.01f) // small chance per frame
                {
                    var chores = gameManager.GetActiveChores();
                    if (chores != null && chores.Count > 0)
                    {
                        gameManager.CompleteChore(chores[0], false);
                        ShowRandomPositiveBubble();
                    }
                }
            }
        }

        public void ShowRandomPositiveBubble()
        {
            Debug.Log("Helper NPC shows positive bubble (placeholder).");
        }

        public void ShowRandomNegativeBubble()
        {
            Debug.Log("Helper NPC shows negative bubble (placeholder).");
        }
    }
}
