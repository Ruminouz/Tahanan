using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HouseChoresGame
{
    public class DayManager : MonoBehaviour
    {
        public int currentDay = 1;
        public int maxDay = 7;
        public float dayLength = 120f; // seconds per day
        private float timer;

        [Header("UI")]
        public Text dayLabel;             // assign a UI Text for "Day X"
        public Image fadeOverlay;         // assign a full‑screen black Image (alpha 0)

        private bool isTransitioning = false;

        private void Start()
        {
            timer = 0f;
            ShowDayLabel();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (!isTransitioning)
            {
                if (ChoreManager.Instance.AllChoresDone())
                {
                    FinishDay();
                }
                else if (timer >= dayLength)
                {
                    FinishDay();
                }
            }
        }

        private void FinishDay()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            if (currentDay >= maxDay)
            {
                Debug.Log("🎉 Victory! All days completed.");
                return;
            }

            currentDay++;
            StartCoroutine(FadeToNextDay());
        }

        private IEnumerator FadeToNextDay()
        {
            // Fade out
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime;
                fadeOverlay.color = new Color(0f, 0f, 0f, t);
                yield return null;
            }

            // Reset chores for new day
            ChoreManager.Instance.ResetChoresForNewDay();
            timer = 0f;

            // Update label and animate it
            ShowDayLabel();
            yield return StartCoroutine(FadeDayLabel());

            // Fade in
            t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime;
                fadeOverlay.color = new Color(0f, 0f, 0f, t);
                yield return null;
            }

            isTransitioning = false;
        }

        private void ShowDayLabel()
        {
            if (dayLabel != null)
                dayLabel.text = $"Day {currentDay} started!";
        }

        private IEnumerator FadeDayLabel()
        {
            if (dayLabel == null) yield break;

            Color baseColor = dayLabel.color;
            dayLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime;
                dayLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, t);
                yield return null;
            }
        }
    }
}
