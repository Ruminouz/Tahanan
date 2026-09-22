using System.Collections.Generic;
using UnityEngine;

public class SpriteTrigger : MonoBehaviour
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

    private SpriteRenderer spriteRenderer;

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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
       
        if (!HasTriggerTag(other))
            return;

        SetAsset(0);
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