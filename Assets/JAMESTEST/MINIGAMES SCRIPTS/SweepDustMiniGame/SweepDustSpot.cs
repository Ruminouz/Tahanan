using UnityEngine;
using UnityEngine.EventSystems;

public class SweepDustSpot : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SweepDustMiniGame miniGame;

    public void OnPointerEnter(PointerEventData eventData)
    {
        miniGame.CleanDust(gameObject);
    }
}