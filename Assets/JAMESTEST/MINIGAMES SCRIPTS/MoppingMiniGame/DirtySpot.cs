using UnityEngine;
using UnityEngine.EventSystems;

public class MopDirtySpot : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private MopFloorMiniGame miniGame;

    public void OnPointerEnter(PointerEventData eventData)
    {
        miniGame.CleanSpot(gameObject);
    }
}