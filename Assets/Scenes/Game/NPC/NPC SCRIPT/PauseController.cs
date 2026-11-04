using System.Collections.Generic;
using UnityEngine;

public class PauseController : MonoBehaviour
{
    private static readonly HashSet<int> pauseOwners = new HashSet<int>();
    private static float previousTimeScale = 1f;

    public static bool IsGamePaused { get; private set; }

    public static void SetTimeScale(float timeScale)
    {
        if (IsGamePaused)
        {
            previousTimeScale = timeScale;
            return;
        }

        Time.timeScale = timeScale;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPauseState()
    {
        if (IsGamePaused)
            Time.timeScale = previousTimeScale;

        pauseOwners.Clear();
        previousTimeScale = 1f;
        IsGamePaused = false;
    }

    public static void SetPause(Object owner, bool pause)
    {
        if (owner == null)
        {
            Debug.LogWarning("Pause state cannot be changed without an owner.");
            return;
        }

        bool wasPaused = IsGamePaused;
        if (pause)
            pauseOwners.Add(owner.GetInstanceID());
        else
            pauseOwners.Remove(owner.GetInstanceID());

        IsGamePaused = pauseOwners.Count > 0;
        if (wasPaused == IsGamePaused)
            return;

        if (IsGamePaused)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }
    }
}
