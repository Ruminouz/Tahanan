using UnityEngine;
using UnityEngine.EventSystems;

public class DishTrashBin : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        DishLeftOver leftover =
            eventData.pointerDrag.GetComponent<DishLeftOver>();

        if (leftover == null)
            return;

        leftover.ThrowIntoTrash();
    }
}