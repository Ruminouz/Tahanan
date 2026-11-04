using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuCanvas;

    private void Start()
    {
        if (menuCanvas == null)
        {
            Debug.LogError("MenuController requires a menu canvas to toggle.", this);
            enabled = false;
            return;
        }

        menuCanvas.SetActive(false);
        SyncPauseState();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            bool showMenu = !menuCanvas.activeSelf;
            menuCanvas.SetActive(showMenu);
            if (showMenu)
                menuCanvas.transform.SetAsLastSibling();
        }

        SyncPauseState();
    }

    private void SyncPauseState()
    {
        PauseController.SetPause(this, menuCanvas != null && menuCanvas.activeSelf);
    }

    private void OnDisable()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        SyncPauseState();
    }
}
