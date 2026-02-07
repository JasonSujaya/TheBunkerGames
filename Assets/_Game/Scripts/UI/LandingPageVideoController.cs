using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    [RequireComponent(typeof(VideoPlayer))]
    public class LandingPageVideoController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
#if ODIN_INSPECTOR
        [Title("Settings")]
#endif
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private List<VideoClip> videoClips = new List<VideoClip>();
        [SerializeField] private bool loopPlaylist = true;
        [SerializeField] private bool playOnAwake = true;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private int currentVideoIndex = 0;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
            }

            videoPlayer.loopPointReached += OnVideoEnd;

            if (playOnAwake && videoClips.Count > 0)
            {
                PlayVideo(0);
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoEnd;
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void PlayVideo(int index)
        {
            if (videoClips.Count == 0)
            {
                Debug.LogWarning("[LandingPageVideoController] No video clips in valid list.");
                return;
            }

            if (index < 0 || index >= videoClips.Count)
            {
                Debug.LogWarning($"[LandingPageVideoController] Invalid video index: {index}. Total clips: {videoClips.Count}");
                return;
            }

            currentVideoIndex = index;
            videoPlayer.clip = videoClips[currentVideoIndex];
            videoPlayer.Play();
        }

        public void PlayNextVideo()
        {
            if (videoClips.Count == 0) return;

            int nextIndex = currentVideoIndex + 1;

            if (nextIndex >= videoClips.Count)
            {
                if (loopPlaylist)
                {
                    nextIndex = 0;
                }
                else
                {
                    return; // End of playlist
                }
            }

            PlayVideo(nextIndex);
        }

        public void PlayRandomVideo()
        {
            if (videoClips.Count == 0) return;

            int randomIndex = Random.Range(0, videoClips.Count);
            PlayVideo(randomIndex);
        }

        // -------------------------------------------------------------------------
        // Event Handlers
        // -------------------------------------------------------------------------
        private void OnVideoEnd(VideoPlayer vp)
        {
            if (loopPlaylist)
            {
                PlayNextVideo();
            }
        }

        // -------------------------------------------------------------------------
        // Debug / Editor Buttons
        // -------------------------------------------------------------------------
#if ODIN_INSPECTOR
        [Button("Play Next", ButtonSizes.Medium)]
        private void Debug_PlayNext()
        {
            PlayNextVideo();
        }

        [Button("Play Random", ButtonSizes.Medium)]
        private void Debug_PlayRandom()
        {
            PlayRandomVideo();
        }
#endif
    }
}
