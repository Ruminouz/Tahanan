
using System.Collections.Generic;
using UnityEngine;

public class FrontDoor : MonoBehaviour
{
    [Header("Assets")]
    [Tooltip("Addable asset slots. Element 0 = Idle, Element 1 = Triggered.")]
    [SerializeField] private List<Sprite> assets = new List<Sprite>(2);

    [Header("Trigger Tags")]
    [Tooltip("Addable tags that can trigger the object.")]
    [SerializeField] private List<string> triggerTags = new List<string>(2)
    {
        "Player",
        "NPC"
    };

    [Header("Sound Cooldown")]
    [Tooltip("Minimum time between door sound triggers.")]
    [SerializeField] private float soundCooldown = 0.15f;

    private SpriteRenderer spriteRenderer;

    private float lastSoundTime = -Mathf.Infinity;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("FrontDoor: No SpriteRenderer found.");
            return;
        }

        SetAsset(0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasTriggerTag(other))
            return;

        SetAsset(1);

        PlayDoorSound("DoorOpen");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!HasTriggerTag(other))
            return;

        SetAsset(0);

        PlayDoorSound("DoorClose");
    }

    private void PlayDoorSound(string soundName)
    {
        if (Time.time - lastSoundTime < soundCooldown)
            return;

        lastSoundTime = Time.time;

        SoundEffectManager.Play(soundName);
    }

    private bool HasTriggerTag(Collider2D other)
    {
        if (triggerTags == null)
            return false;

        foreach (string tag in triggerTags)
        {
            if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void SetAsset(int index)
    {
        if (spriteRenderer == null)
            return;

        if (assets == null || assets.Count <= index)
        {
            Debug.LogWarning($"FrontDoor: Asset slot {index} has not been assigned.");
            return;
        }

        spriteRenderer.sprite = assets[index];
    }
}
