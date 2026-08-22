using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HouseChoresGame
{
    public enum PlateState { Leftovers, Dirty, Clean, Rinsed, Dry }

    public class PlateController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Dishwashing manager;
        public PlateState state = PlateState.Leftovers;
        public int plateIndex;

        [Header("Visuals")]
        public Image dirtOverlay;
        public Image bubbleOverlay;
        public Image cleanSparkleImage;
        public AudioSource cleanAudio;

        private float scrubProgress = 0f;
        public float scrubThreshold = 1f;
        private float scrubDifficulty = 1f;
        private string dishName = "Plate";

        private RectTransform rectTransform;
        private Canvas canvas;
        private Vector2 dragStartPosition;

        public void Initialize(Dishwashing mgr, int index, float difficultyMultiplier, string name)
        {
            manager = mgr;
            plateIndex = index;
            scrubDifficulty = difficultyMultiplier;
            dishName = name;
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void MarkDirty()
        {
            state = PlateState.Dirty;
            scrubProgress = 0f;
            if (dirtOverlay != null) dirtOverlay.enabled = true;
        }

        public void ScrubAt(Vector2 spongePos)
        {
            if (manager != null && !manager.leftoversCleared) return;
            if (state != PlateState.Dirty) return;

            Rect spongeRect = GetWorldRect(GameObject.FindObjectOfType<SpongeController>().GetComponent<RectTransform>());
            Rect plateRect = GetWorldRect(rectTransform);

            if (spongeRect.Overlaps(plateRect))
            {
                scrubProgress += Time.deltaTime / scrubDifficulty;

                float dirtAlpha = Mathf.Lerp(1f, 0f, scrubProgress / scrubThreshold);
                dirtOverlay.color = new Color(dirtOverlay.color.r, dirtOverlay.color.g, dirtOverlay.color.b, dirtAlpha);

                if (scrubProgress >= scrubThreshold)
                {
                    state = PlateState.Clean;
                    PlayCleanAnimation();
                    manager.OnPlateStep(this);
                }
            }
        }

        public string GetDishName() => dishName;

        public void SlideToDoneArea(RectTransform doneArea)
        {
            StartCoroutine(SlideRoutine(doneArea));
        }

        private IEnumerator SlideRoutine(RectTransform doneArea)
        {
            Vector3 start = transform.localPosition;
            Vector3 target = doneArea.localPosition;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.5f;
                transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }
        }

        private void PlayCleanAnimation()
        {
            if (cleanSparkleImage != null)
            {
                cleanSparkleImage.gameObject.SetActive(true);
                StartCoroutine(SparkleRoutine());
            }
            if (cleanAudio != null) cleanAudio.Play();
        }

        private IEnumerator SparkleRoutine()
        {
            float duration = 0.6f;
            float t = 0f;
            Color baseColor = cleanSparkleImage.color;

            while (t < duration)
            {
                t += Time.deltaTime;
                float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.5f + 0.5f;
                cleanSparkleImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, pulse);
                yield return null;
            }

            cleanSparkleImage.gameObject.SetActive(false);
            cleanSparkleImage.color = baseColor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (state != PlateState.Clean) return;
            dragStartPosition = rectTransform.anchoredPosition;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (state != PlateState.Clean) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            rectTransform.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (state != PlateState.Clean) return;

            Rect rinseRect = GetWorldRect(GameObject.FindWithTag("RinsingArea").GetComponent<RectTransform>());
            Rect plateRect = GetWorldRect(rectTransform);

            if (plateRect.Overlaps(rinseRect))
            {
                state = PlateState.Rinsed;
                manager.OnPlateStep(this);
            }
            else
            {
                rectTransform.anchoredPosition = dragStartPosition;
            }
        }

        private Rect GetWorldRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return new Rect(corners[0], corners[2] - corners[0]);
        }
    }
}
    