using UnityEngine;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    private const string VolumePrefsKey = "SFXVolume";
    private static SoundEffectManager Instance;

    private AudioSource audioSource;
    private AudioSource randomPitchAudioSource;
    private AudioSource voiceAudioSource;
    private AudioSource faucetAudioSource;
    private AudioSource musicAudioSource;
    private SoundEffectLibrary soundEffectLibrary;
    [SerializeField] private Slider sfxSlider;
    private float currentVolume = .7f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            AudioSource[] audioSources = GetComponents<AudioSource>();
            audioSource = GetOrAddAudioSource(audioSources, 0);
            randomPitchAudioSource = GetOrAddAudioSource(audioSources, 1);
            voiceAudioSource = GetOrAddAudioSource(audioSources, 2);
            soundEffectLibrary = GetComponent<SoundEffectLibrary>();
            DontDestroyOnLoad(gameObject);
            currentVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, currentVolume));
            ApplyVolume(currentVolume);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private AudioSource GetOrAddAudioSource(AudioSource[] audioSources, int index)
    {
        if (index < audioSources.Length)
            return audioSources[index];

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        return source;
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        if (Instance != null && Instance.soundEffectLibrary != null)
        {
            AudioClip audioClip = Instance.soundEffectLibrary.GetRandomClip(soundName);
            if (audioClip != null)
            {
                if (soundName == "Faucet" || soundName == "Water Stream")
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
        if (soundName == "BGM" &&
            Instance != null &&
            Instance.musicAudioSource != null)
        {
            Instance.musicAudioSource.Stop();
        }

        if ((soundName == "Faucet" || soundName == "Water Stream") &&
            Instance != null &&
            Instance.faucetAudioSource != null)
        {
            Instance.faucetAudioSource.Stop();
        }
    }

    public static void PlayLoop(string soundName)
    {
        if (Instance == null || Instance.soundEffectLibrary == null)
            return;

        AudioClip audioClip = Instance.soundEffectLibrary.GetRandomClip(soundName);
        if (audioClip == null)
            return;

        AudioSource source = Instance.GetMusicAudioSource();
        if (source.isPlaying && source.clip == audioClip)
            return;

        source.clip = audioClip;
        source.loop = true;
        source.Play();
    }

    private AudioSource GetMusicAudioSource()
    {
        if (musicAudioSource != null)
            return musicAudioSource;

        musicAudioSource = gameObject.AddComponent<AudioSource>();
        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f;
        if (audioSource != null)
        {
            musicAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            musicAudioSource.volume = audioSource.volume;
            musicAudioSource.mute = audioSource.mute;
            musicAudioSource.priority = audioSource.priority;
        }

        return musicAudioSource;
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
            sfxSlider.SetValueWithoutNotify(currentVolume);
            sfxSlider.onValueChanged.AddListener(delegate { OnValueChanged(); });
        }
    }

    public static void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumePrefsKey, volume);

        if (Instance != null)
        {
            Instance.currentVolume = volume;
            Instance.ApplyVolume(volume);
        }
    }

    public static float GetVolume()
    {
        return Instance != null
            ? Instance.currentVolume
            : Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, 1f));
    }

    private void ApplyVolume(float volume)
    {
        if (audioSource != null) audioSource.volume = volume;
        if (randomPitchAudioSource != null) randomPitchAudioSource.volume = volume;
        if (voiceAudioSource != null) voiceAudioSource.volume = volume;
        if (faucetAudioSource != null) faucetAudioSource.volume = volume;
        if (musicAudioSource != null) musicAudioSource.volume = volume;
    }

    public void OnValueChanged()
    {
        SetVolume(sfxSlider.value);
    }
}