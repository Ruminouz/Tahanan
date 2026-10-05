using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float StackHorizontalOffset = 7f;
    private const float StackVerticalOffset = 5f;

    [SerializeField] private Image zoneImage;

    private Color originalColor;

    private void Awake()
    {
        if (zoneImage == null)
        {
            zoneImage = GetComponent<Image>();
        }

        if (zoneImage != null)
        {
            originalColor = zoneImage.color;
        }
    }

    public bool CanAccept(WasteType _)
    {
        return true;
    }

    public void StackToy(WasteObject toy, int stackIndex)
    {
        RectTransform toyRect = toy.transform as RectTransform;
        if (toyRect == null)
        {
            toy.transform.SetParent(transform, false);
            toy.transform.localPosition = new Vector3(
                (stackIndex % 3 - 1) * StackHorizontalOffset,
                stackIndex * StackVerticalOffset,
                0f);
        }
        else
        {
            toyRect.SetParent(transform, false);
            toyRect.anchoredPosition = new Vector2(
                (stackIndex % 3 - 1) * StackHorizontalOffset,
                stackIndex * StackVerticalOffset);
            toyRect.localRotation = Quaternion.identity;
        }

        toy.SetCollected();
        toy.transform.SetAsLastSibling();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // WasteObject handles validation in OnEndDrag.
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (zoneImage != null)
        {
            zoneImage.color = new Color(1f, 1f, 1f, 0.8f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (zoneImage != null)
        {
            zoneImage.color = originalColor;
        }
    }
}
