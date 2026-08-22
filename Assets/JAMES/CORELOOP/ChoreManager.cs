using UnityEngine;
using System.Collections.Generic;

namespace HouseChoresGame
{
    public class ChoreManager : MonoBehaviour
    {
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

        [Header("Day1 Setup")]
        public ChoreData sweepChore;
        
        public ChoreData dishesChore;

        private void Start()
        {
            AssignDay1Chores();
        }

        public void AssignDay1Chores()
        {
            AssignChore(sweepChore);
            
            AssignChore(dishesChore);
            Debug.Log("📋 Day1 chores assigned: Sweep, Mop, Dishes");
        }

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

        public void CompleteChore(ChoreData chore)
        {
            if (!activeChores.Contains(chore)) return;

            float timeRemaining = choreTimers.ContainsKey(chore) ? choreTimers[chore] : 0f;

            activeChores.Remove(chore);
            choreTimers.Remove(chore);

            OnChoreCompleted?.Invoke(chore, timeRemaining);
            Debug.Log($"✅ Completed chore: {chore.choreName}");
        }

        public void MissChore(ChoreData chore)
        {
            if (!activeChores.Contains(chore)) return;

            activeChores.Remove(chore);
            choreTimers.Remove(chore);

            OnChoreMissed?.Invoke(chore);
            Debug.Log($"❌ Missed chore: {chore.choreName}");
        }

        public bool AllChoresDone() => activeChores.Count == 0;
        public List<ChoreData> GetActiveChores() => activeChores;
        public float GetRemainingTime(ChoreData chore) => choreTimers.ContainsKey(chore) ? choreTimers[chore] : 0f;
        public void ResetChoresForNewDay()
        {
            activeChores.Clear();
            choreTimers.Clear();
            Debug.Log("🔄 Chores reset for new day.");
        }
    }
}
