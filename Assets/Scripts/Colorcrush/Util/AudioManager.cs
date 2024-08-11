// Copyright (C) 2024 Peter Guld Leth

#region

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

#endregion

namespace Colorcrush.Util
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        private readonly Dictionary<string, AudioClip> _audioClips = new();
        private readonly Dictionary<string, float> _volumeAdjustments = new();
        private AudioSource[] _audioSources;
        private int _currentAudioSourceIndex;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                PreloadAudioClips();
                CalculateVolumeAdjustments();
            }
        }

        private void InitializeAudioSources()
        {
            _audioSources = new AudioSource[ProjectConfig.InstanceConfig.maxAudioSources];
            for (var i = 0; i < ProjectConfig.InstanceConfig.maxAudioSources; i++)
            {
                var audioObject = new GameObject($"AudioSource_{i}");
                audioObject.transform.SetParent(transform);
                _audioSources[i] = audioObject.AddComponent<AudioSource>();
                _audioSources[i].outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        private void PreloadAudioClips()
        {
            var audioClips = Resources.LoadAll(ProjectConfig.InstanceConfig.audioPath, typeof(AudioClip));
            foreach (var obj in audioClips)
            {
                if (obj is AudioClip clip)
                {
                    var clipName = Path.GetFileNameWithoutExtension(clip.name);
                    _audioClips[clipName] = clip;
                }
            }

            Debug.Log($"Preloaded {_audioClips.Count} audio clips.");
        }

        private void CalculateVolumeAdjustments()
        {
            foreach (var clip in _audioClips)
            {
                var volumeAdjustment = CalculateVolumeAdjustment(clip.Value);
                if (volumeAdjustment > 1f)
                {
                    Debug.LogWarning($"Volume adjustment for {clip.Key} is {volumeAdjustment}. Clamping to 1.");
                    volumeAdjustment = 1f;
                }

                _volumeAdjustments[clip.Key] = volumeAdjustment;

                Debug.Log($"Clip: {clip.Key}, Volume Adjustment: {volumeAdjustment}");
            }
        }

        private float CalculateVolumeAdjustment(AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            var rms = Mathf.Sqrt(samples.Select(s => s * s).Average());
            var volumeAdjustment = rms > 0 ? ProjectConfig.InstanceConfig.targetRMS / rms : 1f;

            var mixerGainFactor = Mathf.Pow(10, ProjectConfig.InstanceConfig.mixerGainDB / 20f);
            volumeAdjustment /= mixerGainFactor;

            return Mathf.Clamp(volumeAdjustment, ProjectConfig.InstanceConfig.minVolumeAdjustment, ProjectConfig.InstanceConfig.maxVolumeAdjustment);
        }

        public static void PlaySound(string soundName, float? gain = null)
        {
            Instance.PlaySoundInternal(soundName, gain);
        }

        private void PlaySoundInternal(string soundName, float? gain = null)
        {
            if (_audioClips.TryGetValue(soundName, out var clip))
            {
                var audioSource = GetNextAvailableAudioSource();
                if (audioSource != null)
                {
                    audioSource.clip = clip;

                    var finalVolume = 1f;
                    if (_volumeAdjustments.TryGetValue(soundName, out var adjustment))
                    {
                        finalVolume = adjustment;
                    }

                    if (gain.HasValue)
                    {
                        finalVolume *= Mathf.Clamp01(gain.Value);
                    }

                    finalVolume *= ProjectConfig.InstanceConfig.globalGain;

                    audioSource.volume = finalVolume;
                    audioSource.Play();
                    Debug.Log($"Playing sound: {soundName} with volume: {finalVolume}");
                }
                else
                {
                    Debug.LogWarning($"No available audio source to play sound: {soundName}");
                }
            }
            else
            {
                Debug.LogError($"Audio clip not found: {soundName}");
            }
        }

        private AudioSource GetNextAvailableAudioSource()
        {
            for (var i = 0; i < ProjectConfig.InstanceConfig.maxAudioSources; i++)
            {
                var index = (_currentAudioSourceIndex + i) % ProjectConfig.InstanceConfig.maxAudioSources;
                if (!_audioSources[index].isPlaying)
                {
                    _currentAudioSourceIndex = (index + 1) % ProjectConfig.InstanceConfig.maxAudioSources;
                    return _audioSources[index];
                }
            }

            return null;
        }
    }
}