using UnityEngine;
using UnityEngine.Tilemaps;

public class TimeManager : MonoBehaviour
{
    private const float DayStartHour = 8f;
    private const float DayHours = 12f;
    private const float MorningEnd = 1f / 3f;
    private const float AfternoonEnd = 3f / 4f;
    private const float EveningEnd = 11f / 12f;

    [SerializeField, Min(1f)] private float dayLength = 250f;
    [Header("Outdoor Day/Night Visuals")]
    [SerializeField] private Color morningTint = new Color(1f, 0.96f, 0.88f, 1f);
    [SerializeField] private Color afternoonTint = Color.white;
    [SerializeField] private Color eveningTint = new Color(1f, 0.72f, 0.46f, 1f);
    [SerializeField] private Color nightTint = new Color(0.38f, 0.46f, 0.7f, 1f);

    private float currentTime;
    private bool timeRunning = false;
    private SpriteRenderer[] outdoorSprites = new SpriteRenderer[0];
    private Color[] originalSpriteColors = new Color[0];
    private Tilemap[] outdoorTilemaps = new Tilemap[0];
    private Color[] originalTilemapColors = new Color[0];

    public enum DayPhase
    {
        Morning,
        Afternoon,
        Evening,
        Night
    }

    public float DayLength => dayLength;
    public DayPhase CurrentDayPhase => GetDayPhase(GetDayProgress());

    public string GetDayPhaseLabel()
    {
        return CurrentDayPhase.ToString();
    }

    public float GetDayProgress()
    {
        return dayLength > 0f ? Mathf.Clamp01(currentTime / dayLength) : 0f;
    }

    public string GetClockTimeLabel()
    {
        return FormatClockTime(currentTime);
    }

    public string FormatClockTime(float elapsedSeconds)
    {
        float progress = dayLength > 0f
            ? Mathf.Clamp01(elapsedSeconds / dayLength)
            : 0f;
        int totalMinutes = Mathf.RoundToInt(
            (DayStartHour + progress * DayHours) * 60f);
        int hour24 = totalMinutes / 60;
        int minute = totalMinutes % 60;
        int hour12 = hour24 % 12;

        if (hour12 == 0)
            hour12 = 12;

        string period = hour24 < 12 ? "AM" : "PM";
        return string.Format("{0}:{1:00} {2}", hour12, minute, period);
    }

    public float GetClockHandRotation()
    {
        return -240f - GetDayProgress() * 360f;
    }

    public void AddTime(float seconds)
    {
        currentTime = Mathf.Clamp(currentTime - seconds, 0f, dayLength);
        timeRunning = true;
    }



    private void Start()
    {
        CacheOutdoorRenderers();
        StartDayTime();
        UpdateOutdoorVisuals();
    }


    private void Update()
    {
        if (timeRunning)
        {
            currentTime += Time.deltaTime;

            if (currentTime >= dayLength)
            {
                currentTime = dayLength;
                timeRunning = false;

                Debug.Log("TIME'S UP!");
            }
        }

        UpdateOutdoorVisuals();
    }

    private void OnDisable()
    {
        RestoreOriginalOutdoorColors();
    }

    private DayPhase GetDayPhase(float progress)
    {
        if (progress < MorningEnd)
            return DayPhase.Morning;

        if (progress < AfternoonEnd)
            return DayPhase.Afternoon;

        if (progress < EveningEnd)
            return DayPhase.Evening;

        return DayPhase.Night;
    }

    private void CacheOutdoorRenderers()
    {
        int outdoorLayer = LayerMask.NameToLayer("Outdoor");
        if (outdoorLayer < 0)
        {
            Debug.LogWarning("The Outdoor layer is missing. Add it in Project Settings > Tags and Layers to enable day/night visuals.", this);
            return;
        }

        SpriteRenderer[] allSprites =
            FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int spriteCount = 0;
        foreach (SpriteRenderer sprite in allSprites)
        {
            if (sprite.gameObject.layer == outdoorLayer)
                spriteCount++;
        }

        outdoorSprites = new SpriteRenderer[spriteCount];
        originalSpriteColors = new Color[spriteCount];
        int spriteIndex = 0;
        foreach (SpriteRenderer sprite in allSprites)
        {
            if (sprite.gameObject.layer != outdoorLayer)
                continue;

            outdoorSprites[spriteIndex] = sprite;
            originalSpriteColors[spriteIndex] = sprite.color;
            spriteIndex++;
        }

        Tilemap[] allTilemaps =
            FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int tilemapCount = 0;
        foreach (Tilemap tilemap in allTilemaps)
        {
            if (tilemap.gameObject.layer == outdoorLayer)
                tilemapCount++;
        }

        outdoorTilemaps = new Tilemap[tilemapCount];
        originalTilemapColors = new Color[tilemapCount];
        int tilemapIndex = 0;
        foreach (Tilemap tilemap in allTilemaps)
        {
            if (tilemap.gameObject.layer != outdoorLayer)
                continue;

            outdoorTilemaps[tilemapIndex] = tilemap;
            originalTilemapColors[tilemapIndex] = tilemap.color;
            tilemapIndex++;
        }

        if (spriteCount == 0 && tilemapCount == 0)
        {
            Debug.LogWarning("No outdoor SpriteRenderers or Tilemaps are on the Outdoor layer. Assign exterior visuals to that layer; house interiors can remain on their current layer.", this);
        }
    }

    private void UpdateOutdoorVisuals()
    {
        float progress = GetDayProgress();
        Color tint = GetOutdoorTint(progress);

        for (int i = 0; i < outdoorSprites.Length; i++)
        {
            if (outdoorSprites[i] != null)
                outdoorSprites[i].color = originalSpriteColors[i] * tint;
        }

        for (int i = 0; i < outdoorTilemaps.Length; i++)
        {
            if (outdoorTilemaps[i] != null)
                outdoorTilemaps[i].color = originalTilemapColors[i] * tint;
        }
    }

    private Color GetOutdoorTint(float progress)
    {
        if (progress < MorningEnd)
            return Color.Lerp(morningTint, afternoonTint, progress / MorningEnd);

        if (progress < AfternoonEnd)
            return Color.Lerp(
                afternoonTint,
                eveningTint,
                (progress - MorningEnd) / (AfternoonEnd - MorningEnd));

        if (progress < EveningEnd)
            return Color.Lerp(
                eveningTint,
                nightTint,
                (progress - AfternoonEnd) / (EveningEnd - AfternoonEnd));

        return nightTint;
    }

    private void RestoreOriginalOutdoorColors()
    {
        for (int i = 0; i < outdoorSprites.Length; i++)
        {
            if (outdoorSprites[i] != null)
                outdoorSprites[i].color = originalSpriteColors[i];
        }

        for (int i = 0; i < outdoorTilemaps.Length; i++)
        {
            if (outdoorTilemaps[i] != null)
                outdoorTilemaps[i].color = originalTilemapColors[i];
        }
    }



    public void StartDayTime()
    {
        currentTime = 0f;
        timeRunning = true;

        Debug.Log("Day timer reset and started.");
    }



    public void ResetDayTimer()
    {
        currentTime = 0f;
        timeRunning = true;

        Debug.Log("New day timer started.");
    }



    public float GetTime()
    {
        return currentTime;
    }



    public float GetRemainingTime()
    {
        return dayLength - currentTime;
    }



    public float GetTimePercentage()
    {
        return currentTime / dayLength;
    }
}
