using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "ScriptableObjects/AudioData")]
public class AudioData : ScriptableObject
{
    public List<MusicTrack> tracks = new List<MusicTrack>();
    public List<SoundEffect> sounds = new List<SoundEffect>();
    [Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
    }
    [Serializable]
    public class SoundEffect
    {
        public string name;
        public List<AudioClip> clips;
        [Range(0, 1)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitchVariance = 0.1f; // Độ biến thiên cao độ cho UX sinh động
    }
}