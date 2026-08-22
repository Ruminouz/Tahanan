using UnityEngine;
using UnityEngine.EventSystems;

namespace HouseChoresGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Leftover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Dishwashing manager;

        public AudioClip trashSound;
        public ParticleSystem crumbPuffPrefab;
        private Vector2 spawnPosition;

        public void AssignManager(Dishwashing mgr) => manager = mgr;
        public void SetSpawnPosition(Vector2 pos) => spawnPosition = pos;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
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
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Rect trashRect = GetWorldRect(GameObject.FindWithTag("TrashBin").GetComponent<RectTransform>());
            Rect leftoverRect = GetWorldRect(rectTransform);

            if (leftoverRect.Overlaps(trashRect))
                DisposeIntoTrash();
            else
                rectTransform.anchoredPosition = spawnPosition;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        private void DisposeIntoTrash()
        {
            manager?.OnLeftoverDisposed();
            if (trashSound != null) AudioSource.PlayClipAtPoint(trashSound, Vector3.zero);

            if (crumbPuffPrefab != null)
            {
                ParticleSystem puff = Instantiate(crumbPuffPrefab, rectTransform.position, Quaternion.identity, rectTransform.parent);
                puff.Play();
                Destroy(puff.gameObject, 2f);
            }

            Destroy(gameObject);
        }

        private Rect GetWorldRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return new Rect(corners[0], corners[2] - corners[0]);
        }
    }
}
