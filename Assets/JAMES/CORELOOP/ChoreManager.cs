using UnityEngine;
using System.Collections.Generic;

namespace HouseChoresGame
{
    public class ChoreManager : MonoBehaviour
    {
        // ✅ Singleton
        public static ChoreManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // ✅ Events
        public static event System.Action<ChoreData, float> OnChoreCompleted;
        public static event System.Action<ChoreData> OnChoreMissed;

        // ✅ Active chores + timers
        private List<ChoreData> activeChores = new List<ChoreData>();
        private Dictionary<ChoreData, float> choreTimers = new Dictionary<ChoreData, float>();

        // Assign a new chore
        public void AssignChore(ChoreData chore)
        {
            if (chore == null) return;
            if (!activeChores.Contains(chore))
            {
                activeChores.Add(chore);
                choreTimers[chore] = chore.timeLimit;
                Debug.Log($"📝 Assigned chore: {chore.choreName}");
            }
        }

        private void Update()
        {
            // ✅ Countdown timers
            List<ChoreData> toMiss = new List<ChoreData>();
            foreach (var chore in activeChores)
            {
                if (choreTimers.ContainsKey(chore))
                {
                    choreTimers[chore] -= Time.deltaTime;
                    if (choreTimers[chore] <= 0f)
                        toMiss.Add(chore);
                }
            }

            foreach (var missed in toMiss)
                MissChore(missed);
        }

        // ✅ Complete chore
        public void CompleteChore(ChoreData chore)
        {
            if (!activeChores.Contains(chore)) return;

            float timeRemaining = choreTimers.ContainsKey(chore) ? choreTimers[chore] : 0f;

            activeChores.Remove(chore);
            choreTimers.Remove(chore);

            OnChoreCompleted?.Invoke(chore, timeRemaining);
            Debug.Log($"✅ Completed chore: {chore.choreName}");
        }

        // ✅ Miss chore
        public void MissChore(ChoreData chore)
        {
            if (!activeChores.Contains(chore)) return;

            activeChores.Remove(chore);
            choreTimers.Remove(chore);

            OnChoreMissed?.Invoke(chore);
            Debug.Log($"❌ Missed chore: {chore.choreName}");
        }

        // ✅ Utility methods
        public bool AllChoresDone() => activeChores.Count == 0;

        public List<ChoreData> GetActiveChores()
        {
            return activeChores;
        }

        public float GetRemainingTime(ChoreData chore)
        {
            return choreTimers.ContainsKey(chore) ? choreTimers[chore] : 0f;
        }

        public void ResetChoresForNewDay()
        {
            activeChores.Clear();
            choreTimers.Clear();
            Debug.Log("🔄 Chores reset for new day.");
        }
    }
}
