using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HouseChoresGame
{
    public class DustPile : MonoBehaviour
    {
        [Header("Sprites")]
        public Image[] dustSprites; // assign child dust sprite Images in Inspector

        private int cleanedCount = 0;

        public void SweepStroke()
        {
            if (cleanedCount < dustSprites.Length)
            {
                StartCoroutine(FadeOutSprite(dustSprites[cleanedCount]));
                cleanedCount++;
            }
        }

        public int GetTotalSpritesCount() => dustSprites.Length;
        public int GetSpritesCleanedCount() => cleanedCount;

       public bool CheckOverlap(RectTransform broomRect)
{
    Rect pileRect = GetComponent<RectTransform>().rect;
    Rect broomBounds = broomRect.rect;
    pileRect.position = GetComponent<RectTransform>().position;
    broomBounds.position = broomRect.position;
    return pileRect.Overlaps(broomBounds);
}


        private IEnumerator FadeOutSprite(Image sprite)
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Color startColor = sprite.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                sprite.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            sprite.enabled = false;
        }
    }
}
