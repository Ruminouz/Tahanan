using UnityEngine;

public class PauseController : MonoBehaviour
{
    private static float previousTimeScale = 1f;

    public static bool IsGamePaused { get; private set; }

    public static void SetPause(bool pause)
    {
        if (IsGamePaused == pause)
            return;

        if (pause)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }

        IsGamePaused = pause;
    }
}
