using UnityEngine;
using UnityEngine.EventSystems;

namespace HouseChoresGame
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FaucetClick : MonoBehaviour, IPointerClickHandler
    {
        public Dishwashing dishwashing;
        private CanvasGroup canvasGroup;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dishwashing != null && dishwashing.IsFaucetEnabled())
                dishwashing.OnTapWaterTurnedOn();
        }

        // ✅ Helper to visually enable/disable faucet
        public void SetInteractable(bool enable)
        {
            canvasGroup.alpha = enable ? 1f : 0.5f;
            canvasGroup.blocksRaycasts = enable;
        }
    }
}
