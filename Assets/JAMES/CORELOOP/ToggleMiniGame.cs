using UnityEngine;
using System.Collections.Generic;

namespace HouseChoresGame
{
    public class MiniGameOverlayManager : MonoBehaviour
    {
        public static MiniGameOverlayManager Instance;

        private Dictionary<ChoreData, GameObject> chorePanels = new Dictionary<ChoreData, GameObject>();
        private HashSet<ChoreData> minimizedChores = new HashSet<ChoreData>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Update()
        {
            // ✅ ESC to minimize current active mini-game
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                foreach (var kvp in chorePanels)
                {
                    if (kvp.Value != null && kvp.Value.activeSelf)
                    {
                        kvp.Value.SetActive(false);
                        minimizedChores.Add(kvp.Key);
                        Debug.Log($"ESC → minimized {kvp.Key.choreName}");
                        break;
                    }
                }
            }
        }

        public void RegisterMiniGame(ChoreData chore, GameObject panel)
        {
            if (!chorePanels.ContainsKey(chore))
                chorePanels[chore] = panel;

            minimizedChores.Remove(chore);

            if (panel != null)
            {
                panel.SetActive(true);
                Debug.Log($"Opened {chore.choreName} mini-game.");
            }
        }

        public void RestoreMiniGame(ChoreData chore)
        {
            if (minimizedChores.Contains(chore) && chorePanels.ContainsKey(chore))
            {
                minimizedChores.Remove(chore);
                if (chorePanels[chore] != null)
                {
                    chorePanels[chore].SetActive(true);
                    Debug.Log($"Restored {chore.choreName} mini-game.");
                }
            }
        }
    }
}
