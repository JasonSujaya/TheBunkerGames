using System.Collections.Generic;
using UnityEngine;

namespace TheBunkerGames
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private AudioSource musicSource;

        private List<AudioClip> _currentPlaylist = new List<AudioClip>();
        private bool _shuffleMusic = false;
        private int _currentMusicIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = false; // We handle looping manually for playlists
            }
        }

        private void Update()
        {
            if (musicSource != null && !musicSource.isPlaying && musicSource.clip != null && _currentPlaylist.Count > 0)
            {
                // Check if we need to play next track (similar logic to previous implementation)
                // If it's not looping and time is 0 (or close/finished), play next.
                if (!musicSource.loop && musicSource.time == 0f)
                {
                    PlayNextMusic();
                }
            }
        }

        public void PlaySound(string soundName)
        {
            if (enableDebugLogs) Debug.Log($"[AudioManager] Playing sound: {soundName}");
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            // PlayOneShot allows multiple sounds to overlap
            musicSource.PlayOneShot(clip, volume);
        }

        public void PlayMusicList(List<AudioClip> playlist, bool shuffle)
        {
            if (playlist == null || playlist.Count == 0) return;

            _currentPlaylist = new List<AudioClip>(playlist);
            _shuffleMusic = shuffle;
            _currentMusicIndex = -1;

            PlayNextMusic();
        }

        private void PlayNextMusic()
        {
             if (_currentPlaylist.Count == 0) return;

            int nextIndex = 0;
            if (_shuffleMusic)
            {
                nextIndex = Random.Range(0, _currentPlaylist.Count);
                 // Avoid repeat if possible
                if (_currentPlaylist.Count > 1 && nextIndex == _currentMusicIndex)
                {
                    nextIndex = (_currentMusicIndex + 1) % _currentPlaylist.Count;
                }
            }
            else
            {
                nextIndex = (_currentMusicIndex + 1) % _currentPlaylist.Count;
            }

            PlayMusicAtIndex(nextIndex);
        }

        private void PlayMusicAtIndex(int index)
        {
            if (index < 0 || index >= _currentPlaylist.Count) return;

            _currentMusicIndex = index;
            musicSource.clip = _currentPlaylist[_currentMusicIndex];
            musicSource.loop = false; 
            musicSource.Play();
            
            if (enableDebugLogs) Debug.Log($"[AudioManager] Playing music: {musicSource.clip.name}");
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Play Next Music", Sirenix.OdinInspector.ButtonSizes.Medium)]
        private void Debug_PlayNextMusic()
        {
            PlayNextMusic();
        }

        [Sirenix.OdinInspector.Button("Stop Music", Sirenix.OdinInspector.ButtonSizes.Medium)]
        private void Debug_StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }
#endif
    }
}
