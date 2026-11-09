using System.Collections.Generic;
using UnityEngine;

public class SoundEffectLibrary : MonoBehaviour
{
    [SerializeField] private SoundEffectGroup[] soundEffectGroups;
    private Dictionary<string, List<AudioClip>> soundDictionary;

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        soundDictionary = new Dictionary<string, List<AudioClip>>();
        foreach (SoundEffectGroup soundEffectGroup in soundEffectGroups)
        {
            soundDictionary[soundEffectGroup.name] = soundEffectGroup.audioClips;
        }
    }

    public AudioClip GetRandomClip(string name)
    {
        if (!soundDictionary.TryGetValue(name, out List<AudioClip> audioClips))
        {
            string alternateName = name == "Vaccum"
                ? "Vacuum"
                : name == "Vacuum"
                    ? "Vaccum"
                    : null;
            if (alternateName == null ||
                !soundDictionary.TryGetValue(alternateName, out audioClips))
                return null;
        }

        if (audioClips == null || audioClips.Count == 0)
            return null;

        return audioClips[UnityEngine.Random.Range(0, audioClips.Count)];
    }

    [System.Serializable]
    public struct SoundEffectGroup
    {
        public string name;
        public List<AudioClip> audioClips;
    }
}