using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private WasteType acceptedWasteType;
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

    public bool CanAccept(WasteType wasteType)
    {
        return wasteType == acceptedWasteType;
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
