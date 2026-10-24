using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CustomCursorController : MonoBehaviour
{
    [SerializeField] private Sprite normalCursor;
    [SerializeField] private Sprite clickedCursor;
    [SerializeField, Min(1f)] private float cursorSize = 48f;
    [SerializeField] private Vector2 hotspot = new Vector2(0.1f, 0.9f);

    private static CustomCursorController instance;

    private Canvas cursorCanvas;
    private Image cursorImage;
    private RectTransform canvasRect;
    private Sprite displayedSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject cursorPrefab = Resources.Load<GameObject>("CustomCursor");
        if (cursorPrefab == null)
        {
            Debug.LogError("Custom cursor prefab was not found at Resources/CustomCursor.");
            return;
        }

        if (cursorPrefab.GetComponent<CustomCursorController>() == null)
        {
            Debug.LogError("The Resources/CustomCursor prefab is missing CustomCursorController.");
            return;
        }

        Instantiate(cursorPrefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (normalCursor == null || clickedCursor == null)
        {
            Debug.LogError("Custom cursor requires both normal and clicked sprites.", this);
            enabled = false;
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCursorImage();
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        Cursor.visible = false;

        Mouse mouse = Mouse.current;
        if (mouse == null || Cursor.lockState == CursorLockMode.Locked)
        {
            cursorImage.enabled = false;
            return;
        }

        cursorImage.enabled = true;
        SetDisplayedSprite(mouse.leftButton.isPressed ? clickedCursor : normalCursor);

        Vector2 pointerPosition = mouse.position.ReadValue();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                pointerPosition,
                null,
                out Vector2 localPoint))
        {
            cursorImage.rectTransform.anchoredPosition = localPoint;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            Cursor.visible = false;
        else if (cursorImage != null)
            cursorImage.enabled = false;
    }

    private void CreateCursorImage()
    {
        GameObject canvasObject = new GameObject("Custom Cursor Canvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        cursorCanvas = canvasObject.GetComponent<Canvas>();
        cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cursorCanvas.overrideSorting = true;
        cursorCanvas.sortingOrder = short.MaxValue;
        canvasRect = canvasObject.GetComponent<RectTransform>();

        GameObject imageObject = new GameObject("Custom Cursor", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        cursorImage = imageObject.GetComponent<Image>();
        cursorImage.raycastTarget = false;
        cursorImage.preserveAspect = true;
        cursorImage.rectTransform.anchorMin = Vector2.one * 0.5f;
        cursorImage.rectTransform.anchorMax = Vector2.one * 0.5f;
        cursorImage.rectTransform.pivot = hotspot;
        SetDisplayedSprite(normalCursor);
    }

    private void SetDisplayedSprite(Sprite sprite)
    {
        if (displayedSprite == sprite)
            return;

        displayedSprite = sprite;
        cursorImage.sprite = sprite;

        Vector2 spriteSize = sprite.rect.size;
        float scale = cursorSize / Mathf.Max(spriteSize.x, spriteSize.y);
        cursorImage.rectTransform.sizeDelta = spriteSize * scale;
    }
}
