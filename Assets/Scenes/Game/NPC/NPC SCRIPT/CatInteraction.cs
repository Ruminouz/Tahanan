using UnityEngine;
using System.Collections;

public class CatInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float checkInterval = 8f;
    [SerializeField, Range(0f, 100f)] private float interactionChance = 45f;
    [SerializeField, Min(0.1f)] private float approachSpeed = 1.5f;
    [SerializeField, Min(0.05f)] private float approachDistance = 0.5f;
    [SerializeField, Min(5f)] private float pettingDuration = 5f;
    [SerializeField, Min(0f)] private float moodGain = 10f;

    [Header("Heart Bubble")]
    [SerializeField] private Transform bubbleAnchor;
    [SerializeField] private GameObject heartBubblePrefab;
    [SerializeField, Min(0f)] private float bubbleDuration = 2f;

    private float nextCheckTime;
    private WaypointMover currentNpc;

    private void Start()
    {
        nextCheckTime = Time.time + checkInterval;
    }

    private void Update()
    {
        if (currentNpc != null || Time.time < nextCheckTime || PauseController.IsGamePaused)
            return;

        nextCheckTime = Time.time + checkInterval;
        if (Random.Range(0f, 100f) > interactionChance)
            return;

        WaypointMover[] npcs = FindObjectsByType<WaypointMover>(FindObjectsSortMode.None);
        Shuffle(npcs);
        foreach (WaypointMover npc in npcs)
        {
            if (npc != null && npc.MovementEnabled)
            {
                StartCoroutine(PetCat(npc));
                return;
            }
        }
    }

    private IEnumerator PetCat(WaypointMover npc)
    {
        currentNpc = npc;
        bool wasMoving = npc.MovementEnabled;
        npc.SetMovementEnabled(false);

        while (npc != null && Vector2.Distance(npc.transform.position, transform.position) > approachDistance)
        {
            if (!PauseController.IsGamePaused)
            {
                Vector2 nextPosition = Vector2.MoveTowards(
                    npc.transform.position,
                    transform.position,
                    approachSpeed * Time.deltaTime);
                Rigidbody2D body = npc.GetComponent<Rigidbody2D>();
                if (body != null)
                    body.MovePosition(nextPosition);
                else
                    npc.transform.position = nextPosition;
            }

            yield return null;
        }

        if (npc == null)
        {
            currentNpc = null;
            yield break;
        }

        ShowHeartBubble();
        yield return new WaitForSeconds(Mathf.Max(5f, pettingDuration));

        if (npc != null)
        {
            MoodManager moodManager = MoodManager.Instance != null
                ? MoodManager.Instance
                : FindFirstObjectByType<MoodManager>();
            if (moodManager != null)
                moodManager.AddMood(moodGain);

            npc.SetMovementEnabled(wasMoving);
        }

        currentNpc = null;
    }

    private void ShowHeartBubble()
    {
        if (heartBubblePrefab == null)
            return;

        Transform anchor = bubbleAnchor != null ? bubbleAnchor : transform;
        Canvas canvas = SpeechBubbleCanvas.GetOrCreate(anchor);
        GameObject bubble = Instantiate(heartBubblePrefab, anchor.position, Quaternion.identity, canvas.transform);
        TMPro.TMP_Text text = bubble.GetComponentInChildren<TMPro.TMP_Text>();
        if (text != null)
            text.text = "♥";
        Destroy(bubble, bubbleDuration);
    }

    private static void Shuffle(WaypointMover[] npcs)
    {
        for (int index = npcs.Length - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            WaypointMover temporary = npcs[index];
            npcs[index] = npcs[swapIndex];
            npcs[swapIndex] = temporary;
        }
    }
}