using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    [SerializeField] private LoadingScreenController loadingScreen;

    private void Awake()
    {
        if (loadingScreen != null)
        {
            loadingScreen.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        ButtonSoundEffects.AttachToAllButtons();
    }

    public void PlayGame()
    {
        if (loadingScreen == null)
        {
            Debug.LogError("Loading screen is not assigned on the SceneManager.", this);
            return;
        }

        loadingScreen.LoadScene(1);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Debug.Log("NAG QUIT NAG QUIT");
        Application.Quit();
    }
}