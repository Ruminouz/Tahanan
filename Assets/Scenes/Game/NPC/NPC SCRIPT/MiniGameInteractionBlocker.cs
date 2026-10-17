using System.Collections.Generic;
using UnityEngine;

public static class MiniGameInteractionBlocker
{
    private static readonly List<GameObject> panels = new();

    public static bool IsAnyMiniGameActive
    {
        get
        {
            for (int i = panels.Count - 1; i >= 0; i--)
            {
                GameObject panel = panels[i];
                if (panel == null)
                {
                    panels.RemoveAt(i);
                    continue;
                }

                if (panel.activeInHierarchy)
                    return true;
            }

            return false;
        }
    }

    public static void RegisterPanel(GameObject panel)
    {
        if (panel != null && !panels.Contains(panel))
            panels.Add(panel);
    }
}
