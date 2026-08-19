using UnityEngine;

namespace HouseChoresGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Score & Currency")]
        public int totalScore = 0;
        public int cumulativeScore = 0;
        public int totalCoins = 0;
        public int cumulativeCoins = 0;

        [Header("Mood System")]
        public MoodSystem moodSystem;

        [Header("NPC")]
        public NPCController npcController;
        public HelperNPC npcHelper;
        public int npcTolerance = 2;

        public int TotalMissed { get; private set; } = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            ChoreManager.OnChoreCompleted += HandleChoreCompleted;
            ChoreManager.OnChoreMissed += HandleChoreMissed;
        }

        private void OnDisable()
        {
            ChoreManager.OnChoreCompleted -= HandleChoreCompleted;
            ChoreManager.OnChoreMissed -= HandleChoreMissed;
        }

        private void HandleChoreCompleted(ChoreData chore, float timeRemaining)
        {
            int basePoints = 50;
            int bonusPoints = Mathf.RoundToInt((timeRemaining / chore.timeLimit) * 50);
            int pointsEarned = basePoints + bonusPoints;

            totalScore += pointsEarned;
            cumulativeScore += pointsEarned;

            int baseCoins = 5;
            int bonusCoins = Mathf.RoundToInt((timeRemaining / chore.timeLimit) * 5);
            int coinsEarned = baseCoins + bonusCoins;

            totalCoins += coinsEarned;
            cumulativeCoins += coinsEarned;

            if (moodSystem != null) moodSystem.IncreaseMood(10f);

            Debug.Log($"✅ {chore.choreName} completed → +{pointsEarned} points, +{coinsEarned} coins");
        }

        private void HandleChoreMissed(ChoreData chore)
        {
            TotalMissed++;
            totalScore = Mathf.Max(0, totalScore - 25);
            cumulativeScore = Mathf.Max(0, cumulativeScore - 25);

            if (moodSystem != null) moodSystem.DecreaseMood(10f);

            if (npcController != null && TotalMissed >= npcTolerance)
                npcController.TriggerChase();

            Debug.Log($"❌ {chore.choreName} missed → -25 points");
        }

        // ✅ Wrappers for old references
        public System.Collections.Generic.List<ChoreData> GetActiveChores()
        {
            return ChoreManager.Instance.GetActiveChores();
        }

        public float GetRemainingTime(ChoreData chore)
        {
            return ChoreManager.Instance.GetRemainingTime(chore);
        }

        public void CompleteChore(ChoreData chore, bool completedByPlayer = true)
        {
            ChoreManager.Instance.CompleteChore(chore);
        }
    }
}
