using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;

    void Start()
    {
        menuCanvas.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
        if (!menuCanvas.activeSelf && PauseController.IsGamePaused)
        {
             return;
        }
            bool showMenu = !menuCanvas.activeSelf;
            menuCanvas.SetActive(showMenu);
            if (showMenu)
            {
                menuCanvas.transform.SetAsLastSibling();
            }

            PauseController.SetPause(menuCanvas.activeSelf);
        }
    }
}