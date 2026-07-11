using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

namespace V3.Component
{
    public class SoundComponent : MonoBehaviour
    {
        public static SoundComponent Instance { get; private set; }

        [Header("Data Source")]
        [SerializeField] private AudioData audioData;

        [Header("Pool Settings")]
        [SerializeField] private int sfxPoolSize = 5;
        [SerializeField] private float sfxGlobalCooldown = 0.05f;

        private List<AudioSource> sfxPool = new List<AudioSource>();
        private AudioSource[] musicSources = new AudioSource[2];
        private int activeMusicIndex = 0;

        private float masterVol, musicVol, sfxVol;
        private float lastSfxTime;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }

            SetupPools();
            LoadSettings();
        }

        private void SetupPools()
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject m = new GameObject($"MusicSource_{i}");
                m.transform.SetParent(transform);
                musicSources[i] = m.AddComponent<AudioSource>();
                musicSources[i].loop = true;
                musicSources[i].playOnAwake = false;
            }

            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject s = new GameObject($"SfxSource_{i}");
                s.transform.SetParent(transform);
                AudioSource source = s.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }
        }

        public void PlaySFX(string soundName)
        {
            if (Time.unscaledTime - lastSfxTime < sfxGlobalCooldown) return;
            lastSfxTime = Time.unscaledTime;

            var data = audioData.sounds.FirstOrDefault(s => s.name == soundName);
            if (data == null || data.clips.Count == 0) return;

            AudioSource freeSource = sfxPool.FirstOrDefault(s => !s.isPlaying);
            
            if (freeSource == null) freeSource = sfxPool[0];

            AudioClip clip = data.clips[Random.Range(0, data.clips.Count)];
            
            freeSource.clip = clip;
            freeSource.volume = data.volume * sfxVol * masterVol;
            
            freeSource.pitch = 1f + Random.Range(-data.pitchVariance, data.pitchVariance);
            freeSource.Play();
        }

        public void PlayMusic(string trackName, float fadeDuration = 1.0f)
        {
            var track = audioData.tracks.FirstOrDefault(t => t.name == trackName);
            if (track == null || musicSources[activeMusicIndex].clip == track.clip) return;

            int nextIndex = 1 - activeMusicIndex; // Đổi giữa 0 và 1
            StopAllCoroutines();
            StartCoroutine(CrossFadeRoutine(musicSources[activeMusicIndex], musicSources[nextIndex], track.clip, fadeDuration));
            activeMusicIndex = nextIndex;
        }

        /// <summary>
        /// Dừng nhạc đang phát.
        /// </summary>
        public void StopMusic()
        {
            StopAllCoroutines(); // Dừng mọi crossfade đang diễn ra
            foreach (var source in musicSources)
            {
                source.Stop();
                source.clip = null; // Xóa clip để đảm bảo không phát lại khi resume
            }
        }

        /// <summary>
        /// Dừng và giải phóng toàn bộ âm nhạc và hiệu ứng âm thanh.
        /// </summary>
        public void StopAll()
        {
            StopMusic(); // Dừng tất cả nhạc và xóa clip
            StopAllSfx(); // Dừng tất cả SFX và xóa clip
        }

        /// <summary>
        /// Tạm dừng nhạc đang phát.
        /// </summary>
        public void PauseMusic()
        {
            musicSources[activeMusicIndex].Pause();
        }

        /// <summary>
        /// Tiếp tục phát nhạc đã tạm dừng.
        /// </summary>
        public void ResumeMusic()
        {
            // Chỉ resume nếu có clip và đang không phát
            if (musicSources[activeMusicIndex].clip != null && !musicSources[activeMusicIndex].isPlaying)
            {
                musicSources[activeMusicIndex].UnPause();
            }
        }

        /// <summary>
        /// Dừng tất cả các hiệu ứng âm thanh đang phát.
        /// </summary>
        public void StopAllSfx()
        {
            foreach (var source in sfxPool)
            {
                source.Stop();
                source.clip = null;
            }
        }

        private IEnumerator CrossFadeRoutine(AudioSource outSource, AudioSource inSource, AudioClip nextClip, float duration)
        {
            inSource.clip = nextClip;
            inSource.volume = 0;
            inSource.Play();

            float elapsed = 0;
            float targetVol = musicVol * masterVol;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float percent = elapsed / duration;

                outSource.volume = Mathf.Lerp(targetVol, 0, percent);
                inSource.volume = Mathf.Lerp(0, targetVol, percent);
                yield return null;
            }

            outSource.Stop();
            outSource.volume = 0;
            inSource.volume = targetVol;
        }

        private void LoadSettings()
        {
            masterVol = PlayerPrefs.GetFloat("MASTER_VOL", 1f);
            musicVol = PlayerPrefs.GetFloat("MUSIC_VOL", 1f);
            sfxVol = PlayerPrefs.GetFloat("SFX_VOL", 1f);
        }

        public void MuteMusic(bool active)
        {
            musicVol = active ? 0 : 1;
            musicSources[activeMusicIndex].volume = musicVol * masterVol;
            PlayerPrefs.SetFloat("MUSIC_VOL", musicVol);
        }

        public void MuteSFX(bool active)
        {
            sfxVol = active ? 0 : 1;
            foreach (var source in sfxPool)
            {
                source.volume = sfxVol * masterVol;
            }
            PlayerPrefs.SetFloat("SFX_VOL", sfxVol);
        }

        public bool IsMuteMusic()
        {
            return musicVol == 0;
        }

        public bool IsMuteSFX()
        {
            return sfxVol == 0;
        }
    }
    
}