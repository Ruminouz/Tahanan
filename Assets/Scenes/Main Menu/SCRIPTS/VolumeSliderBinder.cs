using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the same GameObject as your volume Slider.
/// Binds the slider to the music or sound-effect manager at runtime instead
/// of via an Inspector-dragged reference, so it remains valid across scenes.
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeSliderBinder : MonoBehaviour
{
    [SerializeField, Tooltip("Bind this slider to sound-effect volume instead of music volume.")]
    private bool bindToSoundEffects;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        BringContainingCanvasChildToFront();

        if (bindToSoundEffects)
        {
            slider.SetValueWithoutNotify(SoundEffectManager.GetVolume());
            slider.onValueChanged.RemoveListener(SoundEffectManager.SetVolume);
            slider.onValueChanged.AddListener(SoundEffectManager.SetVolume);
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSliderBinder] No AudioManager.Instance found yet.");
            return;
        }

        // Set the slider's visual position to match current volume,
        // without re-triggering SetVolume via the listener below.
        slider.SetValueWithoutNotify(AudioManager.Instance.GetVolume());

        // Clear any stale listeners, then bind fresh to the current instance.
        slider.onValueChanged.RemoveListener(AudioManager.Instance.SetVolume);
        slider.onValueChanged.AddListener(AudioManager.Instance.SetVolume);
    }

    private void BringContainingCanvasChildToFront()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Transform canvasChild = transform;
        while (canvasChild.parent != null && canvasChild.parent != canvas.transform)
            canvasChild = canvasChild.parent;

        if (canvasChild.parent == canvas.transform)
            canvasChild.SetAsLastSibling();
    }

    private void OnDisable()
    {
        if (bindToSoundEffects)
        {
            slider.onValueChanged.RemoveListener(SoundEffectManager.SetVolume);
        }
        else if (AudioManager.Instance != null)
        {
            slider.onValueChanged.RemoveListener(AudioManager.Instance.SetVolume);
        }
    }
}