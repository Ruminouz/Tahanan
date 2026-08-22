using UnityEngine;
using UnityEngine.UI;

namespace HouseChoresGame
{
    public class Sweeping : MonoBehaviour, IChoreMiniGame
    {
        private GameManager gameManager;
        private ChoreData myChore;

        [Header("UI References")]
        public Text statusLabel;
        public Text timerLabel;
        public Slider progressBar;
        public GameObject broomIcon;   // has CanvasGroup + DragHandler
        public GameObject dustpan;     // has CanvasGroup + DragHandler
        public GameObject trashBin;
        public Sprite filledDustpanSprite;

        [Header("Dust Settings")]
        public GameObject[] dustPilePrefabs;
        public int dustPileCount = 3;
        public RectTransform floorBounds;
        public float timeLimit = 45f;

        private float timer;
        private bool dustpanFull = false;
        private DustPile[] dustPiles;

        public void Initialize(GameManager manager, ChoreData chore)
        {
            gameManager = manager;
            myChore = chore;

            timer = timeLimit;
            dustpanFull = false;

            statusLabel.text = "Step 1: Sweep all dust piles!";
            progressBar.value = 0;

            // Spawn dust piles
            dustPiles = new DustPile[dustPileCount];
            float halfW = floorBounds.rect.width / 2f;
            float halfH = floorBounds.rect.height / 2f;

            for (int i = 0; i < dustPileCount; i++)
            {
                Vector2 pos = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
                GameObject pileObj = Instantiate(dustPilePrefabs[Random.Range(0, dustPilePrefabs.Length)], floorBounds);
                pileObj.GetComponent<RectTransform>().anchoredPosition = pos;
                dustPiles[i] = pileObj.GetComponent<DustPile>();
            }

            dustpan.SetActive(true);
            broomIcon.transform.SetAsLastSibling();
        }

        void Update()
        {
            // Timer countdown
            timer -= Time.deltaTime;
            int secondsLeft = Mathf.CeilToInt(timer);
            timerLabel.text = $"Time Left: {secondsLeft}s";
            timerLabel.color = (secondsLeft <= 5) ? Color.red : Color.white;

            if (timer <= 0f)
            {
                statusLabel.text = "Time's up!";
                ChoreManager.Instance.MissChore(myChore);
                MiniGameOverlayManager.Instance.CloseMiniGame(myChore);
                return;
            }

            if (!dustpanFull)
                HandleSweeping();
            else
                HandleDustpanDispose();
        }

        private void HandleSweeping()
        {
            RectTransform broomRect = broomIcon.GetComponent<RectTransform>();

            int total = 0, cleaned = 0;
            foreach (var pile in dustPiles)
            {
                total += pile.GetTotalSpritesCount();
                cleaned += pile.GetSpritesCleanedCount();

                if (pile.CheckOverlap(broomRect))
                    pile.SweepStroke();
            }

            progressBar.value = (total > 0) ? (float)cleaned / total : 0f;

            // Only trigger Step 2 when ALL dust is cleaned
            if (total > 0 && cleaned == total)
            {
                dustpanFull = true;
                statusLabel.text = "Step 2: Drag dustpan to trash!";
                Image dustpanImage = dustpan.GetComponent<Image>();
                if (dustpanImage != null && filledDustpanSprite != null)
                    dustpanImage.sprite = filledDustpanSprite;
            }
        }

       private void HandleDustpanDispose()
{
    RectTransform dustpanRect = dustpan.GetComponent<RectTransform>();
    RectTransform trashRect = trashBin.GetComponent<RectTransform>();

    // Get world-space rects
    Rect dustpanBounds = GetWorldRect(dustpanRect);
    Rect trashBounds = GetWorldRect(trashRect);

    if (dustpanBounds.Overlaps(trashBounds))
    {
        statusLabel.text = "Chore Complete!";
        ChoreManager.Instance.CompleteChore(myChore);
        MiniGameOverlayManager.Instance.CloseMiniGame(myChore);
    }
}

// Helper method
private Rect GetWorldRect(RectTransform rt)
{
    Vector3[] corners = new Vector3[4];
    rt.GetWorldCorners(corners);
    Vector2 size = new Vector2(
        corners[2].x - corners[0].x,
        corners[2].y - corners[0].y
    );
    return new Rect(corners[0], size);
}

    }
}
