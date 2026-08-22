using UnityEngine;
using UnityEngine.EventSystems;

namespace HouseChoresGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public class SpongeController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;

        public AudioClip scrubLoop;
        private AudioSource loopSource;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.loop = true;
            loopSource.playOnAwake = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;

            if (scrubLoop != null && !loopSource.isPlaying)
            {
                loopSource.clip = scrubLoop;
                loopSource.Play();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            rectTransform.anchoredPosition = localPoint;

            foreach (PlateController plate in FindObjectsOfType<PlateController>())
            {
                if (plate.state == PlateState.Dirty)
                    plate.ScrubAt(rectTransform.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            if (loopSource.isPlaying) loopSource.Stop();
        }
    }
}
