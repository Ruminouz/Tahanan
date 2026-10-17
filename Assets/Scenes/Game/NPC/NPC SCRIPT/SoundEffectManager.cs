using UnityEngine;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    private static SoundEffectManager Instance;

    private AudioSource audioSource;
    private AudioSource randomPitchAudioSource;
    private AudioSource voiceAudioSource;
    private AudioSource faucetAudioSource;
    private SoundEffectLibrary soundEffectLibrary;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            AudioSource[] audioSources = GetComponents<AudioSource>();
            audioSource = audioSources[0];
            randomPitchAudioSource = audioSources[1];
            voiceAudioSource = audioSources[2];
            soundEffectLibrary = GetComponent<SoundEffectLibrary>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        if (Instance != null && Instance.soundEffectLibrary != null)
        {
            AudioClip audioClip = Instance.soundEffectLibrary.GetRandomClip(soundName);
            if (audioClip != null)
            {
                if (soundName == "Faucet")
                {
                    AudioSource source = Instance.GetFaucetAudioSource();
                    source.PlayOneShot(audioClip);
                    return;
                }

                if (randomPitch && Instance.randomPitchAudioSource != null)
                {
                    Instance.randomPitchAudioSource.pitch = Random.Range(0.8f, 1.2f);
                    Instance.randomPitchAudioSource.PlayOneShot(audioClip);
                }
                else if (Instance.audioSource != null)
                {
                    Instance.audioSource.PlayOneShot(audioClip);
                }
            }
        }
    }

    public static void Stop(string soundName)
    {
        if (soundName == "Faucet" &&
            Instance != null &&
            Instance.faucetAudioSource != null)
        {
            Instance.faucetAudioSource.Stop();
        }
    }

    private AudioSource GetFaucetAudioSource()
    {
        if (faucetAudioSource != null)
            return faucetAudioSource;

        faucetAudioSource = gameObject.AddComponent<AudioSource>();
        faucetAudioSource.playOnAwake = false;
        if (audioSource != null)
        {
            faucetAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            faucetAudioSource.volume = audioSource.volume;
            faucetAudioSource.pitch = audioSource.pitch;
            faucetAudioSource.mute = audioSource.mute;
            faucetAudioSource.priority = audioSource.priority;
            faucetAudioSource.spatialBlend = audioSource.spatialBlend;
        }

        return faucetAudioSource;
    }

    public static void PlayVoice(AudioClip audioClip, float pitch = 1f)
{
    if (Instance != null && Instance.voiceAudioSource != null && audioClip != null)
    {
        Instance.voiceAudioSource.Stop(); // Force stop previous playback
        Instance.voiceAudioSource.clip = audioClip; // Assign the audio clip
        Instance.voiceAudioSource.pitch = pitch;
        Instance.voiceAudioSource.Play(); // Instantly cuts off old audio and restarts
    }
}

    public static void StopVoice()
    {
        if (Instance != null && Instance.voiceAudioSource != null)
        {
            Instance.voiceAudioSource.Stop();
        }
    }

    void Start()
    {
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
        }
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null)
        {
            if (Instance.audioSource != null) Instance.audioSource.volume = volume;
            if (Instance.randomPitchAudioSource != null) Instance.randomPitchAudioSource.volume = volume;
            if (Instance.voiceAudioSource != null) Instance.voiceAudioSource.volume = volume;
            if (Instance.faucetAudioSource != null) Instance.faucetAudioSource.volume = volume;
        }
    }

    public void OnValueChanged()
    {
        SetVolume(sfxSlider.value);
    }
}