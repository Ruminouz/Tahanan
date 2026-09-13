using UnityEngine;
using UnityEngine.EventSystems;

public class CleanLeafSpot : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private CleanLeavesMiniGame miniGame;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (miniGame != null)
        {
            miniGame.CleanLeaves(gameObject);
        }
    }
}