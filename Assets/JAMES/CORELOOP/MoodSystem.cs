using UnityEngine;

namespace HouseChoresGame
{
    public class MoodSystem : MonoBehaviour
    {
        private float moodValue = 50f; // start at neutral

        public void IncreaseMood(float amount)
        {
            moodValue = Mathf.Clamp(moodValue + amount, 0f, 100f);
            Debug.Log($"Mood increased → {moodValue}");
        }

        public void DecreaseMood(float amount)
        {
            moodValue = Mathf.Clamp(moodValue - amount, 0f, 100f);
            Debug.Log($"Mood decreased → {moodValue}");
        }

        public bool IsHelperActive()
        {
            // Helper active if mood > 50
            return moodValue > 50f;
        }

        public float GetMoodValue()
        {
            return moodValue;
        }
    }
}
