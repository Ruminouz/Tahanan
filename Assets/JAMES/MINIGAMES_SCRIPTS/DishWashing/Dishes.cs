using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HouseChoresGame
{
    public class Dishwashing : MonoBehaviour, IChoreMiniGame
    {
        private GameManager gameManager;
        private ChoreData myChore;

        [Header("UI References")]
        public Text statusLabel;
        public Text timerLabel;
        public Slider progressBar;
        public RectTransform platesContainer;
        public GameObject trashBin;
        public GameObject rinsingArea;

        [Header("Faucet")]
        public Image faucetImage;
        public ParticleSystem waterStream;
        
        public GameObject sinkWaterOverlay;

        [Header("Prefabs")]
        public GameObject[] leftoverPrefabs;
        public GameObject spongePrefab;

        [Header("Dish Options")]
        public DishItem[] dishOptions;

        [Header("Spawn Areas")]
        public RectTransform spongeSpawnArea;
        public RectTransform plateDoneArea;

        [Header("Settings")]
        public int plateCount = 3;
        public int leftoverCount = 5;
        public float timeLimit = 60f;

        private float timer;
        private int cleanedPlates = 0;
        private PlateController[] plates;
        private int remainingLeftovers;
        public bool leftoversCleared = false;
        private bool faucetEnabled = false;
        public bool IsFaucetEnabled()
{
    return faucetEnabled;
}

        public void Initialize(GameManager manager, ChoreData chore)
        {
            gameManager = manager;
            myChore = chore;

            timer = timeLimit;
            cleanedPlates = 0;
            progressBar.value = 0;
            statusLabel.text = "Step 1: Drag all leftovers into trash!";

            // Spawn plates
            plates = new PlateController[plateCount];
            for (int i = 0; i < plateCount; i++)
            {
                DishItem chosenDish = dishOptions[i % dishOptions.Length];
                GameObject dishObj = Instantiate(chosenDish.prefab, platesContainer);
                PlateController plate = dishObj.GetComponent<PlateController>();
                plate.Initialize(this, i, chosenDish.difficultyMultiplier, chosenDish.itemName);
                plates[i] = plate;
                dishObj.SetActive(true);
                if (i == 0) plate.MarkDirty();
            }

            // Spawn leftovers
            remainingLeftovers = leftoverCount;
            for (int i = 0; i < leftoverCount; i++)
            {
                GameObject prefab = leftoverPrefabs[Random.Range(0, leftoverPrefabs.Length)];
                GameObject leftover = Instantiate(prefab, platesContainer);
                leftover.GetComponent<Leftover>().AssignManager(this);
            }

            // Spawn sponge
            if (spongeSpawnArea != null)
            {
                GameObject spongeObj = Instantiate(spongePrefab, spongeSpawnArea);
                spongeObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }

            rinsingArea.SetActive(false);
            EnableFaucet(false);
        }

        void Update()
        {
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

            progressBar.value = (float)cleanedPlates / plateCount;
        }

        public void OnLeftoverDisposed()
        {
            remainingLeftovers--;
            if (remainingLeftovers <= 0)
            {
                leftoversCleared = true;
                plates[0].MarkDirty();
                statusLabel.text = $"Step 2: Scrub the {plates[0].GetDishName()}!";
            }
        }

        public void OnPlateStep(PlateController plate)
        {
            switch (plate.state)
            {
                case PlateState.Clean:
                    cleanedPlates++;
                    plate.SlideToDoneArea(plateDoneArea);
                    PlateController nextPlate = FindNextPlate(plate.plateIndex);
                    if (nextPlate != null)
                    {
                        nextPlate.MarkDirty();
                        statusLabel.text = $"Step 2: Scrub the {nextPlate.GetDishName()}!";
                    }
                    else
                    {
                        rinsingArea.SetActive(true);
                        statusLabel.text = "Step 3: Drag dishes into rinsing area!";
                    }
                    break;

                case PlateState.Rinsed:
                    if (AllPlatesRinsed())
                    {
                        statusLabel.text = "Step 4: Turn on tap water!";
                        EnableFaucet(true);
                    }
                    break;

                case PlateState.Dry:
                    CompleteDishwashing();
                    break;
            }
        }

        private PlateController FindNextPlate(int currentIndex)
        {
            for (int i = currentIndex + 1; i < plates.Length; i++)
                if (plates[i] != null) return plates[i];
            return null;
        }

        private bool AllPlatesRinsed()
        {
            foreach (var plate in plates)
                if (plate.state != PlateState.Rinsed && plate.state != PlateState.Dry)
                    return false;
            return true;
        }

        public void OnTapWaterTurnedOn()
        {
            if (!faucetEnabled) return;
            if (waterStream != null) waterStream.Play();
         
            if (sinkWaterOverlay != null) sinkWaterOverlay.SetActive(true);
            CompleteDishwashing();
        }

        private void EnableFaucet(bool enable)
        {
            faucetEnabled = enable;
            faucetImage.enabled = enable;
        }

        private void CompleteDishwashing()
        {
            statusLabel.text = "All dishes done!";
            ChoreManager.Instance.CompleteChore(myChore);
            MiniGameOverlayManager.Instance.CloseMiniGame(myChore);
        }
    }

    [System.Serializable]
    public class DishItem
    {
        public string itemName;
        public GameObject prefab;
        public float difficultyMultiplier = 1f;
    }
}
