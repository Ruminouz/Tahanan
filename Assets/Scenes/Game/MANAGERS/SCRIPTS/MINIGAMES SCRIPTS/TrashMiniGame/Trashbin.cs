using UnityEngine;
using UnityEngine.UI;


public class TrashBin : MonoBehaviour
{

    public TrashType binType;


    private RectTransform rectTransform;
    private Graphic graphic;
    private Vector3 originalScale;
    private Color originalColor;
    private Coroutine feedbackRoutine;
    private bool isDropTarget;



    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        graphic = GetComponent<Graphic>();
        originalScale = transform.localScale;

        if (graphic != null)
            originalColor = graphic.color;

        Debug.Log(
            gameObject.name 
            + " BIN TYPE = "
            + binType
        );
    }


    private void OnDisable()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        isDropTarget = false;
        transform.localScale = originalScale;

        if (graphic != null)
            graphic.color = originalColor;
    }


    public void SetDropTarget(bool isTarget)
    {
        isDropTarget = isTarget;

        if (feedbackRoutine != null)
            return;

        transform.localScale = isTarget ? originalScale * 1.08f : originalScale;

        if (graphic != null)
        {
            graphic.color = isTarget
                ? Color.Lerp(originalColor, new Color(1f, 0.88f, 0.35f), 0.65f)
                : originalColor;
        }
    }


    public void PlayDropFeedback(bool wasCorrect)
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(ShowDropFeedback(wasCorrect));
    }


    private System.Collections.IEnumerator ShowDropFeedback(bool wasCorrect)
    {
        Color feedbackColor = wasCorrect
            ? new Color(0.35f, 1f, 0.45f)
            : new Color(1f, 0.35f, 0.35f);
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * (wasCorrect ? 1.2f : 1.12f), pulse);

            if (graphic != null)
                graphic.color = Color.Lerp(originalColor, feedbackColor, pulse);

            yield return null;
        }

        feedbackRoutine = null;
        SetDropTarget(isDropTarget);
    }



    public bool IsInsideBin(Vector2 screenPosition)
    {

        bool inside =
        RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            screenPosition
        );


        Debug.Log(
            gameObject.name
            + " Inside Bin? "
            + inside
        );


        return inside;

    }

}