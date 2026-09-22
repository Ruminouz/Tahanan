
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AmbienceSFXManager : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField] private List<AudioClip> playlist = new List<AudioClip>();

    [Header("Loop Settings")]
    [Tooltip("How many times the playlist loops. Set to -1 for infinite looping.")]
    [SerializeField] private int playlistLoopCount = -1;

    [Tooltip("Time in seconds between each SFX.")]
    [SerializeField] private float intervalBetweenSFX = 2f;

    [Header("Audio Routing")]
    [Tooltip("Optional AudioMixerGroup.")]
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.75f;

    [Header("Start Settings")]
    [Tooltip("Time in seconds before the ambience starts automatically.")]
    [SerializeField] private float startDelay = 0f;

    private AudioSource audioSource;
    private Coroutine ambienceCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = volume;

        if (mixerGroup != null)
            audioSource.outputAudioMixerGroup = mixerGroup;
    }

    private void Start()
    {
        if (playlist.Count > 0)
            StartCoroutine(StartAmbienceAfterDelay());
    }

    private IEnumerator StartAmbienceAfterDelay()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        PlayAmbience();
    }

    public void PlayAmbience()
    {
        if (ambienceCoroutine != null)
            StopCoroutine(ambienceCoroutine);

        ambienceCoroutine = StartCoroutine(PlayAmbienceRoutine());
    }

    private IEnumerator PlayAmbienceRoutine()
    {
        int completedLoops = 0;

        while (playlistLoopCount == -1 || completedLoops < playlistLoopCount)
        {
            // Create a temporary list so every SFX plays once per loop
            List<AudioClip> randomPlaylist = new List<AudioClip>(playlist);

            while (randomPlaylist.Count > 0)
            {
                int randomIndex = Random.Range(0, randomPlaylist.Count);
                AudioClip clip = randomPlaylist[randomIndex];

                randomPlaylist.RemoveAt(randomIndex);

                if (clip == null)
                    continue;

                audioSource.clip = clip;
                audioSource.Play();

                // Wait for the current SFX to finish
                yield return new WaitForSeconds(clip.length);

                // Wait before the next SFX
                if (intervalBetweenSFX > 0f)
                    yield return new WaitForSeconds(intervalBetweenSFX);
            }

            completedLoops++;
        }

        ambienceCoroutine = null;
    }

    public void Stop()
    {
        if (ambienceCoroutine != null)
        {
            StopCoroutine(ambienceCoroutine);
            ambienceCoroutine = null;
        }

        audioSource.Stop();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        audioSource.volume = volume;
    }

    public float GetVolume()
    {
        return audioSource.volume;
    }
}

