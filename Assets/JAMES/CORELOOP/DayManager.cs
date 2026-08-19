using UnityEngine;

namespace HouseChoresGame
{
    public class DayManager : MonoBehaviour
    {
        public int currentDay = 1;
        public int maxDay = 7;
        public float dayLength = 120f; // 2 minutes for prototype
        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;

            // ✅ End day if all chores are done
            if (ChoreManager.Instance.AllChoresDone())
            {
                FinishDay();
            }
            // ✅ Or if time runs out
            else if (timer >= dayLength)
            {
                FinishDay();
            }
        }

        public void FinishDay()
        {
            Debug.Log($"📊 Day {currentDay} finished. Score: {GameManager.Instance.totalScore}, Coins: {GameManager.Instance.totalCoins}");

            if (currentDay >= maxDay)
            {
                Debug.Log("🎉 Victory! All days completed.");
                // For prototype: just log victory
            }
            else
            {
                currentDay++;
                Debug.Log($"➡️ Moving to Day {currentDay}");
                // For prototype: no scene load, just reset chores
                ChoreManager.Instance.ResetChoresForNewDay();
                timer = 0f;
            }
        }
    }
}
